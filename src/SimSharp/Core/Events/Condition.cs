#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace SimSharp {
  /// <summary>
  /// Conditions are events that execute when any or all of its sub-events are executed.
  /// </summary>
  public abstract class Condition : Event {

    public new OrderedDictionary Value {
      get { return (OrderedDictionary)base.Value; }
      set { base.Value = value; }
    }

    protected List<Event> Events { get; private set; }

    protected List<Event> FiredEvents { get; private set; }

    protected Condition(Simulation environment, params Event[] events)
      : this(environment, (IEnumerable<Event>)events) { }
    protected Condition(Simulation environment, IEnumerable<Event> events)
      : base(environment) {
      CallbackList.Add(CollectValues);
      Events = new List<Event>(events);
      FiredEvents = new List<Event>();

      foreach (var @event in Events) {
        if (Environment != @event.Environment)
          throw new ArgumentException("It is not allowed to mix events from different environments");
        if (@event.IsProcessed) Check(@event);
        else @event.AddCallback(Check);
      }

      if (IsAlive && Evaluate())
        Succeed();
    }

    protected void Check(Event @event) {
      if (IsTriggered || IsProcessed) {
        if (!@event.IsOk) throw new InvalidOperationException(
@"Errors that happen after the condition has been triggered will not be
handled by the condition and cause the simulation to crash.");
        return;
      }
      FiredEvents.Add(@event);

      if (!@event.IsOk)
        Fail(@event.Value);
      else if (Evaluate()) {
        Succeed();
      }
    }

    protected virtual IEnumerable<KeyValuePair<object, object>> GetValues() {
      var values = new List<KeyValuePair<object, object>>();
      foreach (var e in Events) {
        var condition = e as Condition;
        if (condition != null) {
          values.AddRange(condition.GetValues());
        } else if (e.IsProcessed) {
          values.Add(new KeyValuePair<object, object>(e, e.Value));
        }
      }
      return values;
    }

    protected virtual void CollectValues(Event @event) {
      if (@event.IsOk) {
        var value = new OrderedDictionary();
        foreach (var v in GetValues())
          value.Add(v.Key, v.Value);
        Value = value;
      }
    }

    protected abstract bool Evaluate();
  }
}
