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
using System.Linq;

namespace SimSharp.Samples {
  public class MachineShop {
    /*
Machine shop example

Covers:

- Interrupts
- Resources: PreemptiveResource

Scenario:
  A workshop has *n* identical machines. A stream of jobs (enough to
  keep the machines busy) arrives. Each machine breaks down
  periodically. Repairs are carried out by one repairman. The repairman
  has other, less important tasks to perform, too. Broken machines
  preempt theses tasks. The repairman continues them when he is done
  with the machine repair. The workshop works continuously.
*/

    private const int RANDOM_SEED = 42;
    private const double PT_MEAN = 10.0; // Avg. processing time in minutes
    private const double PT_SIGMA = 2.0; // Sigma of processing time
    private const double MTTF = 300.0; // Mean time to failure in minutes
    private const double BREAK_MEAN = 1 / MTTF; // Param. for expovariate distribution
    private const double REPAIR_TIME = 30.0; // Time it takes to repair a machine in minutes
    private const double JOB_DURATION = 30.0; // Duration of other jobs in minutes
    private const int NUM_MACHINES = 10; // Number of machines in the machine shop
    private static readonly TimeSpan SIM_TIME = TimeSpan.FromDays(28); // Simulation time in minutes

    private Random random;

    public static double TimePerPart(Random random) {
      // Return actual processing time for a concrete part.
      return RandomDist.Normal(random, PT_MEAN, PT_SIGMA);
    }

    public static double TimeToFailure(Random random) {
      // Return time until next failure for a machine.
      return RandomDist.Exponential(random, BREAK_MEAN);
    }

    private class Machine : ActiveObject<Environment> {
      /*
        A machine produces parts and my get broken every now and then.
        If it breaks, it requests a *repairman* and continues the production
        after the it is repaired.

        A machine has a *name* and a numberof *parts_made* thus far.
      */

      public string name;
      public int parts_made;
      public bool broken;
      public Process process;
      private Random random;

      public Machine(Environment env, Random random, string name, PreemptiveResource repairman)
        : base(env) {
        this.random = random;
        this.name = name;
        this.parts_made = 0;
        this.broken = false;

        // Start "working" and "break_machine" processes for this machine.
        this.process = env.Process(working(repairman));
        env.Process(break_machine());
      }

      private IEnumerable<Event> working(PreemptiveResource repairman) {
        /*
          Produce parts as long as the simulation runs.

          While making a part, the machine may break multiple times.
          Request a repairman when this happens.
        */
        while (true) {
          // Start making a new part
          var doneIn = TimeSpan.FromMinutes(TimePerPart(random));
          while (doneIn > TimeSpan.Zero) {
            // Working on the part
            var start = Environment.Now;
            yield return Environment.Timeout(doneIn);
            if (Environment.ActiveProcess.HandleFault()) {
              broken = true;
              doneIn -= Environment.Now - start;
              // How much time left?
              // Request a repairman. This will preempt its "other_job".
              using (var req = repairman.Request(priority: 1, preempt: true)) {
                yield return req;
                yield return Environment.Timeout(TimeSpan.FromMinutes(REPAIR_TIME));
              }
              broken = false;
            } else {
              doneIn = TimeSpan.Zero; // Set to 0 to exit while loop.
            }
          }
          // Part is done.
          parts_made++;
        }
      }

      private IEnumerable<Event> break_machine() {
        // Break the machine every now and then.
        while (true) {
          yield return Environment.Timeout(TimeSpan.FromMinutes(TimeToFailure(random)));
          if (!broken) {
            // Only break the machine if it is currently working.
            process.Interrupt();
          }
        }
      }
    }

    private IEnumerable<Event> other_jobs(Environment env, PreemptiveResource repairman) {
      // The repairman's other (unimportant) job.
      while (true) {
        // Start a new job
        var done_in = TimeSpan.FromMinutes(JOB_DURATION);
        while (done_in > TimeSpan.Zero) {
          // Retry the job until it is done.
          // It's priority is lower than that of machine repairs.
          using (var req = repairman.Request(priority: 2)) {
            yield return req;
            var start = env.Now;
            yield return env.Timeout(done_in);
            if (env.ActiveProcess.HandleFault())
              done_in -= env.Now - start;
            else done_in = TimeSpan.Zero;
          }
        }
      }
    }

    public void Simulate(int rseed = RANDOM_SEED) {
      // Setup and start the simulation
      Console.Out.WriteLine("== Machine shop ==");
      random = new Random(rseed);
      // Create an environment and start the setup process
      var start = new DateTime(2014, 2, 1);
      var env = new Environment(start);
      var repairman = new PreemptiveResource(env, 1);
      var machines = Enumerable.Range(0, NUM_MACHINES).Select(x => new Machine(env, random, "Machine " + x, repairman)).ToArray();
      env.Process(other_jobs(env, repairman));

      var startPerf = DateTime.UtcNow;
      // Execute!
      env.Run(SIM_TIME);
      var perf = DateTime.UtcNow - startPerf;

      // Analyis/results
      Console.Out.WriteLine("Machine shop results after {0} days.", (env.Now - start).TotalDays);
      foreach (var machine in machines)
        Console.Out.WriteLine("{0} made {1} parts.", machine.name, machine.parts_made);
      Console.Out.WriteLine();
      Console.Out.WriteLine("Processed {0} events in {1} seconds ({2} events/s).", env.ProcessedEvents, perf.TotalSeconds, (env.ProcessedEvents / perf.TotalSeconds));
    }
  }
}
