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

namespace SimSharp.Samples {
  public class MachineShopSpecialist {
    /*
     * Machine shop specialist example
     * 
     * Covers:
     *  - Resources: ResourcePool
     * 
     * Scenario:
     *  A variant of the machine shop showing how to use individual
     *  resources in the ResourcePool rather than treating them as
     *  a capacity. A workshop has *n* machines. A stream of jobs
     *  (enough to  keep the machines busy) arrives. Each machine
     *  breaks down periodically. Repairs are carried out by two
     *  repairman: Jack and John that vary in their efficiency with
     *  different machine brands. The workshop works continuously.
     */
    private const int RandomSeed = 42;
    private const int NumMachines = 10; // Number of machines in the machine shop
    private static readonly TimeSpan PtMean = TimeSpan.FromMinutes(10.0); // Avg. processing time in minutes
    private static readonly TimeSpan PtSigma = TimeSpan.FromMinutes(2.0); // Sigma of processing time
    private static readonly TimeSpan Mttf = TimeSpan.FromMinutes(300.0); // Mean time to failure in minutes
    private static readonly TimeSpan SimTime = TimeSpan.FromDays(28); // Simulation time in minutes
    private enum MachineBrands { BigBrand = 0, NiceBrand = 1, OldBrand = 2 }

    private static double RepairTime(MachineBrands brand, object repairman) {
      switch (brand) {
        case MachineBrands.BigBrand:
          return repairman == Jack ? 35.0 : 45.0;
        case MachineBrands.NiceBrand:
          return repairman == Jack ? 40.0 : 30.0;
        case MachineBrands.OldBrand:
          return repairman == Jack ? 35.0 : 60.0;
        default: throw new Exception("Unknown brand.");
      }
    }

    private static readonly object Jack = new object();
    private static readonly object John = new object();

    private class Machine : ActiveObject<Environment> {
      /*
       * A machine produces parts and my get broken every now and then.
       * If it breaks, it requests a *repairman* and continues the production
       * after it is repaired.
       */
      public string Name { get; private set; }
      public MachineBrands Brand { get; private set; }
      public int PartsMade { get; private set; }
      public bool Broken { get; private set; }
      public Process Process { get; private set; }

      public Machine(Environment env, string name, MachineBrands brand, ResourcePool repairman)
        : base(env) {
        Brand = brand;
        Name = name;
        PartsMade = 0;
        Broken = false;

        // Start "working" and "break_machine" processes for this machine.
        Process = env.Process(Working(repairman));
        env.Process(BreakMachine());
      }

      private IEnumerable<Event> Working(ResourcePool repairman) {
        /*
         * Produce parts as long as the simulation runs.
         * 
         * While making a part, the machine may break multiple times.
         * Request a repairman when this happens.
         */
        while (true) {
          // Start making a new part
          var doneIn = Environment.RandNormal(PtMean, PtSigma);
          while (doneIn > TimeSpan.Zero) {
            // Working on the part
            var start = Environment.Now;
            yield return Environment.Timeout(doneIn);
            if (Environment.ActiveProcess.HandleFault()) {
              Broken = true;
              doneIn -= Environment.Now - start;
              // How much time left?
              using (var req = repairman.Request()) {
                yield return req;
                var repairTime = RepairTime(Brand, req);
                //Environment.Log((req.Value == Jack ? "Jack" : "John") + " is working on " + Name + " for " + repairTime + " minutes.");
                yield return Environment.Timeout(TimeSpan.FromMinutes(repairTime));
              }
              Broken = false;
            } else {
              doneIn = TimeSpan.Zero; // Set to 0 to exit while loop.
            }
          }
          // Part is done.
          PartsMade++;
        }
      }

      private IEnumerable<Event> BreakMachine() {
        // Break the machine every now and then.
        while (true) {
          yield return Environment.TimeoutExponential(Mttf);
          if (!Broken) {
            // Only break the machine if it is currently working.
            Process.Interrupt();
          }
        }
      }
    }

    public void Simulate(int rseed = RandomSeed) {
      // Setup and start the simulation
      // Create an environment and start the setup process
      var start = new DateTime(2014, 2, 1);
      var env = new Environment(start, rseed);
      env.Log("== Machine shop specialist ==");
      var repairman = new ResourcePool(env, new[] { Jack, John });
      var machines = Enumerable.Range(0, NumMachines).Select(x => new Machine(env, "Machine " + x, (MachineBrands)(x % Enum.GetValues(typeof(MachineBrands)).Length), repairman)).ToArray();

      // Execute!
      env.Run(SimTime);

      // Analyis/results
      env.Log("Machine shop results after {0} days.", (env.Now - start).TotalDays);
      foreach (var machine in machines)
        env.Log("{0} made {1} parts.", machine.Name, machine.PartsMade);
    }
  }
}
