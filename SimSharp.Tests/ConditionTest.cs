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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimSharp.Tests {
  [TestClass]
  public class ConditionTest {
    [TestMethod]
    public void TestOperatorAnd() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestOperatorAnd(env));
      env.Run();
    }
    private IEnumerable<Event> TestOperatorAnd(Environment env) {
      var timeout = new List<Event>();
      for (int i = 0; i < 3; i++)
        timeout.Add(env.Timeout(TimeSpan.FromSeconds(i)));

      yield return timeout[0] & timeout[1] & timeout[2];

      Assert.IsTrue(timeout.All(t => t.IsProcessed));
    }

    [TestMethod]
    public void TestOperatorOr() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestOperatorOr(env));
      env.Run();
    }
    private IEnumerable<Event> TestOperatorOr(Environment env) {
      var timeout = new List<Event>();
      for (int i = 0; i < 3; i++)
        timeout.Add(env.Timeout(TimeSpan.FromSeconds(i)));

      yield return timeout[0] | timeout[1] | timeout[2];

      Assert.IsTrue(timeout[0].IsProcessed);
      Assert.IsFalse(timeout[1].IsProcessed);
      Assert.IsFalse(timeout[2].IsProcessed);
    }

    [TestMethod]
    public void TestOperatorNestedAnd() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestOperatorNestedAnd(env));
      env.Run();
    }
    private IEnumerable<Event> TestOperatorNestedAnd(Environment env) {
      var timeout = new List<Event>();
      for (int i = 0; i < 3; i++)
        timeout.Add(env.Timeout(TimeSpan.FromSeconds(i)));

      yield return (timeout[0] & timeout[2]) | timeout[1];

      Assert.IsTrue(timeout[0].IsProcessed);
      Assert.IsTrue(timeout[1].IsProcessed);
      Assert.IsFalse(timeout[2].IsProcessed);
    }

    [TestMethod]
    public void TestOperatorNestedOr() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestOperatorNestedOr(env));
      env.Run();
    }
    private IEnumerable<Event> TestOperatorNestedOr(Environment env) {
      var timeout = new List<Event>();
      for (int i = 0; i < 3; i++)
        timeout.Add(env.Timeout(TimeSpan.FromSeconds(i)));

      yield return (timeout[0] | timeout[1]) & timeout[2];

      Assert.IsTrue(timeout[0].IsProcessed);
      Assert.IsTrue(timeout[1].IsProcessed);
      Assert.IsTrue(timeout[2].IsProcessed);

      timeout = new List<Event>();
      for (int i = 0; i < 3; i++)
        timeout.Add(env.Timeout(TimeSpan.FromSeconds(i)));

      yield return (timeout[0] | timeout[2]) & timeout[1];

      Assert.IsTrue(timeout[0].IsProcessed);
      Assert.IsTrue(timeout[1].IsProcessed);
      Assert.IsFalse(timeout[2].IsProcessed);
    }

    [TestMethod]
    public void TestConditionWithError() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestConditionWithError_Process(env));
      env.Run();
    }
    private IEnumerable<Event> TestConditionWithError_Process(Environment env) {
      var proc = env.Process(TestConditionWithError_Explode(env, TimeSpan.FromSeconds(0)));

      yield return proc | env.Timeout(TimeSpan.FromSeconds(1));

      Assert.IsTrue(!proc.IsOk);
      Assert.AreEqual(proc.Value, "Onoes, failed after 0 delay!");
      env.ActiveProcess.HandleFault();
    }
    private IEnumerable<Event> TestConditionWithError_Explode(Environment env, TimeSpan delay) {
      var timeout = env.Timeout(delay);
      yield return timeout;
      env.ActiveProcess.Fail(string.Format("Onoes, failed after {0} delay!", delay.Ticks));
    }

    [TestMethod]
    public void TestConditionWithUncaughtError() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestConditionWithUncaughtError_Process(env));
      try {
        env.Run();
        Assert.Fail("There should have been an InvalidOperationException");
      } catch (Exception e) {
        Assert.IsInstanceOfType(e, typeof(InvalidOperationException));
      }
      Assert.AreEqual(env.Now, new DateTime(2014, 1, 3));
    }

    private IEnumerable<Event> TestConditionWithUncaughtError_Process(Environment env) {
      yield return env.Timeout(TimeSpan.FromDays(1)) | env.Process(TestConditionWithUncaughtError_Explode(env, 2));
    }

    private IEnumerable<Event> TestConditionWithUncaughtError_Explode(Environment env, int delay) {
      yield return env.Timeout(TimeSpan.FromDays(delay));
      env.ActiveProcess.Fail();
    }

    [TestMethod]
    public void TestAndConditionBlocked() {
      var env = new Environment();
      env.Process(TestAndConditionBlockedProcess(env));
      env.RunD(5);
      Assert.AreEqual(5, env.NowD);
    }

    private IEnumerable<Event> TestAndConditionBlockedProcess(Environment env) {
      var t1 = env.TimeoutD(1);
      var e = new Event(env);
      yield return t1;
      yield return t1 & e;
      Assert.Fail("Process should not recover");
    }

    [TestMethod]
    public void TestOperatorAndBlocked() {
      var env = new Environment();
      env.Process(TestOperatorAndBlockedProcess(env));
      env.Run();
    }

    private IEnumerable<Event> TestOperatorAndBlockedProcess(Environment env) {
      var timeout = env.TimeoutD(1);
      var @event = new Event(env);
      yield return env.TimeoutD(1);
      var condition = timeout & @event;
      Assert.IsFalse(condition.IsTriggered);
    }

    [TestMethod]
    public void TestAllOfGenerator() {
      var env = new Environment();
      env.Process(TestAllOfGeneratorProcess(env));
      env.Run();
    }

    private IEnumerable<Event> TestAllOfGeneratorProcess(Environment env) {
      var events = Enumerable.Range(0, 10).Select(x => new Timeout(env, env.ToTimeSpan(x), x));
      var allOf = new AllOf(env, events);
      yield return allOf;
      Assert.IsTrue(Enumerable.Range(0, 10).SequenceEqual(allOf.Value.Values.OfType<int>()));
      Assert.AreEqual(9, env.NowD);
    }

    [TestMethod]
    public void TestAllOfEmptyList() {
      var env = new Environment();
      var evt = new AllOf(env, Enumerable.Empty<Event>());
      Assert.IsTrue(evt.IsTriggered);
      Assert.IsFalse(evt.IsProcessed);
      env.Run(evt);
      Assert.IsTrue(evt.IsProcessed);
      Assert.AreEqual(0, env.NowD);
    }

    [TestMethod]
    public void TestAnyOfEmptyList() {
      var env = new Environment();
      var evt = new AnyOf(env, Enumerable.Empty<Event>());
      Assert.IsTrue(evt.IsTriggered);
      Assert.IsFalse(evt.IsProcessed);
      env.Run(evt);
      Assert.IsTrue(evt.IsProcessed);
      Assert.AreEqual(0, env.NowD);
    }
  }
}
