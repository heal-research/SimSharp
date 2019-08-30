#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2019  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
    private Simulation env;
    private Resource kanban;
    private Resource server;
    private TimeSeriesMonitor stockStat;
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
      stockStat.UpdateTo(kanban.Remaining);
      completedOrders++;
    }

    private IEnumerable<Event> Produce(Request kb) {
      using (var srv = server.Request()) {
        yield return srv;
        yield return env.TimeoutExponential(ProcessingTime);
        kanban.Release(kb);
        stockStat.UpdateTo(kanban.Remaining);
      }
    }

    public void Simulate(int rseed = 42) {
      completedOrders = 0;
      env = new Simulation(randomSeed: rseed);
      env.Log("== Kanban controlled production system ==");
      kanban = new Resource(env, capacity: 15);
      server = new Resource(env, capacity: 1);
      // In this sample stockStat is tracked manually in the process
      // it would also possible to track kanban's utilization and obtain
      // the stock as capacity * (1 - util.mean)
      stockStat = new TimeSeriesMonitor(env, name: "Kanbans in stock", collect: true);
      env.Process(Source());
      env.Run(TimeSpan.FromDays(180));
      Console.WriteLine("Kanbans in stock: {0} ; {1:F1}±{2:F1} ; {3} (Min;Mean±StdDev;Max) kanbans ", stockStat.Min, stockStat.Mean, stockStat.StdDev, stockStat.Max);
      Console.WriteLine("Produced kanbans: {0:N0}", completedOrders);
      Console.WriteLine(stockStat.Summarize(binWidth: 1));
    }
  }
}
