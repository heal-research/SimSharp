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
using Xunit;

namespace SimSharp.Tests {

  public class ProcessTest {
    [Fact]
    public void TestStartNonProcess() {
      // Check that you cannot start a normal function.
      // This always holds due to the static-typed nature of C#
      // a process always expects an IEnumerable<Event>
      Assert.True(true);
    }

    [Fact]
    public void TestGetState() {
      // A process is alive until it's generator has not terminated.
      var env = new Environment();
      var procA = env.Process(GetStatePemA(env));
      env.Process(GetStatePemB(env, procA));
      env.Run();
    }

    private IEnumerable<Event> GetStatePemA(Environment env) {
      yield return env.Timeout(TimeSpan.FromSeconds(3));
    }

    private IEnumerable<Event> GetStatePemB(Environment env, Process pemA) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      Assert.True(pemA.IsAlive);
      yield return env.Timeout(TimeSpan.FromSeconds(3));
      Assert.False(pemA.IsAlive);
    }

    [Fact]
    public void TestTarget() {
      var start = new DateTime(1970, 1, 1, 0, 0, 0);
      var delay = TimeSpan.FromSeconds(5);
      var env = new Environment(start);
      var @event = env.Timeout(delay);
      var proc = env.Process(TargetPem(env, @event));
      while (env.Peek() < start + delay) {
        env.Step();
      }
      Assert.Equal(@event, proc.Target);
      proc.Interrupt();
    }

    private IEnumerable<Event> TargetPem(Environment env, Event @event) {
      yield return @event;
    }

    [Fact]
    public void TestWaitForProc() {
      // A process can wait until another process finishes.
      var executed = false;
      var env = new Environment(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(WaitForProcWaiter(env, () => executed = true));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> WaitForProcFinisher(Environment env) {
      yield return env.Timeout(TimeSpan.FromSeconds(5));
    }

    private IEnumerable<Event> WaitForProcWaiter(Environment env, Action handle) {
      var proc = env.Process(WaitForProcFinisher(env));
      yield return proc; // Wait until "proc" finishes
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 5), env.Now);
      handle();
    }

    [Fact]
    public void TestExit() {
      // Processes can set a return value
      var executed = false;
      var env = new Environment(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ExitParent(env, () => executed = true));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> ExitChild(Environment env) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      env.ActiveProcess.Succeed(env.Now);
    }

    private IEnumerable<Event> ExitParent(Environment env, Action handle) {
      var result1 = env.Process(ExitChild(env));
      yield return result1;
      var result2 = env.Process(ExitChild(env));
      yield return result2;

      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), result1.Value);
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 2), result2.Value);
      handle();
    }

    [Fact]
    public void TestReturnValue() {
      // Processes can set a return value
      var executed = false;
      var env = new Environment(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ReturnValueParent(env, () => executed = true));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> ReturnValueParent(Environment env, Action handle) {
      var proc1 = env.Process(ReturnValueChild(env));
      yield return proc1;
      var proc2 = env.Process(ReturnValueChild(env));
      yield return proc2;
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), proc1.Value);
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 2), proc2.Value);
      handle();
    }

    private IEnumerable<Event> ReturnValueChild(Environment env) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      env.ActiveProcess.Succeed(env.Now);
    }

    [Fact]
    public void TestChildException() {
      // A child catches an exception and sends it to its parent.
      // This is the same as TestExit
      var executed = false;
      var env = new Environment(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ChildExceptionParent(env, () => executed = true));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> ChildExceptionChild(Environment env) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      env.ActiveProcess.Succeed(new Exception("Onoes!"));
    }

    private IEnumerable<Event> ChildExceptionParent(Environment env, Action handle) {
      var child = env.Process(ChildExceptionChild(env));
      yield return child;
      Assert.IsAssignableFrom<Exception>(child.Value);
      handle();
    }

    [Fact]
    public void TestInterruptedJoin() {
      /* Tests that interrupts are raised while the victim is waiting for
         another process. The victim should get unregistered from the other
         process.
       */
      var executed = false;
      var env = new Environment(new DateTime(1970, 1, 1, 0, 0, 0));
      var parent = env.Process(InterruptedJoinParent(env, () => executed = true));
      env.Process(InterruptedJoinInterruptor(env, parent));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> InterruptedJoinInterruptor(Environment env, Process process) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      process.Interrupt();
    }

    private IEnumerable<Event> InterruptedJoinChild(Environment env) {
      yield return env.Timeout(TimeSpan.FromSeconds(2));
    }

    private IEnumerable<Event> InterruptedJoinParent(Environment env, Action handle) {
      var child = env.Process(InterruptedJoinChild(env));
      yield return child;
      if (env.ActiveProcess.HandleFault()) {
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), env.Now);
        Assert.True(child.IsAlive);
        // We should not get resumed when child terminates.
        yield return env.Timeout(TimeSpan.FromSeconds(5));
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 6), env.Now);
        handle();
      } else throw new NotImplementedException("Did not receive an interrupt.");
    }

    [Fact]
    public void TestInterruptedJoinAndRejoin() {
      // Tests that interrupts are raised while the victim is waiting for
      // another process. The victim tries to join again.
      var executed = false;
      var env = new Environment(new DateTime(1970, 1, 1, 0, 0, 0));
      var parent = env.Process(InterruptedJoinAndRejoinParent(env, () => executed = true));
      env.Process(InterruptedJoinAndRejoinInterruptor(env, parent));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> InterruptedJoinAndRejoinInterruptor(Environment env, Process process) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      process.Interrupt();
    }

    private IEnumerable<Event> InterruptedJoinAndRejoinChild(Environment env) {
      yield return env.Timeout(TimeSpan.FromSeconds(2));
    }

    private IEnumerable<Event> InterruptedJoinAndRejoinParent(Environment env, Action handle) {
      var child = env.Process(InterruptedJoinAndRejoinChild(env));
      yield return child;
      if (env.ActiveProcess.HandleFault()) {
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), env.Now);
        Assert.True(child.IsAlive);
        yield return child;
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 2), env.Now);
        handle();
      } else throw new NotImplementedException("Did not receive an interrupt.");
    }

    [Fact]
    public void TestUnregisterAfterInterrupt() {
      // If a process is interrupted while waiting for another one, it
      // should be unregistered from that process.
      var executed = false;
      var env = new Environment(new DateTime(1970, 1, 1, 0, 0, 0));
      var parent = env.Process(UnregisterAfterInterruptParent(env, () => executed = true));
      env.Process(UnregisterAfterInterruptInterruptor(env, parent));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> UnregisterAfterInterruptInterruptor(Environment env, Process process) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      process.Interrupt();
    }

    private IEnumerable<Event> UnregisterAfterInterruptChild(Environment env) {
      yield return env.Timeout(TimeSpan.FromSeconds(2));
    }

    private IEnumerable<Event> UnregisterAfterInterruptParent(Environment env, Action handle) {
      var child = env.Process(UnregisterAfterInterruptChild(env));
      yield return child;
      if (env.ActiveProcess.HandleFault()) {
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), env.Now);
        Assert.True(child.IsAlive);
      } else throw new NotImplementedException("Did not receive an interrupt.");
      yield return env.Timeout(TimeSpan.FromSeconds(2));
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 3), env.Now);
      Assert.False(child.IsAlive);
      handle();
    }

    [Fact]
    public void TestErrorAndInterruptedJoin() {
      var executed = false;
      var env = new Environment(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ErrorAndInterruptedJoinParent(env, () => executed = true));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> ErrorAndInterruptedJoinChildA(Environment env, Process process) {
      process.Interrupt("InterruptA");
      env.ActiveProcess.Succeed();
      yield return env.Timeout(TimeSpan.FromSeconds(1));
    }

    private IEnumerable<Event> ErrorAndInterruptedJoinChildB(Environment env) {
      env.ActiveProcess.Fail("spam");
      yield return env.Timeout(TimeSpan.FromSeconds(1));
    }

    private IEnumerable<Event> ErrorAndInterruptedJoinParent(Environment env, Action handle) {
      env.Process(ErrorAndInterruptedJoinChildA(env, env.ActiveProcess));
      var b = env.Process(ErrorAndInterruptedJoinChildB(env));
      yield return b;
      if (env.ActiveProcess.HandleFault())
        Assert.Equal("InterruptA", env.ActiveProcess.Value);
      yield return env.Timeout(TimeSpan.FromSeconds(0));
      if (env.ActiveProcess.HandleFault())
        throw new NotImplementedException("process should not react.");
      handle();
    }

    [Fact]
    public void TestYieldFailedProcess() {
      var env = new Environment(defaultStep: TimeSpan.FromMinutes(1));
      var proc = env.Process(Proc(env));
      env.Process(Proc(env, proc));
      env.Run();
    }

    private IEnumerable<Event> Proc(Environment env) {
      yield return env.Timeout(TimeSpan.FromMinutes(10));
      env.ActiveProcess.Fail();
    }

    private IEnumerable<Event> Proc(Environment env, Process dep) {
      yield return env.Timeout(TimeSpan.FromMinutes(20));
      yield return dep;
      yield return env.Timeout(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void TestPrioritizedProcesses() {
      var env = new Environment(defaultStep: TimeSpan.FromMinutes(1));
      var order = new List<int>();
      for (var p = 5; p >= -5; p--) {
        // processes are created such that lowest priority process is first
        env.Process(PrioritizedProcess(env, p, order), p);
      }
      env.Run();
      Assert.Equal(11, order.Count);
      // processes must be executed such that highest priority process is started first
      Assert.Equal(new[] { -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 }, order);
    }

    private IEnumerable<Event> PrioritizedProcess(Environment env, int prio, List<int> order) {
      order.Add(prio);
      yield break;
    }
  }
}
