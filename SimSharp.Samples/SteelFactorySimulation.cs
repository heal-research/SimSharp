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
  class SteelFactorySimulation {
    class MySlab {
      public TimeSpan CastTime { get; private set; }
      public MySlab(double castTimeInMinutes) {
        CastTime = TimeSpan.FromMinutes(castTimeInMinutes);
      }
    }

    private static IEnumerable<Event> Cast(Environment environment, Resource crane, string name, IEnumerable<MySlab> castQueue) {
      foreach (var slab in castQueue) {
        yield return environment.Timeout(slab.CastTime);
        Console.Out.WriteLine("Caster {0} finished at {1}", name, environment.Now);
        var token = crane.Request();
        yield return token;
        environment.Process(Transport(environment, crane, token, name));
      }
    }

    private static IEnumerable<Event> Transport(Environment environment, Resource crane, Request token, string caster) {
      Console.Out.WriteLine("Crane transporting from caster {0} at {1}", caster, environment.Now);
      yield return environment.Timeout(TimeSpan.FromMinutes(4));
      crane.Release(token);
    }

    static void Main(string[] args) {
      var env = new Environment();
      var crane = new Resource(env, 1);
      env.Process(Cast(env, crane, "CC1", new[] { new MySlab(4), new MySlab(4), new MySlab(8), new MySlab(3), new MySlab(2) }));
      env.Process(Cast(env, crane, "CC2", new[] { new MySlab(2), new MySlab(3), new MySlab(3), new MySlab(4), new MySlab(3) }));
      env.Run(TimeSpan.FromMinutes(100));
      Console.ReadLine();
    }
  }
}
