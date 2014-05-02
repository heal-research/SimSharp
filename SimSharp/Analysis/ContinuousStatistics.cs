#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2014  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
    private readonly Environment env;

    public int Count { get; private set; }
    public double TotalTimeD { get; private set; }
    public TimeSpan TotalTime { get { return env.ToTimeSpan(TotalTimeD); } }

    public double Min { get; private set; }
    public double Max { get; private set; }
    public double Area { get; private set; }
    public double Mean { get; private set; }
    public double StdDev { get { return Math.Sqrt((TotalTimeD > 0) ? newVar / TotalTimeD : 0.0); } }
    public double Variance { get { return (TotalTimeD > 0) ? newVar / TotalTimeD : 0.0; } }

    private DateTime lastUpdateTime;
    private double lastValue;


    public ContinuousStatistics(Environment env) {
      this.env = env;
      Count = 0;
      TotalTimeD = 0;
      Min = double.MaxValue;
      Max = double.MinValue;
      Area = 0;
      Mean = 0;
      lastUpdateTime = env.Now;
      lastValue = 0;
    }

    public void Update(double value) {
      Count++;

      var timeDiff = env.Now - lastUpdateTime;
      var duration = env.ToDouble(timeDiff);

      Area += (lastValue * duration);
      if (value < Min) Min = value;
      if (value > Max) Max = value;
      UpdateRunningMeanAndVariance(lastValue, duration);

      TotalTimeD += duration;

      lastUpdateTime = env.Now;
      lastValue = value;
    }

    double oldMean = double.NaN;
    double oldVar = double.NaN;
    double newVar = 0;
    private void UpdateRunningMeanAndVariance(double value, double duration) {
      if (TotalTimeD < 1e-12) {
        oldMean = Mean = value;
        oldVar = 0.0;
      } else {
        if (duration == 0) return;
        Mean = oldMean + (value - oldMean) * duration / (duration + TotalTimeD);
        newVar = oldVar + (value - oldMean) * (value - Mean) * duration;

        // set up for next iteration
        oldMean = Mean;
        oldVar = newVar;
      }
    }
  }
}
