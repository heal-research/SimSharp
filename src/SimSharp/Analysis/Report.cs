#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimSharp {
  public sealed class Report {
    [Flags]
    public enum Measures { Min = 1, Max = 2, Sum = 4, Mean = 8, StdDev = 16, Last = 32, All = 63 }
    private enum UpdateType { Auto = 0, Manual = 1, Periodic = 2, Summary = 3 }

    private Simulation environment;
    private List<Key> keys;
    private UpdateType updateType;
    private TimeSpan periodicUpdateInterval;
    private bool withHeaders;
    private string separator;
    private bool useDoubleTime;

    private DateTime lastUpdate;
    private double[] lastFigures;
    private bool firstUpdate;
    private bool headerWritten;

    /// <summary>
    /// Gets or sets the output target.
    /// </summary>
    /// <remarks>
    /// This is not thread-safe and must be set only when the simulation is not running.
    /// </remarks>
    public TextWriter Output { get; set; }

    private Report() {
      keys = new List<Key>();
      updateType = UpdateType.Auto;
      periodicUpdateInterval = TimeSpan.Zero;
      environment = null;
      Output = Console.Out;
      separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
      useDoubleTime = false;
      withHeaders = true;
    }

    private void Initialize() {
      environment.RunStarted += SimulationOnRunStarted;
      environment.RunFinished += SimulationOnRunFinished;
      if (updateType == UpdateType.Auto) {
        foreach (var k in keys) k.Statistics.Updated += StatisticsOnUpdated;
      }
    }

    private void SimulationOnRunStarted(object sender, EventArgs e) {
      var cols = keys.Sum(x => x.TotalMeasures);
      lastFigures = new double[cols];
      lastUpdate = environment.Now;
      firstUpdate = true;
      headerWritten = false;

      if (updateType == UpdateType.Periodic) {
        environment.Process(PeriodicUpdateProcess());
      } else if (updateType == UpdateType.Auto) {
        DoUpdate();
      }
    }

    private void SimulationOnRunFinished(object sender, EventArgs e) {
      if (updateType == UpdateType.Periodic || updateType == UpdateType.Summary) DoUpdate();
      if (updateType == UpdateType.Summary && withHeaders) WriteHeader();
      WriteLastFigures();
      Output.Flush();
    }

    private void DoUpdate() {
      if (updateType != UpdateType.Summary && !firstUpdate && environment.Now > lastUpdate) {
        // values are written only when simulation time has actually passed to prevent 0-time updates
        if (!headerWritten && withHeaders) {
          WriteHeader();
          headerWritten = true;
        }
        WriteLastFigures();
      }
      lastUpdate = environment.Now;
      var col = 0;
      foreach (var fig in keys) {
        if ((fig.Measure & Measures.Min) == Measures.Min)
          lastFigures[col++] = fig.Statistics.Min;
        if ((fig.Measure & Measures.Max) == Measures.Max)
          lastFigures[col++] = fig.Statistics.Max;
        if ((fig.Measure & Measures.Sum) == Measures.Sum)
          lastFigures[col++] = fig.Statistics.Sum;
        if ((fig.Measure & Measures.Mean) == Measures.Mean)
          lastFigures[col++] = fig.Statistics.Mean;
        if ((fig.Measure & Measures.StdDev) == Measures.StdDev)
          lastFigures[col++] = fig.Statistics.StdDev;
        if ((fig.Measure & Measures.Last) == Measures.Last)
          lastFigures[col++] = fig.Statistics.Last;
      }
      firstUpdate = false;
    }

    /// <summary>
    /// Writes the header manually to <see cref="Output"/>. This may be useful if
    /// headers are not automatically added.
    /// </summary>
    public void WriteHeader() {
      Output.Write("Time");
      foreach (var fig in keys) {
        if ((fig.Measure & Measures.Min) == Measures.Min) {
          Output.Write(separator);
          Output.Write(fig.Name + ".Min");
        }
        if ((fig.Measure & Measures.Max) == Measures.Max) {
          Output.Write(separator);
          Output.Write(fig.Name + ".Max");
        }
        if ((fig.Measure & Measures.Sum) == Measures.Sum) {
          Output.Write(separator);
          Output.Write(fig.Name + ".Sum");
        }
        if ((fig.Measure & Measures.Mean) == Measures.Mean) {
          Output.Write(separator);
          Output.Write(fig.Name + ".Mean");
        }
        if ((fig.Measure & Measures.StdDev) == Measures.StdDev) {
          Output.Write(separator);
          Output.Write(fig.Name + ".StdDev");
        }
        if ((fig.Measure & Measures.Last) == Measures.Last) {
          Output.Write(separator);
          Output.Write(fig.Name + ".Last");
        }
      }
      Output.WriteLine();
    }

    private void WriteLastFigures() {
      var col = 0;
      if (useDoubleTime) Output.Write(environment.ToDouble(lastUpdate - environment.StartDate));
      else Output.Write(lastUpdate.ToString());
      foreach (var fig in keys) {
        if ((fig.Measure & Measures.Min) == Measures.Min) {
          Output.Write(separator);
          Output.Write(lastFigures[col++]);
        }
        if ((fig.Measure & Measures.Max) == Measures.Max) {
          Output.Write(separator);
          Output.Write(lastFigures[col++]);
        }
        if ((fig.Measure & Measures.Sum) == Measures.Sum) {
          Output.Write(separator);
          Output.Write(lastFigures[col++]);
        }
        if ((fig.Measure & Measures.Mean) == Measures.Mean) {
          Output.Write(separator);
          Output.Write(lastFigures[col++]);
        }
        if ((fig.Measure & Measures.StdDev) == Measures.StdDev) {
          Output.Write(separator);
          Output.Write(lastFigures[col++]);
        }
        if ((fig.Measure & Measures.Last) == Measures.Last) {
          Output.Write(separator);
          Output.Write(lastFigures[col++]);
        }
      }
      Output.WriteLine();
    }

    /// <summary>
    /// Performs a manual update. It must only be called when manual update is chosen.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when calling this function in another update mode.</exception>
    public void Update() {
      if (updateType != UpdateType.Manual) throw new InvalidOperationException("Update may only be called in manual update mode.");
      DoUpdate();
    }

    private void StatisticsOnUpdated(object sender, EventArgs e) {
      DoUpdate();
    }

    private IEnumerable<Event> PeriodicUpdateProcess() {
      while (true) {
        DoUpdate();
        yield return environment.Timeout(periodicUpdateInterval);
      }
    }

    /// <summary>
    /// Creates a new report builder for configuring the report. A report can be generated by
    /// calling the builder's <see cref="Builder.Build"/> method.
    /// </summary>
    /// <param name="env">The simulation environment for which a report should be generated.</param>
    /// <returns>The builder instance that is used to configure a new report.</returns>
    public static Builder CreateBuilder(Simulation env) {
      return new Builder(env);
    }

    /// <summary>
    /// The Builder class is used to configure and create a new report.
    /// </summary>
    public class Builder {
      private Report instance;
      /// <summary>
      /// Creates a new builder for generating a report.
      /// </summary>
      /// <param name="env">The simulation environment for which the report should be generated.</param>
      public Builder(Simulation env) {
        instance = new Report() { environment = env };
      }

      /// <summary>
      /// Adds a new indicator to the report.
      /// </summary>
      /// <param name="name">The name of the indicator for which the statistic is created.</param>
      /// <param name="statistics">The statistics instance for the indicator that contains the values.</param>
      /// <param name="measure">The measure(s) that should be reported.</param>
      /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty,
      /// or when <paramref name="measure"/> is not valid.</exception>
      /// <exception cref="ArgumentNullException">Thrown when <paramref name="statistics"/> is null.</exception>
      /// <returns>This builder instance.</returns>
      public Builder Add(string name, INumericMonitor statistics, Measures measure = Measures.All) {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name must be a non-empty string", "name");
        if (statistics == null) throw new ArgumentNullException("statistics");
        if (measure == 0 || measure > Measures.All) throw new ArgumentException("No measures have been selected.", "measure");

        instance.keys.Add(new Key { Name = name, Statistics = statistics, Measure = measure, TotalMeasures = CountSetBits((int)measure) });
        return this;
      }

      /// <summary>
      /// In automatic updating mode (default), the report will listen to the
      /// <see cref="IMonitor.Updated"/> event and perform an update whenever
      /// any of its statistics is updated.
      /// </summary>
      /// <remarks>Auto update is mutually exclusive to the other update modes.
      /// Auto update with headers is the default.</remarks>
      /// <param name="withHeaders">Whether the headers should be output before the first values are printed.</param>
      /// <returns>This builder instance.</returns>
      public Builder SetAutoUpdate(bool withHeaders = true) {
        instance.withHeaders = withHeaders;
        instance.updateType = UpdateType.Auto;
        return this;
      }

      /// <summary>
      /// In manual updating mode, the <see cref="Report.Update"/> method needs to be called
      /// manually in order to record the current state.
      /// </summary>
      /// <remarks>Manual update is mutually exclusive to the other update modes.</remarks>
      /// <param name="withHeaders">Whether the headers should be output before the first values are printed.</param>
      /// <returns>This builder instance.</returns>
      public Builder SetManualUpdate(bool withHeaders = true) {
        instance.withHeaders = withHeaders;
        instance.updateType = UpdateType.Manual;
        return this;
      }

      /// <summary>
      /// In periodic updating mode, the report will create a process that periodically
      /// triggers the update. The process will be created upon calling <see cref="Build"/>.
      /// </summary>
      /// <remarks>Periodic update is mutually exclusive to the other update modes.</remarks>
      /// <exception cref="ArgumentException">Thrown when <paramref name="interval"/> is less or equal than TimeSpan.Zero.</exception>
      /// <param name="interval">The interval after which an update occurs.</param>
      /// <param name="withHeaders">Whether the headers should be output before the first values are printed.</param>
      /// <returns>This builder instance.</returns>
      public Builder SetPeriodicUpdate(TimeSpan interval, bool withHeaders = true) {
        if (interval <= TimeSpan.Zero) throw new ArgumentException("Interval must be > 0", "interval");
        instance.periodicUpdateInterval = interval;
        instance.withHeaders = withHeaders;
        instance.updateType = UpdateType.Periodic;
        return this;
      }

      /// <summary>
      /// In periodic updating mode, the report will create a process that periodically
      /// triggers the update. The process will be created upon calling <see cref="Build"/>.
      /// </summary>
      /// <remarks>Periodic update is mutually exclusive to the other update modes.</remarks>
      /// <exception cref="ArgumentException">Thrown when <paramref name="interval"/> is less or equal than 0.</exception>
      /// <param name="interval">The interval after which an update occurs.</param>
      /// <param name="withHeaders">Whether the headers should be output before the first values are printed.</param>
      /// <returns>This builder instance.</returns>
      public Builder SetPeriodicUpdateD(double interval, bool withHeaders = true) {
        if (interval <= 0) throw new ArgumentException("Interval must be > 0", "interval");
        instance.periodicUpdateInterval = instance.environment.ToTimeSpan(interval);
        instance.withHeaders = withHeaders;
        instance.updateType = UpdateType.Periodic;
        return this;
      }

      /// <summary>
      /// In final update mode, the report will only update when the simulation terminates correctly.
      /// This is useful for generating a summary of the results.
      /// </summary>
      /// <remarks>Final update is mutually exclusive to the other update modes.</remarks>
      /// <param name="withHeaders">Whether the headers should be output together with the summary at the end.</param>
      /// <returns>This builder instance.</returns>
      public Builder SetFinalUpdate(bool withHeaders = true) {
        instance.withHeaders = withHeaders;
        instance.updateType = UpdateType.Summary;
        return this;
      }

      /// <summary>
      /// Whether to output the time column in DateTime format or as double (D-API).
      /// </summary>
      /// <param name="useDApi">Whether the time should be output as double.</param>
      /// <returns>This builder instance.</returns>
      public Builder SetTimeAPI(bool useDApi = true) {
        instance.useDoubleTime = useDApi;
        return this;
      }

      /// <summary>
      /// Redirects the output of the report to another target.
      /// By default it is configured to use stdout.
      /// </summary>
      /// <exception cref="ArgumentNullException">Thrown when <paramref name="output"/> is null.</exception>
      /// <param name="output">The target to which the output should be directed.</param>
      /// <returns>This builder instance.</returns>
      public Builder SetOutput(TextWriter output) {
        this.instance.Output = output ?? throw new ArgumentNullException("output");
        return this;
      }

      /// <summary>
      /// Sets the separator for the indicators' values.
      /// </summary>
      /// <param name="seperator">The string that separates the values.</param>
      /// <returns>This builder instance.</returns>
      public Builder SetSeparator(string seperator) {
        if (seperator == null) seperator = string.Empty;
        this.instance.separator = seperator;
        return this;
      }

      /// <summary>
      /// Creates and initializes the report. After calling Build(), this builder instance
      /// is reset and can be reused to create a new report.
      /// </summary>
      /// <exception cref="InvalidOperationException">Thrown when no indicators have been added.</exception>
      /// <returns>The created report instance.</returns>
      public Report Build() {
        if (!instance.keys.Any())
          throw new InvalidOperationException("Nothing to build: No indicators have been added to the Builder.");
        var result = instance;
        instance = new Report();
        result.Initialize();
        return result;
      }

      private static readonly int[] numToBits = new int[16] { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };
      private static int CountSetBits(int num) {
        return numToBits[num & 0xf] +
               numToBits[(num >> 4) & 0xf] +
               numToBits[(num >> 8) & 0xf] +
               numToBits[(num >> 16) & 0xf] +
               numToBits[(num >> 20) & 0xf] +
               numToBits[(num >> 24) & 0xf] +
               numToBits[(num >> 28) & 0xf];
      }
    }

    private class Key {
      public string Name { get; set; }
      public INumericMonitor Statistics { get; set; }
      public Measures Measure { get; set; }
      public int TotalMeasures { get; set; }
    }
  }
  
}
