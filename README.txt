SimSharp
========

A .NET port of SimPy, discrete event simulation framework

SimSharp aims to port the concepts used in SimPy [1] to the .NET world.
It is implemented in C# and builds on the .NET Framework 4.0.

SimPy allows to model processes easily and with little boiler plate code.
A process that is modeled can be a simple method that returns events.
Waiting on an event is as simple as yielding it, while new processes may
be spawned that run next to each other.

To demonstrate how simple models can be expressed with little code,
consider a production facility that has two machines and one person after
the machines who grabs the products and puts each of them into a crate.

private static Random random = new Random();
private static Resource packer;
private static Environment env;
private static TimeSpan delay = TimeSpan.Zero;

private static IEnumerable<Event> Produce() {
  while (true) {
    var processingTimeSec = RandomDist.Normal(random, 20, 5);
    while (processingTimeSec > 0) {
      yield return env.Timeout(TimeSpan.FromSeconds(processingTimeSec));
      var token = packer.Request();
      yield return token;
      delay += env.Now - token.Time;
      env.Process(Pack(token));
    }
  }
}

private static IEnumerable<Event> Pack(Request token) {
  var packingTimeSec = RandomDist.Normal(random, 12, 5);
  while (packingTimeSec > 0) {
    yield return env.Timeout(TimeSpan.FromSeconds(packingTimeSec));
    packer.Release(token);
  }
}

public static void Main(string[] args) {
  env = new Environment();
  packer = new Resource(env, 1);
  env.Process(Produce());
  env.Process(Produce());
  env.Run(TimeSpan.FromHours(8));
  Console.WriteLine("The machines were delayed for {0}", delay);
}

SimSharp tries to be as easy to use as SimPy, but also remains true
to the .NET Framework. The most obvious difference between SimPy
and SimSharp is handling process interruptions. In SimSharp a process
that can be interrupted needs to call

  if (Environment.ActiveProcess.HandleFault()) {...}
  
before continuing to yield further events.
There is no support for SimSharp, it must be used at your own risk.

[1]: https://pypi.python.org/pypi/simpy
