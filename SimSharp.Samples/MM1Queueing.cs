using System;
using System.Collections.Generic;

namespace SimSharp.Samples {
  public class MM1Queueing {
    private Environment env;
    private Resource server;
    private ContinuousStatistics statistics;
    private static readonly TimeSpan OrderArrivalTime = TimeSpan.FromMinutes(3.33);
    private static readonly TimeSpan ProcessingTime = TimeSpan.FromMinutes(2.5);
    private int queueSize;

    private IEnumerable<Event> Source() {
      while (true) {
        yield return env.Timeout(env.RandExponential(OrderArrivalTime));
        env.Process(Order());
      }
    }

    private IEnumerable<Event> Order() {
      statistics.Add(++queueSize);
      var req = server.Request();
      yield return req;
      env.Process(Produce(req));
      statistics.Add(--queueSize);
    }

    private IEnumerable<Event> Produce(Request req) {
      yield return env.Timeout(env.RandExponential(ProcessingTime));
      server.Release(req);
    }

    public void Simulate() {
      queueSize = 0;
      env = new Environment();
      server = new Resource(env, capacity: 1);
      statistics = new ContinuousStatistics(env);
      env.Log("== m/m/1 queuing system ==");
      env.Process(Source());
      env.Run(TimeSpan.FromDays(180));
      Console.WriteLine("QueueSize Statistics:");
      Console.WriteLine("Min: {0}; Max: {1}; Mean: {2:F2}; StdDev: {3:F2}", statistics.Min, statistics.Max, statistics.Mean, statistics.StdDev);
    }
  }
}
