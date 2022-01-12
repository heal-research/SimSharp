﻿#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using static SimSharp.Distributions;

namespace SimSharp.Samples {
  public class BankRenege {

    private const int NewCustomers = 10; // Total number of customers
    private static readonly IDistribution<TimeSpan> Arrival = EXP(TimeSpan.FromMinutes(10.0)); // Generate new customers roughly every x minutes
    private static readonly IDistribution<TimeSpan> Patience = UNIF(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(3)); // Customer patience

    private IEnumerable<Event> Source(Simulation env, Resource counter) {
      for (int i = 0; i < NewCustomers; i++) {
        var c = Customer(env, "Customer " + i, counter, EXP(TimeSpan.FromMinutes(12.0)));
        env.Process(c);
        yield return env.Timeout(Arrival);
      }
    }

    private IEnumerable<Event> Customer(Simulation env, string name, Resource counter, IDistribution<TimeSpan> meanTimeInBank) {
      var arrive = env.Now;

      env.Log("{0} {1}: Here I am", arrive, name);

      using (var req = counter.Request()) {
        // Wait for the counter or abort at the end of our tether
        var timeout = env.Timeout(Patience);
        yield return req | timeout;

        var wait = env.Now - arrive;

        if (req.IsProcessed) {
          // We got the counter
          env.Log("{0} {1}: waited {2}", env.Now, name, wait);

          yield return env.Timeout(meanTimeInBank);
          env.Log("{0} {1}: Finished", env.Now, name);
        } else {
          // We reneged
          env.Log("{0} {1}: RENEGED after {2}", env.Now, name, wait);
        }
      }
    }

    public void Simulate(int rseed = 41) {
      // Setup and start the simulation
      var start = new DateTime(2014, 2, 1);
      // Create an environment and start the setup process
      var env = new Simulation(start, rseed, defaultStep: TimeSpan.FromMinutes(1));
      env.Log("== Bank renege ==");
      var counter = new Resource(env, capacity: 1) {
        BreakOffTime = new SampleMonitor(name: "BreakOffTime", collect: true),
        Utilization = new TimeSeriesMonitor(env, name: "Utilization"),
        WaitingTime = new SampleMonitor(name: "Waiting time", collect: true),
        QueueLength = new TimeSeriesMonitor(env, name: "Queue Length", collect: true),
      };
      env.Process(Source(env, counter));
      env.Run();
      env.Log(counter.BreakOffTime.Summarize());
      env.Log(counter.Utilization.Summarize());
      env.Log(counter.WaitingTime.Summarize());
      env.Log(counter.QueueLength.Summarize());
    }
  }
}
