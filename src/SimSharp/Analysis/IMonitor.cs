#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public interface IMonitor {
    bool Active { get; set; }
    string Name { get; }

    string Summarize();


    event EventHandler Updated;
  }

  public interface INumericMonitor : IMonitor {
    bool Collect { get; }

    double Min { get; }
    double Max { get; }
    double Sum { get; }
    double Mean { get; }
    double StdDev { get; }
    double Last { get; }

    double GetMedian();
    double GetPercentile(double p);
  }

  public interface ISampleMonitor : INumericMonitor {
    void Add(double value);
  }

  public interface ITimeSeriesMonitor : INumericMonitor {
    void Increase(double value);
    void Decrease(double value);
    void UpdateTo(double value);
  }
}
