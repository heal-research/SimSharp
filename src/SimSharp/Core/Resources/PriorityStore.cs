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
  /// A PriorityStore is similar to a <see cref="Store"/>.
  /// However, items are removed from the store in order of their priority.
  /// 
  /// PriorityStore holds a variable number of individual items.
  /// Put and Get are both processed in strict FIFO order.
  /// </summary>
  public class PriorityStore {

    public int Capacity { get; protected set; }

    public int Count { get { return Items.Count; } }

    protected Simulation Environment { get; private set; }

    protected Queue<StorePut> PutQueue { get; private set; }
    protected Queue<StoreGet> GetQueue { get; private set; }
    protected SimplePriorityQueue<StoreItem, double> Items { get; private set; }
    protected List<Event> WhenNewQueue { get; private set; }
    protected List<Event> WhenAnyQueue { get; private set; }
    protected List<Event> WhenFullQueue { get; private set; }
    protected List<Event> WhenEmptyQueue { get; private set; }
    protected List<Event> WhenChangeQueue { get; private set; }

    public ITimeSeriesMonitor Utilization { get; set; }
    public ITimeSeriesMonitor WIP { get; set; }
    public ISampleMonitor LeadTime { get; set; }
    public ITimeSeriesMonitor PutQueueLength { get; set; }
    public ISampleMonitor PutWaitingTime { get; set; }
    public ITimeSeriesMonitor GetQueueLength { get; set; }
    public ISampleMonitor GetWaitingTime { get; set; }

    public PriorityStore(Simulation environment, int capacity = int.MaxValue) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      Environment = environment;
      Capacity = capacity;
      PutQueue = new Queue<StorePut>();
      GetQueue = new Queue<StoreGet>();
      Items = new SimplePriorityQueue<StoreItem, double>();
      WhenNewQueue = new List<Event>();
      WhenAnyQueue = new List<Event>();
      WhenFullQueue = new List<Event>();
      WhenEmptyQueue = new List<Event>();
      WhenChangeQueue = new List<Event>();
    }
    public PriorityStore(Simulation environment, IEnumerable<object> items, IEnumerable<double> priorities, int capacity = int.MaxValue) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      Environment = environment;
      Capacity = capacity;
      PutQueue = new Queue<StorePut>();
      GetQueue = new Queue<StoreGet>();
      Items = new SimplePriorityQueue<StoreItem, double>();
      var itemsList = items.ToList();
      foreach (var zip in itemsList.Zip(priorities, (a, b) => new { Item = a, Prio = b }))
        Items.Enqueue(new StoreItem() { AdmissionDate = environment.Now, Item = zip.Item }, zip.Prio);
      if (Items.Count != itemsList.Count) throw new ArgumentException("Fewer priorities than items are given.", "priorities");

      WhenNewQueue = new List<Event>();
      WhenAnyQueue = new List<Event>();
      WhenFullQueue = new List<Event>();
      WhenEmptyQueue = new List<Event>();
      WhenChangeQueue = new List<Event>();
      if (capacity < Items.Count) throw new ArgumentException("There are more initial items than there is capacity.", "items");
    }

    public virtual StorePut Put(object item, double priority = 1) {
      var put = new StorePut(Environment, TriggerGet, new PriorityItem(priority, item));
      PutQueue.Enqueue(put);
      TriggerPut();
      return put;
    }

    public virtual StoreGet Get() {
      var get = new StoreGet(Environment, TriggerPut);
      GetQueue.Enqueue(get);
      TriggerGet();
      return get;
    }

    public virtual Event WhenNew() {
      var whenNew = new Event(Environment);
      WhenNewQueue.Add(whenNew);
      return whenNew;
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

    protected virtual void DoPut(StorePut put) {
      if (Items.Count < Capacity) {
        var pi = (PriorityItem)put.Value;
        PutWaitingTime?.Add(Environment.ToDouble(Environment.Now - put.Time));
        Items.Enqueue(new StoreItem() { AdmissionDate = Environment.Now, Item = pi.Item}, pi.Priority);
        put.Succeed();
      }
    }

    protected virtual void DoGet(StoreGet get) {
      if (Items.Count > 0) {
        var item = Items.Dequeue();
        GetWaitingTime?.Add(Environment.ToDouble(Environment.Now - get.Time));
        LeadTime?.Add(Environment.ToDouble(Environment.Now - item.AdmissionDate));
        get.Succeed(item.Item);
      }
    }

    protected virtual void TriggerPut(Event @event = null) {
      while (PutQueue.Count > 0) {
        var put = PutQueue.Peek();
        DoPut(put);
        if (put.IsTriggered) {
          PutQueue.Dequeue();
          TriggerWhenNew();
          TriggerWhenAny();
          TriggerWhenFull();
          TriggerWhenChange();
        } else break;
      }
      Utilization?.UpdateTo(Count / (double)Capacity);
      WIP?.UpdateTo(Count + PutQueue.Count + GetQueue.Count);
      PutQueueLength?.UpdateTo(PutQueue.Count);
    }

    protected virtual void TriggerGet(Event @event = null) {
      while (GetQueue.Count > 0) {
        var get = GetQueue.Peek();
        DoGet(get);
        if (get.IsTriggered) {
          GetQueue.Dequeue();
          TriggerWhenEmpty();
          TriggerWhenChange();
        } else break;
      }
      Utilization?.UpdateTo(Count / (double)Capacity);
      WIP?.UpdateTo(Count + PutQueue.Count + GetQueue.Count);
      GetQueueLength?.UpdateTo(GetQueue.Count);
    }

    protected virtual void TriggerWhenNew() {
      if (WhenNewQueue.Count == 0) return;
      foreach (var evt in WhenNewQueue)
        evt.Succeed();
      WhenNewQueue.Clear();
    }

    protected virtual void TriggerWhenAny() {
      if (Items.Count > 0) {
        if (WhenAnyQueue.Count == 0) return;
        foreach (var evt in WhenAnyQueue)
          evt.Succeed();
        WhenAnyQueue.Clear();
      }
    }

    protected virtual void TriggerWhenFull() {
      if (Count == Capacity) {
        if (WhenFullQueue.Count == 0) return;
        foreach (var evt in WhenFullQueue)
          evt.Succeed();
        WhenFullQueue.Clear();
      }
    }

    protected virtual void TriggerWhenEmpty() {
      if (Count == 0) {
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

    protected struct PriorityItem {
      public double Priority { get; }
      public object Item { get; }

      public PriorityItem(double priority, object item) {
        Priority = priority;
        Item = item;
      }
    }
  }
}
