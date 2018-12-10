# Sim# (SimSharp)

A .NET port of SimPy, process-based discrete event simulation framework

[![Build status](https://ci.appveyor.com/api/projects/status/hyn83qegeiga81o2/branch/master?svg=true)](https://ci.appveyor.com/project/abeham/simsharp/branch/master)

---

*Disclaimer: Sim# is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. Sim# is free software: you can redistribute it and/or modify it under the terms of the  GNU General Public License (GPL) as published by the Free Software Foundation, either version 3 of the license, or (at your option) any later version.*

---


Sim# aims to port the concepts used in SimPy [1] to the .NET world. Sim# is implemented in C# and is available via Nuget for .NET Framework 4.5 and is also .NET Standard 2.0 compliant. Sim# uses an efficient event queue (adapted from [3]) that allows to compute models very quickly. Simulating 10 years of the MachineShop sample [4], that uses preemptive resources, requires only 2.5s on a Core i7 2.6Ghz. This model generates more than 5 million events.

SimPy allows to model processes easily and with little boiler plate code. A process is described as a method that yields events. When an event is yielded, the process waits on it. Processes are themselves events and so it is convenient to spawn sub-processes that can either be waited upon or that run next to each other. There is no need to inherit from classes or understand a complex object oriented
design.

To demonstrate how simple models can be expressed with little code, consider a model of an m/m/1 queuing system as expressed in Sim#:

```csharp
TimeSpan ARRIVAL_TIME = TimeSpan.FromSeconds(...);
TimeSpan PROCESSING_TIME = TimeSpan.FromSeconds(...);
TimeSpan SIMULATION_TIME = TimeSpan.FromHours(...);

IEnumerable<Event> MM1Q(Simulation env) {
  var server = new Resource(env, capacity: 1);
  while (true) {
    yield return env.TimeoutExponential(ARRIVAL_TIME);
    env.Process(Item(env, server));
  }
}

IEnumerable<Event> Item(Simulation env, Resource server) {
  using (var s = server.Request()) {
    yield return s;
    yield return env.TimeoutExponential(PROCESSING_TIME);
    Console.WriteLine("Duration {0}", env.Now - s.Time);
  }
}

void RunSimulation() {
  var env = new Simulation(randomSeed: 42);
  env.Process(MM1Q(env));
  env.Run(SIMULATION_TIME);
}
```

Sim# tries to be as easy to use as SimPy, but also remains true to the .NET Framework. The most obvious difference between SimPy and Sim# is handling process interruptions. In Sim# a process that can be interrupted needs to call

  ```csharp
if (Environment.ActiveProcess.HandleFault()) {...}
```

after each yield in which an interruption can occur and before continuing to yield further events. This is due to a limitation of the .Net Framework: In Python it is possible to put a try-except block around a yield statement, and an exception can be injected into the iterator. In .Net this is not possible.

Also in Sim# it was decided to base the unit for current time and delays on `DateTime` and `TimeSpan` in the simulation clock. There is however an API, called D-API (short for double-API) that allows you to use doubles as in SimPy, e.g. `env.Now` returns a `DateTime`, `env.NowD` returns a `double`, `env.Timeout(delay`) expects a `TimeSpan` as delay, `env.TimeoutD(delay)` expects a `double`, etc.. It is possible to initialize the Environment with a default timestep in case both APIs are used:

  ```csharp
var env = new Simulation(defaultStep: TimeSpan.FromMinutes(1));
```

In that environment, calling `env.TimeoutD(1)` would be equal to calling the more elaborate normal API `env.Timeout(TimeSpan.FromMinutes(1))`.

## References

1. [Python Simpy Package](https://pypi.python.org/pypi/simpy)
2. [Nuget package](https://www.nuget.org/packages/SimSharp/)
3. [High speed priority queue](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp)
4. [Machine Shop Example](http://simpy.readthedocs.org/en/latest/examples/machine_shop.html)
