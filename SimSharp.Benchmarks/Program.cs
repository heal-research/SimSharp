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
using System.Diagnostics;
using System.Globalization;
using System.Timers;

namespace SimSharp.Benchmarks {
  class Program {
    private static long perf = 0;
    private static Event terminate;

    private static void Timeout(object sender, ElapsedEventArgs e) {
      terminate.Succeed();
    }

    static void Main(string[] args) {
      var time = -1.0;
      var repetitions = -1;
      var CPU_FRQ = 0L;
      string input;
      do {
        Console.Write("How many repetitions [3]: ");
        input = Console.ReadLine().Trim();
        if (input == string.Empty) repetitions = 3;
        else int.TryParse(input, NumberStyles.None, CultureInfo.CurrentCulture.NumberFormat, out repetitions);
      } while (repetitions < 1);

      do {
        Console.Write("How many seconds per repetition [60]: ");
        input = Console.ReadLine().Trim();
        if (input == string.Empty) time = 60;
        else double.TryParse(input, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture.NumberFormat, out time);
      } while (time <= 0);

      do {
        Console.Write("Please specify your CPU frequency in GHz [{0:0.0}]: ", 2.6);
        input = Console.ReadLine().Trim();
        if (input == string.Empty) CPU_FRQ = 2600000000;
        else {
          double ghz;
          if (double.TryParse(input,
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowThousands,
            CultureInfo.CurrentCulture.NumberFormat, out ghz))
            CPU_FRQ = (long)Math.Round(ghz * 1E9);
        }
      } while (CPU_FRQ <= 0);

      Console.WriteLine();
      Console.WriteLine("== Starting Benchmark: {0} repetitions for {1:##,###}ms each. CPU clock frequency is {2:##,###}Hz ==", repetitions, (int)(time * 1000), CPU_FRQ);
      Console.WriteLine();
      var frqMod = CPU_FRQ / (double)Stopwatch.Frequency;

      var sumTime = 0.0;
      var sumPerf = 0.0;
      foreach (var n in new[] { 1, 10, 100, 1000, 10000 }) {
        for (var r = 0; r < repetitions; r++) {
          var env = new SimSharp.Environment();
          terminate = new Event(env);
          var clk = new Timer((int)(time * 1000));
          clk.Elapsed += Timeout;
          clk.Start();
          sumTime += Benchmark1(env, n);
          sumPerf += perf;
          clk.Stop();
          clk.Elapsed -= Timeout;
        }
        sumTime /= repetitions;
        sumPerf /= repetitions;
        Console.WriteLine("Benchmark 1 (n = {0,6:##,0}): {1,7:0,0} clock cycles ({2,10:0,0} entities / s)", n, frqMod * sumTime / sumPerf, Stopwatch.Frequency * sumPerf / sumTime);
      }

      sumTime = 0.0;
      sumPerf = 0.0;
      for (var r = 0; r < repetitions; r++) {
        var env = new SimSharp.Environment();
        terminate = new Event(env);
        var clk = new Timer((int)(time * 1000));
        clk.Elapsed += Timeout;
        clk.Start();
        sumTime += Benchmark2(env);
        sumPerf += perf;
        clk.Stop();
        clk.Elapsed -= Timeout;
      }
      sumTime /= repetitions;
      sumPerf /= repetitions;
      Console.WriteLine("Benchmark 2: {0,20:0,0} clock cycles ({1,10:0,0} entities / s)", frqMod * sumTime / sumPerf, Stopwatch.Frequency * sumPerf / sumTime);

      sumTime = 0.0;
      sumPerf = 0.0;
      for (var r = 0; r < repetitions; r++) {
        var env = new SimSharp.Environment();
        terminate = new Event(env);
        var clk = new Timer((int)(time * 1000));
        clk.Elapsed += Timeout;
        clk.Start();
        sumTime += Benchmark3(env);
        sumPerf += perf;
        clk.Stop();
        clk.Elapsed -= Timeout;
      }
      sumTime /= repetitions;
      sumPerf /= repetitions;
      Console.WriteLine("Benchmark 3: {0,20:0,0} clock cycles ({1,10:0,0} entities / s)", frqMod * sumTime / sumPerf, Stopwatch.Frequency * sumPerf / sumTime);
      Console.WriteLine();
      Console.WriteLine("== Finished Benchmark ==");
      Console.ReadLine();
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
