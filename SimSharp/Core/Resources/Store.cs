using System;
using System.Collections.Generic;
using System.Linq;

namespace SimSharp {
  public class Store {
    public int Capacity { get; protected set; }
    protected Environment Environment { get; private set; }

    protected List<StorePut> PutQueue { get; private set; }
    protected List<StoreGet> GetQueue { get; private set; }
    protected List<object> Items { get; private set; }

    public Store(Environment environment, int capacity = int.MaxValue) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      Environment = environment;
      Capacity = capacity;
      PutQueue = new List<StorePut>();
      GetQueue = new List<StoreGet>();
      Items = new List<object>();
    }

    public virtual StorePut Put(object item) {
      var put = new StorePut(Environment, TriggerGet, item);
      PutQueue.Add(put);
      DoPut(put);
      return put;
    }

    public virtual StoreGet Get() {
      var get = new StoreGet(Environment, TriggerPut);
      GetQueue.Add(get);
      DoGet(get);
      return get;
    }

    protected virtual void DoPut(StorePut put) {
      if (Items.Count < Capacity) {
        Items.Add(put.Value);
        put.Succeed();
      }
    }

    protected virtual void DoGet(StoreGet get) {
      if (Items.Count > 0) {
        var item = Items.First();
        Items.RemoveAt(0);
        get.Succeed(item);
      }
    }

    protected virtual void TriggerPut(Event @event) {
      GetQueue.Remove((StoreGet)@event);
      foreach (var requestEvent in PutQueue) {
        if (!requestEvent.IsScheduled) DoPut(requestEvent);
        if (!requestEvent.IsScheduled) break;
      }
    }

    protected virtual void TriggerGet(Event @event) {
      PutQueue.Remove((StorePut)@event);
      foreach (var releaseEvent in GetQueue) {
        if (!releaseEvent.IsScheduled) DoGet(releaseEvent);
        if (!releaseEvent.IsScheduled) break;
      }
    }
  }
}
