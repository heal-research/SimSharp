#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

namespace SimSharp {
  public abstract class ActiveObject<T> where T : Simulation {
    protected T Environment { get; private set; }

    protected ActiveObject(T environment) {
      Environment = environment;
    }
  }
}
