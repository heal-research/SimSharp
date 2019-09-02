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
  /// This class calculates some descriptive statistics by
  /// remembering all data. All observed values are equally weighed.
  /// 
  /// It can be used to calculate e.g. lead times of processes.
  /// </summary>
  public sealed class SampleMonitor : ISampleMonitor {
    /// <summary>
    /// Can only be set in the constructor.
    /// When it is true, median and percentiles can be computed and a
    /// histogram can be printed. In addition <see cref="Samples"/>
    /// may return all the remembered values for further processing.
    /// </summary>
    public bool Collect { get; }

    /// <summary>
    /// The monitor can be set to suppress updates. When it is set
    /// to false, the statistics will not be updated and new samples
    /// are ignored.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// The name of the variable that is being monitored.
    /// Used for output in <see cref="Summarize(bool, int, double?, double?)"/>.
    /// </summary>
    public string Name { get; set; }

    public int Count { get; private set; }

    public double Min { get; private set; }
    public double Max { get; private set; }
    public double Total { get; private set; }
    double INumericMonitor.Sum { get { return Total; } }
    public double Mean { get; private set; }
    public double StdDev { get { return Math.Sqrt(Variance); } }
    public double Variance { get { return (Count > 0) ? variance / Count : 0.0; } }
    private double variance;
    public double Last { get; private set; }

    private List<double> samples;
    /// <summary>
    /// Returns the list of collected values, or an empty enumerable
    /// when <see cref="Collect"/> was initialized to false.
    /// </summary>
    public IEnumerable<double> Samples { get { return samples != null ? samples.AsEnumerable() : Enumerable.Empty<double>(); } }


    /// <summary>
    /// Calls <see cref="GetPercentile(double)"/>.
    /// </summary>
    /// <remarks>
    /// Median can only be computed when the monitor was initialized to collect the data.
    /// 
    /// The data is preprocessed on every call, the runtime complexity of this method is therefore O(n * log(n)).
    /// </remarks>
    /// <returns>The median (50th percentile) of the samples.</returns>
    public double GetMedian() {
      return GetPercentile(0.5);
    }

    /// <summary>
    /// Calculates the p-percentile of the samples.
    /// </summary>
    /// <remarks>
    /// Percentiles can only be computed when the monitor was initialized to collect the data.
    /// 
    /// The data is preprocessed on every call, the runtime complexity of this method is therefore O(n * log(n)).
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="p"/> is outside the valid range.</exception>
    /// <param name="p">The percentile has to be in the range [0;1].</param>
    /// <returns>The respective percentile of the samples.</returns>
    public double GetPercentile(double p) {
      if (p < 0 || p > 1) throw new ArgumentException("Percentile must be between 0 and 1", "p");
      if (!Collect) return double.NaN;
      return GetPercentile(samples, p);
    }

    private static double GetPercentile(IList<double> s, double p) {
      if (p < 0 || p > 1) throw new ArgumentException("Percentile must be between 0 and 1", "p");
      if (s.Count == 0) return double.NaN;
      var n = s.Count * p;
      var k = (int)Math.Ceiling(n);
      if (n < k)
        return s.OrderBy(x => x).Skip(k - 1).First();
      return s.OrderBy(x => x).Skip(k - 1).Take(2).Average();
    }

    public SampleMonitor(string name = null, bool collect = false) {
      Active = true;
      Name = name;
      Collect = collect;
      if (collect) samples = new List<double>(64);
    }

    public void Reset() {
      Count = 0;
      Min = Max = Total = Mean = 0;
      variance = 0;
      Last = 0;
      if (Collect) samples.Clear();
    }

    public void Add(double value) {
      if (!Active) return;

      if (double.IsNaN(value) || double.IsInfinity(value))
        throw new ArgumentException("Not a valid double", "value");
      Count++;
      Total += value;
      Last = value;
      if (Collect) samples.Add(value);

      if (Count == 1) {
        Min = Max = Mean = value;
      } else {
        if (value < Min) Min = value;
        if (value > Max) Max = value;

        var oldMean = Mean;
        Mean = oldMean + (value - oldMean) / Count;
        variance = variance + (value - oldMean) * (value - Mean);
      }

      OnUpdated();
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
      var nozero = Collect ? samples.Where(x => x != 0).ToList() : new List<double>();
      var nozeromin = nozero.Count > 0 ? nozero.Min() : double.NaN;
      var nozeromax = nozero.Count > 0 ? nozero.Max() : double.NaN;
      var nozeromean = nozero.Count > 1 ? nozero.Average() : double.NaN;
      var nozerostdev = nozero.Count > 2 ? Math.Sqrt(nozero.Sum(x => (x - nozeromean) * (x - nozeromean)) / (nozero.Count - 1.0)) : double.NaN;
      var sb = new StringBuilder();
      sb.Append("Statistics");
      if (!string.IsNullOrEmpty(Name))
        sb.Append(" of " + Name);
      sb.AppendLine();
      sb.AppendLine("                all             excl.zero       zero           ");
      sb.AppendLine("--------------- --------------- --------------- ---------------");
      sb.AppendLine(string.Format("{0,15} {1,15} {2,15} {3,15}", "Count", Formatter.Format15(Count), Formatter.Format15(Collect ? nozero.Count : double.NaN), Formatter.Format15(Collect ? Count - nozero.Count : double.NaN)));
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
        var min = histMin ?? Min;
        var interval = binWidth ?? (Max - Min) / maxBins;
        var histData = samples.GroupBy(x => x <= min ? 0 : (int)Math.Floor(Math.Min((x - min + interval) / interval, maxBins)))
                           .Select(x => new { Key = x.Key, Value = x.Count() })
                           .OrderBy(x => x.Key);
        sb.AppendLine();
        sb.AppendLine("Histogram");
        sb.AppendLine("<=              count      %     cum%   ");
        sb.AppendLine("--------------- ---------- ----- ------");
        var cumul = 0.0;
        var totStars = 0;
        var last = -1;
        foreach (var kvp in histData) {
          while (kvp.Key > last + 1) {
            last++;
            var tmp = "|".PadLeft(totStars + 1);
            sb.AppendLine(string.Format("{0,15} {1,10} {2,5:F1} {3,5:F1} {4}{5}", Formatter.Format15(min + last * interval), 0, 0, cumul * 100, "", tmp));
          }
          var prob = kvp.Value / (double)Count;
          cumul += prob;
          var probstars = (int)Math.Round(100 * prob / 2);
          var cumulstars = (int)Math.Round(100 * cumul / 2);
          var numstars = probstars;
          if (probstars + totStars < cumulstars) numstars++;
          var stars = string.Join("", Enumerable.Repeat("*", numstars));
          totStars += numstars;
          var cumulbar = "|".PadLeft(totStars + 1 - numstars);
          sb.AppendLine(string.Format("{0,15} {1,10} {2,5:F1} {3,5:F1} {4}{5}",
            (kvp.Key == maxBins && min + kvp.Key * interval < Max) ? "inf" : Formatter.Format15(min + kvp.Key * interval),
            kvp.Value, prob * 100, cumul * 100, stars, cumulbar));
          last = kvp.Key;
        }
      }
      return sb.ToString();
    }
  }
}
