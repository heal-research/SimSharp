#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;

namespace SimSharp {
  /// <summary>
  /// A Process handles the iteration of events. Processes may define steps that
  /// a certain entity in the simulation has to perform. Each time the process
  /// should wait it yields an event and will be resumed when that event is processed.
  /// </summary>
  /// <remarks>
  /// Since an iterator method does not have access to its process, the method can
  /// retrieve the associated Process through the ActiveProcess property of the
  /// environment. Each Process sets and resets that property during Resume.
  /// </remarks>
  public class Process : Event {
    private readonly IEnumerator<Event> generator;
    private Event target;
    /// <summary>
    /// Target is the event that is expected to be executed next in the process.
    /// </summary>
    public Event Target {
      get { return target; }
      protected set { target = value; }
    }

    /// <summary>
    /// Sets up a new process.
    /// The process places an initialize event into the event queue which starts
    /// the process by retrieving events from the generator.
    /// </summary>
    /// <param name="environment">The environment in which the process lives.</param>
    /// <param name="generator">The generator function of the process.</param>
    /// <param name="priority">The priority if multiple processes are started at the same time.</param>
    public Process(Simulation environment, IEnumerable<Event> generator, int priority = 0)
      : base(environment) {
      this.generator = generator.GetEnumerator();
      IsOk = true;
      target = new Initialize(environment, this, priority);
    }

    /// <summary>
    /// This interrupts a process and causes the IsOk flag to be set to false.
    /// If a process is interrupted the iterator method needs to call HandleFault()
    /// before continuing to yield further events.
    /// </summary>
    /// <exception cref="InvalidOperationException">This is thrown in three conditions:
    ///  - If the process has already been triggered.
    ///  - If the process attempts to interrupt itself.
    ///  - If the process continues to yield events despite being faulted.</exception>
    /// <param name="cause">The cause of the interrupt.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    public virtual void Interrupt(object cause = null, int priority = 0) {
      if (IsTriggered) throw new InvalidOperationException("The process has terminated and cannot be interrupted.");
      if (Environment.ActiveProcess == this) throw new InvalidOperationException("A process is not allowed to interrupt itself.");

      var interruptEvent = new Event(Environment);
      interruptEvent.AddCallback(Resume);
      interruptEvent.Fail(cause, priority);

      if (Target != null)
        Target.RemoveCallback(Resume);
    }

    protected virtual void Resume(Event @event) {
      Environment.ActiveProcess = this;
      while (true) {
        if (@event.IsOk) {
          if (generator.MoveNext()) {
            if (IsTriggered) {
              // the generator called e.g. Environment.ActiveProcess.Fail
              Environment.ActiveProcess = null;
              return;
            }
            if (!ProceedToEvent()) {
              @event = target;
              continue;
            } else break;
          } else if (!IsTriggered) {
            Succeed(@event.Value);
            break;
          } else break;
        } else {
          /* Fault handling differs from SimPy as in .NET it is not possible to inject an
         * exception into an enumerator and it is impossible to put a yield return inside
         * a try-catch block. In SimSharp the Process will set IsOk and will then move to
         * the next yield in the generator. However, if after this move IsOk is still false
         * we know that the error was not handled. It is assumed the error is handled if
         * HandleFault() is called on the environment's ActiveProcess which will reset the
         * flag. */
          IsOk = false;
          Value = @event.Value;

          if (generator.MoveNext()) {
            if (IsTriggered) {
              // the generator called e.g. Environment.ActiveProcess.Fail
              Environment.ActiveProcess = null;
              return;
            }
            // if we move next, but IsOk is still false
            if (!IsOk) throw new InvalidOperationException("The process did not react to being faulted.");
            // otherwise HandleFault was called and the fault was handled
            if (ProceedToEvent()) break;
          } else if (!IsTriggered) {
            if (!IsOk) Fail(@event.Value);
            else Succeed(@event.Value);
            break;
          } else break;
        }
      }
      Environment.ActiveProcess = null;
    }
    
    protected virtual bool ProceedToEvent() {
      target = generator.Current;
      Value = target.Value;
      if (target.IsProcessed) return false;
      target.AddCallback(Resume);
      return true;
    }

    /// <summary>
    /// This method must be called to reset the IsOk flag of the process back to true.
    /// The IsOk flag may be set to false if the process waited on an event that failed.
    /// </summary>
    /// <remarks>
    /// In SimPy a faulting process would throw an exception which is then catched and
    /// chained. In SimSharp catching exceptions from a yield is not possible as a yield
    /// return statement may not throw an exception.
    /// If a processes faulted the Value property may indicate a cause for the fault.
    /// </remarks>
    /// <returns>True if a faulting situation needs to be handled, false if the process
    /// is okay and the last yielded event succeeded.</returns>
    public virtual bool HandleFault() {
      if (IsOk) return false;
      IsOk = true;
      return true;
    }

    private class Initialize : Event {
      public Initialize(Simulation environment, Process process, int priority)
        : base(environment) {
        CallbackList.Add(process.Resume);
        IsOk = true;
        IsTriggered = true;
        environment.Schedule(this, priority);
      }
    }
  }

  public class PseudoRealtimeProcess : Process {
    public double RealtimeScale { get; set; }

    public new PseudoRealtimeSimulation Environment {
      get { return (PseudoRealtimeSimulation)base.Environment; }
    }

    /// <summary>
    /// Sets up a new process.
    /// The process places an initialize event into the event queue which starts
    /// the process by retrieving events from the generator.
    /// </summary>
    /// <param name="environment">The environment in which the process lives.</param>
    /// <param name="generator">The generator function of the process.</param>
    /// <param name="priority">The priority if multiple processes are started at the same time.</param>
    /// <param name="realtimeScale">A value strictly greater than 0 used to scale real time events (1 = realtime).</param>
    public PseudoRealtimeProcess(PseudoRealtimeSimulation environment, IEnumerable<Event> generator, int priority = 0, double realtimeScale = PseudoRealtimeSimulation.DefaultRealtimeScale)
      : base(environment, generator, priority) {
      RealtimeScale = realtimeScale;
    }

    protected override void Resume(Event @event) {
      Environment.SetRealtime(RealtimeScale);
      base.Resume(@event);
    }
  }
}
