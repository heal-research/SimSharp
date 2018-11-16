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
  public sealed class PreemptiveRequest : Request, IComparable<PreemptiveRequest>, IComparable {
    public double Priority { get; private set; }
    public bool Preempt { get; private set; }

    public PreemptiveRequest(Simulation environment, Action<Event> callback, Action<Event> disposeCallback, double priority = 1, bool preempt = false)
      : base(environment, callback, disposeCallback) {
      Priority = priority;
      Preempt = preempt;
    }

    public int CompareTo(PreemptiveRequest other) {
      if (Priority > other.Priority) return 1;
      else if (Priority < other.Priority) return -1;
      if (Time > other.Time) return 1;
      else if (Time < other.Time) return -1;
      if (!Preempt && other.Preempt) return 1;
      else if (Preempt && !other.Preempt) return -1;
      return 0;
    }

    public int CompareTo(object obj) {
      if (obj is PreemptiveRequest other) return CompareTo(other);
      if (obj == null) return 1;
      throw new ArgumentException("Can only compare to other objects of type PreemptiveRequest");
    }
  }
}
