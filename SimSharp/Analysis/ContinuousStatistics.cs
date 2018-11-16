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

namespace SimSharp {
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
