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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimSharp.Tests {
  [TestClass]
  public class TimeoutTest {
    [TestMethod]
    public void TestDiscreteTimeSteps() {
      var start = new DateTime(2014, 4, 1);
      var env = new Environment(start);
      var log = new List<DateTime>();
      env.Process(TestDiscreteTimeSteps(env, log));
      env.Run(TimeSpan.FromSeconds(3));

      Assert.AreEqual(3, log.Count);
      for (int i = 0; i < 3; i++)
        Assert.IsTrue(log.Contains(start + TimeSpan.FromSeconds(i)));
      Assert.AreEqual(3, env.ProcessedEvents);
    }

    private IEnumerable<Event> TestDiscreteTimeSteps(Environment env, List<DateTime> log) {
      while (true) {
        log.Add(env.Now);
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    [TestMethod, ExpectedException(typeof(ArgumentException))]
    public void TestNegativeTimeout() {
      var env = new Environment();
      env.Process(TestNegativeTimeout(env));
      env.Run();
    }

    private IEnumerable<Event> TestNegativeTimeout(Environment env) {
      yield return env.Timeout(TimeSpan.FromSeconds(-1));
    }

    [TestMethod]
    public void TestSharedTimeout() {
      var start = new DateTime(2014, 4, 1);
      var env = new Environment(start);
      var timeout = env.Timeout(TimeSpan.FromSeconds(1));
      var log = new Dictionary<int, DateTime>();
      for (int i = 0; i < 3; i++)
        env.Process(TestSharedTimeout(env, timeout, i, log));
      env.Run();

      Assert.AreEqual(3, log.Count);
      foreach (var l in log.Values)
        Assert.AreEqual(start + TimeSpan.FromSeconds(1), l);
    }

    private IEnumerable<Event> TestSharedTimeout(Environment env, Timeout timeout, int id, Dictionary<int, DateTime> log) {
      yield return timeout;
      log.Add(id, env.Now);
    }

    [TestMethod]
    public void TestTriggeredTimeout() {
      var env = new Environment();
      env.Process(TestTriggeredTimeout(env));
      env.Run();
      Assert.AreEqual(2, env.NowD);
    }
    private IEnumerable<Event> TestTriggeredTimeout(Environment env) {
      var @event = env.Timeout(TimeSpan.FromSeconds(1));
      // Start the child after the timeout already happened
      yield return env.Timeout(TimeSpan.FromSeconds(2));
      yield return env.Process(TestTriggeredTimeoutChild(env, @event));
    }
    private IEnumerable<Event> TestTriggeredTimeoutChild(Environment env, Event @event) {
      yield return @event;
    }
  }
}
