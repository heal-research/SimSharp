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

namespace SimSharp.Performance {
  public class MachineShop {
    public static void Main(string[] args) {
      new MachineShop().Simulate();
    }

    /*
     * Machine shop example
     * 
     * Covers:
     *  - Interrupts
     *  - Resources: PreemptiveResource
     * 
     * Scenario:
     *  A workshop has *n* identical machines. A stream of jobs (enough to
     *  keep the machines busy) arrives. Each machine breaks down
     *  periodically. Repairs are carried out by one repairman. The repairman
     *  has other, less important tasks to perform, too. Broken machines
     *  preempt theses tasks. The repairman continues them when he is done
     *  with the machine repair. The workshop works continuously.
     */
    private const int RandomSeed = 42;
    private const double PtMean = 10.0; // Avg. processing time in minutes
    private const double PtSigma = 2.0; // Sigma of processing time
    private const double Mttf = 300.0; // Mean time to failure in minutes
    private const double RepairTime = 30.0; // Time it takes to repair a machine in minutes
    private const double JobDuration = 30.0; // Duration of other jobs in minutes
    private const int NumMachines = 10; // Number of machines in the machine shop
    private static readonly TimeSpan SimTime = TimeSpan.FromDays(3650); // Simulation time in minutes

    private class Machine : ActiveObject<Environment> {
      /*
       * A machine produces parts and my get broken every now and then.
       * If it breaks, it requests a *repairman* and continues the production
       * after the it is repaired.
       * 
       *  A machine has a *name* and a numberof *parts_made* thus far.
       */
      public string Name { get; private set; }
      public int PartsMade { get; private set; }
      public bool Broken { get; private set; }
      public Process Process { get; private set; }

      public Machine(Environment env, string name, PreemptiveResource repairman)
        : base(env) {
        Name = name;
        PartsMade = 0;
        Broken = false;

        // Start "working" and "break_machine" processes for this machine.
        Process = env.Process(Working(repairman));
        env.Process(BreakMachine());
      }

      private IEnumerable<Event> Working(PreemptiveResource repairman) {
        /*
         * Produce parts as long as the simulation runs.
         * 
         * While making a part, the machine may break multiple times.
         * Request a repairman when this happens.
         */
        while (true) {
          // Start making a new part
          var doneIn = TimeSpan.FromMinutes(Environment.RandNormal(PtMean, PtSigma));
          while (doneIn > TimeSpan.Zero) {
            // Working on the part
            var start = Environment.Now;
            yield return Environment.Timeout(doneIn);
            if (Environment.ActiveProcess.HandleFault()) {
              Broken = true;
              doneIn -= Environment.Now - start;
              // How much time left?
              // Request a repairman. This will preempt its "other_job".
              using (var req = repairman.Request(priority: 1, preempt: true)) {
                yield return req;
                yield return Environment.Timeout(TimeSpan.FromMinutes(RepairTime));
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
          yield return Environment.Timeout(TimeSpan.FromMinutes(Environment.RandExponential(Mttf)));
          if (!Broken) {
            // Only break the machine if it is currently working.
            Process.Interrupt();
          }
        }
      }
    }

    private IEnumerable<Event> OtherJobs(Environment env, PreemptiveResource repairman) {
      // The repairman's other (unimportant) job.
      while (true) {
        // Start a new job
        var doneIn = TimeSpan.FromMinutes(JobDuration);
        while (doneIn > TimeSpan.Zero) {
          // Retry the job until it is done.
          // It's priority is lower than that of machine repairs.
          using (var req = repairman.Request(priority: 2)) {
            yield return req;
            var start = env.Now;
            yield return env.Timeout(doneIn);
            if (env.ActiveProcess.HandleFault())
              doneIn -= env.Now - start;
            else doneIn = TimeSpan.Zero;
          }
        }
      }
    }

    public void Simulate(int rseed = RandomSeed) {
      // Setup and start the simulation
      // Create an environment and start the setup process
      var start = new DateTime(2014, 2, 1);
      var env = new Environment(start, rseed);
      env.Log("== Machine shop ==");
      var repairman = new PreemptiveResource(env, 1);
      var machines = Enumerable.Range(0, NumMachines).Select(x => new Machine(env, "Machine " + x, repairman)).ToArray();
      env.Process(OtherJobs(env, repairman));

      var startPerf = DateTime.UtcNow;
      // Execute!
      env.Run(SimTime);
      var perf = DateTime.UtcNow - startPerf;

      // Analyis/results
      env.Log("Machine shop results after {0} days.", (env.Now - start).TotalDays);
      foreach (var machine in machines)
        env.Log("{0} made {1} parts.", machine.Name, machine.PartsMade);
      env.Log(string.Empty);
      env.Log("Processed {0:#,###} events in {1:#.##} seconds ({2:#,###.##} events/s).", env.ProcessedEvents, perf.TotalSeconds, (env.ProcessedEvents / perf.TotalSeconds));
    }
  }
}
