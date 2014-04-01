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
  public class GasStationRefueling {
    /*
Gas Station Refueling example

Covers:

- Resources: Resource
- Resources: Container
- Waiting for other processes

Scenario:
  A gas station has a limited number of gas pumps that share a common
  fuel reservoir. Cars randomly arrive at the gas station, request one
  of the fuel pumps and start refueling from that reservoir.

  A gas station control process observes the gas station's fuel level
  and calls a tank truck for refueling if the station's level drops
  below a threshold.
*/
    private const int RANDOM_SEED = 42;
    private const int GAS_STATION_SIZE = 200; // liters
    private const int THRESHOLD = 10; // Threshold for calling the tank truck (in %)
    private const int FUEL_TANK_SIZE = 50; // liters
    private const int MIN_FUEL_TANK_LEVEL = 5; // Min levels of fuel tanks (in liters)
    private const int MAX_FUEL_TANK_LEVEL = 25; // Max levels of fuel tanks (in liters)
    private const int REFUELING_SPEED = 2; // liters / second
    private const int TANK_TRUCK_TIME = 10; // Minutes it takes the tank truck to arrive
    private const int MIN_T_INTER = 30; // Create a car every min seconds
    private const int MAX_T_INTER = 300; // Create a car every max seconds
    private static readonly TimeSpan SIM_TIME = TimeSpan.FromMinutes(30); // Simulation time in seconds
    private Random random;

    private IEnumerable<Event> Car(string name, Environment env, Resource gas_station, Container fuel_pump) {
      /*
    A car arrives at the gas station for refueling.

    It requests one of the gas station's fuel pumps and tries to get the
    desired amount of gas from it. If the stations reservoir is
    depleted, the car has to wait for the tank truck to arrive.
    */
      var fuel_tank_level = random.Next(MIN_FUEL_TANK_LEVEL, MAX_FUEL_TANK_LEVEL + 1);
      Console.Out.WriteLine("{0} arriving at gas station at {1}", name, env.Now);
      using (var req = gas_station.Request()) {
        var start = env.Now;
        // Request one of the gas pumps
        yield return req;

        // Get the required amount of fuel
        var liters_required = FUEL_TANK_SIZE - fuel_tank_level;
        yield return fuel_pump.Get(liters_required);

        // The "actual" refueling process takes some time
        yield return env.Timeout(TimeSpan.FromSeconds(liters_required / REFUELING_SPEED));

        Console.Out.WriteLine("{0} finished refueling in {1} seconds.", name, (env.Now - start).TotalSeconds);
      }
    }


    private IEnumerable<Event> Gas_station_control(Environment env, Container fuel_pump) {
      /*
    Periodically check the level of the *fuel_pump* and call the tank
    truck if the level falls below a threshold.
    */
      while (true) {
        if (fuel_pump.Level / fuel_pump.Capacity * 100 < THRESHOLD) {
          // We need to call the tank truck now!
          Console.Out.WriteLine("Calling tank truck at {0}", env.Now);
          // Wait for the tank truck to arrive and refuel the station
          yield return env.Process(Tank_truck(env, fuel_pump));

        }
        yield return env.Timeout(TimeSpan.FromSeconds(10)); // Check every 10 seconds
      }
    }

    private IEnumerable<Event> Tank_truck(Environment env, Container fuel_pump) {
      // Arrives at the gas station after a certain delay and refuels it.
      yield return env.Timeout(TimeSpan.FromMinutes(TANK_TRUCK_TIME));
      Console.Out.WriteLine("Tank truck arriving at time {0}", env.Now);
      var amount = fuel_pump.Capacity - fuel_pump.Level;
      Console.Out.WriteLine("Tank truck refuelling {0} liters.", amount);
      yield return fuel_pump.Put(amount);
    }

    private IEnumerable<Event> Car_generator(Environment env, Resource gas_station, Container fuel_pump) {
      // Generate new cars that arrive at the gas station.
      int i = 0;
      while (true) {
        i++;
        yield return env.Timeout(TimeSpan.FromSeconds(random.Next(MIN_T_INTER, MAX_T_INTER + 1)));
        env.Process(Car("Car " + i, env, gas_station, fuel_pump));
      }
    }

    public void Simulate(int rseed = RANDOM_SEED) {

      // Setup and start the simulation
      Console.Out.WriteLine("== Gas Station refuelling ==");
      random = new Random(rseed);
      // Create environment and start processes
      var env = new Environment();
      var gas_station = new Resource(env, 2);
      var fuel_pump = new Container(env, GAS_STATION_SIZE, GAS_STATION_SIZE);
      env.Process(Gas_station_control(env, fuel_pump));
      env.Process(Car_generator(env, gas_station, fuel_pump));

      // Execute!
      env.Run(SIM_TIME);
    }
  }
}
