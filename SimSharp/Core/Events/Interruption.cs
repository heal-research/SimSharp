using System;

namespace SimSharp {
  public class Interruption : Event {
    public Process InterruptedProcess { get; private set; }
    public Interruption(Environment environment, Process process)
      : base(environment) {
      if (process == environment.ActiveProcess) throw new ArgumentException("A process may not interrupt itself.", "process");
      if (process.IsTriggered) throw new InvalidOperationException("Process to interrupt is already triggered.");
      InterruptedProcess = process;
      CallbackList.Add(DoInterrupt);
    }

    private void DoInterrupt(Event @event) {
      if (InterruptedProcess.IsTriggered) return;
      if (InterruptedProcess.Target != @event && InterruptedProcess.Target != null)
        InterruptedProcess.Target.RemoveCallback(InterruptedProcess.Resume);
      InterruptedProcess.Resume(@event);
    }
  }
}
