#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;

namespace SimSharp.Samples {
  public class SteelFactory {
    /*
     * Steel Factory
     * 
     * Covers:
     *  - Passing and manually releasing a resource request
     *
     * Scenario:
     *  A steel factory has two continuous casters that produce slabs.
     *  They require a crante that transports the cast slabs, before
     *  they can start to produce again.
     */
    class Slab {
      public double CastTime { get; private set; }
      public Slab(double castTime) {
        CastTime = castTime;
      }
    }

    private IEnumerable<Event> Cast(Simulation env, Resource crane, string name, IEnumerable<Slab> castQueue) {
      foreach (var slab in castQueue) {
        yield return env.TimeoutD(slab.CastTime);
        env.Log("Caster {0} finished at {1}", name, env.Now);
        var token = crane.Request();
        yield return token;
        env.Process(Transport(env, crane, token, name));
      }
    }

    private IEnumerable<Event> Transport(Simulation env, Resource crane, Request token, string caster) {
      env.Log("Crane transporting from caster {0} at {1}", caster, env.Now);
      yield return env.TimeoutD(4);
      crane.Release(token);
    }

    public void Simulate() {
      var env = new Simulation(TimeSpan.FromMinutes(1));
      env.Log("== Steel Factory ==");
      var crane = new Resource(env, 1);
      env.Process(Cast(env, crane, "CC1", new[] { new Slab(4), new Slab(4), new Slab(8), new Slab(3), new Slab(2) }));
      env.Process(Cast(env, crane, "CC2", new[] { new Slab(2), new Slab(3), new Slab(3), new Slab(4), new Slab(3) }));
      env.Run(TimeSpan.FromMinutes(100));
    }
  }
}
