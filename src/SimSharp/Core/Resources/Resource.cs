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
  /// A resource holds a fixed number of anonymous entities.
  /// 
  /// Requests are processed in FIFO order.
  /// Releases are processed in FIFO order (usually no simulation time passes for a Release).
  /// </summary>
  public class Resource {

    public int Capacity { get; protected set; }

    public int InUse { get { return Users.Count; } }

    public int Remaining { get { return Capacity - InUse; } }

    protected Simulation Environment { get; private set; }

    protected LinkedList<Request> RequestQueue { get; private set; }
    protected Queue<Release> ReleaseQueue { get; private set; }
    protected HashSet<Request> Users { get; private set; }
    protected List<Event> WhenAnyQueue { get; private set; }
    protected List<Event> WhenFullQueue { get; private set; }
    protected List<Event> WhenEmptyQueue { get; private set; }
    protected List<Event> WhenChangeQueue { get; private set; }

    public ITimeSeriesMonitor Utilization { get; set; }
    public ITimeSeriesMonitor WIP { get; set; }
    public ITimeSeriesMonitor QueueLength { get; set; }
    public ISampleMonitor LeadTime { get; set; }
    public ISampleMonitor WaitingTime { get; set; }
    public ISampleMonitor BreakOffTime { get; set; }

    public Resource(Simulation environment, int capacity = 1) {
      if (capacity <= 0) throw new ArgumentException("Capacity must > 0.", "capacity");
      Environment = environment;
      Capacity = capacity;
      RequestQueue = new LinkedList<Request>();
      ReleaseQueue = new Queue<Release>();
      Users = new HashSet<Request>();
      WhenAnyQueue = new List<Event>();
      WhenFullQueue = new List<Event>();
      WhenEmptyQueue = new List<Event>();
      WhenChangeQueue = new List<Event>();
    }

    public virtual Request Request() {
      var request = new Request(Environment, TriggerRelease, DisposeCallback);
      RequestQueue.AddLast(request);
      TriggerRequest();
      return request;
    }

    public virtual Release Release(Request request) {
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

    protected virtual void DisposeCallback(Event @event) {
      var request = @event as Request;
      if (request != null) {
        Release(request);
      }
    }

    protected virtual void DoRequest(Request request) {
      if (Users.Count < Capacity) {
        WaitingTime?.Add(Environment.ToDouble(Environment.Now - request.Time));
        Users.Add(request);
        request.Succeed();
      }
    }

    protected virtual void DoRelease(Release release) {
      if (!Users.Remove(release.Request))
        throw new InvalidOperationException("Released request does not have a user.");
      LeadTime?.Add(Environment.ToDouble(Environment.Now - release.Request.Time));
      release.Succeed();
    }

    protected virtual void TriggerRequest(Event @event = null) {
      while (RequestQueue.Count > 0) {
        var request = RequestQueue.First.Value;
        DoRequest(request);
        if (request.IsTriggered) {
          RequestQueue.RemoveFirst();
          TriggerWhenEmpty();
          TriggerWhenChange();
        } else break;
      }
      Utilization?.UpdateTo(InUse / (double)Capacity);
      WIP?.UpdateTo(InUse + RequestQueue.Count);
      QueueLength?.UpdateTo(RequestQueue.Count);
    }

    protected virtual void TriggerRelease(Event @event = null) {
      while (ReleaseQueue.Count > 0) {
        var release = ReleaseQueue.Peek();
        if (release.Request.IsAlive) {
          if (!RequestQueue.Remove(release.Request))
            throw new InvalidOperationException("Failed to cancel a request.");
          BreakOffTime?.Add(Environment.ToDouble(Environment.Now - release.Request.Time));
          release.Succeed();
          ReleaseQueue.Dequeue();
        } else {
          DoRelease(release);
          if (release.IsTriggered) {
            ReleaseQueue.Dequeue();
            TriggerWhenAny();
            TriggerWhenFull();
            TriggerWhenChange();
          } else break;
        }
      }
      Utilization?.UpdateTo(InUse / (double)Capacity);
      WIP?.UpdateTo(InUse + RequestQueue.Count);
      QueueLength?.UpdateTo(RequestQueue.Count);
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
