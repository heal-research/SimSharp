#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
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
