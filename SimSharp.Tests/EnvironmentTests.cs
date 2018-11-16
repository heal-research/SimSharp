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
      var exceptionRaised = false;
      try {
        var env = new Simulation();
        env.Run(new Event(env));
      } catch (InvalidOperationException e) {
        Assert.Equal("No scheduled events left but \"until\" event was not triggered.", e.Message);
        exceptionRaised = true;
      }
      Assert.True(exceptionRaised);
    }
  }
}
