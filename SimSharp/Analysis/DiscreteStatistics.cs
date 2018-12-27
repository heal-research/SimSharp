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
  /// remembering all data. All observed values are equally weighed.
  /// 
  /// It can be used to calculate e.g. lead times of processes.
  /// </summary>
  public sealed class DiscreteStatistics : IStatistics {
    public int Count { get; private set; }

    public double Min { get; private set; }
    public double Max { get; private set; }
    public double Total { get; private set; }
    double IStatistics.Sum { get { return Total; } }
    public double Mean { get; private set; }
    public double StdDev { get { return Math.Sqrt(Variance); } }
    public double Variance { get { return (Count > 0) ? variance / Count : 0.0; } }
    private double variance;
    public double Last { get; private set; }

    public DiscreteStatistics() {
    }

    public void Reset() {
      Count = 0;
      Min = Max = Total = Mean = 0;
      variance = 0;
      Last = 0;
    }

    public void Add(double value) {
      if (double.IsNaN(value) || double.IsInfinity(value))
        throw new ArgumentException("Not a valid double", "value");
      Count++;
      Total += value;
      Last = value;

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
  }
}
