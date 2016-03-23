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
  public class BankRenege {

    private const int NewCustomers = 10; // Total number of customers
    private static readonly TimeSpan IntervalCustomers = TimeSpan.FromMinutes(10.0); // Generate new customers roughly every x minutes
    private static readonly TimeSpan MinPatience = TimeSpan.FromMinutes(1); // Min. customer patience
    private static readonly TimeSpan MaxPatience = TimeSpan.FromMinutes(3); // Max. customer patience

    private IEnumerable<Event> Source(Environment env, Resource counter) {
      for (int i = 0; i < NewCustomers; i++) {
        var c = Customer(env, "Customer " + i, counter, TimeSpan.FromMinutes(12.0));
        env.Process(c);
        yield return env.TimeoutExponential(IntervalCustomers);
      }
    }

    private IEnumerable<Event> Customer(Environment env, string name, Resource counter, TimeSpan meanTimeInBank) {
      var arrive = env.Now;

      env.Log("{0} {1}: Here I am", arrive, name);

      using (var req = counter.Request()) {
        // Wait for the counter or abort at the end of our tether
        var timeout = env.TimeoutUniform(MinPatience, MaxPatience);
        yield return req | timeout;

        var wait = env.Now - arrive;

        if (req.IsProcessed) {
          // We got the counter
          env.Log("{0} {1}: waited {2}", env.Now, name, wait);

          yield return env.TimeoutExponential(meanTimeInBank);
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
      var env = new Environment(start, 41);
      env.Log("== Bank renege ==");
      var counter = new Resource(env, capacity: 1);
      env.Process(Source(env, counter));
      env.Run();
    }
  }
}
