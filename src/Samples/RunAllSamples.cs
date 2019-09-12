#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Linq;

namespace SimSharp.Samples {
  class RunAllSamples {
    public static void Main(string[] args) {
     // new Population().Simulate();
      new Population().Play();
      // foreach (Type mytype in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
      //   .Where(mytype => mytype .GetInterfaces().Contains(typeof(ISimulate)))) 
      // {
      //     // Run all samples one after another
      //     ((ISimulate)Activator.CreateInstance(mytype)).Simulate();
      //     Console.WriteLine();
      // }
    }
  }
}
