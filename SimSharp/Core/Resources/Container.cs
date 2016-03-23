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
  public class Container {
    public double Level { get; protected set; }
    public double Capacity { get; protected set; }
    protected Environment Environment { get; private set; }

    protected Queue<ContainerPut> PutQueue { get; private set; }
    protected Queue<ContainerGet> GetQueue { get; private set; }

    public Container(Environment environment, double capacity = double.MaxValue, double initial = 0) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      if (initial < 0) throw new ArgumentException("Initial must be >= 0", "initial");
      if (initial > capacity) throw new ArgumentException("Initial must be <= capacity", "initial");
      Environment = environment;
      Capacity = capacity;
      Level = initial;
      PutQueue = new Queue<ContainerPut>();
      GetQueue = new Queue<ContainerGet>();
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
