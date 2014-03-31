#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2014  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
  public class Container {
    public double Capacity { get; protected set; }
    protected Environment Environment { get; private set; }
    public double Level { get; protected set; }

    protected List<ContainerPut> PutQueue { get; private set; }
    protected List<ContainerGet> GetQueue { get; private set; }

    public Container(Environment environment, double capacity = double.MaxValue, double initial = 0) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0", "capacity");
      if (initial < 0) throw new ArgumentException("Initial must be >= 0", "initial");
      if (initial > capacity) throw new ArgumentException("Initial must be <= capacity", "initial");
      Environment = environment;
      Capacity = capacity;
      Level = initial;
      PutQueue = new List<ContainerPut>();
      GetQueue = new List<ContainerGet>();
    }

    public virtual ContainerPut Put(double amount) {
      if (amount > Capacity) throw new ArgumentException("Cannot put more than capacity", "amount");
      var put = new ContainerPut(Environment, TriggerGet, amount);
      PutQueue.Add(put);
      DoPut(put);
      return put;
    }

    public virtual ContainerGet Get(double amount) {
      if (amount > Capacity) throw new ArgumentException("Cannot get more than capacity", "amount");
      var get = new ContainerGet(Environment, TriggerPut, amount);
      GetQueue.Add(get);
      DoGet(get);
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

    protected virtual void TriggerPut(Event @event) {
      GetQueue.Remove((ContainerGet)@event);
      foreach (var requestEvent in PutQueue) {
        if (!requestEvent.IsScheduled) DoPut(requestEvent);
        if (!requestEvent.IsScheduled) break;
      }
    }

    protected virtual void TriggerGet(Event @event) {
      PutQueue.Remove((ContainerPut)@event);
      foreach (var releaseEvent in GetQueue) {
        if (!releaseEvent.IsScheduled) DoGet(releaseEvent);
        if (!releaseEvent.IsScheduled) break;
      }
    }
  }
}
