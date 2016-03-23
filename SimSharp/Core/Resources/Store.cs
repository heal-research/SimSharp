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
  public class Store {
    public int Count { get { return Items.Count; } }
    public int Capacity { get; protected set; }
    protected Environment Environment { get; private set; }

    protected Queue<StorePut> PutQueue { get; private set; }
    protected Queue<StoreGet> GetQueue { get; private set; }
    protected List<object> Items { get; private set; }

    public Store(Environment environment, int capacity = int.MaxValue) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      Environment = environment;
      Capacity = capacity;
      PutQueue = new Queue<StorePut>();
      GetQueue = new Queue<StoreGet>();
      Items = new List<object>();
    }

    public virtual StorePut Put(object item) {
      var put = new StorePut(Environment, TriggerGet, item);
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
  }
}
