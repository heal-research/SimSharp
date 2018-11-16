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
using System.Collections.Generic;
using System.Linq;

namespace SimSharp {
  /// <summary>
  /// A PreemptiveResource is similar to a <see cref="PriorityResource"/>. However,
  /// it may be possible to interrupt a lower-priority user and hand over the resource.
  /// 
  /// PreemptiveResource holds a fixed amount of anonymous entities.
  /// Requests are processed in this order: priority, time, preemption, and finally FIFO.
  /// Releases are processed in FIFO order (usually no simulation time passes for a Release).
  /// </summary>
  /// <remarks>
  /// Working with PreemptiveResource, a process holding a request must always call
  /// <see cref="Process.HandleFault"/> after yielding an event and handle a potential
  /// interruption.
  /// </remarks>
  public class PreemptiveResource {

    public int Capacity { get; protected set; }

    public int InUse { get { return Users.Count; } }

    public int Remaining { get { return Capacity - InUse; } }

    protected Simulation Environment { get; private set; }

    protected SimplePriorityQueue<PreemptiveRequest> RequestQueue { get; private set; }
    protected Queue<Release> ReleaseQueue { get; private set; }
    protected HashSet<PreemptiveRequest> Users { get; private set; }
    protected List<Event> WhenAnyQueue { get; private set; }
    protected List<Event> WhenFullQueue { get; private set; }
    protected List<Event> WhenEmptyQueue { get; private set; }
    protected List<Event> WhenChangeQueue { get; private set; }

    public PreemptiveResource(Simulation environment, int capacity = 1) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0.", "capacity");
      Environment = environment;
      Capacity = capacity;
      RequestQueue = new SimplePriorityQueue<PreemptiveRequest>();
      ReleaseQueue = new Queue<Release>();
      Users = new HashSet<PreemptiveRequest>();
      WhenAnyQueue = new List<Event>();
      WhenFullQueue = new List<Event>();
      WhenEmptyQueue = new List<Event>();
      WhenChangeQueue = new List<Event>();
    }

    public virtual PreemptiveRequest Request(double priority = 1, bool preempt = false) {
      var request = new PreemptiveRequest(Environment, TriggerRelease, DisposeCallback, priority, preempt);
      RequestQueue.Enqueue(request);
      TriggerRequest();
      return request;
    }

    public virtual Release Release(PreemptiveRequest request) {
      var release = new Release(Environment, request, TriggerRequest);
      ReleaseQueue.Enqueue(release);
      TriggerRelease();
      return release;
    }

    public virtual Event WhenAny() {
      var whenAny = new Event(Environment);
      WhenAnyQueue.Add(whenAny);
      TriggerWhenAny();
      return whenAny;
    }

    public virtual Event WhenFull() {
      var whenFull = new Event(Environment);
      WhenFullQueue.Add(whenFull);
      TriggerWhenFull();
      return whenFull;
    }

    public virtual Event WhenEmpty() {
      var whenEmpty = new Event(Environment);
      WhenEmptyQueue.Add(whenEmpty);
      TriggerWhenEmpty();
      return whenEmpty;
    }

    public virtual Event WhenChange() {
      var whenChange = new Event(Environment);
      WhenChangeQueue.Add(whenChange);
      return whenChange;
    }

    protected void DisposeCallback(Event @event) {
      var request = @event as PreemptiveRequest;
      if (request != null) Release(request);
    }

    protected virtual void DoRequest(PreemptiveRequest request) {
      if (Users.Count >= Capacity && request.Preempt) {
        // Check if we can preempt another process
        // MaxItems are the least important according to priorty, time, and preemption flag
        var preempt = Users.MaxItems(x => x).Last();
        if (preempt.CompareTo(request) > 0) {
          Users.Remove(preempt);
          preempt.Process.Interrupt(new Preempted(request.Process, preempt.Time));
        }
      }
      if (Users.Count < Capacity) {
        Users.Add(request);
        request.Succeed();
      }
    }

    protected virtual void DoRelease(Release release) {
      var req = (PreemptiveRequest)release.Request;
      if (!Users.Remove(req)) {
        RequestQueue.TryRemove(req);
      }
      release.Succeed();
    }

    protected virtual void TriggerRequest(Event @event = null) {
      while (RequestQueue.Count > 0) {
        var request = RequestQueue.First;
        DoRequest(request);
        if (request.IsTriggered) {
          RequestQueue.Dequeue();
          TriggerWhenEmpty();
          TriggerWhenChange();
        } else break;
      }
    }

    protected virtual void TriggerRelease(Event @event = null) {
      while (ReleaseQueue.Count > 0) {
        var release = ReleaseQueue.Peek();
        DoRelease(release);
        if (release.IsTriggered) {
          ReleaseQueue.Dequeue();
          TriggerWhenAny();
          TriggerWhenFull();
          TriggerWhenChange();
        } else break;
      }
    }

    protected virtual void TriggerWhenAny() {
      if (Remaining > 0) {
        if (WhenAnyQueue.Count == 0) return;
        foreach (var evt in WhenAnyQueue)
          evt.Succeed();
        WhenAnyQueue.Clear();
      }
    }

    protected virtual void TriggerWhenFull() {
      if (InUse == 0) {
        if (WhenFullQueue.Count == 0) return;
        foreach (var evt in WhenFullQueue)
          evt.Succeed();
        WhenFullQueue.Clear();
      }
    }

    protected virtual void TriggerWhenEmpty() {
      if (Remaining == 0) {
        if (WhenEmptyQueue.Count == 0) return;
        foreach (var evt in WhenEmptyQueue)
          evt.Succeed();
        WhenEmptyQueue.Clear();
      }
    }

    protected virtual void TriggerWhenChange() {
      if (WhenChangeQueue.Count == 0) return;
      foreach (var evt in WhenChangeQueue)
        evt.Succeed();
      WhenChangeQueue.Clear();
    }
  }
}
