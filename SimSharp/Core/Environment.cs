#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2016  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace SimSharp {

  /// <summary>
  /// Environments hold the event queues, schedule and process events.
  /// </summary>
  [Obsolete("Use class Simulation or ThreadSafeSimulation instead. Due to name clashes with System.Environment the class SimSharp.Environment is being outphased.")]
  public class Environment : ThreadSafeSimulation {
    public Environment() : base() { }
    public Environment(TimeSpan? defaultStep) : base(defaultStep) { }
    public Environment(int randomSeed, TimeSpan? defaultStep = null) : base(randomSeed, defaultStep) { }
    public Environment(DateTime initialDateTime, TimeSpan? defaultStep = null) : base(initialDateTime, defaultStep) { }
    public Environment(DateTime initialDateTime, int randomSeed, TimeSpan? defaultStep = null) : base(initialDateTime, randomSeed, defaultStep) { }
  }

  /// <summary>
  /// Simulation hold the event queues, schedule and process events.
  /// </summary>
  /// <remarks>
  /// This class is not thread-safe against manipulation of the event queue. If you supply a termination
  /// event that is set outside the simulation, please use the <see cref="ThreadSafeSimulation"/> environment.
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

    /// <summary>
    /// The current simulation time as a calendar date.
    /// </summary>
    public DateTime Now { get; protected set; }

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
    public Simulation(DateTime initialDateTime, TimeSpan? defaultStep = null) : this(new SystemRandom(), initialDateTime, defaultStep) { }
    public Simulation(IRandom random, DateTime initialDateTime, TimeSpan? defaultStep = null) {
      DefaultTimeStepSeconds = (defaultStep ?? TimeSpan.FromSeconds(1)).Duration().TotalSeconds;
      StartDate = initialDateTime;
      Now = initialDateTime;
      Random = random;
      ScheduleQ = new EventQueue(InitialMaxEvents);
      Logger = Console.Out;
    }
    public Simulation(int randomSeed, TimeSpan? defaultStep = null) : this(new DateTime(1970, 1, 1), randomSeed, defaultStep) { }
    public Simulation(DateTime initialDateTime, int randomSeed, TimeSpan? defaultStep = null) : this(new SystemRandom(), initialDateTime, randomSeed, defaultStep) { }
    public Simulation(IRandom random, DateTime initialDateTime, int randomSeed, TimeSpan? defaultStep = null) {
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
      Random = new SystemRandom(randomSeed);
      ScheduleQ = new EventQueue(InitialMaxEvents);
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

    protected bool _stopRequested = false;
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
      _stopRequested = false;
      if (stopEvent != null) {
        if (stopEvent.IsProcessed) return stopEvent.Value;
        stopEvent.AddCallback(StopSimulation);
      }

      try {
        var stop = ScheduleQ.Count == 0 || _stopRequested;
        while (!stop) {
          Step();
          ProcessedEvents++;
          stop = ScheduleQ.Count == 0 || _stopRequested;
        }
      } catch (StopSimulationException e) { return e.Value; }
      if (stopEvent == null) return null;
      if (!stopEvent.IsTriggered) throw new InvalidOperationException("No scheduled events left but \"until\" event was not triggered.");
      return stopEvent.Value;
    }

    public virtual void StopAsync() {
      _stopRequested = true;
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
    public double RandUniform(double a, double b) {
      return a + (b - a) * Random.NextDouble();
    }

    public TimeSpan RandUniform(TimeSpan a, TimeSpan b) {
      return TimeSpan.FromSeconds(RandUniform(a.TotalSeconds, b.TotalSeconds));
    }

    public double RandTriangular(double low, double high) {
      var u = Random.NextDouble();
      if (u > 0.5)
        return high + (low - high) * Math.Sqrt(((1.0 - u) / 2));
      return low + (high - low) * Math.Sqrt(u / 2);
    }

    public TimeSpan RandTriangular(TimeSpan low, TimeSpan high) {
      return TimeSpan.FromSeconds(RandTriangular(low.TotalSeconds, high.TotalSeconds));
    }

    public double RandTriangular(double low, double high, double mode) {
      var u = Random.NextDouble();
      var c = (mode - low) / (high - low);
      if (u > c)
        return high + (low - high) * Math.Sqrt(((1.0 - u) * (1.0 - c)));
      return low + (high - low) * Math.Sqrt(u * c);
    }

    public TimeSpan RandTriangular(TimeSpan low, TimeSpan high, TimeSpan mode) {
      return TimeSpan.FromSeconds(RandTriangular(low.TotalSeconds, high.TotalSeconds, mode.TotalSeconds));
    }

    /// <summary>
    /// Returns a number that is exponentially distributed given a certain mean.
    /// </summary>
    /// <remarks>
    /// Unlike in other APIs here the mean should be given and not the lambda parameter.
    /// </remarks>
    /// <param name="mean">The mean(!) of the distribution is 1 / lambda.</param>
    /// <returns>A number that is exponentially distributed</returns>
    public double RandExponential(double mean) {
      return -Math.Log(1 - Random.NextDouble()) * mean;
    }

    /// <summary>
    /// Returns a timespan that is exponentially distributed given a certain mean.
    /// </summary>
    /// <remarks>
    /// Unlike in other APIs here the mean should be given and not the lambda parameter.
    /// </remarks>
    /// <param name="mean">The mean(!) of the distribution is 1 / lambda.</param>
    /// <returns>A number that is exponentially distributed</returns>
    public TimeSpan RandExponential(TimeSpan mean) {
      return TimeSpan.FromSeconds(RandExponential(mean.TotalSeconds));
    }

    private bool useSpareNormal = false;
    private double spareNormal = double.NaN;
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
    public double RandNormal(double mu, double sigma) {
      if (useSpareNormal) {
        useSpareNormal = false;
        return spareNormal * sigma + mu;
      } else {
        double u, v, s;
        do {
          u = Random.NextDouble() * 2 - 1;
          v = Random.NextDouble() * 2 - 1;
          s = u * u + v * v;
        } while (s >= 1 || s == 0);
        var mul = Math.Sqrt(-2.0 * Math.Log(s) / s);
        spareNormal = v * mul;
        useSpareNormal = true;
        return mu + sigma * u * mul;
      }
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
    public TimeSpan RandNormal(TimeSpan mu, TimeSpan sigma) {
      return TimeSpan.FromSeconds(RandNormal(mu.TotalSeconds, sigma.TotalSeconds));
    }

    public double RandNormalPositive(double mu, double sigma) {
      double val;
      do {
        val = RandNormal(mu, sigma);
      } while (val <= 0);
      return val;
    }

    public TimeSpan RandNormalPositive(TimeSpan mu, TimeSpan sigma) {
      return TimeSpan.FromSeconds(RandNormalPositive(mu.TotalSeconds, sigma.TotalSeconds));
    }

    public double RandNormalNegative(double mu, double sigma) {
      double val;
      do {
        val = RandNormal(mu, sigma);
      } while (val >= 0);
      return val;
    }

    public TimeSpan RandNormalNegative(TimeSpan mu, TimeSpan sigma) {
      return TimeSpan.FromSeconds(RandNormalNegative(mu.TotalSeconds, sigma.TotalSeconds));
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
    public double RandLogNormal(double mu, double sigma) {
      return Math.Exp(RandNormal(mu, sigma));
    }

    /// <summary>
    /// Returns values from a log-normal distribution with
    /// the mean <paramref name="mean"/> and standard deviation <paramref name="stdev"/>.
    /// </summary>
    /// <param name="mean">The distribution mean.</param>
    /// <param name="stdev">The distribution standard deviation.</param>
    /// <returns>A log-normal distributed random value.</returns>
    public double RandLogNormal2(double mean, double stdev) {
      if (stdev == 0) return mean;
      var alpha = Math.Sqrt(mean * stdev) / mean;
      var sigma = Math.Sqrt(Math.Log(1 + (alpha * alpha)));
      var mu = Math.Log(mean) - 0.5 * sigma * sigma;
      return Math.Exp(RandNormal(mu, sigma));
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
    public TimeSpan RandLogNormal(TimeSpan mu, TimeSpan sigma) {
      return TimeSpan.FromSeconds(RandLogNormal(mu.TotalSeconds, sigma.TotalSeconds));
    }

    /// <summary>
    /// Returns a timespan value from a log-normal distribution with
    /// the mean <paramref name="mean"/> and standard deviation <paramref name="stdev"/>.
    /// </summary>
    /// <param name="mean">The distribution mean.</param>
    /// <param name="stdev">The distribution standard deviation.</param>
    /// <returns>A log-normal distributed random timespan.</returns>
    public TimeSpan RandLogNormal2(TimeSpan mean, TimeSpan stdev) {
      return TimeSpan.FromSeconds(RandLogNormal2(mean.TotalSeconds, stdev.TotalSeconds));
    }

    public double RandCauchy(double x0, double gamma) {
      return x0 + gamma * Math.Tan(Math.PI * (Random.NextDouble() - 0.5));
    }

    public TimeSpan RandCauchy(TimeSpan x0, TimeSpan gamma) {
      return TimeSpan.FromSeconds(RandCauchy(x0.TotalSeconds, gamma.TotalSeconds));
    }

    public double RandWeibull(double alpha, double beta) {
      return alpha * Math.Pow(-Math.Log(1 - Random.NextDouble()), 1 / beta);
    }

    public TimeSpan RandWeibull(TimeSpan alpha, TimeSpan beta) {
      return TimeSpan.FromSeconds(RandWeibull(alpha.TotalSeconds, beta.TotalSeconds));
    }
    #endregion

    #region Random timeouts
    public Timeout TimeoutUniformD(double a, double b) {
      return new Timeout(this, ToTimeSpan(RandUniform(a, b)));
    }

    public Timeout TimeoutUniform(TimeSpan a, TimeSpan b) {
      return new Timeout(this, RandUniform(a, b));
    }

    public Timeout TimeoutTriangularD(double low, double high) {
      return new Timeout(this, ToTimeSpan(RandTriangular(low, high)));
    }

    public Timeout TimeoutTriangular(TimeSpan low, TimeSpan high) {
      return new Timeout(this, RandTriangular(low, high));
    }

    public Timeout TimeoutTriangularD(double low, double high, double mode) {
      return new Timeout(this, ToTimeSpan(RandTriangular(low, high, mode)));
    }

    public Timeout TimeoutTriangular(TimeSpan low, TimeSpan high, TimeSpan mode) {
      return new Timeout(this, RandTriangular(low, high, mode));
    }

    public Timeout TimeoutExponentialD(double mean) {
      return new Timeout(this, ToTimeSpan(RandExponential(mean)));
    }

    public Timeout TimeoutExponential(TimeSpan mean) {
      return new Timeout(this, RandExponential(mean));
    }

    public Timeout TimeoutNormalPositiveD(double mu, double sigma) {
      return new Timeout(this, ToTimeSpan(RandNormalPositive(mu, sigma)));
    }

    public Timeout TimeoutNormalPositive(TimeSpan mu, TimeSpan sigma) {
      return new Timeout(this, RandNormalPositive(mu, sigma));
    }

    public Timeout TimeoutLogNormalD(double mu, double sigma) {
      return new Timeout(this, ToTimeSpan(RandLogNormal(mu, sigma)));
    }

    public Timeout TimeoutLogNormal2D(double mean, double stdev) {
      return new Timeout(this, ToTimeSpan(RandLogNormal2(mean, stdev)));
    }

    public Timeout TimeoutLogNormal(TimeSpan mu, TimeSpan sigma) {
      return new Timeout(this, RandLogNormal(mu, sigma));
    }

    public Timeout TimeoutLogNormal2(TimeSpan mean, TimeSpan stdev) {
      return new Timeout(this, RandLogNormal2(mean, stdev));
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
  /// call <see cref="Simulation.StopAsync"/> to request termination at the next possible synchronization point.
  /// 
  /// The simulation will still run in only one thread and execute all events sequentially.
  /// </remarks>
  public class ThreadSafeSimulation : Simulation {
    protected object _locker;

    public ThreadSafeSimulation() : this(new DateTime(1970, 1, 1)) { }
    public ThreadSafeSimulation(TimeSpan? defaultStep) : this(new DateTime(1970, 1, 1), defaultStep) { }
    public ThreadSafeSimulation(DateTime initialDateTime, TimeSpan? defaultStep = null) : this(new SystemRandom(), initialDateTime, defaultStep) { }
    public ThreadSafeSimulation(IRandom random, DateTime initialDateTime, TimeSpan? defaultStep = null) : base(random, initialDateTime, defaultStep) {
      _locker = new object();
    }
    public ThreadSafeSimulation(int randomSeed, TimeSpan? defaultStep = null) : this(new DateTime(1970, 1, 1), randomSeed, defaultStep) { }
    public ThreadSafeSimulation(DateTime initialDateTime, int randomSeed, TimeSpan? defaultStep = null) : this(new SystemRandom(), initialDateTime, randomSeed, defaultStep) { }
    public ThreadSafeSimulation(IRandom random, DateTime initialDateTime, int randomSeed, TimeSpan? defaultStep = null)
      : base(random, initialDateTime, randomSeed, defaultStep) {
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
      _stopRequested = false;
      if (stopEvent != null) {
        if (stopEvent.IsProcessed) return stopEvent.Value;
        stopEvent.AddCallback(StopSimulation);
      }

      try {
        var stop = false;
        lock (_locker) {
          stop = ScheduleQ.Count == 0 || _stopRequested;
        }
        while (!stop) {
          Step();
          ProcessedEvents++;
          lock (_locker) {
            stop = ScheduleQ.Count == 0 || _stopRequested;
          }
        }
      } catch (StopSimulationException e) { return e.Value; }
      if (stopEvent == null) return null;
      if (!stopEvent.IsTriggered) throw new InvalidOperationException("No scheduled events left but \"until\" event was not triggered.");
      return stopEvent.Value;
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
}
