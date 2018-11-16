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
  /// A container holds a variable amount of a single continuous entity, e.g. water, coal, grain, etc.
  /// 
  /// Put and Get are in FIFO order only when they can be satisfied.
  /// Any put or get that can be satisfied takes precedence.
  /// Put events that attempt to add more to the Container than there is capacity for and
  /// Get events that remove more than there is are backlogged.
  /// </summary>
  public class Container {

    public double Capacity { get; protected set; }

    public double Level { get; protected set; }

    protected Simulation Environment { get; private set; }

    protected Queue<ContainerPut> PutQueue { get; private set; }
    protected Queue<ContainerGet> GetQueue { get; private set; }
    protected SimplePriorityQueue<Event, double> WhenAtLeastQueue { get; private set; }
    protected SimplePriorityQueue<Event, double> WhenAtMostQueue { get; private set; }
    protected List<Event> WhenChangeQueue { get; private set; }

    public Container(Simulation environment, double capacity = double.MaxValue, double initial = 0) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      if (initial < 0) throw new ArgumentException("Initial must be >= 0", "initial");
      if (initial > capacity) throw new ArgumentException("Initial must be <= capacity", "initial");
      Environment = environment;
      Capacity = capacity;
      Level = initial;
      PutQueue = new Queue<ContainerPut>();
      GetQueue = new Queue<ContainerGet>();
      WhenAtLeastQueue = new SimplePriorityQueue<Event, double>();
      WhenAtMostQueue = new SimplePriorityQueue<Event, double>(new ReverseComparer<double>());
      WhenChangeQueue = new List<Event>();
    }

    public virtual ContainerPut Put(double amount) {
      if (amount > Capacity) throw new ArgumentException("Cannot put more than capacity", "amount");
      var put = new ContainerPut(Environment, TriggerGet, amount);
      PutQueue.Enqueue(put);
      TriggerPut();
      return put;
    }

    public virtual ContainerGet Get(double amount) {
      if (amount > Capacity) throw new ArgumentException("Cannot get more than capacity", "amount");
      var get = new ContainerGet(Environment, TriggerPut, amount);
      GetQueue.Enqueue(get);
      TriggerGet();
      return get;
    }

    public virtual Event WhenAtLeast(double level) {
      var whenAtLeast = new Event(Environment);
      WhenAtLeastQueue.Enqueue(whenAtLeast, level);
      TriggerWhenAtLeast();
      return whenAtLeast;
    }

    public virtual Event WhenFull() {
      return WhenAtLeast(Capacity);
    }

    public virtual Event WhenAtMost(double level) {
      var whenAtMost = new Event(Environment);
      WhenAtMostQueue.Enqueue(whenAtMost, level);
      TriggerWhenAtMost();
      return whenAtMost;
    }

    public virtual Event WhenEmpty() {
      return WhenAtMost(0);
    }

    public virtual Event WhenChange() {
      var whenChange = new Event(Environment);
      WhenChangeQueue.Add(whenChange);
      return whenChange;
    }

    protected virtual void DoPut(ContainerPut put) {
      if (Capacity - Level >= put.Amount) {
        Level += put.Amount;
        put.Succeed();
      }
    }

    protected virtual void DoGet(ContainerGet get) {
      if (Level >= get.Amount) {
        Level -= get.Amount;
        get.Succeed();
      }
    }

    protected virtual void TriggerPut(Event @event = null) {
      while (PutQueue.Count > 0) {
        var put = PutQueue.Peek();
        DoPut(put);
        if (put.IsTriggered) {
          PutQueue.Dequeue();
          TriggerWhenAtLeast();
          TriggerWhenChange();
        } else break;
      }
    }

    protected virtual void TriggerGet(Event @event = null) {
      while (GetQueue.Count > 0) {
        var get = GetQueue.Peek();
        DoGet(get);
        if (get.IsTriggered) {
          GetQueue.Dequeue();
          TriggerWhenAtMost();
          TriggerWhenChange();
        } else break;
      }
    }

    protected virtual void TriggerWhenAtLeast() {
      while (WhenAtLeastQueue.Count > 0 && Level >= WhenAtLeastQueue.Peek) {
        var whenAtLeast = WhenAtLeastQueue.Dequeue();
        whenAtLeast.Succeed();
      }
    }

    protected virtual void TriggerWhenAtMost() {
      while (WhenAtMostQueue.Count > 0 && Level <= WhenAtMostQueue.Peek) {
        var whenAtMost = WhenAtMostQueue.Dequeue();
        whenAtMost.Succeed();
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
