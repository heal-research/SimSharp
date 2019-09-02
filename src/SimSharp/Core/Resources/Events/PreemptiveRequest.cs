#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public sealed class PreemptiveRequest : Request, IComparable<PreemptiveRequest>, IComparable {
    public double Priority { get; private set; }
    public bool Preempt { get; private set; }
    public bool IsPreempted { get; internal set; }

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
