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
        var procTime = env.RandNormalPositive(MachineProcTimeMu, MachineProcTimeSigma);
        yield return env.Timeout(procTime);
        var token = packer.Request();
        yield return token;
        delay += env.Now - token.Time;
        env.Process(Pack(env, packer, token));
      }
    }

    static IEnumerable<Event> Pack(Environment env, Resource packer, Request token) {
      var packTimeSec = env.RandNormalPositive(PackerProcTimeMu, PackerProcTimeSigma);
      yield return env.Timeout(packTimeSec);
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
