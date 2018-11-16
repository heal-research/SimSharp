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
  public class StorePut : Event {
    public DateTime Time { get; private set; }
    public Process Process { get; private set; }

    public StorePut(Simulation environment, Action<Event> callback, object value)
      : base(environment) {
      if (value == null) throw new ArgumentNullException("value", "Value to put in a Store cannot be null.");
      CallbackList.Add(callback);
      Value = value;
      Time = environment.Now;
      Process = environment.ActiveProcess;
    }
  }
}
