#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2019  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
using System.Linq;
using Xunit;

namespace SimSharp.Tests {
  public class AnalysisTest {

    [Theory]
    [InlineData(new double[] { 0, 1, 1, 1, 1, 1 }, new double[] { 6, 2, 3, 5, -1, -4 }, -4, 6, 3, 6, 15)]
    [InlineData(new double[] { 0, 10, 0, 1, 1, 1, 1 }, new double[] { 0, 6, 2, 3, 5, -1, -4 }, -4, 6, 0.64285714285714, 2.37244897959184, 9)]
    [InlineData(new double[] { 1, 1, 1, 2, 0, 0, 4, 7, 4 }, new double[] { 3, -2, 5, 6, -4, 1, 0, -2, 3 }, -4, 6, 0.3684210526315789, 4.232686980609418, 7)]
    public void TestContinuousStatisticsSimple(double[] times, double[] values, double min, double max,
      double mean, double variance, double area) {
      var env = new Simulation();
      var stat = new TimeSeriesMonitor(env);
      foreach (var v in times.Zip(values, Tuple.Create)) {
        if (v.Item1 > 0) env.RunD(v.Item1);
        stat.UpdateTo(v.Item2);
      }
      Assert.Equal(min, stat.Min);
      Assert.Equal(max, stat.Max);
      Assert.Equal(mean, stat.Mean, 14);
      Assert.Equal(variance, stat.Variance, 14);
      Assert.Equal(area, stat.Area);

      stat.Reset();
      foreach (var v in times.Zip(values, Tuple.Create)) {
        if (v.Item1 > 0) env.RunD(v.Item1);
        stat.UpdateTo(v.Item2);
      }
      Assert.Equal(min, stat.Min);
      Assert.Equal(max, stat.Max);
      Assert.Equal(mean, stat.Mean, 14);
      Assert.Equal(variance, stat.Variance, 14);
      Assert.Equal(area, stat.Area);
    }

    [Fact]
    public void TestContinuousStatisticsAutoUpdate() {
      var env = new Simulation();
      var stat = new TimeSeriesMonitor(env);
      env.Process(StatProcess(env, stat));
      env.Run();

      stat.Reset();
      env.Process(StatProcess(env, stat));
      env.Run();
    }

    private IEnumerable<Event> StatProcess(Simulation env, TimeSeriesMonitor stat) {
      stat.UpdateTo(3);
      yield return env.TimeoutD(1);
      stat.UpdateTo(1);
      yield return env.TimeoutD(1);
      Assert.Equal(3, stat.Max);
      Assert.Equal(2, stat.Mean);
      Assert.Equal(1, stat.Current);
      Assert.Equal(4, stat.Area);
      Assert.Equal(2, stat.TotalTimeD);
      Assert.Equal(1, stat.Variance);
    }

    [Theory]
    [InlineData(new double[] {  1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
    [InlineData(new double[] { 10, 9, 8, 7, 6, 5, 4, 3, 2,  1 })]
    [InlineData(new double[] { -1, -2, -3, -4, -5, 6, 7, 8, 9, 10 })]
    [InlineData(new double[] { -10, -9, -8, -7, -6, -5, 4, 3, 2, 1 })]
    [InlineData(new double[] { 0 })]
    [InlineData(new double[] { 9 })]
    [InlineData(new double[] { 0, 0 })]
    [InlineData(new double[] { 5, 5, 5, 5, 5 })]
    [InlineData(new double[] { 0, 0, 5, 0, 0 })]
    [InlineData(new double[] { 0, 1, double.NaN, 2 })]
    [InlineData(new double[] { 0, 1, double.PositiveInfinity, 2 })]
    [InlineData(new double[] { 0, 1, double.NegativeInfinity, 2 })]
    public void TestDiscreteStatistics(IEnumerable<double> data) {
      var stat = new SampleMonitor();
      var data_list = data.ToList();
      if (data_list.All(x => !double.IsNaN(x) && !double.IsInfinity(x))) {
        foreach (var d in data_list) stat.Add(d);
        var avg = data_list.Average();
        var sum = data_list.Sum();
        var cnt = data_list.Count;
        var min = data_list.Min();
        var max = data_list.Max();
        var pvar = 0.0;
        foreach (var d in data_list) pvar += (d - avg) * (d - avg);
        pvar /= cnt;
        Assert.Equal(avg, stat.Mean);
        Assert.Equal(sum, stat.Total);
        Assert.Equal(cnt, stat.Count);
        Assert.Equal(min, stat.Min);
        Assert.Equal(max, stat.Max);
        Assert.Equal(pvar, stat.Variance, 14);

        stat.Reset();
        foreach (var d in data_list) stat.Add(d);
        Assert.Equal(avg, stat.Mean);
        Assert.Equal(sum, stat.Total);
        Assert.Equal(cnt, stat.Count);
        Assert.Equal(min, stat.Min);
        Assert.Equal(max, stat.Max);
        Assert.Equal(pvar, stat.Variance, 14);
      } else {
        Assert.Throws<ArgumentException>(() => {
          foreach (var d in data_list) stat.Add(d);
        });
      }
    }
  }
}
