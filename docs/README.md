# Sim# Documentation

The documentation covers the following aspects:

* [Introduction](#introduction)
* [Modeling](#modeling)
  + [Processes](#processes)
  + [Resources](#resources)
  + [Samples](#samples)
  + [Environments](#environments)
  + [Distributions](#distributions)
* [Monitoring](#monitoring)
  + [Reports](#reports)


## Introduction

Sim# is available as a [nuget package](https://www.nuget.org/packages/SimSharp/). To use Sim# in your code simply add this package to your .NET project using Visual Studio or the dotnet command line tool:

```
dotnet add package SimSharp
```

It is available for .NET Framework 4.5 and .NET Standard 2.0 and thus can be used with almost any .NET Framework version published since 2012.

## Modeling

Sim# is a _process-based_ discrete event simulation framework. Models are defined by creating *processes* that interact with each other or with shared *resources*. Both processes and resources reference a common *environment* which also contains the simulation time, random number generators, and the event queue.

### Processes

A process is defined by a more or less complex sequence of *events* and is implemented in form of a method. At any time only one process may be active. In this time the process can do calculations and return a next event or finish eventually. A process that returns an event becomes passive and is activated again when the event is processed or when it is interrupted.

The following simple snippet defines a process that outputs the simulation time whenever it is active.

```c#
static IEnumerable<Event> AProcess(Simulation env) {
  env.Log("The time is {0}", env.NowD);
  yield return env.TimeoutD(3.0);
  env.Log("The time is {0}", env.NowD);
  yield return env.TimeoutD(3.0);
  env.Log("The time is {0}", env.NowD);
}

static void Main(string[] args) {
  var env = new Simulation();
  env.Process(AProcess(env));
  env.Run();
}
// outputs:
//The time is 0
//The time is 3
//The time is 6
```

### Resources

Resources in a model describe shared items that processes may compete for or which are used to achieve communication between processes. The standard resources provided by Sim# can be distinguished into three categories:

* *Spectrum*: Discrete or Continuous
* *Mixture*: Homogeneous or Heterogeneous
* *Contract*: Lease or Consume

Discrete resources usually consider a finite number of entities which are represented by a discrete number or instances of an object. On the other hand, continuous resources usually just capture the total quantity in a continuous number.

Continuous resources implicitly assume a single homogeneous entity of varying size. However, discrete resources may either be homogeneous or heterogeneous. In the later case the entities have further, potentially unique properties, while for a homogeneous resource all entities are exactly alike and only their number is of interest.

Finally, the contract category describes whether a certain amount or quantity of the resource is leased and has to be returned or whether it is consumed. For instance, a worker will usually be modelled as being leased, while a warehouse may be a resource that allows stocking and consuming items. 

The following resources are implemented:

* **Resource, PriorityResource, PreemptivePriorityResource** - Discrete, Homogeneous, Lease
* **ResourcePool** - Discrete, Heterogeneous, Lease
* **Store, FilterStore, PriorityStore** - Discrete, Heterogeneous, Consume
* **Container** - Continuous, Homogeneous, Consume

A *Resource* contains a discrete number of homogeneous entities that can be requested and which have to be released back to the resource eventually. It employs a FIFO queue to arrange requests. There exist variants in form of a *PriorityResource* and a *PreemptivePriorityResource* to which requests with a certain priority may be made in both cases. Preemptive resources may additionally retract a processes' lease prematurely. The process that holds the lease is interrupted and has to handle the preemption or fault otherwise.

The *ResourcePool* is similar to the above Resource, but consists of identifiable entities. This type is not part of SimPy and has been introduced in Sim\#. This may be useful, for instance when modeling a pool of employees with their individual characteristics, e.g., qualifications. An entity from the resource pool may only be borrowed for some time and has to be returned. Requests to a ResourcePool may specify a filter to define the properties of the individual to be requested (e.g. some qualification).

A *Store* contains a discrete set of heterogeneous items that can be added to and removed from. Stores may have a maximum capacity. A so called *FilterStore* exists to retrieve items that fulfill some criteria. Store and ResourcePool are very similar, however in a Store the item does not need to return (consume instead of lease). A *PriorityStore* exists in which items have some form of priority and they are consumed in priority order, while still each put and get operation is executed in FIFO order.

A *Container* contains a continuous amount of some substance. Again, the substance may be stocked in the container and consumed.

In any case, it is simple to extend the standard resources given that the code is open source. The classes are of moderate complexity, e.g. the *Resource* class is described in about 200 lines of code.

### Environments

Sim# offers several different simulation environments. The default environment is called `Simulation` and is the fastest. There is also a `ThreadSafeSimulation` which synchronises access to the event queue. This is useful if there is a separate thread that may create processes or which may fire events. It should be noted that the simulation itself is not multi-threaded. At any time there may only be a single process that is active. The "simulation thread" is that thread that called the `Run()` method.

#### Realtime Simulation

Starting with Sim# 3.3 we introduce a `PseudoRealtimeSimulation` environment that derives from `ThreadSafeSimulation`. It will put the simulation thread to sleep for the duration given by the next event in the queue, i.e., a delay of 5 seconds will put the simulation to sleep 5 seconds. An actual realtime guarantee is however not made. In fact, the overhead of the environment, for instance waking up processes, adding and removing from the event queue, and so on are not accounted for. Thus, this may lag behind the actual wall clock time.

However, this environment allows *switching between real and virtual time* and also accepts a realtime scale so that it can be used to perform faster and slower than the actual real time by a certain factor. It offers to method to control the speed:

* `SetVirtualTime()` will process items as fast as `ThreadSafeSimulation`
* `SetRealtime(double scale)` will switch the simulation into realtime mode.

These methods are thread safe and may be called from different threads.

Obtaining the current simulation time using `Now` in realtime mode will account for the duration that the simulation is sleeping. Thus, calling this property from a different thread will always receive the actual simulation time, i.e., the time of the last event plus the elapsed time.

### Distributions
Starting from Sim# 3.4, distributions are included as classes. This allows parameterizing processes with the actual distributions instead of having to predetermine the distribution within the process. For instance, a process that describes a customer arrival can now be specified and instantiated with different distribution types, e.g., exponential, triangular, and others. Creating a distribution is easy and cheap. For best readability, it is advised to include `SimSharp.Distributions` in the using section as a `using static`.

  ```csharp
using SimSharp;
using static SimSharp.Distributions;

namespace MyApplication {
  public class MyModel {
    IEnumerable<Event> CustomerArrival(Simulation env, IDistribution<TimeSpan> arrivalDist) {
      while (true) {
        yield return env.Timeout(arrivalDist);
        // Logic of creating a customer
      }
    }

    public void Run() {
      var env = new Simulation();
      // Exponential distribution with a mean of 5
      env.Process(CustomerArrival(env, EXP(TimeSpan.FromMinutes(5))));
      env.Run(TimeSpan.FromHours(24));
    }
  }
}

### Samples

Processes that interact with common resources may create highly dynamic behavior which may not be analytically tractable and thus have to be simulated. Sim# includes a number of samples that, while being easy to understand and simple, show how to model certain processes such as preemption, interruption, handover of resource requests and more. A short summary of the provided samples together with the highlights are given in the following:

##### [BankRenege](../src/Samples/BankRenege.cs)

- Timeout when waiting for a resource
- Track and print statistics

The bank renege sample uses a single shared resource (bank teller) that customers queue for. There is a single queue and each customer has a certain patience for waiting in the queue. When the patience has run out, the customer will leave the queue without doing business. The queue is not explicitly added to the model, but is part of the Resource that models the bank teller. This resource can automatically track statistics on the queue's length and waiting time.

##### [GasStationRefueling](../src/Samples/GasStationRefueling.cs)

- Combination of discrete (fuel pump) and continuous resource (tank)
- Use of *When*-events to react to resupply of the tank

The gas station refueling sample uses both a discrete (fuel pump) and a continuous (tank) resource. Cars are queuing at the fuel pump and consume gasoline from the tank. When the tank runs below a certain threshold a truck is dispatched to refill it. *When*-events of resources were introduced as part of the Sim# 3.1 release (they are ported from the [desmod](https://desmod.readthedocs.io/en/latest/) package - an extension of SimPy). Note that the Sim# is a discrete event simulation kernel, thus there cannot be a continuous drain from the tank.

##### [KanbanControl](../src/Samples/KanbanControl.cs)

- Usage of Monitors to track variables in user code

The kanban control sample shows how a simple production system that uses kanban can be modeled. It shows how to use monitors to track a variable of interest (the number of kanbans in stock over time).

##### [MachineShop](../src/Samples/MachineShop.cs) and [MachineShopSpecialist](../src/Samples/MachineShopSpecialist.cs)

- Interrupting other processes
- Usage of PreemptivePriorityResource and ResourcePool

In this model a production system with machine break-downs is described. In the event of a break-down the current job is interrupted and suspended until a repairman has been dispatched to fix it. The -Specialist model additionally shows how to use the ResourcePool to model repairman with different characteristics.

##### [MM1Queueing](../src/Samples/MM1Queueing.cs)

- Comparison of analytic with simulated results
- Tracking of statistics
- Reports

The "Hello World" of simulation models. It shows how to perform repetitions and track statistical properties. It also prints the analytical properties of this system - which are known in this case.

##### [ProcessCommunication](../src/Samples/ProcessCommunication.cs)

- Interaction between processes
- Store resource

This model describes a simple producer-consumer situation. It shows how processes may interact with each other using a Store resource.

##### [SimpleShop](../src/Samples/SimpleShop.cs) and [SteelFactory](../src/Samples/SteelFactory.cs)

* Acquiring and releasing a resource in separate processes

These model describe a two-step production. The first step may be blocked by the second. The models should show how one process may obtain a resource, but another processes releases that resource.

##### [PseudoRealTimeSample](../src/Samples/PseudoRealTimeSample.cs)

* Running a process that awaits some timeout in realtime

This model describes a single process that will define a timeout which is awaited in realtime.

## Monitoring

Monitoring has been introduced with Sim# 3.2. Instead of following the SimPy approach which is difficult to translate to .NET. The implementation in Sim# is more akin to [Salabim](https://www.salabim.org).

There are two different kinds of monitors:

1. *SampleMonitor* - is used when the distribution of a variable is characterized by independent samples. For instance, the lead time of a process is such a variable. For each process there is an individual lead time which together form some kind of distribution.
2. *TimeSeriesMonitor* - is used when the variable is a time series, that is, when a variable may change its state over time. For instance, the utilization of a resource may change over time and a weighted average with respect to the duration of each level is required.

Each resource class defines certain variables and the respective type of monitor for that variable. By default no monitoring is tacking place, but such variables may be assigned a monitor and subsequently this will be tracked. In the following snippet a server is created where its *utilization* and the *waiting time* in the queue are tracked:

```c#
var env = new Simulation();
var server = new Resource(env, capacity: 5) {
  Utilization = new TimeSeriesMonitor(env),
  WaitingTime = new SampleMonitor()
};
```

Monitors may be created with ```collect: true```. This means the monitor will keep the datapoints and thus can compute median, percentiles, and print a histogram of the data. By default collect is false, which still allows computing min, max, mean, standard deviation, count, and sum and in a memory efficient way.

As has been mentioned above, the KanbanControl sample shows how to use monitors in the code that describes the model in order to track own variables of interest.

### Reports

A report may be defined that summarizes many monitors and prints the selected statistical properties in one line. A report may be directed to a different file and may be performed only at the end, for every update to the statistic or periodically. This enables a quick way to follow some basic statistics in the execution of a model.

Examples of how to use reports are present in the [MM1Queueing](../src/Samples/MM1Queueing.cs) sample:

```c#
var report = Report.CreateBuilder(env)
  .Add("Utilization", utilization, Report.Measures.Mean | Report.Measures.StdDev)
  .Add("WIP", wip, Report.Measures.Min | Report.Measures.Mean | Report.Measures.Max)
  .Add("Leadtime", leadtime, Report.Measures.Min | Report.Measures.Mean | Report.Measures.Max)
  .Add("WaitingTime", waitingtime, Report.Measures.Min | Report.Measures.Mean | Report.Measures.Max)
  .SetOutput(env.Logger) // use a "new StreamWriter("report.csv")" to direct to a file
  .SetSeparator("\t")
  .SetPeriodicUpdate(TimeSpan.FromDays(7), withHeaders: true)
  .Build();
```

