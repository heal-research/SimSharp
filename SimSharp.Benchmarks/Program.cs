using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace SimSharp.Benchmarks {
  class Program {
    private static long perf = 0;
    private static Event terminate;
    private const int time = 3000;
    private const long CPU_FRQ = 2530957000; // Hz

    private static void Timeout(object sender, ElapsedEventArgs e) {
      terminate.Succeed();
    }

    static void Main(string[] args) {
      var frqMod = CPU_FRQ / (double)Stopwatch.Frequency;

      foreach (var n in new[] { 1, 10, 100, 1000, 10000 }) {
        var env = new SimSharp.Environment();
        terminate = new Event(env);
        var clk = new Timer(time);
        clk.Elapsed += Timeout;
        clk.Start();
        var elapsedTicks = Benchmark1(env, n);
        clk.Stop();
        clk.Elapsed -= Timeout;
        Console.WriteLine("Benchmark 1 (n = {0,6:##,0}): {1,7:0,0} clock cycles ({2,10:0,0} entities / s)", n, frqMod * elapsedTicks / perf, 1000 * perf / (double)time);
      }

      {
        var env = new SimSharp.Environment();
        terminate = new Event(env);
        var clk = new Timer(time);
        clk.Elapsed += Timeout;
        clk.Start();
        var elapsedTicks = Benchmark2(env);
        clk.Stop();
        clk.Elapsed -= Timeout;
        Console.WriteLine("Benchmark 2: {0,20:0,0} clock cycles ({1,10:0,0} entities / s)",
          frqMod * elapsedTicks / perf, 1000 * perf / (double)time);
      }

      {
        var env = new SimSharp.Environment();
        terminate = new Event(env);
        var clk = new Timer(time);
        clk.Elapsed += Timeout;
        clk.Start();
        var elapsedTicks = Benchmark3(env);
        clk.Stop();
        clk.Elapsed -= Timeout;
        Console.WriteLine("Benchmark 3: {0,20:0,0} clock cycles ({1,10:0,0} entities / s)",
          frqMod * elapsedTicks / perf, 1000 * perf / (double)time);
      }
      Console.WriteLine();
      Console.WriteLine("CPU Frequency: {0:0,0} (must be changed in code)", CPU_FRQ);
      Console.WriteLine("Stopwatch Frequency = {0:0,0}", Stopwatch.Frequency);
    }

    /// <summary>
    /// This method will benchmark Sim#'s performance with respect to the list
    /// of future events. A large number of processes that exist in the system
    /// stress tests the performance of operations on the event queue.
    /// </summary>
    /// <param name="n">The number of concurrent processes.</param>
    static long Benchmark1(Environment env, int n) {
      perf = 0;
      for (var i = 0; i < n; i++) {
        env.Process(Benchmark1Proc(env, n));
      }
      var watch = Stopwatch.StartNew();
      env.Run(terminate);
      watch.Stop();
      return watch.ElapsedTicks;
    }

    static IEnumerable<Event> Benchmark1Proc(Environment env, int n) {
      while (true) {
        yield return env.TimeoutUniform(TimeSpan.Zero, TimeSpan.FromSeconds(2 * n));
        perf++;
      }
    }

    /// <summary>
    /// This method will benchmark Sim#'s performance with respect to creation
    /// of entities. In SimPy and also Sim# the equivalence of an entity is a
    /// process. This stress tests the performance of creating processes.
    /// </summary>
    static long Benchmark2(Environment env) {
      perf = 0;
      env.Process(Benchmark2Source(env));
      var watch = Stopwatch.StartNew();
      env.Run(terminate);
      watch.Stop();
      return watch.ElapsedTicks;
    }

    static IEnumerable<Event> Benchmark2Source(Environment env) {
      while (true) {
        yield return env.Process(Benchmark2Sink(env));
        perf++;
      }
    }

    static IEnumerable<Event> Benchmark2Sink(Environment env) {
      yield break;
    }

    /// <summary>
    /// This method will benchmark Sim#'s performance with respect to
    /// seizing and releasing resources, a common task in DES models.
    /// </summary>
    static long Benchmark3(Environment env) {
      perf = 0;
      var res = new Resource(env, capacity: 1);
      env.Process(Benchmark3Proc(env, res));
      env.Process(Benchmark3Proc(env, res));
      var watch = Stopwatch.StartNew();
      env.Run(terminate);
      watch.Stop();
      return watch.ElapsedTicks;
    }

    static IEnumerable<Event> Benchmark3Proc(Environment env, Resource resource) {
      while (true) {
        var req = resource.Request();
        yield return req;
        yield return resource.Release(req);
        perf++;
      }
    }
  }
}
