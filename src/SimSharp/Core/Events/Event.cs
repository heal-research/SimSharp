#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;

namespace SimSharp {
  /// <summary>
  /// The base class for all events in SimSharp.
  /// An event can be in one of three states at any time:
  ///  - Alive: The event object exists, but is neither scheduled to
  ///           be executed, nor is it already executed.
  ///  - Triggered: The event has been put in the event queue and is
  ///               going to be executed.
  ///  - Processed: The event has been executed.
  /// 
  /// Usually, the event is alive until its Trigger, Succeed, or Fail
  /// method have been called. Then it becomes triggered. When the
  /// Environment progresses to the event and executes its callbacks
  /// the event becomes processed.
  /// </summary>
  public class Event {
    protected internal Simulation Environment { get; private set; }
    protected List<Action<Event>> CallbackList { get; set; }

    /// <summary>
    /// The value property can be used to return arbitrary data from a
    /// process or an event. It also represents the interrupt cause to
    /// a process.
    /// </summary>
    public object Value { get; protected set; }

    /// <summary>
    /// The IsOk flag indicates if the event succeeded or failed. An event
    /// that failed indicates to a waiting process that the action could
    /// not be performed and that the faulting situation must be handled.
    /// Typically, interrupting a process sets the IsOk flag to false.
    /// </summary>
    public bool IsOk { get; protected set; }
    /// <summary>
    /// An event is alive when it is not triggered and not processed. That
    /// is, when it exists in memory without being scheduled. Typically,
    /// a Process is alive until its last event has been processed and the
    /// process event itself is to be processed.
    /// </summary>
    public bool IsAlive { get { return !IsTriggered && !IsProcessed; } }
    /// <summary>
    /// An event becomes processed when its callbacks have been executed.
    /// Events may only be processed once and an exception will be thrown
    /// if they are to be processed multiple times.
    /// </summary>
    public bool IsProcessed { get; protected set; }
    /// <summary>
    /// An event becomes triggered when it is placed into the event queue.
    /// That is, when its callbacks are going to be executed.
    /// An even that is triggered may later not be failed or retriggered.
    /// </summary>
    public bool IsTriggered { get; protected set; }

    public Event(Simulation environment) {
      Environment = environment;
      CallbackList = new List<Action<Event>>();
    }

    /// <summary>
    /// This method schedules the event right now. It takes the IsOk state
    /// and uses the <see cref="Value"/> of the given <paramref name="@event"/>.
    /// Thus if the given event fails, this event will also be triggered as
    /// failing.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the event has already been triggered.
    /// </exception>
    /// <remarks>
    /// The signature of this method allows it to be used as a callback.
    /// </remarks>
    /// <param name="event">The event that triggers this event.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    public virtual void Trigger(Event @event, int priority = 0) {
      if (IsTriggered)
        throw new InvalidOperationException("Event has already been triggered.");
      IsOk = @event.IsOk;
      Value = @event.Value;
      IsTriggered = true;
      Environment.Schedule(this, priority);
    }

    /// <summary>
    /// This method schedules the event right now. It sets IsOk state to true
    /// and optionally uses also the value. If urgent is given, the event may
    /// be scheduled as urgent. Urgent events are placed in a separate event
    /// queue. The callbacks of urgent events are executed before normal events.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the event has already been triggered.
    /// </exception>
    /// <param name="value">The value that the event should use.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    public virtual void Succeed(object value = null, int priority = 0) {
      if (IsTriggered)
        throw new InvalidOperationException("Event has already been triggered.");
      IsOk = true;
      Value = value;
      IsTriggered = true;
      Environment.Schedule(this, priority);
    }

    /// <summary>
    /// This method schedules the event right now. It sets IsOk state to false
    /// and optionally uses also the value. If urgent is given, the event may
    /// be scheduled as urgent. Urgent events are placed in a separate event
    /// queue. The callbacks of urgent events are executed before normal events.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the event has already been triggered.
    /// </exception>
    /// <param name="value">The value that the event should use.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    public virtual void Fail(object value = null, int priority = 0) {
      if (IsTriggered)
        throw new InvalidOperationException("Event has already been triggered.");
      IsOk = false;
      Value = value;
      IsTriggered = true;
      Environment.Schedule(this, priority);
    }

    /// <summary>
    /// This method adds a callback to the list of callbacks. Callbacks will be
    /// executed in the order they have been added.
    /// </summary>
    /// <param name="callback">The callback to execute when the event is being
    /// processed.</param>
    public virtual void AddCallback(Action<Event> callback) {
      if (IsProcessed) throw new InvalidOperationException("Event is already processed.");
      CallbackList.Add(callback);
    }

    /// <summary>
    /// This method adds a range of callbacks to the list of callbacks. Callbacks
    /// will be executed in the order they have been added.
    /// </summary>
    /// <param name="callbacks">The callbacks to execute when the event is being
    /// processed.</param>
    public virtual void AddCallbacks(IEnumerable<Action<Event>> callbacks) {
      if (IsProcessed) throw new InvalidOperationException("Event is already processed.");
      CallbackList.AddRange(callbacks);
    }

    /// <summary>
    /// This method removes a callback to the list of callbacks.
    /// </summary>
    /// <remarks>
    /// It is not checked if the callback has actually been added before and
    /// no exception will be thrown if it had not been present.
    /// </remarks>
    /// <param name="callback">The callback to remove.</param>
    public virtual void RemoveCallback(Action<Event> callback) {
      if (IsProcessed) throw new InvalidOperationException("Event is already processed.");
      CallbackList.Remove(callback);
    }

    /// <summary>
    /// This method processes the event, that is, it calls all the callbacks.
    /// When it finishes it will be marked IsProcessed and cannot be processed
    /// again.
    /// </summary>
    /// <exception cref="InvalidOperationException">When the event has already
    /// been processed.</exception>
    public virtual void Process() {
      if (IsProcessed) throw new InvalidOperationException("Event has already been processed.");
      IsProcessed = true;
      for (var i = 0; i < CallbackList.Count; i++)
        CallbackList[i](this);
      CallbackList = null;
    }

    public static Condition operator &(Event event1, Event event2) {
      return new AllOf(event1.Environment, event1, event2);
    }
    public static Condition operator |(Event event1, Event event2) {
      return new AnyOf(event1.Environment, event1, event2);
    }
  }
}
