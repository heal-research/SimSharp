#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
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
