#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
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

    [Fact]
    public void PseudoRealTimeEnvTestStopTest() {
      var then = DateTime.UtcNow;
      var env = new PseudoRealTimeSimulation();
      env.Run(TimeSpan.FromSeconds(1));
      var now = DateTime.UtcNow;
      Assert.True(now - then >= TimeSpan.FromSeconds(1));

      var t = Task.Run(() => env.Run(TimeSpan.FromMinutes(1)));
      Task.Delay(TimeSpan.FromMilliseconds(200)).Wait();
      env.StopAsync();
      Task.Delay(TimeSpan.FromMilliseconds(200)).Wait();
      Assert.True(t.IsCompleted);
    }

    [Fact]
    public void PseudoRealTimeEnvTest() {
      var then = DateTime.UtcNow;
      var delay = TimeSpan.FromSeconds(1);
      var env = new PseudoRealTimeSimulation();
      env.Process(RealTimeDelay(env, delay));
      env.Run();
      var now = DateTime.UtcNow;
      Assert.True(now - then >= delay);
    }

    private IEnumerable<Event> RealTimeDelay(Simulation env, TimeSpan delay) {
      yield return env.Timeout(delay);
    }

    [Fact]
    public void MixedPseudoRealTimeEnvTest() {
      var delay0 = TimeSpan.FromSeconds(8.0);
      var delay1 = TimeSpan.FromSeconds(1.0);
      var delay2 = TimeSpan.FromSeconds(5.0);
      var env = new PseudoRealTimeSimulation();

      // process 1
      env.Process(MixedTestRealTimeDelay(env, delay0, delay1, delay2)); // should take at least 8 - 6 + 1 = 3 seconds in real time
      // process 2
      env.Process(MixedTestVirtualTimeDelay(env, delay0, delay1, delay2)); // should take 1 second in real and 5 seconds in virtual time

      var then = DateTime.UtcNow;
      env.Run();
      var now = DateTime.UtcNow;
      Assert.True(now - then >= delay0 - delay2); // delay2 is virtual
    }

    // yields events for process 1
    private IEnumerable<Event> MixedTestRealTimeDelay(PseudoRealTimeSimulation env, TimeSpan delay0, TimeSpan delay1, TimeSpan delay2) {
      Assert.True(env.Now == env.StartDate);
      var then = DateTime.UtcNow;
      yield return env.Timeout(delay0);
      var now = DateTime.UtcNow;
      Assert.True(env.Now == env.StartDate + delay0);
      Assert.True(now - then >= delay0 - delay2); // delay2 is virtual
    }

    // yields events for process 2
    private IEnumerable<Event> MixedTestVirtualTimeDelay(PseudoRealTimeSimulation env, TimeSpan delay0, TimeSpan delay1, TimeSpan delay2) {
      Assert.True(env.Now == env.StartDate);
      var then = DateTime.UtcNow;
      yield return env.Timeout(delay1); // delays 1 second in real time
      var now = DateTime.UtcNow;
      Assert.True(env.Now == env.StartDate + delay1);
      Assert.True(now - then >= delay1);

      env.SwitchToVirtualTime();

      yield return env.Timeout(delay2); // delays 5 seconds in virtual time
      Assert.True(env.Now == env.StartDate + delay1 + delay2);

      // switch back to real time happens at virtual time 00:00:06
      env.SwitchToRealTime(); // real time timeout in process 1 should delay at least 8 - 6 + 1 = 3 seconds
    }
  }
}
