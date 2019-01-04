#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2019  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
      var env = new Simulation();
      var procA = env.Process(GetStatePemA(env));
      env.Process(GetStatePemB(env, procA));
      env.Run();
    }

    private IEnumerable<Event> GetStatePemA(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(3));
    }

    private IEnumerable<Event> GetStatePemB(Simulation env, Process pemA) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      Assert.True(pemA.IsAlive);
      yield return env.Timeout(TimeSpan.FromSeconds(3));
      Assert.False(pemA.IsAlive);
    }

    [Fact]
    public void TestTarget() {
      var start = new DateTime(1970, 1, 1, 0, 0, 0);
      var delay = TimeSpan.FromSeconds(5);
      var env = new Simulation(start);
      var @event = env.Timeout(delay);
      var proc = env.Process(TargetPem(env, @event));
      while (env.Peek() < start + delay) {
        env.Step();
      }
      Assert.Equal(@event, proc.Target);
      proc.Interrupt();
    }

    private IEnumerable<Event> TargetPem(Simulation env, Event @event) {
      yield return @event;
    }

    [Fact]
    public void TestWaitForProc() {
      // A process can wait until another process finishes.
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(WaitForProcWaiter(env, () => executed = true));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> WaitForProcFinisher(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(5));
    }

    private IEnumerable<Event> WaitForProcWaiter(Simulation env, Action handle) {
      var proc = env.Process(WaitForProcFinisher(env));
      yield return proc; // Wait until "proc" finishes
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 5), env.Now);
      handle();
    }

    [Fact]
    public void TestExit() {
      // Processes can set a return value
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ExitParent(env, () => executed = true));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> ExitChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      env.ActiveProcess.Succeed(env.Now);
    }

    private IEnumerable<Event> ExitParent(Simulation env, Action handle) {
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
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ReturnValueParent(env, () => executed = true));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> ReturnValueParent(Simulation env, Action handle) {
      var proc1 = env.Process(ReturnValueChild(env));
      yield return proc1;
      var proc2 = env.Process(ReturnValueChild(env));
      yield return proc2;
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), proc1.Value);
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 2), proc2.Value);
      handle();
    }

    private IEnumerable<Event> ReturnValueChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      env.ActiveProcess.Succeed(env.Now);
    }

    [Fact]
    public void TestChildException() {
      // A child catches an exception and sends it to its parent.
      // This is the same as TestExit
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ChildExceptionParent(env, () => executed = true));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> ChildExceptionChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      env.ActiveProcess.Succeed(new Exception("Onoes!"));
    }

    private IEnumerable<Event> ChildExceptionParent(Simulation env, Action handle) {
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
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      var parent = env.Process(InterruptedJoinParent(env, () => executed = true));
      env.Process(InterruptedJoinInterruptor(env, parent));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> InterruptedJoinInterruptor(Simulation env, Process process) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      process.Interrupt();
    }

    private IEnumerable<Event> InterruptedJoinChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(2));
    }

    private IEnumerable<Event> InterruptedJoinParent(Simulation env, Action handle) {
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
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      var parent = env.Process(InterruptedJoinAndRejoinParent(env, () => executed = true));
      env.Process(InterruptedJoinAndRejoinInterruptor(env, parent));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> InterruptedJoinAndRejoinInterruptor(Simulation env, Process process) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      process.Interrupt();
    }

    private IEnumerable<Event> InterruptedJoinAndRejoinChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(2));
    }

    private IEnumerable<Event> InterruptedJoinAndRejoinParent(Simulation env, Action handle) {
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
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      var parent = env.Process(UnregisterAfterInterruptParent(env, () => executed = true));
      env.Process(UnregisterAfterInterruptInterruptor(env, parent));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> UnregisterAfterInterruptInterruptor(Simulation env, Process process) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      process.Interrupt();
    }

    private IEnumerable<Event> UnregisterAfterInterruptChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(2));
    }

    private IEnumerable<Event> UnregisterAfterInterruptParent(Simulation env, Action handle) {
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
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ErrorAndInterruptedJoinParent(env, () => executed = true));
      env.Run();
      Assert.True(executed);
    }

    private IEnumerable<Event> ErrorAndInterruptedJoinChildA(Simulation env, Process process) {
      process.Interrupt("InterruptA");
      env.ActiveProcess.Succeed();
      yield return env.Timeout(TimeSpan.FromSeconds(1));
    }

    private IEnumerable<Event> ErrorAndInterruptedJoinChildB(Simulation env) {
      env.ActiveProcess.Fail("spam");
      yield return env.Timeout(TimeSpan.FromSeconds(1));
    }

    private IEnumerable<Event> ErrorAndInterruptedJoinParent(Simulation env, Action handle) {
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
      var env = new Simulation(defaultStep: TimeSpan.FromMinutes(1));
      var proc = env.Process(Proc(env));
      env.Process(Proc1(env, proc));
      var p2 = env.Process(Proc2(env, proc));
      Assert.Throws<InvalidOperationException>(() => env.Run());
      env.Run();
      Assert.Equal(42, (int)p2.Value);
    }

    private IEnumerable<Event> Proc(Simulation env) {
      yield return env.Timeout(TimeSpan.FromMinutes(10));
      env.ActiveProcess.Fail();
    }

    private IEnumerable<Event> Proc1(Simulation env, Process dep) {
      yield return env.Timeout(TimeSpan.FromMinutes(20));
      yield return dep;
      yield return env.Timeout(TimeSpan.FromMinutes(5));
      throw new NotImplementedException("process should not be able to continue");
    }

    private IEnumerable<Event> Proc2(Simulation env, Process dep) {
      yield return env.Timeout(TimeSpan.FromMinutes(20));
      yield return dep;
      env.ActiveProcess.HandleFault();
      yield return env.Timeout(TimeSpan.FromMinutes(5));
      env.ActiveProcess.Succeed(42);
    }

    [Fact]
    public void TestPrioritizedProcesses() {
      var env = new Simulation(defaultStep: TimeSpan.FromMinutes(1));
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

    private IEnumerable<Event> PrioritizedProcess(Simulation env, int prio, List<int> order) {
      order.Add(prio);
      yield break;
    }
  }
}
