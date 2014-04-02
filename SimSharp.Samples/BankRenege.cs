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

namespace SimSharp.Samples {
  public class BankRenege {

    private const int NewCustomers = 5; // Total number of customers
    private const double IntervalCustomers = 10.0; // Generate new customers roughly every x minutes
    private const int MinPatience = 1; // Min. customer patience
    private const int MaxPatience = 3; // Max. customer patience

    private IEnumerable<Event> Source(Environment env, int number, double interval, Resource counter) {
      for (int i = 0; i < number; i++) {
        var c = Customer(env, "Customer " + i, counter, timeInBank: 12.0);
        env.Process(c);
        var t = RandomDist.Exponential(env.Random, 1.0 / interval);
        yield return env.Timeout(TimeSpan.FromMinutes(t));
      }
    }

    private IEnumerable<Event> Customer(Environment env, string name, Resource counter, double timeInBank) {
      var arrive = env.Now;

      env.Log("{0} {1}: Here I am", arrive, name);

      using (var req = counter.Request()) {
        var patience = RandomDist.Uniform(env.Random, MinPatience, MaxPatience);

        // Wait for the counter or abort at the end of our tether
        var timeout = env.Timeout(TimeSpan.FromMinutes(patience));
        yield return req | timeout;

        var wait = env.Now - arrive;

        if (req.IsProcessed) {
          // We got the counter
          env.Log("{0} {1}: waited {2}", env.Now, name, wait);

          var tib = RandomDist.Exponential(env.Random, 1.0 / timeInBank);
          yield return env.Timeout(TimeSpan.FromMinutes(tib));
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
      env.Process(Source(env, NewCustomers, IntervalCustomers, counter));
      env.Run();
    }
  }
}
