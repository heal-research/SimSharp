#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public class Request : Event, IDisposable {
    private readonly Action<Event> disposeCallback;
    public DateTime Time { get; private set; }
    public Process Owner { get; set; }

    public Request(Simulation environment, Action<Event> callback, Action<Event> disposeCallback)
      : base(environment) {
      CallbackList.Add(callback);
      this.disposeCallback = disposeCallback;
      Time = environment.Now;
      Owner = environment.ActiveProcess;
    }

    public virtual void Dispose() {
      if (disposeCallback != null) disposeCallback(this);
    }
  }
}
