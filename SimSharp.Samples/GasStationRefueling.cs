#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2016  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
  public class GasStationRefueling {
    /*
     * Gas Station Refueling example
     *
     * Covers:
     *  - Resources: Resource
     *  - Resources: Container
     *  - Waiting for other processes
     *
     * Scenario:
     *  A gas station has a limited number of gas pumps that share a common
     *  fuel reservoir. Cars randomly arrive at the gas station, request one
     *  of the fuel pumps and start refueling from that reservoir.
     *
     *  A gas station control process observes the gas station's fuel level
     *  and calls a tank truck for refueling if the station's level drops
     *  below a threshold.
     */
    private const int RandomSeed = 42;
    private const int GasStationSize = 200; // liters
    private const int Threshold = 10; // Threshold for calling the tank truck (in %)
    private const int FuelTankSize = 50; // liters
    private const int MinFuelTankLevel = 5; // Min levels of fuel tanks (in liters)
    private const int MaxFuelTankLevel = 25; // Max levels of fuel tanks (in liters)
    private const int RefuelingSpeed = 2; // liters / second
    private static readonly TimeSpan TankTruckTime = TimeSpan.FromMinutes(10); // Minutes it takes the tank truck to arrive
    private static readonly TimeSpan MinTInter = TimeSpan.FromMinutes(30); // Create a car every min seconds
    private static readonly TimeSpan MaxTInter = TimeSpan.FromMinutes(300); // Create a car every max seconds
    private static readonly TimeSpan SimTime = TimeSpan.FromMinutes(30); // Simulation time in seconds

    private IEnumerable<Event> Car(string name, Environment env, Resource gasStation, Container fuelPump) {
      /*
       * A car arrives at the gas station for refueling.
       * 
       * It requests one of the gas station's fuel pumps and tries to get the
       * desired amount of gas from it. If the stations reservoir is
       * depleted, the car has to wait for the tank truck to arrive.
       */
      var fuelTankLevel = env.RandUniform(MinFuelTankLevel, MaxFuelTankLevel + 1);
      env.Log("{0} arriving at gas station at {1}", name, env.Now);
      using (var req = gasStation.Request()) {
        var start = env.Now;
        // Request one of the gas pumps
        yield return req;

        // Get the required amount of fuel
        var litersRequired = FuelTankSize - fuelTankLevel;
        yield return fuelPump.Get(litersRequired);

        // The "actual" refueling process takes some time
        yield return env.Timeout(TimeSpan.FromSeconds(litersRequired / RefuelingSpeed));

        env.Log("{0} finished refueling in {1} seconds.", name, (env.Now - start).TotalSeconds);
      }
    }


    private IEnumerable<Event> GasStationControl(Environment env, Container fuelPump) {
      /*
       * Periodically check the level of the *fuel_pump* and call the tank
       * truck if the level falls below a threshold.
       */
      while (true) {
        if (fuelPump.Level / fuelPump.Capacity * 100 < Threshold) {
          // We need to call the tank truck now!
          env.Log("Calling tank truck at {0}", env.Now);
          // Wait for the tank truck to arrive and refuel the station
          yield return env.Process(TankTruck(env, fuelPump));

        }
        yield return env.Timeout(TimeSpan.FromSeconds(10)); // Check every 10 seconds
      }
    }

    private IEnumerable<Event> TankTruck(Environment env, Container fuelPump) {
      // Arrives at the gas station after a certain delay and refuels it.
      yield return env.Timeout(TankTruckTime);
      env.Log("Tank truck arriving at time {0}", env.Now);
      var amount = fuelPump.Capacity - fuelPump.Level;
      env.Log("Tank truck refuelling {0} liters.", amount);
      yield return fuelPump.Put(amount);
    }

    private IEnumerable<Event> CarGenerator(Environment env, Resource gasStation, Container fuelPump) {
      // Generate new cars that arrive at the gas station.
      var i = 0;
      while (true) {
        i++;
        yield return env.Timeout(env.RandUniform(MinTInter, MaxTInter));
        env.Process(Car("Car " + i, env, gasStation, fuelPump));
      }
    }

    public void Simulate(int rseed = RandomSeed) {
      // Setup and start the simulation
      // Create environment and start processes
      var env = new Environment(rseed);
      env.Log("== Gas Station refuelling ==");
      var gasStation = new Resource(env, 2);
      var fuelPump = new Container(env, GasStationSize, GasStationSize);
      env.Process(GasStationControl(env, fuelPump));
      env.Process(CarGenerator(env, gasStation, fuelPump));

      // Execute!
      env.Run(SimTime);
    }
  }
}
