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
    private Environment env;

    private int count;
    public int Count { get { return count; } }
    private double totalTime;
    public double TotalTimeD { get { return totalTime; } }
    public TimeSpan TotalTime { get { return env.ToTimeSpan(totalTime); } }

    private double min;
    public double Min { get { return min; } }
    private double max;
    public double Max { get { return max; } }
    private double area;
    public double Area { get { return area; } }
    public double Mean { get; private set; }
    public double StdDev { get { return Math.Sqrt((totalTime > 0) ? newVar / totalTime : 0.0); } }
    public double Variance { get { return (totalTime > 0) ? newVar / totalTime : 0.0; } }

    private DateTime lastUpdateTime;
    private double lastValue;


    public ContinuousStatistics(Environment env) {
      this.env = env;
      count = 0;
      min = double.MaxValue;
      max = double.MinValue;
      area = 0;
      lastUpdateTime = env.Now;
      lastValue = 0;
    }

    public void Add(double value) {
      count++;

      var timeDiff = env.Now - lastUpdateTime;
      var duration = env.ToDouble(timeDiff);
      totalTime += duration;

      area += (lastValue * duration);
      if (value < min) min = value;
      if (value > max) max = value;
      UpdateRunningMeanAndVariance(lastValue, duration);

      lastUpdateTime = env.Now;
      lastValue = value;
    }

    double oldMean;
    double oldVar;
    double newVar;
    private void UpdateRunningMeanAndVariance(double value, double duration) {
      if (count == 1) {
        oldMean = Mean = value;
        oldVar = 0.0;
      } else {
        Mean = oldMean + (value - oldMean) * duration / totalTime;
        newVar = oldVar + (value - oldMean) * (value - Mean) * duration;

        // set up for next iteration
        oldMean = Mean;
        oldVar = newVar;
      }
    }
  }
}
