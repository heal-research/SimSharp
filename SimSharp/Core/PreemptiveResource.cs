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

using System.Linq;

namespace SimSharp {
  public class PreemptiveResource : PriorityResource {
    public PreemptiveResource(Environment environment, int capacity = 1) : base(environment, capacity) { }

    protected override void Request(Request request) {
      if (Users.Count >= MaxCapacity && request.Preempt) {
        // Check if we can preempt another process
        var oldest = Users.OrderByDescending(x => x.Priority).ThenByDescending(x => x.Time).First();
        if (oldest.Priority > request.Priority || (oldest.Priority == request.Priority && oldest.Time > request.Time)) {
          Users.Remove(oldest);
          oldest.Process.Interrupt(new Preempted(request.Process, oldest.Time));
        }
      }
      base.Request(request);
    }
  }
}
