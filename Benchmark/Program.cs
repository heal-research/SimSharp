#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2018  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
