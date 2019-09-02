#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  /// <summary>
  /// An exception that is thrown to stop the simulation.
  /// </summary>
  public class StopSimulationException : Exception {
    public object Value { get; private set; }
    
    public StopSimulationException(object value) {
      Value = value;
    }
  }
}
