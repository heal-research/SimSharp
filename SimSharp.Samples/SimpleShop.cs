using System;
using System.Collections.Generic;

namespace SimSharp.Samples {
  class SimpleShop {
    static TimeSpan delay = TimeSpan.Zero;

    static IEnumerable<Event> Machine(Environment env, Resource packer) {
      while (true) {
        var procTimeSec = env.RandNormalPositive(20, 5);
        yield return env.Timeout(TimeSpan.FromSeconds(procTimeSec));
        var token = packer.Request();
        yield return token;
        delay += env.Now - token.Time;
        env.Process(Pack(env, packer, token));
      }
    }

    static IEnumerable<Event> Pack(Environment env, Resource packer, Request token) {
      var packTimeSec = env.RandNormalPositive(10, 2);
      yield return env.Timeout(TimeSpan.FromSeconds(packTimeSec));
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
