#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using CommandLine;

namespace SimSharp.Benchmarks {
  [Verb("machineshop", HelpText = "Benchmark the machine shop sample running 10 years.")]
  public class MachineShopOptions { }

  [Verb("synthetic", HelpText = "Benchmark synthetic cases representing various common simulation tasks.")]
  public class SyntheticOptions {
    [Option(shortName: 'r', longName: "repetitions", Default = 3, HelpText = "Repetitions for the synthetic benchmark")]
    public int Repetitions { get; set; }
    [Option(shortName: 't', longName: "time", Default = 60, HelpText = "Runtime of each repetition in seconds")]
    public double Time { get; set; }
    [Option(shortName: 'c', longName: "cpufreq", Default = 2.6, HelpText = "CPU Frequence in Ghz")]
    public double CpuFreq { get; set; }
  }

  class Program {
    static int Main(string[] args) {
      return Parser.Default.ParseArguments<MachineShopOptions, SyntheticOptions>(args)
        .MapResult(
          (MachineShopOptions opts) => MachineShopBenchmark.Run(opts),
          (SyntheticOptions opts) => SyntheticBenchmark.Run(opts),
          errs => 1);
    }
  }
}
