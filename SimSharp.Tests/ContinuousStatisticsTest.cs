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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace SimSharp.Tests {
  [TestClass]
  public class ContinuousStatisticsTest {

    [TestMethod]
    public void TestContinuousStatisticsSimple() {
      var env = new Environment();
      var times = new double[] { 0, 1, 1, 1, 1, 1 };
      var values = new double[] { 6, 2, 3, 5, -1, -4 };
      var stat = new ContinuousStatistics(env);
      foreach (var v in times.Zip(values, Tuple.Create)) {
        if (v.Item1 > 0) env.RunD(v.Item1);
        stat.Update(v.Item2);
      }
      Assert.AreEqual(-4, stat.Min);
      Assert.AreEqual(6, stat.Max);
      Assert.AreEqual(3, stat.Mean, 1e-12);
      Assert.AreEqual(6, stat.Variance, 1e-12);
      Assert.AreEqual(15, stat.Area);
    }

    [TestMethod]
    public void TestContinuousStatisticsComplex() {
      var env = new Environment();
      var times = new double[] { 0, 10, 0, 1, 1, 1, 1 };
      var values = new double[] { 0, 6, 2, 3, 5, -1, -4 };
      var stat = new ContinuousStatistics(env);
      foreach (var v in times.Zip(values, Tuple.Create)) {
        if (v.Item1 > 0) env.RunD(v.Item1);
        stat.Update(v.Item2);
      }
      Assert.AreEqual(-4, stat.Min);
      Assert.AreEqual(6, stat.Max);
      Assert.AreEqual(0.642857142857, stat.Mean, 1e-12);
      Assert.AreEqual(2.372448979592, stat.Variance, 1e-12);
      Assert.AreEqual(9, stat.Area);
    }

    [TestMethod]
    public void TestContinuousStatisticsComplex2() {
      var env = new Environment();
      var times = new double[] { 1, 1, 1, 2, 0, 0, 4, 7, 4 };
      var values = new double[] { 3, -2, 5, 6, -4, 1, 0, -2, 3 };
      var stat = new ContinuousStatistics(env);
      foreach (var v in times.Zip(values, Tuple.Create)) {
        if (v.Item1 > 0) env.RunD(v.Item1);
        stat.Update(v.Item2);
      }
      Assert.AreEqual(-4, stat.Min);
      Assert.AreEqual(6, stat.Max);
      Assert.AreEqual(0.3684210526315789, stat.Mean, 1e-12);
      Assert.AreEqual(4.232686980609418, stat.Variance, 1e-12);
      Assert.AreEqual(7, stat.Area);
    }
  }
}
