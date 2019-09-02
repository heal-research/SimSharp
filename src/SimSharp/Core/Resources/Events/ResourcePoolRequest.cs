#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public class ResourcePoolRequest : Request {
    public Func<object, bool> Filter { get; private set; }

    public ResourcePoolRequest(Simulation environment, Action<Event> callback, Action<Event> disposeCallback, Func<object, bool> filter)
      : base(environment, callback, disposeCallback) {
      Filter = filter;
    }
  }
}
