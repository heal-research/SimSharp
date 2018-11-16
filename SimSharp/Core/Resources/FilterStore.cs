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
  /// A filter store is similar to a <see cref="Store"/>.
  /// However, in Get it is possible to specify the property of the item wished to retrieve.
  /// 
  /// FilterStore holds a variable number of individual items.
  /// Put are always performed in FIFO order. 
  /// Get are performed in FIFO order only when they match at least one item in the store.
  /// </summary>
  public class FilterStore {
    protected static readonly Func<object, bool> TrueFunc = _ => true;

    public int Capacity { get; protected set; }

    public int Count { get { return Items.Count; } }

    protected Simulation Environment { get; private set; }

    protected Queue<StorePut> PutQueue { get; private set; }
    protected LinkedList<FilterStoreGet> GetQueue { get; private set; }
    protected List<object> Items { get; private set; }
    protected List<Event> WhenNewQueue { get; private set; }
    protected List<Event> WhenAnyQueue { get; private set; }
    protected List<Event> WhenFullQueue { get; private set; }
    protected List<Event> WhenEmptyQueue { get; private set; }
    protected List<Event> WhenChangeQueue { get; private set; }

    public FilterStore(Simulation environment, int capacity = int.MaxValue) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      Environment = environment;
      Capacity = capacity;
      PutQueue = new Queue<StorePut>();
      GetQueue = new LinkedList<FilterStoreGet>();
      Items = new List<object>();
      WhenNewQueue = new List<Event>();
      WhenAnyQueue = new List<Event>();
      WhenFullQueue = new List<Event>();
      WhenEmptyQueue = new List<Event>();
      WhenChangeQueue = new List<Event>();
    }
    public FilterStore(Simulation environment, IEnumerable<object> items, int capacity = int.MaxValue) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      Environment = environment;
      Capacity = capacity;
      PutQueue = new Queue<StorePut>();
      GetQueue = new LinkedList<FilterStoreGet>();
      Items = new List<object>(items);
      WhenNewQueue = new List<Event>();
      WhenAnyQueue = new List<Event>();
      WhenFullQueue = new List<Event>();
      WhenEmptyQueue = new List<Event>();
      WhenChangeQueue = new List<Event>();
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
      var get = new FilterStoreGet(Environment, TriggerPut, filter ?? TrueFunc);
      GetQueue.AddLast(get);
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
          TriggerWhenNew();
          TriggerWhenAny();
          TriggerWhenFull();
          TriggerWhenChange();
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
          TriggerWhenEmpty();
          TriggerWhenChange();
        } else current = current.Next;
        if (Items.Count == 0) break;
      }
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
  }
}
