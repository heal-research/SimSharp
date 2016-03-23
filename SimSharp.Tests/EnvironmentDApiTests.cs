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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimSharp.Tests {
  [TestClass]
  public class EnvironmentDApiTests {

    private static IEnumerable<Event> AProcess(Environment env, List<string> log) {
      while (env.NowD < 2) {
        log.Add(env.NowD.ToString("00", CultureInfo.InvariantCulture.NumberFormat));
        yield return env.TimeoutD(1);
      }
    }

    [TestMethod]
    public void TestEventQueueEmptyDApi() {
      /*The simulation should stop if there are no more events, that means, no
        more active process.*/
      var log = new List<string>();
      var env = new Environment(defaultStep: TimeSpan.FromMinutes(1));
      env.Process(AProcess(env, log));
      env.RunD(10);
      Assert.IsTrue(log.SequenceEqual(new[] { "00", "01" }));
    }

    [TestMethod]
    public void TestRunNegativeUntilDApi() {
      /*Test passing a negative time to run.*/
      var env = new Environment(defaultStep: TimeSpan.FromMinutes(1));
      var errorThrown = false;
      try {
        env.RunD(-1);
      } catch (InvalidOperationException) {
        errorThrown = true;
      }
      Assert.IsTrue(errorThrown);
    }

    [TestMethod]
    public void TestRunResumeDApi() {
      /* Stopped simulation can be resumed. */
      var env = new Environment(defaultStep: TimeSpan.FromMinutes(1));
      var events = new List<Event>() {
        env.TimeoutD(5),
        env.TimeoutD(10),
        env.TimeoutD(15),
        env.TimeoutD(20)
      };

      Assert.AreEqual(env.NowD, 0);
      Assert.AreEqual(env.PeekD(), 5);
      Assert.IsFalse(events.Any(x => x.IsProcessed));

      env.RunD(10);
      Assert.AreEqual(env.NowD, 10);
      Assert.AreEqual(env.PeekD(), 10);
      Assert.IsTrue(events[0].IsProcessed);
      Assert.IsFalse(events[1].IsProcessed);
      Assert.IsFalse(events[2].IsProcessed);

      env.RunD(5);
      Assert.AreEqual(env.NowD, 15);
      Assert.AreEqual(env.PeekD(), 15);
      Assert.IsTrue(events[0].IsProcessed);
      Assert.IsTrue(events[1].IsProcessed);
      Assert.IsFalse(events[2].IsProcessed);

      env.RunD(1);
      Assert.AreEqual(env.NowD, 16);
      Assert.AreEqual(env.PeekD(), 20);
      Assert.IsTrue(events[0].IsProcessed);
      Assert.IsTrue(events[1].IsProcessed);
      Assert.IsTrue(events[2].IsProcessed);

      env.RunD();
      Assert.AreEqual(env.NowD, 20);
      Assert.AreEqual(env.PeekD(), double.MaxValue);
    }

    [TestMethod]
    public void TestRunUntilValueDApi() {
      /* Anything that can be converted to a float is a valid until value. */
      var env = new Environment(defaultStep: TimeSpan.FromMinutes(1));
      env.RunD(100);
      Assert.AreEqual(env.NowD, 100);
    }
  }
}
