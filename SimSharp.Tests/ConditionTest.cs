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

  public class ConditionTest {
    [Fact]
    public void TestOperatorAnd() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestOperatorAndProc(env));
      env.Run();
    }
    private IEnumerable<Event> TestOperatorAndProc(Environment env) {
      var timeout = new List<Event>();
      for (int i = 0; i < 3; i++)
        timeout.Add(env.Timeout(TimeSpan.FromSeconds(i)));

      yield return timeout[0] & timeout[1] & timeout[2];

      Assert.True(timeout.All(t => t.IsProcessed));
    }

    [Fact]
    public void TestOperatorOr() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestOperatorOrProc(env));
      env.Run();
    }
    private IEnumerable<Event> TestOperatorOrProc(Environment env) {
      var timeout = new List<Event>();
      for (int i = 0; i < 3; i++)
        timeout.Add(env.Timeout(TimeSpan.FromSeconds(i)));

      yield return timeout[0] | timeout[1] | timeout[2];

      Assert.True(timeout[0].IsProcessed);
      Assert.False(timeout[1].IsProcessed);
      Assert.False(timeout[2].IsProcessed);
    }

    [Fact]
    public void TestOperatorNestedAnd() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestOperatorNestedAndProc(env));
      env.Run();
    }
    private IEnumerable<Event> TestOperatorNestedAndProc(Environment env) {
      var timeout = new List<Event>();
      for (int i = 0; i < 3; i++)
        timeout.Add(env.Timeout(TimeSpan.FromSeconds(i)));

      yield return (timeout[0] & timeout[2]) | timeout[1];

      Assert.True(timeout[0].IsProcessed);
      Assert.True(timeout[1].IsProcessed);
      Assert.False(timeout[2].IsProcessed);
    }

    [Fact]
    public void TestOperatorNestedOr() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestOperatorNestedOrProc(env));
      env.Run();
    }
    private IEnumerable<Event> TestOperatorNestedOrProc(Environment env) {
      var timeout = new List<Event>();
      for (int i = 0; i < 3; i++)
        timeout.Add(env.Timeout(TimeSpan.FromSeconds(i)));

      yield return (timeout[0] | timeout[1]) & timeout[2];

      Assert.True(timeout[0].IsProcessed);
      Assert.True(timeout[1].IsProcessed);
      Assert.True(timeout[2].IsProcessed);

      timeout = new List<Event>();
      for (int i = 0; i < 3; i++)
        timeout.Add(env.Timeout(TimeSpan.FromSeconds(i)));

      yield return (timeout[0] | timeout[2]) & timeout[1];

      Assert.True(timeout[0].IsProcessed);
      Assert.True(timeout[1].IsProcessed);
      Assert.False(timeout[2].IsProcessed);
    }

    [Fact]
    public void TestConditionWithError() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestConditionWithError_Process(env));
      env.Run();
    }
    private IEnumerable<Event> TestConditionWithError_Process(Environment env) {
      var proc = env.Process(TestConditionWithError_Explode(env, TimeSpan.FromSeconds(0)));

      yield return proc | env.Timeout(TimeSpan.FromSeconds(1));

      Assert.True(!proc.IsOk);
      Assert.Equal("Onoes, failed after 0 delay!", proc.Value);
      env.ActiveProcess.HandleFault();
    }
    private IEnumerable<Event> TestConditionWithError_Explode(Environment env, TimeSpan delay) {
      var timeout = env.Timeout(delay);
      yield return timeout;
      env.ActiveProcess.Fail(string.Format("Onoes, failed after {0} delay!", delay.Ticks));
    }

    [Fact]
    public void TestConditionWithUncaughtError() {
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestConditionWithUncaughtError_Process(env));
      Assert.Throws<InvalidOperationException>(() => env.Run());
      Assert.Equal(new DateTime(2014, 1, 3), env.Now);
    }

    private IEnumerable<Event> TestConditionWithUncaughtError_Process(Environment env) {
      yield return env.Timeout(TimeSpan.FromDays(1)) | env.Process(TestConditionWithUncaughtError_Explode(env, 2));
    }

    private IEnumerable<Event> TestConditionWithUncaughtError_Explode(Environment env, int delay) {
      yield return env.Timeout(TimeSpan.FromDays(delay));
      env.ActiveProcess.Fail();
    }

    [Fact]
    public void TestAndConditionBlocked() {
      var env = new Environment();
      env.Process(TestAndConditionBlockedProcess(env));
      env.RunD(5);
      Assert.Equal(5, env.NowD);
    }

    private IEnumerable<Event> TestAndConditionBlockedProcess(Environment env) {
      var t1 = env.TimeoutD(1);
      var e = new Event(env);
      yield return t1;
      yield return t1 & e;
      throw new NotImplementedException("Process should not recover");
    }

    [Fact]
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
      Assert.False(condition.IsTriggered);
    }

    [Fact]
    public void TestAllOfGenerator() {
      var env = new Environment();
      env.Process(TestAllOfGeneratorProcess(env));
      env.Run();
    }

    private IEnumerable<Event> TestAllOfGeneratorProcess(Environment env) {
      var events = Enumerable.Range(0, 10).Select(x => new Timeout(env, env.ToTimeSpan(x), x));
      var allOf = new AllOf(env, events);
      yield return allOf;
      Assert.True(Enumerable.Range(0, 10).SequenceEqual(allOf.Value.Values.OfType<int>()));
      Assert.Equal(9, env.NowD);
    }

    [Fact]
    public void TestAllOfEmptyList() {
      var env = new Environment();
      var evt = new AllOf(env, Enumerable.Empty<Event>());
      Assert.True(evt.IsTriggered);
      Assert.False(evt.IsProcessed);
      env.Run(evt);
      Assert.True(evt.IsProcessed);
      Assert.Equal(0, env.NowD);
    }

    [Fact]
    public void TestAnyOfEmptyList() {
      var env = new Environment();
      var evt = new AnyOf(env, Enumerable.Empty<Event>());
      Assert.True(evt.IsTriggered);
      Assert.False(evt.IsProcessed);
      env.Run(evt);
      Assert.True(evt.IsProcessed);
      Assert.Equal(0, env.NowD);
    }
  }
}
