#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2018  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
using System.Linq;
using Xunit;

namespace SimSharp.Tests {

  public class EnvironmentTests {

    private static IEnumerable<Event> AProcess(Simulation env, List<string> log) {
      while (env.Now < new DateTime(1970, 1, 1, 0, 2, 0)) {
        log.Add(env.Now.ToString("mm"));
        yield return env.Timeout(TimeSpan.FromMinutes(1));
      }
    }

    [Fact]
    public void TestEventQueueEmpty() {
      /*The simulation should stop if there are no more events, that means, no
        more active process.*/
      var log = new List<string>();
      var env = new Simulation();
      env.Process(AProcess(env, log));
      env.Run(TimeSpan.FromMinutes(10));
      Assert.True(log.SequenceEqual(new[] { "00", "01" }));
    }

    [Fact]
    public void TestRunNegativeUntil() {
      /*Test passing a negative time to run.*/
      var env = new Simulation();
      var errorThrown = false;
      try {
        env.Run(new DateTime(1969, 12, 30));
      } catch (InvalidOperationException) {
        errorThrown = true;
      }
      Assert.True(errorThrown);
    }

    [Fact]
    public void TestRunResume() {
      /* Stopped simulation can be resumed. */
      var env = new Simulation();
      var events = new List<Event>() {
        env.Timeout(TimeSpan.FromMinutes(5)),
        env.Timeout(TimeSpan.FromMinutes(10)),
        env.Timeout(TimeSpan.FromMinutes(15)),
        env.Timeout(TimeSpan.FromMinutes(20))
      };

      Assert.Equal(new DateTime(1970, 1, 1), env.Now);
      Assert.Equal(new DateTime(1970, 1, 1, 0, 5, 0), env.Peek());
      Assert.DoesNotContain(events, x => x.IsProcessed);

      env.Run(TimeSpan.FromMinutes(10));
      Assert.Equal(new DateTime(1970, 1, 1, 0, 10, 0), env.Now);
      Assert.Equal(new DateTime(1970, 1, 1, 0, 10, 0), env.Peek());
      Assert.True(events[0].IsProcessed);
      Assert.False(events[1].IsProcessed);
      Assert.False(events[2].IsProcessed);

      env.Run(TimeSpan.FromMinutes(5));
      Assert.Equal(new DateTime(1970, 1, 1, 0, 15, 0), env.Now);
      Assert.Equal(new DateTime(1970, 1, 1, 0, 15, 0), env.Peek());
      Assert.True(events[0].IsProcessed);
      Assert.True(events[1].IsProcessed);
      Assert.False(events[2].IsProcessed);

      env.Run(TimeSpan.FromMinutes(1));
      Assert.Equal(new DateTime(1970, 1, 1, 0, 16, 0), env.Now);
      Assert.Equal(new DateTime(1970, 1, 1, 0, 20, 0), env.Peek());
      Assert.True(events[0].IsProcessed);
      Assert.True(events[1].IsProcessed);
      Assert.True(events[2].IsProcessed);

      env.Run();
      Assert.Equal(new DateTime(1970, 1, 1, 0, 20, 0), env.Now);
      Assert.Equal(DateTime.MaxValue, env.Peek());
    }

    [Fact]
    public void TestRunUntilValue() {
      /* Anything that can be converted to a float is a valid until value. */
      var env = new Simulation(new DateTime(2014, 1, 1));
      env.Run(new DateTime(2014, 3, 1));
      Assert.Equal(new DateTime(2014, 3, 1), env.Now);
    }

    [Fact]
    public void TestRunWithProcessedEvent() {
      var env = new Simulation();
      var timeout = new Timeout(env, env.ToTimeSpan(1), "spam");
      var val = env.Run(timeout);
      Assert.Equal(1, env.NowD);
      Assert.Equal("spam", val);
      val = env.Run(timeout);
      Assert.Equal(1, env.NowD);
      Assert.Equal("spam", val);
    }

    [Fact]
    public void TestRunWithUntriggeredEvent() {
      Assert.Throws<InvalidOperationException>(() => {
        var env = new Simulation();
        env.Run(new Event(env));
      });
    }

    [Fact]
    public void TestReproducibility() {
      var env = new Simulation(randomSeed: 42);
      var proc = env.Process(ReproducibleProcess(env));
      var env2 = new Simulation(randomSeed: 42);
      var proc2 = env2.Process(ReproducibleProcess(env2));
      env.Run(); env2.Run();
      Assert.Equal((double)proc.Value, (double)proc2.Value);
      Assert.Equal(5, env.ProcessedEvents); // initialize + 3 timeouts + process events
      Assert.Equal(5, env2.ProcessedEvents);
      env.Reset(randomSeed: 13);
      proc = env.Process(ReproducibleProcess(env));
      env2 = new Simulation(randomSeed: 13);
      proc2 = env2.Process(ReproducibleProcess(env2));
      env.Run(); env2.Run();
      Assert.Equal((double)proc.Value, (double)proc2.Value);
      Assert.Equal(5, env.ProcessedEvents);
      Assert.Equal(5, env2.ProcessedEvents);
      env.Reset(randomSeed: 17);
      proc = env.Process(ReproducibleProcess(env));
      env2.Reset(randomSeed: 17);
      proc2 = env2.Process(ReproducibleProcess(env2));
      env.Run(); env2.Run();
      Assert.Equal((double)proc.Value, (double)proc2.Value);
      Assert.Equal(5, env.ProcessedEvents);
      Assert.Equal(5, env2.ProcessedEvents);
    }

    private IEnumerable<Event> ReproducibleProcess(Simulation env) {
      var t1 = env.RandUniform(1, 3);
      yield return env.TimeoutD(t1);
      var t2 = env.RandNormal(3, 0.1);
      yield return env.TimeoutD(t2);
      var t3 = env.RandExponential(2);
      yield return env.TimeoutD(t3);
      env.ActiveProcess.Succeed(t1 + t2 + t3);
    }

    [Fact]
    public void EnvironmentBackwardsCompat() {
      // make sure it returns the same normal-distributed random numbers as the 3.0.11 Environment
#pragma warning disable CS0618 // Type or member is obsolete
      var env = new Environment(randomSeed: 10);
#pragma warning restore CS0618 // Type or member is obsolete
      var rndNumbers = Enumerable.Range(0, 10).Select(x => env.RandNormal(0, 1)).ToArray();
      var old = new[] { 1.439249790053017, 0.539700657754765, -0.35962836744484883,
0.37645276686883905, 0.037506631053281031, -0.92536789644140882, -0.87027850838312693,
0.65864875161591829, 0.46713487767696055, -0.37878389025311837};
      Assert.Equal(old, rndNumbers);
    }
  }
}
