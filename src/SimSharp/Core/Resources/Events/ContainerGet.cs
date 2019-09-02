#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public class ContainerGet : Event {
    public double Amount { get; protected set; }
    public DateTime Time { get; private set; }
    public Process Owner { get; set; }

    public ContainerGet(Simulation environment, Action<Event> callback, double amount)
      : base(environment) {
      if (amount <= 0) throw new ArgumentException("Amount must be > 0.", "amount");
      Amount = amount;
      CallbackList.Add(callback);
      Time = environment.Now;
      Owner = environment.ActiveProcess;
    }
  }
}
