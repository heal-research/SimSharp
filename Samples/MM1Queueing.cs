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
  public class MM1Queueing {
    private Simulation env;
    private Resource server;
    private ContinuousStatistics wip, utilization;
    private DiscreteStatistics waitingtime, leadtime;
    private static readonly TimeSpan OrderArrivalTime = TimeSpan.FromMinutes(3.33);
    private static readonly TimeSpan ProcessingTime = TimeSpan.FromMinutes(2.5);
    
    private IEnumerable<Event> Source() {
      while (true) {
        yield return env.TimeoutExponential(OrderArrivalTime);
        env.Process(Order());
      }
    }

    private IEnumerable<Event> Order() {
      var start = env.Now;
      wip.Increase();
      var req = server.Request();
      yield return req;
      utilization.UpdateTo(server.InUse / (double)server.Capacity);
      waitingtime.Add((env.Now - start).TotalMinutes);
      yield return env.Process(Produce(req));
      wip.Decrease();
      leadtime.Add((env.Now - start).TotalMinutes);
    }

    private IEnumerable<Event> Produce(Request req) {
      yield return env.TimeoutExponential(ProcessingTime);
      yield return server.Release(req);
      utilization.UpdateTo(server.InUse / (double)server.Capacity);
    }

    public void Simulate() {
      var lambda = 1 / OrderArrivalTime.TotalMinutes;
      var mu = 1 / ProcessingTime.TotalMinutes;
      var rho = lambda / mu;
      var analyticWIP = rho / (1 - rho);
      var analyticLeadtime = 1 / (mu - lambda);
      var analyticWaitingtime = rho / (mu - lambda);

      env = new Simulation(randomSeed: 1);
      server = new Resource(env, capacity: 1);
      wip = new ContinuousStatistics(env);
      utilization = new ContinuousStatistics(env);
      leadtime = new DiscreteStatistics();
      waitingtime = new DiscreteStatistics();

      env.Log("Analytical results of this system:");
      env.Log("\tUtilization.Mean\tWIP.Mean\tLeadtime.Mean\tWaitingTime.Mean");
      env.Log("\t{0}\t{1}\t{2}\t{3}", rho, analyticWIP, analyticLeadtime, analyticWaitingtime);

      // example to create a running report of these measures every simulated week
      //var report = Report.CreateBuilder(env)
      //  .Add("Utilization", utilization, Report.Measures.Mean | Report.Measures.StdDev)
      //  .Add("WIP", wip, Report.Measures.Min | Report.Measures.Mean | Report.Measures.Max)
      //  .Add("Leadtime", leadtime, Report.Measures.Min | Report.Measures.Mean | Report.Measures.Max)
      //  .Add("WaitingTime", waitingtime, Report.Measures.Min | Report.Measures.Mean | Report.Measures.Max)
      //  .SetOutput(env.Logger) // use a "new StreamWriter("report.csv")" to direct to a file
      //  .SetSeparator("\t")
      //  .SetPeriodicUpdate(TimeSpan.FromDays(7), withHeaders: true)
      //  .Build();

      var summary = Report.CreateBuilder(env)
        .Add("Utilization", utilization, Report.Measures.Mean)
        .Add("WIP", wip, Report.Measures.Mean)
        .Add("Leadtime", leadtime, Report.Measures.Mean)
        .Add("WaitingTime", waitingtime, Report.Measures.Mean)
        .SetOutput(env.Logger)
        .SetSeparator("\t")
        .SetFinalUpdate(withHeaders: true) // creates a summary of the means at the end
        .Build();

      env.Log("== m/m/1 queuing system (run 1) ==");
      env.Process(Source());
      env.Run(TimeSpan.FromDays(365));
      
      env.Reset(2); // reset environment
      server = new Resource(env, capacity: 1); // reset resources
      wip.Reset(); // reset statistics
      utilization.Reset();
      leadtime.Reset();

      env.Log("== m/m/1 queuing system (run 2) ==");
      env.Process(Source());
      env.Run(TimeSpan.FromDays(365));
    }
  }
}
