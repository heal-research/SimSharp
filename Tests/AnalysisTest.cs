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
    [InlineData(new double[] { 3, 2, 5, 0, 1, 1, 1, 1, 1, 3, 2 }, new double[] { 10, 100, -100, 6, 2, 3, 5, -1, -4, 2, 1 }, -4, 6, 3, 6, 15, 3)]
    [InlineData(new double[] { 1, 3, 1, 0, 10, 0, 1, 1, 1, 1, 3, 2 }, new double[] { 1, 0, 1, 0, 6, 2, 3, 5, -1, -4, 2, 1 }, -4, 6, 0.64285714285714, 2.37244897959184, 9, 0)]
    [InlineData(new double[] { 1, 1, 10, 1, 1, 1, 2, 0, 0, 4, 7, 4, 3, 2 }, new double[] { -1, -1, -1, 3, -2, 5, 6, -4, 1, 0, -2, 3, 2, 1 }, -4, 6, 0.3684210526315789, 4.232686980609418, 7, 0)]
    public void TestTimeSeriesMonitor(double[] times, double[] values, double min, double max,
      double mean, double variance, double area, double median) {
      var env = new Simulation();
      var stat = new TimeSeriesMonitor(env, collect: false) { Active = false };
      var stat_collect = new TimeSeriesMonitor(env, collect: true) { Active = false };
      var count = 0;
      foreach (var v in times.Zip(values, Tuple.Create)) {
        if (v.Item1 > 0) env.RunD(v.Item1);
        if (count == 3) { stat.Active = stat_collect.Active = true; }
        stat.UpdateTo(v.Item2);
        stat_collect.UpdateTo(v.Item2);
        if (count == times.Length - 3) { stat.Active = stat_collect.Active = false; }
        count++;
      }
      Assert.Equal(min, stat.Min);
      Assert.Equal(max, stat.Max);
      Assert.Equal(mean, stat.Mean, 14);
      Assert.Equal(variance, stat.Variance, 14);
      Assert.Equal(area, stat.Area);
      Assert.True(double.IsNaN(stat.GetMedian()));
      Assert.True(double.IsNaN(stat.GetPercentile(0.25)));
      Assert.True(double.IsNaN(stat.GetPercentile(0.75)));
      Assert.Empty(stat.Series);
      Assert.Equal(min, stat_collect.Min);
      Assert.Equal(max, stat_collect.Max);
      Assert.Equal(mean, stat_collect.Mean, 14);
      Assert.Equal(variance, stat_collect.Variance, 14);
      Assert.Equal(area, stat_collect.Area);
      Assert.Equal(median, stat_collect.GetMedian());
      Assert.True(stat_collect.GetPercentile(0.25) <= median);
      Assert.True(stat_collect.GetPercentile(0.75) >= median);
      Assert.Equal(values.Length - 5, stat_collect.Series.Count());

      stat.Reset();
      Assert.False(stat.Active);
      stat_collect.Reset();
      Assert.False(stat_collect.Active);
      count = 0;
      foreach (var v in times.Zip(values, Tuple.Create)) {
        if (v.Item1 > 0) env.RunD(v.Item1);
        if (count == 3) { stat.Active = stat_collect.Active = true; }
        stat.UpdateTo(v.Item2);
        stat_collect.UpdateTo(v.Item2);
        if (count == times.Length - 3) { stat.Active = stat_collect.Active = false; }
        count++;
      }
      Assert.Equal(min, stat.Min);
      Assert.Equal(max, stat.Max);
      Assert.Equal(mean, stat.Mean, 14);
      Assert.Equal(variance, stat.Variance, 14);
      Assert.Equal(area, stat.Area);
      Assert.True(double.IsNaN(stat.GetMedian()));
      Assert.True(double.IsNaN(stat.GetPercentile(0.25)));
      Assert.True(double.IsNaN(stat.GetPercentile(0.75)));
      Assert.Empty(stat.Series);
      Assert.Equal(min, stat_collect.Min);
      Assert.Equal(max, stat_collect.Max);
      Assert.Equal(mean, stat_collect.Mean, 14);
      Assert.Equal(variance, stat_collect.Variance, 14);
      Assert.Equal(area, stat_collect.Area);
      Assert.Equal(median, stat_collect.GetMedian());
      Assert.True(stat_collect.GetPercentile(0.25) <= median);
      Assert.True(stat_collect.GetPercentile(0.75) >= median);
      Assert.Equal(values.Length - 5, stat_collect.Series.Count());
    }

    [Fact]
    public void TestTimeSeriesMonitorAutoUpdate() {
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
    [InlineData(new double[] { 5, 5,  1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
    [InlineData(new double[] { 5, 5, 10, 9, 8, 7, 6, 5, 4, 3, 2,  1 })]
    [InlineData(new double[] { 5, 5, -1, -2, -3, -4, -5, 6, 7, 8, 9, 10 })]
    [InlineData(new double[] { 5, 5, -10, -9, -8, -7, -6, -5, 4, 3, 2, 1 })]
    [InlineData(new double[] { 5, 5, 0 })]
    [InlineData(new double[] { 5, 5, 9 })]
    [InlineData(new double[] { 5, 5, 0, 0 })]
    [InlineData(new double[] { 5, 5, 5, 5, 5, 5, 5 })]
    [InlineData(new double[] { 5, 5, 0, 0, 5, 0, 0 })]
    [InlineData(new double[] { 5, 5, 0, 1, double.NaN, 2 })]
    [InlineData(new double[] { 5, 5, 0, 1, double.PositiveInfinity, 2 })]
    [InlineData(new double[] { 5, 5, 0, 1, double.NegativeInfinity, 2 })]
    public void TestSampleMonitor(IEnumerable<double> data) {
      var stat = new SampleMonitor(collect: false) { Active = false };
      var stat_collect = new SampleMonitor(collect: true) { Active = false };
      var data_list = data.ToList();
      if (data_list.All(x => !double.IsNaN(x) && !double.IsInfinity(x))) {
        var count = 0;
        foreach (var d in data_list) {
          if (count == 2) { stat.Active = stat_collect.Active = true; }
          stat.Add(d); stat_collect.Add(d);
          count++;
        }
        var avg = data_list.Skip(2).Average();
        var sum = data_list.Skip(2).Sum();
        var cnt = data_list.Skip(2).Count();
        var min = data_list.Skip(2).Min();
        var max = data_list.Skip(2).Max();
        var pvar = 0.0;
        foreach (var d in data_list.Skip(2)) pvar += (d - avg) * (d - avg);
        pvar /= cnt;
        var med = data_list.Skip(2).Count() % 2 == 1 ? data_list.Skip(2).OrderBy(x => x).Skip(data_list.Skip(2).Count() / 2 - 1).First()
          : data_list.Skip(2).OrderBy(x => x).Skip(data_list.Skip(2).Count() / 2 - 1).Take(2).Average();
        Assert.Equal(avg, stat.Mean);
        Assert.Equal(sum, stat.Total);
        Assert.Equal(cnt, stat.Count);
        Assert.Equal(min, stat.Min);
        Assert.Equal(max, stat.Max);
        Assert.Equal(pvar, stat.Variance, 14);
        Assert.True(double.IsNaN(stat.GetMedian()));
        Assert.Empty(stat.Samples);
        Assert.Equal(avg, stat_collect.Mean);
        Assert.Equal(sum, stat_collect.Total);
        Assert.Equal(cnt, stat_collect.Count);
        Assert.Equal(min, stat_collect.Min);
        Assert.Equal(max, stat_collect.Max);
        Assert.Equal(pvar, stat_collect.Variance, 14);
        Assert.Equal(med, stat_collect.GetMedian());
        Assert.Equal(data_list.Skip(2).Count(), stat_collect.Samples.Count());

        stat.Active = false;
        stat.Reset();
        Assert.False(stat.Active);
        stat_collect.Active = false;
        stat_collect.Reset();
        Assert.False(stat_collect.Active);

        count = 0;
        foreach (var d in data_list) {
          if (count == 2) { stat.Active = stat_collect.Active = true; }
          stat.Add(d); stat_collect.Add(d);
          count++;
        }
        Assert.Equal(avg, stat.Mean);
        Assert.Equal(sum, stat.Total);
        Assert.Equal(cnt, stat.Count);
        Assert.Equal(min, stat.Min);
        Assert.Equal(max, stat.Max);
        Assert.Equal(pvar, stat.Variance, 14);
        Assert.True(double.IsNaN(stat.GetMedian()));
        Assert.Empty(stat.Samples);
        Assert.Equal(avg, stat_collect.Mean);
        Assert.Equal(sum, stat_collect.Total);
        Assert.Equal(cnt, stat_collect.Count);
        Assert.Equal(min, stat_collect.Min);
        Assert.Equal(max, stat_collect.Max);
        Assert.Equal(pvar, stat_collect.Variance, 14);
        Assert.Equal(med, stat_collect.GetMedian());
        Assert.Equal(data_list.Skip(2).Count(), stat_collect.Samples.Count());
      } else {
        stat.Active = true;
        Assert.Throws<ArgumentException>(() => {
          foreach (var d in data_list) stat.Add(d);
        });
      }
    }
  }
}
