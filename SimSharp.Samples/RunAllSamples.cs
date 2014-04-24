#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2014  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.*/
#endregion

using System;
using System.Collections.Generic;

namespace SimSharp.Samples {
  class RunAllSamples {
    public static void Main(string[] args) {
      // Run all samples one after another
      /*new BankRenege().Simulate();
      Console.WriteLine();
      new GasStationRefueling().Simulate();
      Console.WriteLine();
      new MachineShop().Simulate();
      Console.WriteLine();
      new ProcessCommunication().Simulate();
      Console.WriteLine();
      new SteelFactory().Simulate();
      Console.WriteLine();
      new MachineShopSpecialist().Simulate();
      Console.WriteLine();
      new SimpleShop().Simulate();*/
      new RunAllSamples().RunSimulation();
    }

    private double ARRIVAL_TIME = 10;
    private double PROCESSING_TIME = 9;
    private TimeSpan SIMULATION_TIME = TimeSpan.FromMinutes(60);

    IEnumerable<Event> SSQ(Environment env) {
      var server = new Resource(env, capacity: 1);
      while (true) {
        yield return env.TimeoutD(env.RandExponential(ARRIVAL_TIME));
        env.Process(Item(env, server));
      }
    }

    IEnumerable<Event> Item(Environment env, Resource server) {
      using (var s = server.Request()) {
        yield return s;
        yield return env.TimeoutD(env.RandExponential(PROCESSING_TIME));
        Console.WriteLine("Duration {0}", env.Now - s.Time);
      }
    }

    void RunSimulation() {
      var env = new Environment();
      env.Process(SSQ(env));
      env.Run(SIMULATION_TIME);
    }
  }
}
