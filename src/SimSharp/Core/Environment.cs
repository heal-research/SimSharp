#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimSharp {
  /// <summary>
  /// Simulation hold the event queues, schedule and process events.
  /// </summary>
  /// <remarks>
  /// This class is not thread-safe against manipulation of the event queue. If you supply a termination
  /// event that is set outside the simulation thread, please use the <see cref="ThreadSafeSimulation"/> environment.
  /// 
  /// For most purposes <see cref="Simulation"/> is however the better and faster choice.
  /// </remarks>
  public class Simulation {
    private const int InitialMaxEvents = 1024;

    /// <summary>
    /// Describes the number of seconds that a logical step of 1 in the *D-API takes.
    /// </summary>
    protected double DefaultTimeStepSeconds { get; private set; }

    /// <summary>
    /// Calculates the logical date of the simulation by the amount of default steps
    /// that have passed.
    /// </summary>
    public double NowD {
      get { return (Now - StartDate).TotalSeconds / DefaultTimeStepSeconds; }
    }

    private DateTime now;
    /// <summary>
    /// The current simulation time as a calendar date.
    /// </summary>
    public virtual DateTime Now { get => now; protected set => now = value; }

    /// <summary>
    /// The calendar date when the simulation started. This defaults to 1970-1-1 if
    /// no other date has been specified in the overloaded constructor.
    /// </summary>
    public DateTime StartDate { get; protected set; }

    /// <summary>
    /// The random number generator that is to be used in all events in
    /// order to produce reproducible results.
    /// </summary>
    protected IRandom Random { get; set; }

    protected EventQueue ScheduleQ;
    public Process ActiveProcess { get; set; }

    public TextWriter Logger { get; set; }
    public int ProcessedEvents { get; protected set; }

    public Simulation() : this(new DateTime(1970, 1, 1)) { }
    public Simulation(TimeSpan? defaultStep) : this(new DateTime(1970, 1, 1), defaultStep) { }
    public Simulation(int randomSeed, TimeSpan? defaultStep = null) : this(new DateTime(1970, 1, 1), randomSeed, defaultStep) { }
    public Simulation(DateTime initialDateTime, TimeSpan? defaultStep = null) : this(new PcgRandom(), initialDateTime, defaultStep) { }
    public Simulation(DateTime initialDateTime, int randomSeed, TimeSpan? defaultStep = null) : this(new PcgRandom(randomSeed), initialDateTime, defaultStep) { }
    public Simulation(IRandom random, DateTime initialDateTime, TimeSpan? defaultStep = null) {
      DefaultTimeStepSeconds = (defaultStep ?? TimeSpan.FromSeconds(1)).Duration().TotalSeconds;
      StartDate = initialDateTime;
      Now = initialDateTime;
      Random = random;
      ScheduleQ = new EventQueue(InitialMaxEvents);
      Logger = Console.Out;
    }

    public double ToDouble(TimeSpan span) {
      return span.TotalSeconds / DefaultTimeStepSeconds;
    }

    public TimeSpan ToTimeSpan(double span) {
      return TimeSpan.FromSeconds(DefaultTimeStepSeconds * span);
    }

    /// <summary>
    /// Creates a new process from an event generator. The process is automatically
    /// scheduled to be started at the current simulation time.
    /// </summary>
    /// <param name="generator">The generator function that represents the process.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    /// <returns>The scheduled process that was created.</returns>
    public Process Process(IEnumerable<Event> generator, int priority = 0) {
      return new Process(this, generator, priority);
    }

    /// <summary>
    /// Creates and returns a new timeout.
    /// </summary>
    /// <param name="delay">The time after which the timeout is fired.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    /// <returns>The scheduled timeout event that was created.</returns>
    public Timeout TimeoutD(double delay, int priority = 0) {
      return Timeout(TimeSpan.FromSeconds(DefaultTimeStepSeconds * delay), priority);
    }

    /// <summary>
    /// Creates and returns a new timeout.
    /// </summary>
    /// <param name="delay">The time after which the timeout is fired.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    /// <returns>The scheduled timeout event that was created.</returns>
    public Timeout Timeout(TimeSpan delay, int priority = 0) {
      return new Timeout(this, delay, priority: priority);
    }

    public virtual void Reset(int randomSeed) {
      ProcessedEvents = 0;
      Now = StartDate;
      Random = new PcgRandom(randomSeed);
      ScheduleQ = new EventQueue(InitialMaxEvents);
      useSpareNormal = false;
    }

    public virtual void ScheduleD(double delay, Event @event) {
      Schedule(TimeSpan.FromSeconds(DefaultTimeStepSeconds * delay), @event);
    }

    /// <summary>
    /// Schedules an event to occur at the same simulation time as the call was made.
    /// </summary>
    /// <param name="event">The event that should be scheduled.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    public virtual void Schedule(Event @event, int priority = 0) {
      DoSchedule(Now, @event, priority);
    }

    /// <summary>
    /// Schedules an event to occur after a certain (positive) delay.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="delay"/> is negative.
    /// </exception>
    /// <param name="delay">The (positive) delay after which the event should be fired.</param>
    /// <param name="event">The event that should be scheduled.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    public virtual void Schedule(TimeSpan delay, Event @event, int priority = 0) {
      if (delay < TimeSpan.Zero)
        throw new ArgumentException("Negative delays are not allowed in Schedule(TimeSpan, Event).");
      var eventTime = Now + delay;
      DoSchedule(eventTime, @event, priority);
    }

    protected virtual EventQueueNode DoSchedule(DateTime date, Event @event, int priority = 0) {
      if (ScheduleQ.MaxSize == ScheduleQ.Count) {
        // the capacity has to be adjusted, there are more events in the queue than anticipated
        var oldSchedule = ScheduleQ;
        ScheduleQ = new EventQueue(ScheduleQ.MaxSize * 2);
        foreach (var e in oldSchedule) ScheduleQ.Enqueue(e.PrimaryPriority, e.Event, e.SecondaryPriority);
      }
      return ScheduleQ.Enqueue(date, @event, priority);
    }

    public virtual object RunD(double? until = null) {
      if (!until.HasValue) return Run();
      return Run(Now + TimeSpan.FromSeconds(DefaultTimeStepSeconds * until.Value));
    }

    public virtual object Run(TimeSpan span) {
      return Run(Now + span);
    }

    public virtual object Run(DateTime until) {
      if (until <= Now) throw new InvalidOperationException("Simulation end date must lie in the future.");
      var stopEvent = new Event(this);
      var node = DoSchedule(until, stopEvent);
      // stop event is always the first to execute at the given time
      node.InsertionIndex = -1;
      ScheduleQ.OnNodeUpdated(node);
      return Run(stopEvent);
    }

    protected CancellationTokenSource _stop = null;
    /// <summary>
    /// Run until a certain event is processed.
    /// </summary>
    /// <remarks>
    /// This simulation environment is not thread-safe, thus triggering this event outside the environment
    /// leads to potential race conditions. Please use the <see cref="ThreadSafeSimulation"/> environment in case you
    /// require this functionality. Note that the performance of <see cref="ThreadSafeSimulation"/> is lower due to locking.
    /// 
    /// For real-time based termination, you can also call <see cref="StopAsync"/> which sets a flag indicating the simulation
    /// to stop before processing the next event.
    /// </remarks>
    /// <param name="stopEvent">The event that stops the simulation.</param>
    /// <returns></returns>
    public virtual object Run(Event stopEvent = null) {
      _stop = new CancellationTokenSource();
      if (stopEvent != null) {
        if (stopEvent.IsProcessed) {
          return stopEvent.Value;
        }
        stopEvent.AddCallback(StopSimulation);
      }
      OnRunStarted();
      try {
        var stop = ScheduleQ.Count == 0 || _stop.IsCancellationRequested;
        while (!stop) {
          Step();
          stop = ScheduleQ.Count == 0 || _stop.IsCancellationRequested;
        }
      } catch (StopSimulationException e) { OnRunFinished(); return e.Value; }
      OnRunFinished();
      if (stopEvent == null) return null;
      if (!_stop.IsCancellationRequested && !stopEvent.IsTriggered) throw new InvalidOperationException("No scheduled events left but \"until\" event was not triggered.");
      return stopEvent.Value;
    }

    public virtual void StopAsync() {
      _stop?.Cancel();
    }

    public event EventHandler RunStarted;
    protected void OnRunStarted() {
      RunStarted?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler RunFinished;
    protected void OnRunFinished() {
      RunFinished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Performs a single step of the simulation, i.e. process a single event
    /// </summary>
    /// <remarks>
    /// This method is not thread-safe
    /// </remarks>
    public virtual void Step() {
      Event evt;
      var next = ScheduleQ.Dequeue();
      Now = next.PrimaryPriority;
      evt = next.Event;
      evt.Process();
      ProcessedEvents++;
    }

    /// <summary>
    /// Peeks at the time of the next event in terms of the defined step
    /// </summary>
    /// <remarks>
    /// This method is not thread-safe
    /// </remarks>
    public virtual double PeekD() {
      if (ScheduleQ.Count == 0) return double.MaxValue;
      return (Peek() - StartDate).TotalSeconds / DefaultTimeStepSeconds;
    }

    /// <summary>
    /// Peeks at the time of the next event
    /// </summary>
    /// <remarks>
    /// This method is not thread-safe
    /// </remarks>
    public virtual DateTime Peek() {
      return ScheduleQ.Count > 0 ? ScheduleQ.First.PrimaryPriority : DateTime.MaxValue;
    }

    protected virtual void StopSimulation(Event @event) {
      throw new StopSimulationException(@event.Value);
    }

    public virtual void Log(string message, params object[] args) {
      if (Logger != null)
        Logger.WriteLine(message, args);
    }

    #region Random number distributions
    public T Rand<T>(IDistribution<T> distribution) {
      return distribution.Sample(Random);
    }
    public bool TryRand<T>(IRejectionSampledDistribution<T> distribution, out T sample) {
      return distribution.TrySample(Random, out sample);
    }
    public TimeSpan RandToTime(IDistribution<double> distribution) {
      return ToTimeSpan(distribution.Sample(Random));
    }

    [Obsolete("Consider to use the respective distribution class")]
    public double RandUniform(IRandom random, double a, double b) {
      return Uniform.Sample(random, a, b);
    }
    [Obsolete("Consider to use the respective distribution class")]
    public double RandUniform(double a, double b) {
      return RandUniform(Random, a, b);
    }

    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandUniform(IRandom random, TimeSpan a, TimeSpan b) {
      return TimeSpan.FromSeconds(RandUniform(random, a.TotalSeconds, b.TotalSeconds));
    }
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandUniform(TimeSpan a, TimeSpan b) {
      return RandUniform(Random, a, b);
    }
    [Obsolete("Consider to use the respective distribution class")]
    public double RandTriangular(IRandom random, double low, double high) {
      return Triangular.Sample(random, low, high);
    }
    [Obsolete("Consider to use the respective distribution class")]
    public double RandTriangular(double low, double high) {
      return RandTriangular(Random, low, high);
    }

    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandTriangular(IRandom random, TimeSpan low, TimeSpan high) {
      return TimeSpan.FromSeconds(RandTriangular(random, low.TotalSeconds, high.TotalSeconds));
    }
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandTriangular(TimeSpan low, TimeSpan high) {
      return RandTriangular(Random, low, high);
    }

    [Obsolete("Consider to use the respective distribution class")]
    public double RandTriangular(IRandom random, double low, double high, double mode) {
      return Triangular.Sample(random, low, high, mode);
    }
    [Obsolete("Consider to use the respective distribution class")]
    public double RandTriangular(double low, double high, double mode) {
      return RandTriangular(Random, low, high, mode);
    }

    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandTriangular(IRandom random, TimeSpan low, TimeSpan high, TimeSpan mode) {
      return TimeSpan.FromSeconds(RandTriangular(random, low.TotalSeconds, high.TotalSeconds, mode.TotalSeconds));
    }
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandTriangular(TimeSpan low, TimeSpan high, TimeSpan mode) {
      return RandTriangular(Random, low, high, mode);
    }

    /// <summary>
    /// Returns a number that is exponentially distributed given a certain mean.
    /// </summary>
    /// <remarks>
    /// Unlike in other APIs here the mean should be given and not the lambda parameter.
    /// </remarks>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="mean">The mean(!) of the distribution is 1 / lambda.</param>
    /// <returns>A number that is exponentially distributed</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public double RandExponential(IRandom random, double mean) {
      return Exponential.Sample(random, mean);
    }
    /// <summary>
    /// Returns a number that is exponentially distributed given a certain mean.
    /// </summary>
    /// <remarks>
    /// Unlike in other APIs here the mean should be given and not the lambda parameter.
    /// </remarks>
    /// <param name="mean">The mean(!) of the distribution is 1 / lambda.</param>
    /// <returns>A number that is exponentially distributed</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public double RandExponential(double mean) {
      return RandExponential(Random, mean);
    }

    /// <summary>
    /// Returns a timespan that is exponentially distributed given a certain mean.
    /// </summary>
    /// <remarks>
    /// Unlike in other APIs here the mean should be given and not the lambda parameter.
    /// </remarks>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="mean">The mean(!) of the distribution is 1 / lambda.</param>
    /// <returns>A number that is exponentially distributed</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandExponential(IRandom random, TimeSpan mean) {
      return TimeSpan.FromSeconds(RandExponential(random, mean.TotalSeconds));
    }
    /// <summary>
    /// Returns a timespan that is exponentially distributed given a certain mean.
    /// </summary>
    /// <remarks>
    /// Unlike in other APIs here the mean should be given and not the lambda parameter.
    /// </remarks>
    /// <param name="mean">The mean(!) of the distribution is 1 / lambda.</param>
    /// <returns>A number that is exponentially distributed</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandExponential(TimeSpan mean) {
      return RandExponential(Random, mean);
    }

    [Obsolete("Will be removed in a subsequent version")]
    private bool useSpareNormal = false;
    [Obsolete("Will be removed in a subsequent version")]
    private double spareNormal = double.NaN;

    /// <summary>
    /// Uses the Marsaglia polar method to generate a random variable
    /// from two uniform random distributed values.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="RandNormal(double, double)"/> this method does not
    /// make use of a spare random variable. It discards the spare and thus
    /// requires twice the number of calls to the underlying IRandom instance.
    /// </remarks>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="mu">The mean of the normal distribution.</param>
    /// <param name="sigma">The standard deviation of the normal distribution.</param>
    /// <returns>A number that is normal distributed.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public virtual double RandNormal(IRandom random, double mu, double sigma) {
      return Normal.Sample(random, mu, sigma);
    }
    /// <summary>
    /// Uses the Marsaglia polar method to generate a random variable
    /// from two uniform random distributed values.
    /// </summary>
    /// <remarks>
    /// A spare random variable is generated from the second uniformly
    /// distributed value. Thus, the two calls to the uniform random number
    /// generator will be made only every second call.
    /// </remarks>
    /// <param name="mu">The mean of the normal distribution.</param>
    /// <param name="sigma">The standard deviation of the normal distribution.</param>
    /// <returns>A number that is normal distributed.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public virtual double RandNormal(double mu, double sigma) {
      if (useSpareNormal) {
        useSpareNormal = false;
        return spareNormal * sigma + mu;
      } else {
        useSpareNormal = true;
        return MarsagliaPolar(Random, mu, sigma, out spareNormal);
      }
    }
    [Obsolete("This will be removed in a subsequent version")]
    private double MarsagliaPolar(IRandom random, double mu, double sigma, out double spare) {
      double u, v, s;
      do {
        u = random.NextDouble() * 2 - 1;
        v = random.NextDouble() * 2 - 1;
        s = u * u + v * v;
      } while (s > 1 || s == 0);
      var mul = Math.Sqrt(-2.0 * Math.Log(s) / s);
      spare = v * mul;
      return mu + sigma * u * mul;
    }

    /// <summary>
    /// Uses the Marsaglia polar method to generate a random variable
    /// from two uniform random distributed values.
    /// </summary>
    /// <remarks>
    /// A spare random variable is generated from the second uniformly
    /// distributed value. Thus, the two calls to the uniform random number
    /// generator will be made only every second call.
    /// </remarks>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="mu">The mean of the normal distribution.</param>
    /// <param name="sigma">The standard deviation of the normal distribution.</param>
    /// <returns>A number that is normal distributed.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandNormal(IRandom random, TimeSpan mu, TimeSpan sigma) {
      return TimeSpan.FromSeconds(RandNormal(random, mu.TotalSeconds, sigma.TotalSeconds));
    }
    /// <summary>
    /// Uses the Marsaglia polar method to generate a random variable
    /// from two uniform random distributed values.
    /// </summary>
    /// <remarks>
    /// A spare random variable is generated from the second uniformly
    /// distributed value. Thus, the two calls to the uniform random number
    /// generator will be made only every second call.
    /// </remarks>
    /// <param name="mu">The mean of the normal distribution.</param>
    /// <param name="sigma">The standard deviation of the normal distribution.</param>
    /// <returns>A number that is normal distributed.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandNormal(TimeSpan mu, TimeSpan sigma) {
      return RandNormal(Random, mu, sigma);
    }

    [Obsolete("Consider to use the bounded distribution class")]
    public double RandNormalPositive(IRandom random, double mu, double sigma) {
      double val;
      do {
        val = RandNormal(random, mu, sigma);
      } while (val <= 0);
      return val;
    }
    [Obsolete("Consider to use the bounded distribution class")]
    public double RandNormalPositive(double mu, double sigma) {
      return RandNormalPositive(Random, mu, sigma);
    }

    [Obsolete("Consider to use the bounded distribution class")]
    public TimeSpan RandNormalPositive(IRandom random, TimeSpan mu, TimeSpan sigma) {
      return TimeSpan.FromSeconds(RandNormalPositive(random, mu.TotalSeconds, sigma.TotalSeconds));
    }
    [Obsolete("Consider to use the bounded distribution class")]
    public TimeSpan RandNormalPositive(TimeSpan mu, TimeSpan sigma) {
      return RandNormalPositive(Random, mu, sigma);
    }

    [Obsolete("Consider to use the bounded distribution class")]
    public double RandNormalNegative(IRandom random, double mu, double sigma) {
      double val;
      do {
        val = RandNormal(random, mu, sigma);
      } while (val >= 0);
      return val;
    }
    [Obsolete("Consider to use the bounded distribution class")]
    public double RandNormalNegative(double mu, double sigma) {
      return RandNormalNegative(Random, mu, sigma);
    }

    [Obsolete("Consider to use the bounded distribution class")]
    public TimeSpan RandNormalNegative(IRandom random, TimeSpan mu, TimeSpan sigma) {
      return TimeSpan.FromSeconds(RandNormalNegative(random, mu.TotalSeconds, sigma.TotalSeconds));
    }
    [Obsolete("Consider to use the bounded distribution class")]
    public TimeSpan RandNormalNegative(TimeSpan mu, TimeSpan sigma) {
      return RandNormalNegative(Random, mu, sigma);
    }

    /// <summary>
    /// Returns values from a log-normal distribution with the mean
    /// exp(mu + sigma^2 / 2)
    /// and the standard deviation
    /// sqrt([exp(sigma^2)-1] * exp(2 * mu + sigma^2))
    /// </summary>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="mu">The mu parameter of the log-normal distribution (not the mean).</param>
    /// <param name="sigma">The sigma parameter of the log-normal distribution (not the standard deviation).</param>
    /// <returns>A log-normal distributed random value.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public double RandLogNormal(IRandom random, double mu, double sigma) {
      return Math.Exp(RandNormal(random, mu, sigma));
    }
    /// <summary>
    /// Returns values from a log-normal distribution with the mean
    /// exp(mu + sigma^2 / 2)
    /// and the standard deviation
    /// sqrt([exp(sigma^2)-1] * exp(2 * mu + sigma^2))
    /// </summary>
    /// <param name="mu">The mu parameter of the log-normal distribution (not the mean).</param>
    /// <param name="sigma">The sigma parameter of the log-normal distribution (not the standard deviation).</param>
    /// <returns>A log-normal distributed random value.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public double RandLogNormal(double mu, double sigma) {
      return RandLogNormal(Random, mu, sigma);
    }

    /// <summary>
    /// Returns values from a log-normal distribution with
    /// the mean <paramref name="mean"/> and standard deviation <paramref name="stdev"/>.
    /// </summary>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="mean">The distribution mean.</param>
    /// <param name="stdev">The distribution standard deviation.</param>
    /// <returns>A log-normal distributed random value.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public double RandLogNormal2(IRandom random, double mean, double stdev) {
      if (stdev == 0) return mean;
      var sigma = Math.Sqrt(Math.Log(stdev * stdev / (mean * mean) + 1));
      var mu = Math.Log(mean) - 0.5 * sigma * sigma;
      return Math.Exp(RandNormal(random, mu, sigma));
    }
    /// <summary>
    /// Returns values from a log-normal distribution with
    /// the mean <paramref name="mean"/> and standard deviation <paramref name="stdev"/>.
    /// </summary>
    /// <param name="mean">The distribution mean.</param>
    /// <param name="stdev">The distribution standard deviation.</param>
    /// <returns>A log-normal distributed random value.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public double RandLogNormal2(double mean, double stdev) {
      return RandLogNormal2(Random, mean, stdev);
    }

    /// <summary>
    /// Returns a timespan value from a log-normal distribution with the mean
    /// exp(mu + sigma^2 / 2)
    /// and the standard deviation
    /// sqrt([exp(sigma^2)-1] * exp(2 * mu + sigma^2))
    /// </summary>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="mu">The mu parameter of the log-normal distribution (not the mean).</param>
    /// <param name="sigma">The sigma parameter of the log-normal distribution (not the standard deviation).</param>
    /// <returns>A log-normal distributed random timespan.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandLogNormal(IRandom random, TimeSpan mu, TimeSpan sigma) {
      return TimeSpan.FromSeconds(RandLogNormal(random, mu.TotalSeconds, sigma.TotalSeconds));
    }
    /// <summary>
    /// Returns a timespan value from a log-normal distribution with the mean
    /// exp(mu + sigma^2 / 2)
    /// and the standard deviation
    /// sqrt([exp(sigma^2)-1] * exp(2 * mu + sigma^2))
    /// </summary>
    /// <param name="mu">The mu parameter of the log-normal distribution (not the mean).</param>
    /// <param name="sigma">The sigma parameter of the log-normal distribution (not the standard deviation).</param>
    /// <returns>A log-normal distributed random timespan.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandLogNormal(TimeSpan mu, TimeSpan sigma) {
      return RandLogNormal(Random, mu, sigma);
    }

    /// <summary>
    /// Returns a timespan value from a log-normal distribution with
    /// the mean <paramref name="mean"/> and standard deviation <paramref name="stdev"/>.
    /// </summary>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="mean">The distribution mean.</param>
    /// <param name="stdev">The distribution standard deviation.</param>
    /// <returns>A log-normal distributed random timespan.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandLogNormal2(IRandom random, TimeSpan mean, TimeSpan stdev) {
      return TimeSpan.FromSeconds(RandLogNormal2(random, mean.TotalSeconds, stdev.TotalSeconds));
    }
    /// <summary>
    /// Returns a timespan value from a log-normal distribution with
    /// the mean <paramref name="mean"/> and standard deviation <paramref name="stdev"/>.
    /// </summary>
    /// <param name="mean">The distribution mean.</param>
    /// <param name="stdev">The distribution standard deviation.</param>
    /// <returns>A log-normal distributed random timespan.</returns>
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandLogNormal2(TimeSpan mean, TimeSpan stdev) {
      return RandLogNormal2(Random, mean, stdev);
    }

    [Obsolete("Consider to use the respective distribution class")]
    public double RandCauchy(IRandom random, double x0, double gamma) {
      return Cauchy.Sample(random, x0, gamma);
    }
    [Obsolete("Consider to use the respective distribution class")]
    public double RandCauchy(double x0, double gamma) {
      return RandCauchy(Random, x0, gamma);
    }

    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandCauchy(IRandom random, TimeSpan x0, TimeSpan gamma) {
      return TimeSpan.FromSeconds(RandCauchy(random, x0.TotalSeconds, gamma.TotalSeconds));
    }
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandCauchy(TimeSpan x0, TimeSpan gamma) {
      return RandCauchy(Random, x0, gamma);
    }

    [Obsolete("Consider to use the respective distribution class")]
    public double RandWeibull(IRandom random, double alpha, double beta) {
      return Weibull.Sample(random, alpha, beta);
    }
    [Obsolete("Consider to use the respective distribution class")]
    public double RandWeibull(double alpha, double beta) {
      return RandWeibull(Random, alpha, beta);
    }

    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandWeibull(IRandom random, TimeSpan alpha, TimeSpan beta) {
      return TimeSpan.FromSeconds(RandWeibull(random, alpha.TotalSeconds, beta.TotalSeconds));
    }
    [Obsolete("Consider to use the respective distribution class")]
    public TimeSpan RandWeibull(TimeSpan alpha, TimeSpan beta) {
      return RandWeibull(Random, alpha, beta);
    }

    /// <summary>
    /// This method chooses a single element from <paramref name="source"/> with equal probability.
    /// </summary>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="source">The elements to choose from.</param>
    /// <typeparam name="T">The type of the elements to be chosen.</typeparam>
    /// <returns>The chosen element.</returns>
    [Obsolete("Consider to use the EmpiricalUniform distribution class")]
    public T RandChoice<T>(IRandom random, IList<T> source) {
      var idx = random.Next(source.Count);
      return source[idx];
    }
    /// <summary>
    /// Calls <see cref="RandChoice{T}(IRandom, IList{T})"/> with the default RNG instance <see cref="Random"/>.
    /// </summary>
    /// <param name="source">The elements to choose from.</param>
    /// <typeparam name="T">The type of the elements to be chosen.</typeparam>
    /// <returns>The chosen element.</returns>
    [Obsolete("Consider to use the EmpiricalUniform distribution class")]
    public T RandChoice<T>(IList<T> source) {
      return RandChoice(Random, source);
    }

    /// <summary>
    /// This method chooses <paramref name="count"/> elements from <paramref name="source"/> with repetition.
    /// </summary>
    /// <remarks>
    /// Runtime complexity for selecting M out of a list of N elements is O(M).
    /// Order is not preserved, the items are returned in arbitrary order.
    /// 
    /// Parameter <paramref name="count"/> can be 0 in which case the enumerable will be empty.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="count"/> is negative.
    /// </exception>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="source">The elements to choose from.</param>
    /// <param name="count">The number of elements to choose.</param>
    /// <typeparam name="T">The type of the elements to be chosen.</typeparam>
    /// <returns>An enumeration of the elements.</returns>
    [Obsolete("Consider to use the EmpiricalUniform distribution class")]
    public IEnumerable<T> RandChoice<T>(IRandom random, IList<T> source, int count) {
      return new EmpiricalUniform<T>(source).Sample(random, count);
    }    
    /// <summary>
    /// Calls <see cref="RandChoice{T}(IRandom, IList{T}, int)"/> with the default RNG instance <see cref="Random"/>.
    /// </summary>
    /// <param name="source">The elements to choose from.</param>
    /// <param name="count">The number of elements to choose.</param>
    /// <typeparam name="T">The type of the elements to be chosen.</typeparam>
    /// <returns>The chosen element.</returns>
    [Obsolete("Consider to use the EmpiricalUniform distribution class")]
    public IEnumerable<T> RandChoice<T>(IList<T> source, int count) {
      return RandChoice(Random, source, count);
    }

    /// <summary>
    /// Generates a random sample from a given source
    /// </summary>
    /// <typeparam name="T">The type of the element in parameter source</typeparam>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="source"/> and <paramref name="weights"/> have different size.
    /// or when <paramref name="weights"/> contains an invalid or negative value.
    /// or when <paramref name="weights"/> sum equals zero or an invalid value.
    /// </exception>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="source">a random sample is generated from its elements.</param>
    /// <param name="weights">The weight associated with each entry in source.</param>
    /// <returns>The generated random samples</returns>
    [Obsolete("Consider to use the EmpiricalNonUniform distribution class")]
    public T RandChoice<T>(IRandom random, IList<T> source, IList<double> weights) {
      return EmpiricalNonUniform<T>.Sample(random, source, weights);
    }
    /// <summary>
    /// Generates a random sample from a given source
    /// </summary>
    /// <typeparam name="T">The type of the element in parameter source</typeparam>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="source"/> and <paramref name="weights"/> have different size.
    /// or when <paramref name="weights"/> contains an invalid or negative value.
    /// or when <paramref name="weights"/> sum equals zero
    /// </exception>
    /// <param name="source">a random sample is generated from its elements.</param>
    /// <param name="weights">The weight associated with each entry in source.</param>
    /// <returns>The generated random samples</returns>
    [Obsolete("Consider to use the EmpiricalNonUniform distribution class")]
    public T RandChoice<T>(IList<T> source, IList<double> weights) {
      return RandChoice<T>(Random, source, weights);
    }

    /// <summary>
    /// This methods chooses a single element from <paramref name="source"/> randomly and by only enumerating
    /// the elements.
    /// </summary>
    /// <remarks>
    /// The preferred and faster method is <see cref="RandChoice{T}(IRandom, IList{T})"/>. Use of this method should
    /// be limited to cases where it is undesirable to reserve a contiguous block of memory for <paramref name="source"/>.
    /// 
    /// This method iterates over all elements and calls the RNG each time, thus runtime complexity is O(N).
    /// </remarks>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="source">The elements to choose from.</param>
    /// <typeparam name="T">The type of the elements to be chosen.</typeparam>
    /// <returns>The chosen element.</returns>
    [Obsolete("Consider to use EmpiricalUniform.SampleOnline")]
    public T RandChoiceOnline<T>(IRandom random, IEnumerable<T> source) {
      return EmpiricalUniform<T>.SampleOnline(random, source);
    }
    /// <summary>
    /// Calls <see cref="RandChoiceOnline{T}(IRandom, IEnumerable{T})"/> with the default RNG instance <see cref="Random"/>.
    /// </summary>
    /// <remarks>
    /// The preferred and faster method is <see cref="RandChoice{T}(IList{T})"/>. Use of this method should
    /// be limited to cases where it is undesirable to reserve a contiguous block of memory for <paramref name="source"/>.
    /// 
    /// This method iterates over all elements and calls the RNG each time, thus runtime complexity is O(N).
    /// </remarks>
    /// <param name="source">The elements to choose from.</param>
    /// <typeparam name="T">The type of the elements to be chosen.</typeparam>
    /// <returns>The chosen element.</returns>
    [Obsolete("Consider to use the UniformAny distribution")]
    public T RandChoiceOnline<T>(IEnumerable<T> source) {
      return RandChoiceOnline<T>(Random, source);
    }

    /// <summary>
    /// This methods chooses <paramref name="count"/> element from <paramref name="source"/> randomly and by only enumerating
    /// the elements.
    /// </summary>
    /// <remarks>
    /// The preferred and faster method is <see cref="RandChoice{T}(IRandom, IList{T}, int)"/>. Use of this method should
    /// be limited to cases where it is undesirable to reserve a contiguous block of memory for <paramref name="source"/>.
    /// However, the method itself reserves an array of length <paramref name="count"/> and uses a single pass of all elements
    /// in <parmaref name="source"/>.
    /// 
    /// Using a count of 0 is possible and will return an empty enumerable.
    /// 
    /// For selecting M from a source of N elements runtime complexity is O(N*M). The random number generator will also be called M*N times.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="count"/> is negative.</exception>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="source">The elements to choose from.</param>
    /// <param name="count">The number of elements to choose.</param>
    /// <typeparam name="T">The type of the elements to be chosen.</typeparam>
    /// <returns>An enumeration of the chosen elements.</returns>
    [Obsolete("Consider to use EmpiricalUniform.SampleOnline")]
    public IEnumerable<T> RandChoiceOnline<T>(IRandom random, IEnumerable<T> source, int count) {
      return EmpiricalUniform<T>.SampleOnline(random, source, count);
    }
    /// <summary>
    /// Calls <see cref="RandChoiceOnline{T}(IRandom, IEnumerable{T}, int)"/> with the default RNG instance <see cref="Random"/>.
    /// </summary>
    /// <remarks>
    /// The preferred and faster method is <see cref="RandChoice{T}(IList{T}, int)"/>. Use of this method should
    /// be limited to cases where it is undesirable to reserve a contiguous block of memory for <paramref name="source"/>.
    /// However, the method itself reserves an array of length <paramref name="count"/> and uses a single pass of all elements
    /// in <parmaref name="source"/>.
    /// 
    /// Using a count of 0 is possible and will return an empty enumerable.
    /// 
    /// For selecting M from a source of N elements runtime complexity is O(N*M). The random number generator will also be called M*N times.
    /// </remarks>
    /// <param name="source">The elements to choose from.</param>
    /// <param name="count">The number of elements to choose.</param>
    /// <typeparam name="T">The type of the elements to be chosen.</typeparam>
    /// <returns>An enumeration of the chosen elements.</returns>
    [Obsolete("Consider to use the EmpiricalUniform distribution")]
    public IEnumerable<T> RandChoiceOnline<T>(IEnumerable<T> source, int count) {
      return RandChoiceOnline<T>(Random, source, count);
    }

    /// <summary>
    /// This method chooses <paramref name="count"/> elements from <parameref name="source"/> such that
    /// no element is selected twice. Respectively, elements that are M times in the source may also
    /// be selected up to M times.
    /// </summary>
    /// <remarks>
    /// The method is implemented to iterate over all elements respectively until <paramref name="count"/> elements
    /// are selected. Runtime complexity for selecting M out of a list of N elements is thus O(N).
    /// 
    /// Order is preserved, the items are returned in the same relative order as they appear in <paramref name="source"/>.
    /// 
    /// Parameter <paramref name="count"/> can be 0 in which case the enumerable will be empty.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="count"/> is negative or when there are not enough items in <paramerf name="source"/>
    /// to choose from.
    /// </exception>
    /// <param name="random">The random number generator to use.</param>
    /// <param name="source">The elements to choose from.</param>
    /// <param name="count">The number of elements to choose.</param>
    /// <returns>An enumeration of the elements.</returns>
    [Obsolete("Consider to use EmpiricalUniform.SampleNoRepetition or the online static method")]
    public IEnumerable<T> RandChoiceNoRepetition<T>(IRandom random, IEnumerable<T> source, int count) {
      return EmpiricalUniform<T>.SampleOnlineNoRepetition(random, source, count);
    }
    /// <summary>
    /// Calls <see cref="RandChoiceNoRepetition{T}(IRandom, IEnumerable{T}, int)"/> with the default RNG instance <see cref="Random"/>.
    /// </summary>
    /// <remarks>
    /// The method is implemented to iterate over all elements respectively until <paramref name="count"/> elements
    /// are selected. Runtime complexity for selecting M out of a list of N elements is thus O(N).
    /// 
    /// Order is preserved, the items are returned in the same relative order as they appear in <paramref name="source"/>.
    /// 
    /// Parameter <paramref name="count"/> can be 0 in which case the enumerable will be empty.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="count"/> is negative or when there are not enough items in <paramerf name="source"/>
    /// to choose from.
    /// </exception>
    /// <param name="source">The elements to choose from without repetition.</param>
    /// <param name="count">The number of elements that should be chosen.</param>
    /// <typeparam name="T">The type of elements to be chosen.</typeparam>
    /// <returns>An enumeration of the elements.</returns>
    [Obsolete("Consider to use the EmpiricalUniform distribution")]
    public IEnumerable<T> RandChoiceNoRepetition<T>(IEnumerable<T> source, int count) {
      return RandChoiceNoRepetition<T>(Random, source, count);
    }

    #endregion

    #region Random timeouts
    public Timeout Timeout(IRandom random, IDistribution<double> duration) {
      return new Timeout(this, ToTimeSpan(duration.Sample(random)));
    }
    public Timeout Timeout(IDistribution<double> duration) {
      return new Timeout(this, ToTimeSpan(duration.Sample(Random)));
    }
    public Timeout Timeout(IRandom random, IDistribution<int> duration) {
      return new Timeout(this, ToTimeSpan(duration.Sample(random)));
    }
    public Timeout Timeout(IDistribution<int> duration) {
      return new Timeout(this, ToTimeSpan(duration.Sample(Random)));
    }
    public Timeout Timeout(IRandom random, IDistribution<TimeSpan> duration) {
      return new Timeout(this, duration.Sample(random));
    }
    public Timeout Timeout(IDistribution<TimeSpan> duration) {
      return new Timeout(this, duration.Sample(Random));
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutUniformD(IRandom random, double a, double b) {
      return new Timeout(this, ToTimeSpan(RandUniform(random, a, b)));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutUniformD(double a, double b) {
      return TimeoutUniformD(Random, a, b);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutUniform(IRandom random, TimeSpan a, TimeSpan b) {
      return new Timeout(this, RandUniform(random, a, b));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutUniform(TimeSpan a, TimeSpan b) {
      return TimeoutUniform(Random, a, b);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutTriangularD(IRandom random, double low, double high) {
      return new Timeout(this, ToTimeSpan(RandTriangular(random, low, high)));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutTriangularD(double low, double high) {
      return TimeoutTriangularD(Random, low, high);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutTriangular(IRandom random, TimeSpan low, TimeSpan high) {
      return new Timeout(this, RandTriangular(random, low, high));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutTriangular(TimeSpan low, TimeSpan high) {
      return TimeoutTriangular(Random, low, high);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutTriangularD(IRandom random, double low, double high, double mode) {
      return new Timeout(this, ToTimeSpan(RandTriangular(random, low, high, mode)));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutTriangularD(double low, double high, double mode) {
      return TimeoutTriangularD(Random, low, high, mode);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutTriangular(IRandom random, TimeSpan low, TimeSpan high, TimeSpan mode) {
      return new Timeout(this, RandTriangular(random, low, high, mode));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutTriangular(TimeSpan low, TimeSpan high, TimeSpan mode) {
      return TimeoutTriangular(Random, low, high, mode);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutExponentialD(IRandom random, double mean) {
      return new Timeout(this, ToTimeSpan(RandExponential(random, mean)));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutExponentialD(double mean) {
      return TimeoutExponentialD(Random, mean);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutExponential(IRandom random, TimeSpan mean) {
      return new Timeout(this, RandExponential(random, mean));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutExponential(TimeSpan mean) {
      return TimeoutExponential(Random, mean);
    }

    [Obsolete("Consider to use TimeoutRandom with the bounded distribution class")]
    public Timeout TimeoutNormalPositiveD(IRandom random, double mu, double sigma) {
      return new Timeout(this, ToTimeSpan(RandNormalPositive(random, mu, sigma)));
    }
    [Obsolete("Consider to use TimeoutRandom with the bounded distribution class")]
    public Timeout TimeoutNormalPositiveD(double mu, double sigma) {
      return TimeoutNormalPositiveD(Random, mu, sigma);
    }

    [Obsolete("Consider to use TimeoutRandom with the bounded distribution class")]
    public Timeout TimeoutNormalPositive(IRandom random, TimeSpan mu, TimeSpan sigma) {
      return new Timeout(this, RandNormalPositive(random, mu, sigma));
    }
    [Obsolete("Consider to use TimeoutRandom with the bounded distribution class")]
    public Timeout TimeoutNormalPositive(TimeSpan mu, TimeSpan sigma) {
      return TimeoutNormalPositive(Random, mu, sigma);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutLogNormalD(IRandom random, double mu, double sigma) {
      return new Timeout(this, ToTimeSpan(RandLogNormal(random, mu, sigma)));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutLogNormalD(double mu, double sigma) {
      return TimeoutLogNormalD(Random, mu, sigma);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutLogNormal2D(IRandom random, double mean, double stdev) {
      return new Timeout(this, ToTimeSpan(RandLogNormal2(random, mean, stdev)));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutLogNormal2D(double mean, double stdev) {
      return TimeoutLogNormal2D(Random, mean, stdev);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutLogNormal(IRandom random, TimeSpan mu, TimeSpan sigma) {
      return new Timeout(this, RandLogNormal(random, mu, sigma));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutLogNormal(TimeSpan mu, TimeSpan sigma) {
      return TimeoutLogNormal(Random, mu, sigma);
    }

    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutLogNormal2(IRandom random, TimeSpan mean, TimeSpan stdev) {
      return new Timeout(this, RandLogNormal2(random, mean, stdev));
    }
    [Obsolete("Consider to use TimeoutRandom with an appropriate distribution class")]
    public Timeout TimeoutLogNormal2(TimeSpan mean, TimeSpan stdev) {
      return TimeoutLogNormal2(Random, mean, stdev);
    }
    #endregion
  }

  /// <summary>
  /// Provides a simulation environment that is thread-safe against manipulations of the event queue.
  /// Its performance is somewhat lower than the non-thread-safe environment (cf. <see cref="Simulation"/>)
  /// due to the locking involved.
  /// </summary>
  /// <remarks>
  /// Please carefully consider if you must really schedule the stop event in a separate thread. You can also
  /// call <see cref="Simulation.StopAsync"/> to request termination after the current event has been processed.
  /// 
  /// The simulation will still run in only one thread and execute all events sequentially.
  /// </remarks>
  public class ThreadSafeSimulation : Simulation {
    protected object _locker;

    public ThreadSafeSimulation() : this(new DateTime(1970, 1, 1)) { }
    public ThreadSafeSimulation(TimeSpan? defaultStep) : this(new DateTime(1970, 1, 1), defaultStep) { }
    public ThreadSafeSimulation(DateTime initialDateTime, TimeSpan? defaultStep = null) : this(new PcgRandom(), initialDateTime, defaultStep) { }
    public ThreadSafeSimulation(int randomSeed, TimeSpan? defaultStep = null) : this(new DateTime(1970, 1, 1), randomSeed, defaultStep) { }
    public ThreadSafeSimulation(DateTime initialDateTime, int randomSeed, TimeSpan? defaultStep = null) : this(new PcgRandom(randomSeed), initialDateTime, defaultStep) { }
    public ThreadSafeSimulation(IRandom random, DateTime initialDateTime, TimeSpan? defaultStep = null) : base(random, initialDateTime, defaultStep) {
      _locker = new object();
    }


    /// <summary>
    /// Schedules an event to occur at the same simulation time as the call was made.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe against manipulations of the event queue
    /// </remarks>
    /// <param name="event">The event that should be scheduled.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    public override void Schedule(Event @event, int priority = 0) {
      lock (_locker) {
        DoSchedule(Now, @event, priority);
      }
    }

    /// <summary>
    /// Schedules an event to occur after a certain (positive) delay.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe against manipulations of the event queue
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="delay"/> is negative.
    /// </exception>
    /// <param name="delay">The (positive) delay after which the event should be fired.</param>
    /// <param name="event">The event that should be scheduled.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    public override void Schedule(TimeSpan delay, Event @event, int priority = 0) {
      if (delay < TimeSpan.Zero)
        throw new ArgumentException("Negative delays are not allowed in Schedule(TimeSpan, Event).");
      lock (_locker) {
        var eventTime = Now + delay;
        DoSchedule(eventTime, @event, priority);
      }
    }

    /// <summary>
    /// Run until a certain event is processed.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe against manipulations of the event queue
    /// </remarks>
    /// <param name="stopEvent">The event that stops the simulation.</param>
    /// <returns></returns>
    public override object Run(Event stopEvent = null) {
      _stop = new CancellationTokenSource();
      if (stopEvent != null) {
        if (stopEvent.IsProcessed) {
          return stopEvent.Value;
        }
        stopEvent.AddCallback(StopSimulation);
      }
      OnRunStarted();
      try {
        var stop = false;
        lock (_locker) {
          stop = ScheduleQ.Count == 0 || _stop.IsCancellationRequested;
        }
        while (!stop) {
          Step();
          lock (_locker) {
            stop = ScheduleQ.Count == 0 || _stop.IsCancellationRequested;
          }
        }
      } catch (StopSimulationException e) { OnRunFinished(); return e.Value; }
      OnRunFinished();
      if (stopEvent == null) return null;
      if (!_stop.IsCancellationRequested && !stopEvent.IsTriggered) throw new InvalidOperationException("No scheduled events left but \"until\" event was not triggered.");
      return stopEvent.Value;
    }

    public Task<object> RunAsync(TimeSpan duration) {
      return Task.Run(() => Run(duration));
    }

    public Task<object> RunAsync(DateTime until) {
      return Task.Run(() => Run(until));
    }

    /// <summary>
    /// Run until a certain event is processed, but does not block.
    /// </summary>
    /// <param name="stopEvent">The event that stops the simulation.</param>
    /// <returns></returns>
    public Task<object> RunAsync(Event stopEvent = null) {
      return Task.Run(() => Run(stopEvent));
    }

    /// <summary>
    /// Performs a single step of the simulation, i.e. process a single event
    /// </summary>
    /// <remarks>
    /// This method is thread-safe against manipulations of the event queue
    /// </remarks>
    public override void Step() {
      Event evt;
      lock (_locker) {
        var next = ScheduleQ.Dequeue();
        Now = next.PrimaryPriority;
        evt = next.Event;
      }
      evt.Process();
      ProcessedEvents++;
    }

    /// <summary>
    /// Peeks at the time of the next event in terms of the defined step
    /// </summary>
    /// <remarks>
    /// This method is thread-safe against manipulations of the event queue
    /// </remarks>
    public override double PeekD() {
      lock (_locker) {
        if (ScheduleQ.Count == 0) return double.MaxValue;
        return (Peek() - StartDate).TotalSeconds / DefaultTimeStepSeconds;
      }
    }

    /// <summary>
    /// Peeks at the time of the next event
    /// </summary>
    /// <remarks>
    /// This method is thread-safe against manipulations of the event queue
    /// </remarks>
    public override DateTime Peek() {
      lock (_locker) {
        return ScheduleQ.Count > 0 ? ScheduleQ.First.PrimaryPriority : DateTime.MaxValue;
      }
    }
  }

  /// <summary>
  /// Provides a simulation environment where delays in simulation time may result in a similar
  /// delay in wall-clock time. The environment is not an actual realtime simulation environment
  /// in that there is no guarantee that 3 seconds in model time are also exactly 3 seconds in
  /// observed wall-clock time. This simulation environment is a bit slower, as the overhead of
  /// the simulation kernel (event creation, queuing, processing, etc.) is not accounted for.
  /// 
  /// However, it features a switch between virtual and realtime, thus allowing it to be used
  /// in contexts where realtime is only necessary sometimes (e.g. during interaction with
  /// long-running co-processes). Such use cases may arise in simulation control problems.
  /// </summary>
  public class PseudoRealtimeSimulation : ThreadSafeSimulation {
    public const double DefaultRealtimeScale = 1;

    /// <summary>
    /// The scale at which the simulation runs in comparison to realtime. A value smaller
    /// than 1 results in longer-than-realtime delays, while a value larger than 1 results
    /// in shorter-than-realtime delays. A value of exactly 1 is realtime.
    /// </summary>
    public double? RealtimeScale { get; protected set; } = DefaultRealtimeScale;
    /// <summary>
    /// Whether a non-null <see cref="RealtimeScale"/> has been set.
    /// </summary>
    public bool IsRunningInRealtime => RealtimeScale.HasValue;

    private object _timeLocker = new object();
    /// <summary>
    /// The current model time. Note that, while in realtime, this may continuously change.
    /// </summary>
    public override DateTime Now {
      get {
        lock (_timeLocker) {
          if (!IsRunningInRealtime) return base.Now;
          return base.Now + TimeSpan.FromMilliseconds(_rtDelayTime.Elapsed.TotalMilliseconds * RealtimeScale.Value);
        }
      }
      protected set => base.Now = value;
    }

    protected CancellationTokenSource _rtDelayCtrl = null;
    protected Stopwatch _rtDelayTime = new Stopwatch();


    public PseudoRealtimeSimulation() : this(new DateTime(1970, 1, 1)) { }
    public PseudoRealtimeSimulation(TimeSpan? defaultStep) : this(new DateTime(1970, 1, 1), defaultStep) { }
    public PseudoRealtimeSimulation(DateTime initialDateTime, TimeSpan? defaultStep = null) : this(new PcgRandom(), initialDateTime, defaultStep) { }
    public PseudoRealtimeSimulation(int randomSeed, TimeSpan? defaultStep = null) : this(new DateTime(1970, 1, 1), randomSeed, defaultStep) { }
    public PseudoRealtimeSimulation(DateTime initialDateTime, int randomSeed, TimeSpan? defaultStep = null) : this(new PcgRandom(randomSeed), initialDateTime, defaultStep) { }
    public PseudoRealtimeSimulation(IRandom random, DateTime initialDateTime, TimeSpan? defaultStep = null) : base(random, initialDateTime, defaultStep) { }
    
    protected override EventQueueNode DoSchedule(DateTime date, Event @event, int priority = 0) {
      if (ScheduleQ.Count > 0 && date < ScheduleQ.First.PrimaryPriority) _rtDelayCtrl?.Cancel();
      return base.DoSchedule(date, @event, priority);
    }

    public override void Step() {
      var delay = TimeSpan.Zero;
      double? rtScale = null;
      lock (_locker) {
        if (IsRunningInRealtime) {
          rtScale = RealtimeScale;
          var next = ScheduleQ.First.PrimaryPriority;
          delay = next - base.Now;
          if (rtScale.Value != 1.0) delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds / rtScale.Value);
          _rtDelayCtrl = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
        }
      }

      if (delay > TimeSpan.Zero) {
        _rtDelayTime.Start();
        Task.Delay(delay, _rtDelayCtrl.Token).ContinueWith(_ => { }).Wait();
        _rtDelayTime.Stop();
        var observed = _rtDelayTime.Elapsed;

        lock (_locker) {
          if (rtScale.Value != 1.0) observed = TimeSpan.FromMilliseconds(observed.TotalMilliseconds * rtScale.Value);
          if (_rtDelayCtrl.IsCancellationRequested && observed < delay) {
            lock (_timeLocker) {
              Now = base.Now + observed;
              _rtDelayTime.Reset();
            }
            return; // next event is not processed, step is not actually completed
          }
        }
      }

      Event evt;
      lock (_locker) {
        var next = ScheduleQ.Dequeue();
        lock (_timeLocker) {
          _rtDelayTime.Reset();
          Now = next.PrimaryPriority;
        }
        evt = next.Event;
      }
      evt.Process();
      ProcessedEvents++;
    }

    /// <summary>
    /// Switches the simulation to virtual time mode, i.e., running as fast as possible.
    /// In this mode, events are processed without delay just like in a <see cref="ThreadSafeSimulation"/>.
    /// </summary>
    /// <remarks>
    /// An ongoing real-time delay is being canceled when this method is called. Usually, this
    /// is only the case when this method is called from a thread other than the main simulation thread.
    /// 
    /// If the simulation is already in virtual time mode, this method has no effect.
    /// </remarks>
    public virtual void SetVirtualtime() {
      lock (_locker) {
        if (!IsRunningInRealtime) return;
        RealtimeScale = null;
        _rtDelayCtrl?.Cancel();
      }
    }

    /// <summary>
    /// Switches the simulation to real time mode. The real time factor of
    /// this default mode is configurable.
    /// </summary>
    /// <remarks>
    /// If this method is called while running in real-time mode, but given a different
    /// <paramref name="realtimeScale"/>, the current delay is canceled and the remaining
    /// time is delayed using the new time factor.
    /// 
    /// The default factor is 1, i.e., real time - a timeout of 5 seconds would cause
    /// a wall-clock delay of 5 seconds. With a factor of 2, the delay as measured by
    /// a wall clock would be 2.5 seconds, whereas a factor of 0.5, a wall-clock delay of
    /// 10 seconds would be observed.
    /// </remarks>
    /// <param name="realtimeScale">A value strictly greater than 0 used to scale real time events.</param>
    public virtual void SetRealtime(double realtimeScale = DefaultRealtimeScale) {
      lock (_locker) {
        if (realtimeScale <= 0.0) throw new ArgumentException("The simulation speed scaling factor must be strictly positive.", nameof(realtimeScale));
        if (IsRunningInRealtime && realtimeScale != RealtimeScale) _rtDelayCtrl?.Cancel();
        RealtimeScale = realtimeScale;
      }
    }

    /// <summary>
    /// This is only a convenience for mixed real- and virtual time simulations.
    /// It creates a new pseudo realtime process which will set the simulation
    /// to realtime every time it continues (e.g., if it has been set to virtual time).
    /// The process is automatically scheduled to be started at the current simulation time.
    /// </summary>
    /// <param name="generator">The generator function that represents the process.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    /// <param name="realtimeScale">A value strictly greater than 0 used to scale real time events (1 = realtime).</param>
    /// <returns>The scheduled process that was created.</returns>
    public Process PseudoRealtimeProcess(IEnumerable<Event> generator, int priority = 0, double realtimeScale = DefaultRealtimeScale) {
      return new PseudoRealtimeProcess(this, generator, priority, realtimeScale);
    }
  }

  /// <summary>
  /// Environments hold the event queues, schedule and process events.
  /// </summary>
  [Obsolete("Use class Simulation or ThreadSafeSimulation instead. Due to name clashes with System.Environment the class SimSharp.Environment is being outphased.")]
  public class Environment : ThreadSafeSimulation {
    public Environment()
      : base() {
      Random = new SystemRandom();
    }
    public Environment(TimeSpan? defaultStep)
      : base(defaultStep) {
      Random = new SystemRandom();
    }
    public Environment(int randomSeed, TimeSpan? defaultStep = null)
      : base(randomSeed, defaultStep) {
      Random = new SystemRandom(randomSeed);
    }
    public Environment(DateTime initialDateTime, TimeSpan? defaultStep = null)
      : base(initialDateTime, defaultStep) {
      Random = new SystemRandom();
    }
    public Environment(DateTime initialDateTime, int randomSeed, TimeSpan? defaultStep = null)
      : base(initialDateTime, randomSeed, defaultStep) {
      Random = new SystemRandom(randomSeed);
    }

    protected static readonly double NormalMagicConst = 4 * Math.Exp(-0.5) / Math.Sqrt(2.0);
    public override double RandNormal(double mu, double sigma) {
      return RandNormal(Random, mu, sigma);
    }
    public override double RandNormal(IRandom random, double mu, double sigma) {
      double z, zz, u1, u2;
      do {
        u1 = random.NextDouble();
        u2 = 1 - random.NextDouble();
        z = NormalMagicConst * (u1 - 0.5) / u2;
        zz = z * z / 4.0;
      } while (zz > -Math.Log(u2));
      return mu + z * sigma;
    }
  }
}
