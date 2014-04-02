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
    /// <summary>
    /// The current simulation date
    /// </summary>
    public DateTime Now { get; set; }
    /// <summary>
    /// The random number generator that is to be used in all events in
    /// order to produce reproducible results.
    /// </summary>
    public Random Random { get; protected set; }

    private SortedList<DateTime, Tuple<Queue<Event>, Queue<Event>>> queue;
    public Process ActiveProcess { get; set; }

    public TextWriter Logger { get; set; }
    public int ProcessedEvents { get; protected set; }

    public Environment() : this(new DateTime(1970, 1, 1)) { }
    public Environment(int randomSeed) : this(new DateTime(1970, 1, 1), randomSeed) { }
    public Environment(DateTime initialDateTime) {
      Now = initialDateTime;
      Random = new Random();
      queue = new SortedList<DateTime, Tuple<Queue<Event>, Queue<Event>>>();
      Logger = Console.Out;
    }
    public Environment(DateTime initialDateTime, int randomSeed) {
      Now = initialDateTime;
      Random = new Random(randomSeed);
      queue = new SortedList<DateTime, Tuple<Queue<Event>, Queue<Event>>>();
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
      queue = new SortedList<DateTime, Tuple<Queue<Event>, Queue<Event>>>();
    }

    public virtual void Schedule(Event @event, bool urgent = false) {
      Schedule(TimeSpan.FromSeconds(0), @event, urgent);
    }

    public virtual void Schedule(TimeSpan delay, Event @event, bool urgent = false) {
      if (delay < TimeSpan.Zero)
        throw new ArgumentException("Negative delays are not allowed.");

      var eventTime = Now + delay;
      if (queue.ContainsKey(eventTime)) {
        if (urgent) queue[eventTime].Item1.Enqueue(@event);
        else queue[eventTime].Item2.Enqueue(@event);
      } else {
        if (urgent) queue.Add(eventTime, Tuple.Create(new Queue<Event>(new[] { @event }), new Queue<Event>()));
        else queue.Add(eventTime, Tuple.Create(new Queue<Event>(), new Queue<Event>(new[] { @event })));
      }
    }

    public virtual void Run(TimeSpan span) {
      Run(Now + span);
    }

    public virtual void Run(DateTime? until = null) {
      var limit = until ?? DateTime.MaxValue;
      if (limit <= Now) throw new InvalidOperationException("Simulation end date must lie in the future.");
      var stopEvent = new Event(this);
      if (limit < DateTime.MaxValue)
        Schedule(limit - Now, stopEvent, urgent: true);
      Run(stopEvent);
    }

    public virtual void Run(Event stopEvent) {
      stopEvent.AddCallback(StopSimulation);
      try {
        while (queue.Count > 0) {
          Step();
          ProcessedEvents++;
        }
      } catch (EmptyScheduleException) { }
    }

    public virtual void Step() {
      var nextEvents = queue.First();
      Now = nextEvents.Key;
      var @event = nextEvents.Value.Item1.Count > 0 ? nextEvents.Value.Item1.Dequeue() : nextEvents.Value.Item2.Dequeue();
      if (nextEvents.Value.Item1.Count == 0 && nextEvents.Value.Item2.Count == 0)
        queue.Remove(Now);
      @event.Process();
    }

    public virtual DateTime Peek() {
      return queue.Count > 0 ? queue.First().Key : DateTime.MaxValue;
    }

    protected virtual void StopSimulation(Event @event) {
      throw new EmptyScheduleException();
    }

    public void Log(string message, params object[] args) {
      if (Logger != null)
        Logger.WriteLine(message, args);
    }
  }
}
