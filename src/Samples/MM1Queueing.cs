#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;

namespace SimSharp.Samples {
  public class MM1Queueing : ISimulate{
    private static readonly TimeSpan OrderArrivalTime = TimeSpan.FromMinutes(3.33);
    private static readonly TimeSpan ProcessingTime = TimeSpan.FromMinutes(2.5);
    
    private IEnumerable<Event> Source(Simulation env, Resource server) {
      while (true) {
        yield return env.TimeoutExponential(OrderArrivalTime);
        env.Process(Order(env, server));
      }
    }

    private IEnumerable<Event> Order(Simulation env, Resource server) {
      using (var req = server.Request()) {
        yield return req;
        yield return env.TimeoutExponential(ProcessingTime);
      }
    }

    private IEnumerable<Event> HandleWarmup(Simulation env, TimeSpan warmupTime, params IMonitor[] monitors) {
      foreach (var mon in monitors) mon.Active = false;
      yield return env.Timeout(warmupTime);
      foreach (var mon in monitors) mon.Active = true;
    }

    public void Simulate() {
      var repetitions = 5;
      var lambda = 1 / OrderArrivalTime.TotalDays;
      var mu = 1 / ProcessingTime.TotalDays;
      var rho = lambda / mu;
      var analyticWIP = rho / (1 - rho);
      var analyticLeadtime = 1 / (mu - lambda);
      var analyticWaitingtime = rho / (mu - lambda);

      var env = new Simulation(randomSeed: 1, defaultStep: TimeSpan.FromDays(1));
      var utilization = new TimeSeriesMonitor(env, name: "Utilization");
      var wip = new TimeSeriesMonitor(env, name: "WIP", collect: true);
      var leadtime = new SampleMonitor(name: "Lead time", collect: true);
      var waitingtime = new SampleMonitor(name: "Waiting time", collect: true);

      env.Log("Analytical results of this system:");
      env.Log("Time\tUtilization.Mean\tWIP.Mean\tLeadtime.Mean\tWaitingTime.Mean");
      env.Log("{4}\t{0}\t{1}\t{2}\t{3}", rho, analyticWIP, analyticLeadtime, analyticWaitingtime, double.PositiveInfinity);
      env.Log("");

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
        .SetFinalUpdate(withHeaders: false) // creates a summary of the means at the end
        .SetTimeAPI(useDApi: true)
        .Build();

      env.Log("Simulated results of this system (" + repetitions + " repetitions):");
      env.Log("");
      summary.WriteHeader(); // write the header just once

      for (var i = 0; i < repetitions; i++) {
        env.Reset(i + 1); // reset environment
        utilization.Reset(); // reset monitors
        wip.Reset();
        leadtime.Reset();
        waitingtime.Reset();
        var server = new Resource(env, capacity: 1) {
          Utilization = utilization,
          WIP = wip,
          LeadTime = leadtime,
          WaitingTime = waitingtime,
        };

        env.Process(Source(env, server));
        env.Process(HandleWarmup(env, TimeSpan.FromDays(32), utilization, wip, leadtime, waitingtime));
        env.Run(TimeSpan.FromDays(365));
      }

      env.Log("");
      env.Log("Detailed results from the last run:");
      env.Log("");
      env.Log(utilization.Summarize());
      env.Log(wip.Summarize(maxBins: 10, binWidth: 2));
      env.Log(leadtime.Summarize(maxBins: 10, binWidth: 5 / 1440.0));
      env.Log(waitingtime.Summarize(maxBins: 10, binWidth: 4 / 1440.0));  ;
    }
  }
}
