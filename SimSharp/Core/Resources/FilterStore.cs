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
  public class FilterStore {
    public int Count { get { return Items.Count; } }
    public int Capacity { get; protected set; }
    protected Environment Environment { get; private set; }

    protected Queue<StorePut> PutQueue { get; private set; }
    protected LinkedList<FilterStoreGet> GetQueue { get; private set; }
    protected List<object> Items { get; private set; }

    public FilterStore(Environment environment, int capacity = int.MaxValue) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      Environment = environment;
      Capacity = capacity;
      PutQueue = new Queue<StorePut>();
      GetQueue = new LinkedList<FilterStoreGet>();
      Items = new List<object>();
    }
    public FilterStore(Environment environment, IEnumerable<object> items, int capacity = int.MaxValue) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      Environment = environment;
      Capacity = capacity;
      PutQueue = new Queue<StorePut>();
      GetQueue = new LinkedList<FilterStoreGet>();
      Items = new List<object>(items);
      if (capacity < Items.Count) throw new ArgumentException("There are more initial items than there is capacity.", "items");
    }

    public virtual bool IsAvailable(Func<object, bool> filter) {
      return Items.Any(filter);
    }

    public virtual StorePut Put(object item) {
      var put = new StorePut(Environment, TriggerGet, item);
      PutQueue.Enqueue(put);
      TriggerPut();
      return put;
    }

    public virtual FilterStoreGet Get(Func<object, bool> filter = null) {
      if (filter == null) filter = _ => true;
      var get = new FilterStoreGet(Environment, TriggerPut, filter);
      GetQueue.AddLast(get);
      TriggerGet();
      return get;
    }

    protected virtual void DoPut(StorePut put) {
      if (Items.Count < Capacity) {
        Items.Add(put.Value);
        put.Succeed();
      }
    }

    protected virtual void DoGet(FilterStoreGet get) {
      for (int i = 0; i < Items.Count; i++) {
        var item = Items[i];
        if (!get.Filter(item)) continue;
        Items.RemoveAt(i);
        get.Succeed(item);
        return;
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
      var current = GetQueue.First;
      while (current != null) {
        var get = current.Value;
        DoGet(get);
        if (get.IsTriggered) {
          var next = current.Next;
          GetQueue.Remove(current);
          current = next;
        } else current = current.Next;
        if (Items.Count == 0) break;
      }
    }
  }
}
