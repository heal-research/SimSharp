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

namespace SimSharp.Samples {
  public class KanbanControl {
    private Environment env;
    private Resource kanban;
    private Resource server;
    private ContinuousStatistics stockStat;
    private static readonly TimeSpan OrderArrivalTime = TimeSpan.FromMinutes(3.33);
    private static readonly TimeSpan ProcessingTime = TimeSpan.FromMinutes(2.5);
    private int completedOrders;

    private IEnumerable<Event> Source() {
      while (true) {
        yield return env.TimeoutExponential(OrderArrivalTime);
        env.Process(Order());
      }
    }

    private IEnumerable<Event> Order() {
      var kb = kanban.Request();
      yield return kb;
      env.Process(Produce(kb));
      stockStat.Update(kanban.Remaining);
      completedOrders++;
    }

    private IEnumerable<Event> Produce(Request kb) {
      using (var srv = server.Request()) {
        yield return srv;
        yield return env.TimeoutExponential(ProcessingTime);
        kanban.Release(kb);
        stockStat.Update(kanban.Remaining);
      }
    }

    public void Simulate() {
      completedOrders = 0;
      env = new Environment();
      env.Log("== Kanban controlled production system ==");
      kanban = new Resource(env, capacity: 15);
      server = new Resource(env, capacity: 1);
      stockStat = new ContinuousStatistics(env);
      env.Process(Source());
      env.Run(TimeSpan.FromDays(180));
      Console.WriteLine("Stock: {0} ; {1:F3}±{2:F3} ; {3} (Min;Mean±StdDev;Max) kanbans ", stockStat.Min, stockStat.Mean, stockStat.StdDev, stockStat.Max);
      Console.WriteLine("Produced kanbans: {0:N0}", completedOrders);
    }
  }
}
