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

namespace SimSharp {
  public class Event {
    protected internal Environment Environment { get; private set; }
    protected internal List<Action<Event>> CallbackList { get; set; }
    public IEnumerable<Action<Event>> Callbacks { get { return CallbackList.AsReadOnly(); } }

    public object Value { get; protected set; }

    public bool IsOk { get; protected set; }
    public bool IsProcessed { get { return CallbackList == null; } }
    public bool IsScheduled { get; protected set; }

    public Event(Environment environment) {
      Environment = environment;
      CallbackList = new List<Action<Event>>();
    }

    public virtual Event Succeed(object value = null, bool urgent = false) {
      if (IsScheduled)
        throw new InvalidOperationException("Event has already been triggered.");
      IsOk = true;
      Value = value;
      IsScheduled = true;
      Environment.Schedule(this, urgent: urgent);
      return this;
    }

    public virtual Event Fail(object value = null, bool urgent = false) {
      if (IsScheduled)
        throw new InvalidOperationException("Event has already been triggered.");
      IsOk = false;
      Value = value;
      IsScheduled = true;
      Environment.Schedule(this, urgent: urgent);
      return this;
    }

    public static Condition operator &(Event event1, Event event2) {
      return new Condition(event1.Environment, Condition.Operator.All, event1, event2);
    }
    public static Condition operator |(Event event1, Event event2) {
      return new Condition(event1.Environment, Condition.Operator.Any, event1, event2);
    }
  }
}
