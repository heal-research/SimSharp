![SimSharp Logo](docs/SimSharp_s.png)

Sim# (SimSharp) is a .NET port and extension of SimPy, process-based discrete event simulation framework

[![Build status](https://ci.appveyor.com/api/projects/status/hyn83qegeiga81o2/branch/master?svg=true)](https://ci.appveyor.com/project/abeham/simsharp/branch/master)

---

*Disclaimer: Sim# is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. Sim# is free software: Sim# can be redistributed and/or modified under the terms of the MIT License.*

---

This is a short introduction, please refer to the [documentation](docs/README.md) for more information on working with Sim# and an overview of the sample models provided.

Sim# aims to port the concepts used in SimPy [1] to the .NET world. Sim# is implemented in C# and is available via Nuget for .NET Framework 4.5 and is also .NET Standard 2.0 compliant. Sim# uses an efficient event queue (adapted from [3]) that allows to compute models very quickly. Simulating 10 years of the MachineShop sample [4], that uses preemptive resources, requires less than 1.5s on a Core i7-7 2.7Ghz. This model generates more than 5 million events.

Sim# allows modeling processes easily and with little boiler plate code. A process is described as a method that yields events. When an event is yielded, the process waits on it. Processes are themselves events and so it is convenient to spawn sub-processes that can either be waited upon or that run next to each other. There is no need to inherit from classes or understand a complex object oriented design.

To demonstrate how simple models can be expressed with little code, consider a model of an m/m/1 queuing system as expressed in the current version of Sim#:

```csharp
using static SimSharp.Distributions;

ExponentialTime ARRIVAL = EXP(TimeSpan.FromSeconds(...));
ExponentialTime PROCESSING = EXP(TimeSpan.FromSeconds(...));
TimeSpan SIMULATION_TIME = TimeSpan.FromHours(...);

IEnumerable<Event> MM1Q(Simulation env, Resource server) {
  while (true) {
    yield return env.Timeout(ARRIVAL);
    env.Process(Item(env, server));
  }
}

IEnumerable<Event> Item(Simulation env, Resource server) {
  using (var s = server.Request()) {
    yield return s;
    yield return env.Timeout(PROCESSING);
    Console.WriteLine("Duration {0}", env.Now - s.Time);
  }
}

void RunSimulation() {
  var env = new Simulation(randomSeed: 42);
  var server = new Resource(env, capacity: 1) {
    QueueLength = new TimeSeriesMonitor(env, collect: true)
  };
  env.Process(MM1Q(env, server));
  env.Run(SIMULATION_TIME);
  Console.WriteLine(server.QueueLength.Summarize());
}
```

This model uses the monitoring capabilities introduced with Sim# 3.2. Monitoring allows describing certain variables in the model, e.g. the number of waiting items before the server. The monitor maintains a set of statistical properties such as mean, standard deviation, and can also print histograms. Monitors can be assigned to the variables exposed by resources or they can be used to track other variables.

Sim# tries to be as easy to use as SimPy, but also remains true to the .NET Framework. The most obvious difference between SimPy and Sim# is handling process interruptions. In Sim# a process that can be interrupted needs to call

  ```csharp
if (Environment.ActiveProcess.HandleFault()) {...}
  ```

after each yield in which an interruption can occur and before continuing to yield further events. This is due to a limitation of the .NET Framework: In Python it is possible to put a try-except block around a yield statement, and an exception can be injected into the iterator. In .NET this is not possible.

Also in Sim# it was decided to base the unit for current time and delays on `DateTime` and `TimeSpan` in the simulation clock. There is however an API, called D-API (short for double-API) that allows you to use doubles as in SimPy, e.g. `env.Now` returns a `DateTime`, `env.NowD` returns a `double`, `env.Timeout(delay`) expects a `TimeSpan` as delay, `env.TimeoutD(delay)` expects a `double`, etc.. It is possible to initialize the Environment with a default timestep in case both APIs are used:

  ```csharp
var env = new Simulation(defaultStep: TimeSpan.FromMinutes(1));
  ```

In that environment, calling `env.TimeoutD(1)` would be equal to calling the more elaborate standard API `env.Timeout(TimeSpan.FromMinutes(1))`. In case timeouts are sampled from a distribution, it is important to distinguish the `TimeoutD(IDistribution<double>)`and `Timeout(IDistribution<TimeSpan>)` methods. Again, the former assumes the unit that is given in `defaultStep`, e.g., minutes as in the case above. For instance, `env.TimeoutD(new Exponential(2))` would indicate a mean of 2 minutes in the above environment, while `env.Timeout(new Exponential(TimeSpan.FromMinutes(2))` would always mean two minutes, regardless of the `defaultStep`. In generally, the `TimeSpan` API is preferred as it already expresses time in the appropriate units.

For shortcuts of the distribution classes a static class `Distributions` exists. You can put `using static SimSharp.Distributions;` in the using declarations and then use those methods without a qualifier. The following code snippet shows this feature.

  ```csharp
using static SimSharp.Distributions;
// ... additional code excluded
yield return env.TimeoutD(UNIF(10, 20));
  ```

## References

1. [Python Simpy Package](https://pypi.python.org/pypi/simpy)
2. [Nuget package](https://www.nuget.org/packages/SimSharp/)
3. [High speed priority queue](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp)
4. [Machine Shop Example](src/Samples/MachineShop.cs)
