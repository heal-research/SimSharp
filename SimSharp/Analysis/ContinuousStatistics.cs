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

namespace SimSharp {
  /// <summary>
  /// This class calculates some descriptive statistics online without
  /// remembering all data. It takes into account the amount of time
  /// that has passed since the last update.
  /// 
  /// It can be used to calculate e.g. utilization of some resource or
  /// inventory levels.
  /// </summary>
  public sealed class ContinuousStatistics : IStatistics {
    private readonly Simulation env;

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
    double IStatistics.Sum { get { return Area; } }
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
    double IStatistics.Last { get { return Current; } }

    private bool UpToDate { get { return env.NowD == lastUpdateTime; } }

    private double lastUpdateTime;
    private double variance;

    private bool firstSample;
    private double area;
    private double mean;

    public ContinuousStatistics(Simulation env) {
      this.env = env;
      lastUpdateTime = env.NowD;
    }
    public ContinuousStatistics(Simulation env, double initial) {
      this.env = env;
      lastUpdateTime = env.NowD;
      firstSample = true;
      Current = Min = Max = mean = initial;
    }

    public void Reset() {
      Count = 0;
      TotalTimeD = 0;
      Current = Min = Max = area = mean = 0;
      variance = 0;
      firstSample = false;
      lastUpdateTime = env.NowD;
    }

    public void Reset(double initial) {
      Count = 0;
      TotalTimeD = 0;
      Current = Min = Max = mean = initial;
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

    [Obsolete("Use UpdateTo instead")]
    public void Update(double value) { UpdateTo(value); }
    public void UpdateTo(double value) {
      Count++;

      if (!firstSample) {
        Min = Max = mean = value;
        firstSample = true;
        lastUpdateTime = env.NowD;
      } else {
        if (value < Min) Min = value;
        if (value > Max) Max = value;

        OnlineUpdate();
      }

      Current = value;
      OnUpdated();
    }

    private void OnlineUpdate() {
      var duration = env.NowD - lastUpdateTime;
      if (duration > 0) {
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
  }
}
