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
using System.Linq;

namespace SimSharp {
  /// <summary>
  /// Environments hold the event queues, schedule and process events.
  /// </summary>
  public class Environment {
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

    private SortedList<DateTime, Queue<Event>> schedule;
    private Queue<Event> priority;
    private Queue<Event> queue;
    public Process ActiveProcess { get; set; }

    public TextWriter Logger { get; set; }
    public int ProcessedEvents { get; protected set; }

    public Environment() : this(new DateTime(1970, 1, 1)) { }
    public Environment(int randomSeed) : this(new DateTime(1970, 1, 1), randomSeed) { }
    public Environment(DateTime initialDateTime) {
      Now = initialDateTime;
      Random = new Random();
      schedule = new SortedList<DateTime, Queue<Event>>();
      priority = new Queue<Event>();
      queue = new Queue<Event>();
      Logger = Console.Out;
    }
    public Environment(DateTime initialDateTime, int randomSeed) {
      Now = initialDateTime;
      Random = new Random(randomSeed);
      schedule = new SortedList<DateTime, Queue<Event>>();
      priority = new Queue<Event>();
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
      schedule = new SortedList<DateTime, Queue<Event>>();
      queue = new Queue<Event>();
    }

    public virtual void Schedule(Event @event, bool urgent = false) {
      if (urgent) priority.Enqueue(@event);
      else queue.Enqueue(@event);
    }

    public virtual void Schedule(TimeSpan delay, Event @event) {
      if (delay < TimeSpan.Zero)
        throw new ArgumentException("Zero or negative delays are not allowed in Schedule(TimeSpan, Event). Use Schedule(Event, bool) for zero delays.");
      if (delay == TimeSpan.Zero) {
        Schedule(@event);
        return;
      }
      var eventTime = Now + delay;
      if (schedule.ContainsKey(eventTime)) {
        schedule[eventTime].Enqueue(@event);
      } else {
        var q = new Queue<Event>();
        q.Enqueue(@event);
        schedule.Add(eventTime, q);
      }
    }

    public virtual void Run(TimeSpan span) {
      Run(Now + span);
    }

    public virtual void Run(DateTime? until = null) {
      var limit = until ?? DateTime.MaxValue;
      if (limit <= Now) throw new InvalidOperationException("Simulation end date must lie in the future.");
      var stopEvent = new Event(this);
      if (limit < DateTime.MaxValue) {
        if (schedule.ContainsKey(limit))
          schedule[limit] = new Queue<Event>(new[] { stopEvent }.Concat(schedule[limit].ToArray()));
        else schedule.Add(limit, new Queue<Event>(new[] { stopEvent }));
      }
      Run(stopEvent);
    }

    public virtual void Run(Event stopEvent) {
      stopEvent.AddCallback(StopSimulation);
      try {
        while (priority.Count > 0 || queue.Count > 0 || schedule.Count > 0) {
          Step();
          ProcessedEvents++;
        }
      } catch (EmptyScheduleException) { }
    }

    public virtual void Step() {
      if (priority.Count > 0) {
        priority.Dequeue().Process();
      } else {
        if (queue.Count == 0) {
          var next = schedule.First();
          Now = next.Key;
          queue = next.Value;
          schedule.Remove(next.Key);
        }
        queue.Dequeue().Process();
      }
    }

    public virtual DateTime Peek() {
      return (queue.Count > 0 || priority.Count > 0) ? Now : (schedule.Count > 0 ? schedule.First().Key : DateTime.MaxValue);
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
