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
  public class Environment {
    private const int InitialMaxEvents = 1024;
    private object locker = new object();

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
    protected Queue<Event> Queue;
    public Process ActiveProcess { get; set; }

    public TextWriter Logger { get; set; }
    public int ProcessedEvents { get; protected set; }

    public Environment() : this(new DateTime(1970, 1, 1)) { }
    public Environment(TimeSpan? defaultStep) : this(new DateTime(1970, 1, 1), defaultStep) { }
    public Environment(int randomSeed, TimeSpan? defaultStep = null) : this(new DateTime(1970, 1, 1), randomSeed, defaultStep) { }
    public Environment(DateTime initialDateTime, TimeSpan? defaultStep = null) {
      DefaultTimeStepSeconds = (defaultStep ?? TimeSpan.FromSeconds(1)).Duration().TotalSeconds;
      StartDate = initialDateTime;
      Now = initialDateTime;
      Random = new SystemRandom();
      ScheduleQ = new EventQueue(InitialMaxEvents);
      Queue = new Queue<Event>();
      Logger = Console.Out;
    }
    public Environment(DateTime initialDateTime, int randomSeed, TimeSpan? defaultStep = null) {
      DefaultTimeStepSeconds = (defaultStep ?? TimeSpan.FromSeconds(1)).Duration().TotalSeconds;
      StartDate = initialDateTime;
      Now = initialDateTime;
      Random = new SystemRandom(randomSeed);
      ScheduleQ = new EventQueue(InitialMaxEvents);
      Queue = new Queue<Event>();
      Logger = Console.Out;
    }

    public double ToDouble(TimeSpan span) {
      return span.TotalSeconds / DefaultTimeStepSeconds;
    }

    public TimeSpan ToTimeSpan(double span) {
      return TimeSpan.FromSeconds(DefaultTimeStepSeconds * span);
    }

    public Process Process(IEnumerable<Event> generator) {
      return new Process(this, generator);
    }

    public Timeout TimeoutD(double delay) {
      return Timeout(TimeSpan.FromSeconds(DefaultTimeStepSeconds * delay));
    }

    public Timeout Timeout(TimeSpan delay) {
      return new Timeout(this, delay);
    }

    public virtual void Reset(int randomSeed) {
      ProcessedEvents = 0;
      Now = StartDate;
      Random = new SystemRandom(randomSeed);
      ScheduleQ = new EventQueue(InitialMaxEvents);
      Queue = new Queue<Event>();
    }

    public virtual void ScheduleD(double delay, Event @event) {
      Schedule(TimeSpan.FromSeconds(DefaultTimeStepSeconds * delay), @event);
    }

    public virtual void Schedule(Event @event) {
      lock (locker) {
        Queue.Enqueue(@event);
      }
    }

    public virtual void Schedule(TimeSpan delay, Event @event) {
      if (delay < TimeSpan.Zero)
        throw new ArgumentException("Negative delays are not allowed in Schedule(TimeSpan, Event).");
      lock (locker) {
        if (delay == TimeSpan.Zero) {
          Queue.Enqueue(@event);
          return;
        }
        var eventTime = Now + delay;
        DoSchedule(eventTime, @event);
      }
    }

    protected virtual EventQueueNode DoSchedule(DateTime date, Event @event) {
      if (ScheduleQ.MaxSize == ScheduleQ.Count) {
        // the capacity has to be adjusted, there are more events in the queue than anticipated
        var oldSchedule = ScheduleQ;
        ScheduleQ = new EventQueue(ScheduleQ.MaxSize * 2);
        foreach (var e in oldSchedule) ScheduleQ.Enqueue(e.Priority, e.Event);
      }
      return ScheduleQ.Enqueue(date, @event);
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

    public virtual object Run(Event stopEvent = null) {
      if (stopEvent != null) {
        if (stopEvent.IsProcessed) return stopEvent.Value;
        stopEvent.AddCallback(StopSimulation);
      }

      try {
        var stop = Queue.Count == 0 && ScheduleQ.Count == 0;
        while (!stop) {
          Step();
          ProcessedEvents++;
          lock (locker) {
            stop = Queue.Count == 0 && ScheduleQ.Count == 0;
          }
        }
      } catch (StopSimulationException e) { return e.Value; }
      if (stopEvent == null) return null;
      if (!stopEvent.IsTriggered) throw new InvalidOperationException("No scheduled events left but \"until\" event was not triggered.");
      return stopEvent.Value;
    }

    public virtual void Step() {
      Event evt;
      lock (locker) {
        if (Queue.Count == 0) {
          var next = ScheduleQ.Dequeue();
          Now = next.Priority;
          evt = next.Event;
        } else evt = Queue.Dequeue();
      }
      evt.Process();
    }

    public virtual double PeekD() {
      lock (locker) {
        if (Queue.Count == 0 && ScheduleQ.Count == 0) return double.MaxValue;
        return (Peek() - StartDate).TotalSeconds / DefaultTimeStepSeconds;
      }
    }

    public virtual DateTime Peek() {
      lock (locker) {
        return Queue.Count > 0 ? Now : (ScheduleQ.Count > 0 ? ScheduleQ.First.Priority : DateTime.MaxValue);
      }
    }

    protected virtual void StopSimulation(Event @event) {
      throw new StopSimulationException(@event.Value);
    }

    public virtual void Log(string message, params object[] args) {
      if (Logger != null)
        Logger.WriteLine(message, args);
    }

    #region Random number distributions
    protected static readonly double NormalMagicConst = 4 * Math.Exp(-0.5) / Math.Sqrt(2.0);

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

    public double RandNormal(double mu, double sigma) {
      double z, zz, u1, u2;
      do {
        u1 = Random.NextDouble();
        u2 = 1 - Random.NextDouble();
        z = NormalMagicConst * (u1 - 0.5) / u2;
        zz = z * z / 4.0;
      } while (zz > -Math.Log(u2));
      return mu + z * sigma;
    }

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

    public double RandLogNormal(double mu, double sigma) {
      return Math.Exp(RandNormal(mu, sigma));
    }

    public TimeSpan RandLogNormal(TimeSpan mu, TimeSpan sigma) {
      return TimeSpan.FromSeconds(RandLogNormal(mu.TotalSeconds, sigma.TotalSeconds));
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

    public TimeSpan RandWeibull(TimeSpan mu, TimeSpan sigma) {
      return TimeSpan.FromSeconds(RandWeibull(mu.TotalSeconds, sigma.TotalSeconds));
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

    public Timeout TimeoutLogNormal(TimeSpan mu, TimeSpan sigma) {
      return new Timeout(this, RandLogNormal(mu, sigma));
    }
    #endregion
  }
}
