#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System.Collections.Generic;

namespace SimSharp {
  public class AllOf : Condition {
    public AllOf(Simulation environment, params Event[] events) : base(environment, events) { }
    public AllOf(Simulation environment, IEnumerable<Event> events) : base(environment, events) { }

    protected override bool Evaluate() {
      return FiredEvents.Count == Events.Count;
    }
  }
}
