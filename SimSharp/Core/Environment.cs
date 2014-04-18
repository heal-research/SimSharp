#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2014  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
    protected const int InitialMaxEvents = 1024;
    protected static readonly double NormalMagicConst = 4 * Math.Exp(-0.5) / Math.Sqrt(2.0);

    /// <summary>
    /// The current simulation date
    /// </summary>
    public DateTime Now { get; set; }
    /// <summary>
    /// The random number generator that is to be used in all events in
    /// order to produce reproducible results.
    /// </summary>
    protected Random Random { get; set; }

    private EventQueue schedule;
    private Queue<Event> queue;
    public Process ActiveProcess { get; set; }

    public TextWriter Logger { get; set; }
    public int ProcessedEvents { get; protected set; }

    public Environment() : this(new DateTime(1970, 1, 1)) { }
    public Environment(int randomSeed) : this(new DateTime(1970, 1, 1), randomSeed) { }
    public Environment(DateTime initialDateTime) {
      Now = initialDateTime;
      Random = new Random();
      schedule = new EventQueue(InitialMaxEvents);
      queue = new Queue<Event>();
      Logger = Console.Out;
    }
    public Environment(DateTime initialDateTime, int randomSeed) {
      Now = initialDateTime;
      Random = new Random(randomSeed);
      schedule = new EventQueue(InitialMaxEvents);
      queue = new Queue<Event>();
      Logger = Console.Out;
    }

    public Process Process(IEnumerable<Event> generator) {
      return new Process(this, generator);
    }

    public Timeout Timeout(TimeSpan delay) {
      return new Timeout(this, delay);
    }

    public virtual void Reset(DateTime initialTime, int randomSeed) {
      Now = initialTime;
      Random = new Random(randomSeed);
      schedule = new EventQueue(InitialMaxEvents);
      queue = new Queue<Event>();
    }

    public virtual void Schedule(Event @event) {
      queue.Enqueue(@event);
    }

    public virtual void Schedule(TimeSpan delay, Event @event) {
      if (delay < TimeSpan.Zero)
        throw new ArgumentException("Zero or negative delays are not allowed in Schedule(TimeSpan, Event). Use Schedule(Event, bool) for zero delays.");
      if (delay == TimeSpan.Zero) {
        queue.Enqueue(@event);
        return;
      }
      var eventTime = Now + delay;
      DoSchedule(eventTime, @event);
    }

    protected virtual EventQueueNode DoSchedule(DateTime date, Event @event) {
      if (schedule.MaxSize == schedule.Count) {
        // the capacity has to be adjusted, there are more events in the queue than anticipated
        var oldSchedule = schedule;
        schedule = new EventQueue(schedule.MaxSize * 2);
        foreach (var e in oldSchedule) schedule.Enqueue(e.Priority, e.Event);
      }
      return schedule.Enqueue(date, @event);
    }

    public virtual void Run(TimeSpan span) {
      Run(Now + span);
    }

    public virtual void Run(DateTime? until = null) {
      var limit = until ?? DateTime.MaxValue;
      if (limit <= Now) throw new InvalidOperationException("Simulation end date must lie in the future.");
      var stopEvent = new Event(this);
      if (limit < DateTime.MaxValue) {
        var node = DoSchedule(limit, stopEvent);
        // stop event is always the first to execute at the given time
        node.InsertionIndex = -1;
        schedule.OnNodeUpdated(node);
      }
      Run(stopEvent);
    }

    public virtual void Run(Event stopEvent) {
      stopEvent.AddCallback(StopSimulation);
      try {
        while (queue.Count > 0 || schedule.Count > 0) {
          Step();
          ProcessedEvents++;
        }
      } catch (EmptyScheduleException) { }
    }

    public virtual void Step() {
      if (queue.Count == 0) {
        var next = schedule.Dequeue();
        Now = next.Priority;
        next.Event.Process();
      } else queue.Dequeue().Process();
    }

    public virtual DateTime Peek() {
      return queue.Count > 0 ? Now : (schedule.Count > 0 ? schedule.First.Priority : DateTime.MaxValue);
    }

    protected virtual void StopSimulation(Event @event) {
      throw new EmptyScheduleException();
    }

    public void Log(string message, params object[] args) {
      if (Logger != null)
        Logger.WriteLine(message, args);
    }

    #region Random number distributions
    public double RandUniform(double a, double b) {
      return a + (b - a) * Random.NextDouble();
    }

    public double RandTriangular(double low, double high) {
      var u = Random.NextDouble();
      if (u > 0.5)
        return high + (low - high) * Math.Sqrt(((1.0 - u) / 2));
      return low + (high - low) * Math.Sqrt(u / 2);
    }

    public double RandTriangular(double low, double high, double mode) {
      var u = Random.NextDouble();
      var c = (mode - low) / (high - low);
      if (u > c)
        return high + (low - high) * Math.Sqrt(((1.0 - u) * (1.0 - c)));
      return low + (high - low) * Math.Sqrt(u * c);
    }

    public double RandExponential(double lambda) {
      return -Math.Log(1 - Random.NextDouble()) / lambda;
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

    public double RandNormalPositive(double mu, double sigma) {
      double val;
      do {
        val = RandNormal(mu, sigma);
      } while (val <= 0);
      return val;
    }

    public double RandNormalNegative(double mu, double sigma) {
      double val;
      do {
        val = RandNormal(mu, sigma);
      } while (val >= 0);
      return val;
    }


    public double RandLogNormal(double mu, double sigma) {
      return Math.Exp(RandNormal(mu, sigma));
    }

    public double RandCauchy(double x0, double gamma) {
      return x0 + gamma * Math.Tan(Math.PI * (Random.NextDouble() - 0.5));
    }

    public double RandWeibull(double alpha, double beta) {
      return alpha * Math.Pow(-Math.Log(1 - Random.NextDouble()), 1 / beta);
    }
    #endregion
  }
}
