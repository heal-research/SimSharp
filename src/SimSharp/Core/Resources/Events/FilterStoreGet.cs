#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public class FilterStoreGet : StoreGet {
    public Func<object, bool> Filter { get; private set; }

    public FilterStoreGet(Simulation environment, Action<Event> callback, Func<object, bool> filter)
      : base(environment, callback) {
      Filter = filter;
    }
  }
}
