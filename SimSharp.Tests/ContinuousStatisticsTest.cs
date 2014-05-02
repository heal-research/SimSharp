using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
  }
}
