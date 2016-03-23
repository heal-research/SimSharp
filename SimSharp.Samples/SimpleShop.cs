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
  class SimpleShop {
    static TimeSpan delay = TimeSpan.Zero;
    private static readonly TimeSpan MachineProcTimeMu = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan MachineProcTimeSigma = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PackerProcTimeMu = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PackerProcTimeSigma = TimeSpan.FromSeconds(2);

    static IEnumerable<Event> Machine(Environment env, Resource packer) {
      while (true) {
        yield return env.TimeoutNormalPositive(MachineProcTimeMu, MachineProcTimeSigma);
        var token = packer.Request();
        yield return token;
        delay += env.Now - token.Time;
        env.Process(Pack(env, packer, token));
      }
    }

    static IEnumerable<Event> Pack(Environment env, Resource packer, Request token) {
      yield return env.TimeoutNormalPositive(PackerProcTimeMu, PackerProcTimeSigma);
      packer.Release(token);
    }

    public void Simulate() {
      var env = new Environment(randomSeed: 41);
      var packer = new Resource(env, 1);
      env.Process(Machine(env, packer));
      env.Process(Machine(env, packer));
      env.Run(TimeSpan.FromHours(8));
      Console.WriteLine("The machines were delayed for {0}", delay);
    }
  }
}
