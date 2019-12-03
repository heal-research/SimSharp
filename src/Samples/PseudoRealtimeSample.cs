#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimSharp.Samples {
  public class PseudoRealtimeSample {

    public IEnumerable<Event> Process(PseudoRealtimeSimulation sim) {
      var then = sim.Now;
      var sw = Stopwatch.StartNew();
      yield return sim.Timeout(TimeSpan.FromSeconds(1));
      sw.Stop();
      var now = sim.Now;
      Console.WriteLine($"Elapsed wall clock time {sw.Elapsed.TotalSeconds}s, elapsed simulation time {(now - then).TotalSeconds}s.");
    }

    public void Simulate(int rseed = 42) {
      // Setup and start the simulation
      var env = new PseudoRealtimeSimulation(rseed);
      env.Log("== Pseudo-Realtime Sample ==");

      env.Process(Process(env));

      env.Run(TimeSpan.FromSeconds(2));
    }
  }
}