#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using Xunit;

namespace SimSharp.Tests {

  public class TimeoutTest {
    [Fact]
    public void TestDiscreteTimeSteps() {
      var start = new DateTime(2014, 4, 1);
      var env = new Simulation(start);
      var log = new List<DateTime>();
      env.Process(TestDiscreteTimeStepsProc(env, log));
      env.Run(TimeSpan.FromSeconds(3));

      Assert.Equal(3, log.Count);
      for (int i = 0; i < 3; i++)
        Assert.Contains(start + TimeSpan.FromSeconds(i), log);
      Assert.Equal(3, env.ProcessedEvents);
    }

    private IEnumerable<Event> TestDiscreteTimeStepsProc(Simulation env, List<DateTime> log) {
      while (true) {
        log.Add(env.Now);
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    [Fact]
    public void TestNegativeTimeout() {
      var env = new Simulation();
      env.Process(TestNegativeTimeoutProc(env));
      Assert.Throws<ArgumentException>(() => env.Run());
    }

    private IEnumerable<Event> TestNegativeTimeoutProc(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(-1));
    }

    [Fact]
    public void TestSharedTimeout() {
      var start = new DateTime(2014, 4, 1);
      var env = new Simulation(start);
      var timeout = env.Timeout(TimeSpan.FromSeconds(1));
      var log = new Dictionary<int, DateTime>();
      for (int i = 0; i < 3; i++)
        env.Process(TestSharedTimeoutProc(env, timeout, i, log));
      env.Run();

      Assert.Equal(3, log.Count);
      foreach (var l in log.Values)
        Assert.Equal(start + TimeSpan.FromSeconds(1), l);
    }

    private IEnumerable<Event> TestSharedTimeoutProc(Simulation env, Timeout timeout, int id, Dictionary<int, DateTime> log) {
      yield return timeout;
      log.Add(id, env.Now);
    }

    [Fact]
    public void TestTriggeredTimeout() {
      var env = new Simulation();
      env.Process(TestTriggeredTimeoutProc(env));
      env.Run();
      Assert.Equal(2, env.NowD);
    }
    private IEnumerable<Event> TestTriggeredTimeoutProc(Simulation env) {
      var @event = env.Timeout(TimeSpan.FromSeconds(1));
      // Start the child after the timeout already happened
      yield return env.Timeout(TimeSpan.FromSeconds(2));
      yield return env.Process(TestTriggeredTimeoutChild(env, @event));
    }
    private IEnumerable<Event> TestTriggeredTimeoutChild(Simulation env, Event @event) {
      yield return @event;
    }

    [Fact]
    public void TestPrioritizedTimeouts() {
      var env = new Simulation(defaultStep: TimeSpan.FromMinutes(1));
      var procLowest = env.Process(PrioritizedTimeoutProc(env, 0));
      var procHighest = env.Process(PrioritizedTimeoutProc(env, -2));
      var procMiddle = env.Process(PrioritizedTimeoutProcs(env, procLowest, procHighest));
      procMiddle.AddCallback(_ => {
        Assert.True(procHighest.IsProcessed);
        Assert.True(procLowest.IsTriggered);
        Assert.False(procLowest.IsProcessed);
      });
      env.Run();
    }

    private IEnumerable<Event> PrioritizedTimeoutProcs(Simulation env, Process procLowest, Process procHighest) {
      yield return env.TimeoutD(1, -1);
      Assert.True(procLowest.IsAlive);
      Assert.False(procLowest.IsTriggered);
      Assert.False(procLowest.IsProcessed);
      Assert.False(procHighest.IsAlive);
      Assert.True(procHighest.IsTriggered);
      Assert.False(procHighest.IsProcessed);
    }

    private IEnumerable<Event> PrioritizedTimeoutProc(Simulation env, int prio) {
      yield return env.TimeoutD(1, prio);
    }
  }
}
