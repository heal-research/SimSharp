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
