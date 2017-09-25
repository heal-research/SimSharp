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
using System.Globalization;
using System.Linq;
using Xunit;

namespace SimSharp.Tests {

  public class EnvironmentDApiTests {

    private static IEnumerable<Event> AProcess(Environment env, List<string> log) {
      while (env.NowD < 2) {
        log.Add(env.NowD.ToString("00", CultureInfo.InvariantCulture.NumberFormat));
        yield return env.TimeoutD(1);
      }
    }

    [Fact]
    public void TestEventQueueEmptyDApi() {
      /*The simulation should stop if there are no more events, that means, no
        more active process.*/
      var log = new List<string>();
      var env = new Environment(defaultStep: TimeSpan.FromMinutes(1));
      env.Process(AProcess(env, log));
      env.RunD(10);
      Assert.True(log.SequenceEqual(new[] { "00", "01" }));
    }

    [Fact]
    public void TestRunNegativeUntilDApi() {
      /*Test passing a negative time to run.*/
      var env = new Environment(defaultStep: TimeSpan.FromMinutes(1));
      var errorThrown = false;
      try {
        env.RunD(-1);
      } catch (InvalidOperationException) {
        errorThrown = true;
      }
      Assert.True(errorThrown);
    }

    [Fact]
    public void TestRunResumeDApi() {
      /* Stopped simulation can be resumed. */
      var env = new Environment(defaultStep: TimeSpan.FromMinutes(1));
      var events = new List<Event>() {
        env.TimeoutD(5),
        env.TimeoutD(10),
        env.TimeoutD(15),
        env.TimeoutD(20)
      };

      Assert.Equal(0, env.NowD);
      Assert.Equal(5, env.PeekD());
      Assert.DoesNotContain(events, x => x.IsProcessed);

      env.RunD(10);
      Assert.Equal(10, env.NowD);
      Assert.Equal(10, env.PeekD());
      Assert.True(events[0].IsProcessed);
      Assert.False(events[1].IsProcessed);
      Assert.False(events[2].IsProcessed);

      env.RunD(5);
      Assert.Equal(15, env.NowD);
      Assert.Equal(15, env.PeekD());
      Assert.True(events[0].IsProcessed);
      Assert.True(events[1].IsProcessed);
      Assert.False(events[2].IsProcessed);

      env.RunD(1);
      Assert.Equal(16, env.NowD);
      Assert.Equal(20, env.PeekD());
      Assert.True(events[0].IsProcessed);
      Assert.True(events[1].IsProcessed);
      Assert.True(events[2].IsProcessed);

      env.RunD();
      Assert.Equal(20, env.NowD);
      Assert.Equal(double.MaxValue, env.PeekD());
    }

    [Fact]
    public void TestRunUntilValueDApi() {
      /* Anything that can be converted to a float is a valid until value. */
      var env = new Environment(defaultStep: TimeSpan.FromMinutes(1));
      env.RunD(100);
      Assert.Equal(100, env.NowD);
    }
  }
}
