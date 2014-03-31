using System;

namespace SimSharp {
  public class ContainerGet : Event {
    public double Amount { get; protected set; }
    public DateTime Time { get; private set; }
    public Process Process { get; private set; }

    public ContainerGet(Environment environment, Action<Event> callback, double amount)
      : base(environment) {
      if (amount <= 0) throw new ArgumentException("Amount must be > 0.", "amount");
      Amount = amount;
      CallbackList.Add(callback);
      Time = environment.Now;
      Process = environment.ActiveProcess;
    }
  }
}
