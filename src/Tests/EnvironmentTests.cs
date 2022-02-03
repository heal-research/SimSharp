#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
      var t1 = env.Rand(new Uniform(1, 3));
      yield return env.TimeoutD(t1);
      var t2 = env.Rand(new Normal(3, 0.1));
      yield return env.TimeoutD(t2);
      var t3 = env.Rand(new Exponential(2));
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

    [Fact]
    public void PseudoRealtimeEnvTestStopTest() {
      var then = DateTime.UtcNow;
      var env = new PseudoRealtimeSimulation();
      env.Run(TimeSpan.FromSeconds(1));
      var now = DateTime.UtcNow;
      Assert.True(now - then >= TimeSpan.FromSeconds(1));

      then = DateTime.UtcNow;
      var t = Task.Run(() => env.Run(TimeSpan.FromMinutes(1)));
      Task.Delay(TimeSpan.FromMilliseconds(200)).Wait();
      env.StopAsync();
      t.Wait();
      now = DateTime.UtcNow;
      Assert.True(now - then < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void PseudoRealtimeEnvTest() {
      var then = DateTime.UtcNow;
      var delay = TimeSpan.FromSeconds(1);
      var env = new PseudoRealtimeSimulation();
      env.Process(RealtimeDelay(env, delay));
      env.Run();
      var now = DateTime.UtcNow;
      Assert.True(now - then >= delay);
    }

    private IEnumerable<Event> RealtimeDelay(Simulation env, TimeSpan delay) {
      yield return env.Timeout(delay);
    }

    [Fact]
    public void PseudoRealtimeMixedTest() {
      var rtDelay7s = TimeSpan.FromSeconds(7.0);
      var rtDelay1s = TimeSpan.FromSeconds(1.0);
      var vtDelay5s = TimeSpan.FromSeconds(5.0);
      var env = new PseudoRealtimeSimulation();

      // process 1
      env.Process(MixedTestRealtimeDelay(env, rtDelay7s, vtDelay: TimeSpan.Zero));
      // process 2
      env.Process(MixedTestRealtimeDelay(env, rtDelay1s, vtDelay5s));

      var sw = Stopwatch.StartNew();
      env.Run();
      sw.Stop();
      Assert.True(sw.Elapsed >= TimeSpan.FromSeconds(2)); // process with 7s in realtime is interrupted for 5s in virtual time
    }

    private IEnumerable<Event> MixedTestRealtimeDelay(PseudoRealtimeSimulation env, TimeSpan rtDelay, TimeSpan vtDelay) {
      var sw = Stopwatch.StartNew();
      yield return env.Timeout(rtDelay);
      sw.Stop();
      Assert.True(env.Now == env.StartDate + rtDelay);
      // it's not guaranteed that we'd pass rtDelay in wall-clock time when we may switch between real and virtual time

      if (vtDelay > TimeSpan.Zero) {
        env.SetVirtualtime();

        sw.Restart();
        yield return env.Timeout(vtDelay);
        sw.Stop();
        Assert.True(env.Now == env.StartDate + rtDelay + vtDelay);
        Assert.True(sw.Elapsed < TimeSpan.FromMilliseconds(10)); // much less, but 10ms should be a pretty safe upper limit

        env.SetRealtime();
      }
    }

    [Fact]
    public async void PseudoRealtimeMultiThreadedTest() {
      var env = new PseudoRealtimeSimulation();
      using (var sync = new AutoResetEvent(false)) {
        env.Process(MultiThreadedRealtimeProcess(env, sync));
        var sw = Stopwatch.StartNew();
        await env.RunAsync();
        Assert.True(sw.Elapsed >= TimeSpan.FromSeconds(3.5), $"a {sw.Elapsed} >= {TimeSpan.FromSeconds(3.5)}");
      }
    }

    private IEnumerable<Event> MultiThreadedRealtimeProcess(PseudoRealtimeSimulation env, AutoResetEvent wh) {
      Task.Run(() => MultiThreadInteractor(env, wh));

      var simulatedDelay = TimeSpan.FromSeconds(1);
      var wallClock = Stopwatch.StartNew();
      yield return env.Timeout(simulatedDelay); // after 500ms, realtime scale is set to 0.5
      Assert.True(env.Now == env.StartDate + simulatedDelay);
      Assert.True(wallClock.Elapsed >= TimeSpan.FromMilliseconds(1400), $"b {wallClock.Elapsed} >= {TimeSpan.FromMilliseconds(1400)}");
      wallClock.Restart();
      yield return env.Timeout(simulatedDelay); // still runs at 0.5 scale
      Assert.True(env.Now == env.StartDate + 2 * simulatedDelay);
      Assert.True(wallClock.Elapsed >= TimeSpan.FromMilliseconds(1900), $"c {wallClock.Elapsed} >= {TimeSpan.FromMilliseconds(1900)}");
      wh.Set(); // SYNC1
      wallClock.Restart();
      yield return env.Timeout(simulatedDelay); // after the synchronization, realtime scale is set to 2
      Assert.True(env.Now == env.StartDate + 3 * simulatedDelay);
      Assert.True(wallClock.Elapsed >= TimeSpan.FromMilliseconds(400), $"d {wallClock.Elapsed} >= {TimeSpan.FromMilliseconds(400)}");
      wh.Set(); // SYNC2
      wallClock.Restart();
      yield return env.Timeout(simulatedDelay); // after the syncrhonization, virtual time is used
      Assert.True(env.Now == env.StartDate + 4 * simulatedDelay);
      Assert.True(wallClock.Elapsed <= TimeSpan.FromMilliseconds(100), $"e {wallClock.Elapsed} <= {TimeSpan.FromMilliseconds(100)}");
    }

    private void MultiThreadInteractor(PseudoRealtimeSimulation env, AutoResetEvent wh) {
      Task.Delay(500).Wait();
      env.SetRealtime(0.5);
      wh.WaitOne(); // SYNC1
      env.SetRealtime(2);
      wh.WaitOne(); // SYNC2
      env.SetVirtualtime();
    }

    [Fact]
    public async void PseudoRealtimeMultiThreadedTest2() {
      var env = new PseudoRealtimeSimulation();
      env.PseudoRealtimeProcess(AnotherMultiThreadedRealtimeProcess(env));
      var sw = Stopwatch.StartNew();
      await env.RunAsync();
      Assert.True(sw.Elapsed >= TimeSpan.FromSeconds(1.5), $"a {sw.Elapsed.TotalMilliseconds} >= 1500");
    }

    private IEnumerable<Event> AnotherMultiThreadedRealtimeProcess(PseudoRealtimeSimulation env) {
      Task.Run(() => AnotherMultiThreadInteractor(env));
      var simulatedDelay = TimeSpan.FromSeconds(5);
      var sw = Stopwatch.StartNew();
      yield return env.Timeout(simulatedDelay);
      var elapsed = sw.Elapsed;
      Assert.True(elapsed < (env.Now - env.StartDate), $"b {elapsed.TotalMilliseconds} < {(env.Now - env.StartDate).TotalMilliseconds}");
    }

    private void AnotherMultiThreadInteractor(PseudoRealtimeSimulation env) {
      Task.Delay(500).Wait();
      env.Process(AProcessOnAnotherThread(env));
    }

    private IEnumerable<Event> AProcessOnAnotherThread(PseudoRealtimeSimulation env) {
      var sw = Stopwatch.StartNew();
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      var elapsed = sw.Elapsed;
      Assert.True(elapsed >= TimeSpan.FromMilliseconds(1000), $"c {elapsed.TotalMilliseconds} >= 1000");
      env.SetVirtualtime();
    }
  }
}
