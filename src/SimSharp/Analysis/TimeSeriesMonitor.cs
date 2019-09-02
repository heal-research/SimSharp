#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
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

    private bool active;
    /// <summary>
    /// The monitor can be set to suppress updates. When it is set
    /// to false, the statistics, except for <see cref="Current"/>
    /// will not be updated and new samples are ignored.
    /// When set to true it will resume updates from the current
    /// simulation time onward.
    /// </summary>
    public bool Active {
      get { return active; }
      set {
        if (active == value) return;
        if (firstSampleObserved && lastUpdateTime < env.NowD) {
          if (value) {
            // enable it
            if (Current < Min) Min = Current;
            if (Current > Max) Max = Current;

            lastUpdateTime = env.NowD;
            series.Add(new Entry { Date = env.NowD, Level = Current });
          } else if (!value) {
            // disable it
            OnlineUpdate();
          }
          active = value;
          OnUpdated();
        } else active = value;
      }
    }

    /// <summary>
    /// The name of the variable that is being monitored.
    /// Used for output in <see cref="Summarize(bool, int, double?, double?)"/>.
    /// </summary>
    public string Name { get; set; }

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
    double INumericMonitor.Sum { get { return Area; } }
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
    double INumericMonitor.Last { get { return Current; } }

    private bool UpToDate { get { return !Active || env.NowD == lastUpdateTime; } }

    private double lastUpdateTime;
    private double variance;

    private bool firstSampleObserved;
    private double area;
    private double mean;

    private List<Entry> series;
    /// <summary>
    /// Returns the list of collected values, or an empty enumerable
    /// when <see cref="Collect"/> was initialized to false.
    /// </summary>
    public IEnumerable<Entry> Series {
      get {
        if (!UpToDate) OnlineUpdate();
        return series != null ? series.AsEnumerable() : Enumerable.Empty<Entry>();
      }
    }

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
      var ilower = 0;
      while (ilower < seq.Count) {
        if (seq[ilower].CumulatedDuration >= n) break;
        ilower++;
      }
      if (seq[ilower].CumulatedDuration == n) {
        if (ilower < seq.Count - 1)
          return (seq[ilower].Level + seq[ilower + 1].Level) / 2.0;
      }
      return seq[ilower].Level;
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

    public TimeSeriesMonitor(Simulation env, string name = null, bool collect = false) {
      this.env = env;
      active = true;
      Name = name;
      Collect = collect;
      lastUpdateTime = env.NowD;
      if (Collect) series = new List<Entry>(64);
    }
    public TimeSeriesMonitor(Simulation env, double initial, string name = null, bool collect = false) {
      this.env = env;
      active = true;
      Name = name;
      Collect = collect;
      lastUpdateTime = env.NowD;
      firstSampleObserved = true;
      Current = Min = Max = mean = initial;
      if (Collect) series = new List<Entry>(64) { new Entry { Date = env.NowD, Level = initial } };
    }

    public void Reset() {
      TotalTimeD = 0;
      Current = Min = Max = area = mean = 0;
      if (Collect) series.Clear();
      variance = 0;
      firstSampleObserved = false;
      lastUpdateTime = env.NowD;
    }

    public void Reset(double initial) {
      TotalTimeD = 0;
      Current = Min = Max = mean = initial;
      if (Collect) {
        series.Clear();
        series.Add(new Entry { Date = env.NowD, Level = initial });
      }
      area = 0;
      variance = 0;
      firstSampleObserved = true;
      lastUpdateTime = env.NowD;
    }

    public void Increase(double value = 1) {
      UpdateTo(Current + value);
    }

    public void Decrease(double value = 1) {
      UpdateTo(Current - value);
    }

    public void UpdateTo(double value) {
      if (!Active) {
        Current = value;
        return;
      }

      if (!firstSampleObserved) {
        Min = Max = mean = value;
        firstSampleObserved = true;
        lastUpdateTime = env.NowD;
        if (Collect) series.Add(new Entry { Date = env.NowD, Level = value });
        Current = value;
      } else {
        if (value < Min) Min = value;
        if (value > Max) Max = value;

        OnlineUpdate();

        if (Current != value) {
          if (Collect) series.Add(new Entry { Date = env.NowD, Level = value });
          Current = value;
        }
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

    string IMonitor.Summarize() {
      return Summarize();
    }

    /// <summary>
    /// Provides a summary of the statistics in a certain format.
    /// If the monitor is configured to collect data, it may also print a histogram.
    /// </summary>
    /// <param name="withHistogram">Whether to suppress the histogram.
    /// This is only effective if <see cref="Collect"/> was set to true, otherwise
    /// the data to produce the histogram is not available in the first place.</param>
    /// <param name="maxBins">The maximum number of bins that should be used.
    /// Note that the bin width and thus the number of bins is also governed by
    /// <paramref name="binWidth"/> if it is defined.
    /// This is only effective if <see cref="Collect"/> and <paramref name="withHistogram"/>
    /// was set to true, otherwise the data to produce the histogram is not available
    /// in the first place.</param>
    /// <param name="histMin">The minimum for the histogram to start at or the sample
    /// minimum in case the default (null) is given.
    /// This is only effective if <see cref="Collect"/> and <paramref name="withHistogram"/>
    /// was set to true, otherwise the data to produce the histogram is not available
    /// in the first place.</param>
    /// <param name="binWidth">The interval for the bins of the histogram or the
    /// range (<see cref="Max"/> - <see cref="Min"/>) divided by the number of bins
    /// (<paramref name="maxBins"/>) in case the default value (null) is given.
    /// This is only effective if <see cref="Collect"/> and <paramref name="withHistogram"/>
    /// was set to true, otherwise the data to produce the histogram is not available
    /// in the first place.</param>
    /// <returns>A formatted string that provides a summary of the statistics.</returns>
    public string Summarize(bool withHistogram = true, int maxBins = 20, double? histMin = null, double? binWidth = null) {
      var nozero = Collect ? series.Where(x => x.Level != 0 && x.Duration > 0).ToList() : new List<Entry>();
      var nozeromin = nozero.Count > 0 ? nozero.Min(x => x.Level) : double.NaN;
      var nozeromax = nozero.Count > 0 ? nozero.Max(x => x.Level) : double.NaN;
      var nozeroduration = Collect ? nozero.Sum(x => x.Duration) : double.NaN;
      var nozeromean = nozero.Count > 1 ? nozero.Sum(x => x.Level * x.Duration / nozeroduration) : double.NaN;
      var nozerostdev = nozero.Count > 2 ? Math.Sqrt(nozero.Sum(x => x.Duration * (x.Level - nozeromean) * (x.Level - nozeromean)) / nozeroduration) : double.NaN;
      var sb = new StringBuilder();
      sb.Append("Time series statistics");
      if (!string.IsNullOrEmpty(Name))
        sb.Append(" of " + Name);
      sb.AppendLine();
      sb.AppendLine("                all             excl.zero       zero           ");
      sb.AppendLine("--------------- --------------- --------------- ---------------");
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

      if (Collect && withHistogram) {
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
          for (var bin = 0; bin <= maxBins; bin++) {
            var next = (histMin ?? Min) + bin * (binWidth ?? (Max - Min) / 20.0);
            var dur = 0.0;
            var prob = 0.0;
            while (moreData && (kvp.Level <= next || bin == maxBins)) {
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
            sb.AppendLine(string.Format("{0,15} {1,15} {2,5:F1} {3,5:F1} {4}{5}",
              (!moreData && next < Max) ? "inf" : Formatter.Format15(next),
              Formatter.Format15(dur), prob * 100, cumul * 100, stars, cumulbar));
            if (!moreData) break;
          }
        }
      }
      return sb.ToString();
    }

    public override string ToString() {
      return Summarize();
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

  [Obsolete("Use the class TimeSeriesMonitor instead.")]
  public sealed class ContinuousStatistics {
    private readonly Simulation env;

    public int Count { get; private set; }
    public double TotalTimeD { get; private set; }
    public TimeSpan TotalTime { get { return env.ToTimeSpan(TotalTimeD); } }

    public double Min { get; private set; }
    public double Max { get; private set; }
    public double Area { get; private set; }
    public double Mean { get; private set; }
    public double StdDev { get { return Math.Sqrt(Variance); } }
    public double Variance { get { return (TotalTimeD > 0) ? variance / TotalTimeD : 0.0; } }

    private double lastUpdateTime;
    private double lastValue;
    private double variance;

    private bool firstSample;


    public ContinuousStatistics(Simulation env) {
      this.env = env;
      lastUpdateTime = env.NowD;
    }

    public void Update(double value) {
      Count++;

      if (!firstSample) {
        Min = Max = Mean = value;
        firstSample = true;
      } else {
        if (value < Min) Min = value;
        if (value > Max) Max = value;

        var duration = env.NowD - lastUpdateTime;
        if (duration > 0) {
          Area += (lastValue * duration);
          var oldMean = Mean;
          Mean = oldMean + (lastValue - oldMean) * duration / (duration + TotalTimeD);
          variance = variance + (lastValue - oldMean) * (lastValue - Mean) * duration;
          TotalTimeD += duration;
        }
      }

      lastUpdateTime = env.NowD;
      lastValue = value;
    }
  }
}
