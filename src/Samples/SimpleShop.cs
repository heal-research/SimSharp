#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
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

    static IEnumerable<Event> Machine(Simulation env, Resource packer) {
      while (true) {
        yield return env.TimeoutNormalPositive(MachineProcTimeMu, MachineProcTimeSigma);
        var token = packer.Request();
        yield return token;
        delay += env.Now - token.Time;
        env.Process(Pack(env, packer, token));
      }
    }

    static IEnumerable<Event> Pack(Simulation env, Resource packer, Request token) {
      yield return env.TimeoutNormalPositive(PackerProcTimeMu, PackerProcTimeSigma);
      packer.Release(token);
    }

    public void Simulate() {
      var env = new Simulation(randomSeed: 41);
      var packer = new Resource(env, 1);
      env.Process(Machine(env, packer));
      env.Process(Machine(env, packer));
      env.Run(TimeSpan.FromHours(8));
      Console.WriteLine("The machines were delayed for {0}", delay);
    }
  }
}
