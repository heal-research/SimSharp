Sim# (SimSharp)
========

A .NET port of SimPy, discrete event simulation framework

Disclaimer:
Sim# is distributed in the hope that it will be useful, but WITHOUT
ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
FITNESS FOR A PARTICULAR PURPOSE.
Sim# is free software: you can redistribute it and/or modify it under
the terms of the  GNU General Public License (GPL) as published by the
Free Software Foundation, either version 3 of the license, or (at your
option) any later version. 

Sim# aims to port the concepts used in SimPy [1] to the .NET world.
It is implemented in C# and builds on the .NET Framework 4.0.

SimPy allows to model processes easily and with little boiler plate code.
A process that is modeled can be a simple method that returns events.
Waiting on an event is as simple as yielding it, while new processes may
be spawned that run next to each other.

To demonstrate how simple models can be expressed with little code,
consider a production facility that has two machines and one person after
the machines who grabs the products and puts each of them into a crate.
The machine cannot continue until the person is available to grab the
product. The total waiting time of the machine is recorded.

private static TimeSpan delay = TimeSpan.Zero;

private static IEnumerable<Event> Machine(Environment env, Resource packer) {
  while (true) {
    var procTimeSec = env.RandNormalPositive(20, 5);
    yield return env.Timeout(TimeSpan.FromSeconds(procTimeSec));
    var token = packer.Request();
    yield return token;
    delay += env.Now - token.Time;
    env.Process(Pack(env, packer, token));
  }
}

private static IEnumerable<Event> Pack(Environment env, Resource packer, Request token) {
  var packTimeSec = env.RandNormalPositive(10, 2);
  yield return env.Timeout(TimeSpan.FromSeconds(packTimeSec));
  packer.Release(token);
}

public static void Main(string[] args) {
  var env = new Environment(randomSeed: 41);
  var packer = new Resource(env, 1);
  env.Process(Machine(env, packer));
  env.Process(Machine(env, packer));
  env.Run(TimeSpan.FromHours(8));
  Console.WriteLine("The machines were delayed for {0}", delay);
}

Sim# tries to be as easy to use as SimPy, but also remains true
to the .NET Framework. The most obvious difference between SimPy
and Sim# is handling process interruptions. In Sim# a process
that can be interrupted needs to call

  if (Environment.ActiveProcess.HandleFault()) {...}
  
before continuing to yield further events.


[1]: https://pypi.python.org/pypi/simpy
