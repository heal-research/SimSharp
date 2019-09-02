#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public sealed class Release : Event {
    public Request Request { get; private set; }

    public Release(Simulation environment, Request request, Action<Event> callback)
      : base(environment) {
      Request = request;
      CallbackList.Add(callback);
    }
  }
}
