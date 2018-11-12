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

    protected Environment Environment { get; private set; }

    protected Queue<StorePut> PutQueue { get; private set; }
    protected Queue<StoreGet> GetQueue { get; private set; }
    protected SimplePriorityQueue<object, double> Items { get; private set; }

    public PriorityStore(Environment environment, int capacity = int.MaxValue) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      Environment = environment;
      Capacity = capacity;
      PutQueue = new Queue<StorePut>();
      GetQueue = new Queue<StoreGet>();
      Items = new SimplePriorityQueue<object, double>();
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

    protected virtual void DoPut(StorePut put) {
      if (Items.Count < Capacity) {
        var pi = (PriorityItem)put.Value;
        Items.Enqueue(pi.Item, pi.Priority);
        put.Succeed();
      }
    }

    protected virtual void DoGet(StoreGet get) {
      if (Items.Count > 0) {
        var item = Items.Dequeue();
        get.Succeed(item);
      }
    }

    protected virtual void TriggerPut(Event @event = null) {
      while (PutQueue.Count > 0) {
        var put = PutQueue.Peek();
        DoPut(put);
        if (put.IsTriggered) {
          PutQueue.Dequeue();
        } else break;
      }
    }

    protected virtual void TriggerGet(Event @event = null) {
      while (GetQueue.Count > 0) {
        var get = GetQueue.Peek();
        DoGet(get);
        if (get.IsTriggered) {
          GetQueue.Dequeue();
        } else break;
      }
    }

    protected class PriorityItem {
      public double Priority { get; protected set; }
      public object Item { get; protected set; }

      public PriorityItem(double priority, object item) {
        Priority = priority;
        Item = item;
      }
    }
  }
}
