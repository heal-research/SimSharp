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

using System.Collections.Generic;

namespace SimSharp {
  public class AnyOf : Condition {
    public AnyOf(Simulation environment, params Event[] events) : base(environment, events) { }
    public AnyOf(Simulation environment, IEnumerable<Event> events) : base(environment, events) { }

    protected override bool Evaluate() {
      return FiredEvents.Count > 0 || Events.Count == 0;
    }
  }
}
