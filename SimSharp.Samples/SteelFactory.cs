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

    private IEnumerable<Event> Cast(Environment env, Resource crane, string name, IEnumerable<Slab> castQueue) {
      foreach (var slab in castQueue) {
        yield return env.TimeoutD(slab.CastTime);
        env.Log("Caster {0} finished at {1}", name, env.Now);
        var token = crane.Request();
        yield return token;
        env.Process(Transport(env, crane, token, name));
      }
    }

    private IEnumerable<Event> Transport(Environment env, Resource crane, Request token, string caster) {
      env.Log("Crane transporting from caster {0} at {1}", caster, env.Now);
      yield return env.TimeoutD(4);
      crane.Release(token);
    }

    public void Simulate() {
      var env = new Environment(TimeSpan.FromMinutes(1));
      env.Log("== Steel Factory ==");
      var crane = new Resource(env, 1);
      env.Process(Cast(env, crane, "CC1", new[] { new Slab(4), new Slab(4), new Slab(8), new Slab(3), new Slab(2) }));
      env.Process(Cast(env, crane, "CC2", new[] { new Slab(2), new Slab(3), new Slab(3), new Slab(4), new Slab(3) }));
      env.Run(TimeSpan.FromMinutes(100));
    }
  }
}
