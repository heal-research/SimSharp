#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public class StoreGet : Event {
    public DateTime Time { get; private set; }
    public Process Owner { get; set; }

    public StoreGet(Simulation environment, Action<Event> callback)
      : base(environment) {
      CallbackList.Add(callback);
      Time = environment.Now;
      Owner = environment.ActiveProcess;
    }
  }
}
