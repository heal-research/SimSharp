#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimSharp {
  /// <summary>
  /// A ResourcePool is a crossover between a <see cref="Resource"/> and a <see cref="Store"/>.
  /// There is a fixed number of non-anonymous resources.
  /// 
  /// Requests are performed in FIFO order only when they match at least one resource in the pool.
  /// Releases are processed in FIFO order (usually no simulation time passes for a Release).
  /// </summary>
  public class ResourcePool {
    protected static readonly Func<object, bool> TrueFunc = _ => true;

    public int Capacity { get; protected set; }

    public int InUse { get { return Capacity - Remaining; } }

    public int Remaining { get { return Resources.Count; } }

    protected Simulation Environment { get; private set; }

    protected LinkedList<ResourcePoolRequest> RequestQueue { get; private set; }
    protected Queue<Release> ReleaseQueue { get; private set; }
    protected List<object> Resources { get; private set; }
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

    public ResourcePool(Simulation environment, IEnumerable<object> resources) {
      Environment = environment;
      if (resources == null) throw new ArgumentNullException("resources");
      Resources = new List<object>(resources);
      Capacity = Resources.Count;
      if (Capacity == 0) throw new ArgumentException("There must be at least one resource", "resources");
      RequestQueue = new LinkedList<ResourcePoolRequest>();
      ReleaseQueue = new Queue<Release>();
      WhenAnyQueue = new List<Event>();
      WhenFullQueue = new List<Event>();
      WhenEmptyQueue = new List<Event>();
      WhenChangeQueue = new List<Event>();
    }

    public virtual bool IsAvailable(Func<object, bool> filter) {
      return Resources.Any(filter);
    }

    public virtual ResourcePoolRequest Request(Func<object, bool> filter = null) {
      var request = new ResourcePoolRequest(Environment, TriggerRelease, DisposeCallback, filter ?? TrueFunc);
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
      if (request != null) Release(request);
    }

    protected virtual void DoRequest(ResourcePoolRequest request) {
      foreach (var o in Resources) {
        if (!request.Filter(o)) continue;
        WaitingTime?.Add(Environment.ToDouble(Environment.Now - request.Time));
        Resources.Remove(o);
        request.Succeed(o);
        return;
      }
    }

    protected virtual void DoRelease(Release release) {
      Resources.Add(release.Request.Value);
      LeadTime?.Add(Environment.ToDouble(Environment.Now - release.Request.Time));
      release.Succeed();
    }

    protected virtual void TriggerRequest(Event @event = null) {
      var current = RequestQueue.First;
      while (current != null) {
        var request = current.Value;
        DoRequest(request);
        if (request.IsTriggered) {
          var next = current.Next;
          RequestQueue.Remove(current);
          current = next;
          TriggerWhenEmpty();
          TriggerWhenChange();
        } else current = current.Next;
        if (Resources.Count == 0) break;
      }
      Utilization?.UpdateTo(InUse / (double)Capacity);
      WIP?.UpdateTo(InUse + RequestQueue.Count);
      QueueLength?.UpdateTo(RequestQueue.Count);
    }

    protected virtual void TriggerRelease(Event @event = null) {
      while (ReleaseQueue.Count > 0) {
        var release = ReleaseQueue.Peek();
        if (release.Request.IsAlive) {
          if (!RequestQueue.Remove((ResourcePoolRequest)release.Request))
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
