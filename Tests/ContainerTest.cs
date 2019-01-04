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
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SimSharp.Tests {

  public class ContainerTest {
    [Fact]
    public void TestContainer() {
      var start = new DateTime(2014, 4, 2);
      var env = new Simulation(start);
      var buf = new Container(env, initial: 0, capacity: 2);
      var log = new List<Tuple<char, DateTime>>();
      env.Process(TestContainerPutter(env, buf, log));
      env.Process(TestContainerGetter(env, buf, log));
      env.Run(TimeSpan.FromSeconds(5));
      var expected = new List<Tuple<char, int>> {
        Tuple.Create('p', 1), Tuple.Create('g', 1), Tuple.Create('g', 2), Tuple.Create('p', 2)
      }.Select(x => Tuple.Create(x.Item1, start + TimeSpan.FromSeconds(x.Item2))).ToList();
      Assert.Equal(expected, log);
    }
    private IEnumerable<Event> TestContainerPutter(Simulation env, Container buf, List<Tuple<char, DateTime>> log) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      while (true) {
        yield return buf.Put(2);
        log.Add(Tuple.Create('p', env.Now));
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }
    private IEnumerable<Event> TestContainerGetter(Simulation env, Container buf, List<Tuple<char, DateTime>> log) {
      yield return buf.Get(1);
      log.Add(Tuple.Create('g', env.Now));

      yield return env.Timeout(TimeSpan.FromSeconds(1));
      yield return buf.Get(1);
      log.Add(Tuple.Create('g', env.Now));
    }

    [Fact]
    public void TestInitialiContainerCapacity() {
      var env = new Simulation();
      var container = new Container(env);
      Assert.Equal(0, container.Level);
      Assert.Equal(double.MaxValue, container.Capacity);
    }
  }
}
