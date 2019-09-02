#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  /// <summary>
  /// Timeouts are simple events that are executed after a certain timespan has passed.
  /// </summary>
  public sealed class Timeout : Event {
    /// <summary>
    /// A timeout is an event that is executed after a certain timespan has passed.
    /// </summary>
    /// <remarks>
    /// Timeout events are scheduled when they are created. They are always triggered
    /// when they are created.
    /// </remarks>
    /// <param name="environment">The environment in which it is scheduled.</param>
    /// <param name="delay">The timespan for the timeout.</param>
    /// <param name="value">The value of the timeout.</param>
    /// <param name="isOk">Whether the timeout should succeed or fail.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    public Timeout(Simulation environment, TimeSpan delay, object value = null, bool isOk = true, int priority = 0)
      : base(environment) {
      IsOk = isOk;
      Value = value;
      IsTriggered = true;
      environment.Schedule(delay, this, priority);
    }
  }
}
