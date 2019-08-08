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
using System.Text;

namespace SimSharp {
  /// <summary>
  /// This class calculates weighted statistics of a time series variable.
  /// The weight is given as the duration of the variable's value.
  /// 
  /// It is typically used to calculate utilization of resources or inventory levels.
  /// </summary>
  /// <remarks>
  /// The monitor updates the statistics online, except for <see cref="GetMedian"/>
  /// which is calculated given the collected data whenever it is called.
  /// When the monitor was initialized with collect = false in the constructor,
  /// median and other percentiles cannot be computed (double.NaN is returned).
  /// Also to print a histogram requires that the monitor was initialized to collect
  /// all the changes to the variable's value.
  /// 
  /// Collecting the data naturally incurs some memory overhead.
  /// </remarks>
  public sealed class TimeSeriesMonitor : ITimeSeriesMonitor {
    private readonly Simulation env;
    /// <summary>
    /// Can only be set in the constructor.
    /// When it is true, median and percentiles can be computed and a
    /// histogram can be printed. In addition <see cref="Series"/>
    /// may return all the remembered values for further processing.
    /// </summary>
    public bool Collect { get; }

    public int Count { get; private set; }
    public double TotalTimeD { get; private set; }
    public TimeSpan TotalTime { get { return env.ToTimeSpan(TotalTimeD); } }

    public double Min { get; private set; }
    public double Max { get; private set; }
    public double Area {
      get {
        if (!UpToDate) OnlineUpdate();
        return area;
      }
      private set => area = value;
    }
    double IMonitor.Sum { get { return Area; } }
    public double Mean {
      get {
        if (!UpToDate) OnlineUpdate();
        return mean;
      }
      private set => mean = value;
    }
    public double StdDev { get { return Math.Sqrt(Variance); } }
    public double Variance {
      get {
        if (!UpToDate) OnlineUpdate();
        return (TotalTimeD > 0) ? variance / TotalTimeD : 0.0;
      }
    }
    public double Current { get; private set; }
    double IMonitor.Last { get { return Current; } }

    private bool UpToDate { get { return env.NowD == lastUpdateTime; } }

    private double lastUpdateTime;
    private double variance;

    private bool firstSample;
    private double area;
    private double mean;

    private List<Entry> series;
    /// <summary>
    /// Returns the list of collected values, or an empty enumerable
    /// when <see cref="Collect"/> was initialized to false.
    /// </summary>
    public IEnumerable<Entry> Series { get { return series != null ? series.AsEnumerable() : Enumerable.Empty<Entry>(); } }

    /// <summary>
    /// Calls <see cref="GetPercentile(double)"/>.
    /// </summary>
    /// <remarks>
    /// Median can only be computed when the monitor was initialized to collect the data.
    /// 
    /// The data is preprocessed on every call, the runtime complexity of this method is therefore O(n * log(n)).
    /// </remarks>
    /// <returns>The median (50th percentile) of the time series.</returns>
    public double GetMedian() {
      return GetPercentile(0.5);
    }

    /// <summary>
    /// Calculates the weighted p-percentile of the sampled levels (duration is the weight).
    /// </summary>
    /// <remarks>
    /// Percentiles can only be computed when the monitor was initialized to collect the data.
    /// 
    /// The data is preprocessed on every call, the runtime complexity of this method is therefore O(n * log(n)).
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="p"/> is outside the valid range.</exception>
    /// <param name="p">The percentile has to be in the range [0;1].</param>
    /// <returns>The respective percentile of the time series.</returns>
    public double GetPercentile(double p) {
      if (p < 0 || p > 1) throw new ArgumentException("Percentile must be between 0 and 1", "p");
      if (!Collect) return double.NaN;
      return GetPercentile(series, p);
    }
    private static double GetPercentile(IList<Entry> s, double p) {
      var seq = Cumulate(s).ToList();
      if (seq.Count == 0) return double.NaN;
      var total = seq.Last().CumulatedDuration;
      var n = total * p;
      int ilower = 0, iupper = seq.Count - 1;
      for (var i = 0; i < seq.Count; i++) {
        if (seq[i].CumulatedDuration < n) ilower = i;
        if (total - seq[i].CumulatedDuration < n) iupper = seq.Count - i - 1;
      }
      if (ilower == iupper) return seq[ilower].Level;
      if (seq[ilower].Duration < seq[iupper].Duration) return seq[ilower].Level;
      return seq[iupper].Level;
    }

    private static IEnumerable<LevelDuration> Cumulate(IList<Entry> s) {
      var totalDuration = 0.0;
      // The last entry will always be skipped as it has Duration == 0
      foreach (var g in s.Where(x => x.Duration > 0).GroupBy(x => x.Level, x => x.Duration).OrderBy(x => x.Key)) {
        var duration = g.Sum();
        totalDuration += duration;
        yield return new LevelDuration { Level = g.Key, Duration = duration, CumulatedDuration = totalDuration };
      }
    }

    public TimeSeriesMonitor(Simulation env, bool collect = false) {
      this.env = env;
      Collect = collect;
      lastUpdateTime = env.NowD;
      if (Collect) series = new List<Entry>(64);
    }
    public TimeSeriesMonitor(Simulation env, double initial, bool collect = false) {
      this.env = env;
      Collect = collect;
      lastUpdateTime = env.NowD;
      firstSample = true;
      Current = Min = Max = mean = initial;
      if (Collect) series = new List<Entry>(64) { new Entry { Date = env.NowD, Level = initial } };
    }

    public void Reset() {
      Count = 0;
      TotalTimeD = 0;
      Current = Min = Max = area = mean = 0;
      if (Collect) series.Clear();
      variance = 0;
      firstSample = false;
      lastUpdateTime = env.NowD;
    }

    public void Reset(double initial) {
      Count = 0;
      TotalTimeD = 0;
      Current = Min = Max = mean = initial;
      if (Collect) series.Clear();
      area = 0;
      variance = 0;
      firstSample = true;
      lastUpdateTime = env.NowD;
    }

    public void Increase(double value = 1) {
      UpdateTo(Current + value);
    }

    public void Decrease(double value = 1) {
      UpdateTo(Current - value);
    }

    public void UpdateTo(double value) {
      Count++;

      if (!firstSample) {
        Min = Max = mean = value;
        firstSample = true;
        lastUpdateTime = env.NowD;
        if (Collect) series.Add(new Entry { Date = env.NowD, Level = value });
      } else {
        if (value < Min) Min = value;
        if (value > Max) Max = value;

        OnlineUpdate();
      }

      if (Current != value) {
        if (Collect) series.Add(new Entry { Date = env.NowD, Level = value });
        Current = value;
      }
      OnUpdated();
    }

    private void OnlineUpdate() {
      var duration = env.NowD - lastUpdateTime;
      if (duration > 0) {
        if (Collect) {
          var prevIdx = series.Count - 1;
          var prev = series[prevIdx];
          prev.Duration = env.NowD - prev.Date;
          series[prevIdx] = prev;
        }
        area += (Current * duration);
        var oldMean = mean;
        mean = oldMean + (Current - oldMean) * duration / (duration + TotalTimeD);
        variance = variance + (Current - oldMean) * (Current - mean) * duration;
        TotalTimeD += duration;
      }
      lastUpdateTime = env.NowD;
    }

    public event EventHandler Updated;
    private void OnUpdated() {
      Updated?.Invoke(this, EventArgs.Empty);
    }

    public string Print(string name = null, double? histMin = null, double? histInterval = null) {
      var nozero = Collect ? series.Where(x => x.Level != 0 && x.Duration > 0).ToList() : new List<Entry>();
      var nozeromin = nozero.Count > 0 ? nozero.Min(x => x.Level) : double.NaN;
      var nozeromax = nozero.Count > 0 ? nozero.Max(x => x.Level) : double.NaN;
      var nozeroduration = Collect ? nozero.Sum(x => x.Duration) : double.NaN;
      var nozeromean = nozero.Count > 1 ? nozero.Sum(x => x.Level * x.Duration / nozeroduration) : double.NaN;
      var nozerostdev = nozero.Count > 2 ? Math.Sqrt(nozero.Sum(x => x.Duration * (x.Level - nozeromean) * (x.Level - nozeromean)) / nozeroduration) : double.NaN;
      var sb = new StringBuilder();
      sb.Append("Time series statistics");
      if (!string.IsNullOrEmpty(name))
        sb.Append(" of " + name);
      sb.AppendLine();
      sb.AppendLine("                all             excl.zero       zero           ");
      sb.AppendLine("--------------- --------------- --------------- ---------------");
      sb.AppendLine(string.Format("{0,15} {1,15} {2,15} {3,15}", "Count", Formatter.Format15(Count), Formatter.Format15(Collect ? nozero.Count : double.NaN), Formatter.Format15(Collect ? Count - nozero.Count : double.NaN)));
      sb.AppendLine(string.Format("{0,15} {1,15} {2,15} {3,15}", "Duration", Formatter.Format15(TotalTimeD), Formatter.Format15(nozeroduration), Formatter.Format15(TotalTimeD - nozeroduration)));
      sb.AppendLine(string.Format("{0,15} {1,15} {2,15}", "Mean", Formatter.Format15(Mean), Formatter.Format15(nozeromean)));
      sb.AppendLine(string.Format("{0,15} {1,15} {2,15}", "Std.dev", Formatter.Format15(StdDev), Formatter.Format15(nozerostdev)));
      sb.AppendLine();
      sb.AppendLine(string.Format("{0,15} {1,15} {2,15}", "Minimum", Formatter.Format15(Min), Formatter.Format15(nozeromin)));
      if (Collect) {
        sb.AppendLine(string.Format("{0,15} {1,15} {2,15}", "Percentile-5%", Formatter.Format15(GetPercentile(0.05)), Formatter.Format15(GetPercentile(nozero, 0.05))));
        sb.AppendLine(string.Format("{0,15} {1,15} {2,15}", "Median", Formatter.Format15(GetMedian()), Formatter.Format15(GetPercentile(nozero, 0.5))));
        sb.AppendLine(string.Format("{0,15} {1,15} {2,15}", "Percentile-95%", Formatter.Format15(GetPercentile(0.95)), Formatter.Format15(GetPercentile(nozero, 0.95))));
      }
      sb.AppendLine(string.Format("{0,15} {1,15} {2,15}", "Maximum", Formatter.Format15(Max), Formatter.Format15(nozeromax)));

      if (Collect) {
        var histData = Cumulate(series);
        sb.AppendLine();
        sb.AppendLine("Histogram");
        sb.AppendLine("<=              duration        %     cum%   ");
        sb.AppendLine("--------------- --------------- ----- ------");
        var totStars = 0;
        var iter = histData.GetEnumerator();
        var cumul = 0.0;
        if (!iter.MoveNext()) {
          sb.AppendLine("no data");
        } else {
          var kvp = iter.Current;
          var moreData = true;
          for (var bin = 0; bin <= 20; bin++) {
            var next = (histMin ?? Min) + bin * (histInterval ?? (Max - Min) / 20.0);
            var dur = 0.0;
            var prob = 0.0;
            while (moreData && kvp.Level <= next) {
              dur += kvp.Duration;
              prob += kvp.Duration / TotalTimeD;
              cumul = kvp.CumulatedDuration / TotalTimeD;
              moreData = iter.MoveNext();
              kvp = iter.Current;
            }
            var probstars = (int)Math.Round(100 * prob / 2);
            var cumulstars = (int)Math.Round(100 * cumul / 2);
            var numstars = probstars;
            if (numstars + totStars < cumulstars) numstars++;
            var stars = string.Join("", Enumerable.Repeat("*", numstars));
            totStars += numstars;
            var cumulbar = "|".PadLeft(totStars + 1 - numstars);
            sb.AppendLine(string.Format("{0,15} {1,15} {2,5:F1} {3,5:F1} {4}{5}", Formatter.Format15(next), Formatter.Format15(dur), prob * 100, cumul * 100, stars, cumulbar));
          }
        }
      }
      return sb.ToString();
    }

    public override string ToString() {
      return Print();
    }

    public struct Entry {
      public double Date;
      public double Duration;
      public double Level;
    }

    private struct LevelDuration {
      public double Level;
      public double Duration;
      public double CumulatedDuration;
    }
  }
}
