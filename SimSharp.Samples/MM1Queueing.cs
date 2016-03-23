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
  public class MM1Queueing {
    private Environment env;
    private Resource server;
    private ContinuousStatistics statistics;
    private static readonly TimeSpan OrderArrivalTime = TimeSpan.FromMinutes(3.33);
    private static readonly TimeSpan ProcessingTime = TimeSpan.FromMinutes(2.5);
    private int queueSize;

    private IEnumerable<Event> Source() {
      while (true) {
        yield return env.TimeoutExponential(OrderArrivalTime);
        env.Process(Order());
      }
    }

    private IEnumerable<Event> Order() {
      statistics.Update(++queueSize);
      var req = server.Request();
      yield return req;
      env.Process(Produce(req));
      statistics.Update(--queueSize);
    }

    private IEnumerable<Event> Produce(Request req) {
      yield return env.TimeoutExponential(ProcessingTime);
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
