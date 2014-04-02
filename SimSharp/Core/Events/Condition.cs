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
using System.Diagnostics;
using System.Linq;

namespace SimSharp {
  /// <summary>
  /// Conditions are events that execute when any or all of its sub-events are executed.
  /// </summary>
  public class Condition : Event {
    /// <summary>
    /// The operation of the condition can be to wait for All or Any event.
    /// </summary>
    public Operator Operation { get; private set; }

    protected List<Event> Events { get; private set; }

    protected List<Event> FiredEvents { get; private set; }

    public enum Operator {
      All,
      Any
    }

    public Condition(Environment environment, Operator operation, params Event[] events)
      : base(environment) {
      Debug.Assert(events.All(e => ReferenceEquals(e.Environment, environment)));
      Operation = operation;

      Events = new List<Event>(events.Length);
      FiredEvents = new List<Event>();

      foreach (var @event in events)
        AddEvent(@event);
    }

    protected virtual void AddEvent(Event @event) {
      Events.Add(@event);
      @event.AddCallback(Check);
    }

    protected virtual void Check(Event @event) {
      FiredEvents.Add(@event);

      if (!@event.IsOk)
        Fail(@event.Value);
      else if (!IsProcessed && Evaluate()) {
        Succeed();
      }
    }

    protected virtual bool Evaluate() {
      switch (Operation) {
        case Operator.All:
          return FiredEvents.Count == Events.Count;
        case Operator.Any:
          return FiredEvents.Count > 0 || Events.Count == 0;
        default:
          throw new InvalidOperationException("Invalid Condition Operator.");
      }
    }
  }
}
