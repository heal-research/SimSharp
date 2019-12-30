using System.Linq;
using System.Threading;
using Xunit;

namespace SimSharp.Tests {
  public class RandomTest {

    [Fact]
    public void PcgReproducibilityTest() {
      var pcg = new PcgRandom(15);
      var seq = Enumerable.Range(0, 1000).Select(x => pcg.Next()).ToArray();
      Thread.Sleep(100);
      pcg = new PcgRandom(15);
      var seq2 = Enumerable.Range(0, 1000).Select(x => pcg.Next()).ToArray();
      Assert.Equal(seq, seq2);
    }

    [Theory]
    [InlineData(new double[] { double.NaN, 0.3 })]
    [InlineData(new double[] { 1.0 / 0.0, 0.3 })]
    [InlineData(new double[] { 0, 0 })]
    [InlineData(new double[] { -1, 10 })]
    [InlineData(new double[] { double.MaxValue, double.MaxValue })]
    public void RandChoiceTestArgumentException(double[] weights) {
      var env = new Simulation(15);
      Assert.Throws<System.ArgumentException>(
        () => env.RandChoice(new[] { "a", "b" }, weights));
    }

    [Fact]
    public void RandChoiceTestContainZeroWeight() {
      var env = new Simulation(15);
      var source = new[] { "a", "b", "c" };
      var res1 = Enumerable.Range(1, 100)
        .Select(_ => env.RandChoice(source, new[] { 0.7, 0.2, 0 }));
      Assert.DoesNotContain("c", res1);
    }

    [Fact]
    public void RandChoiceTestTotalWeightMoreThanOne() {
      var source = new[] { "a", "b", "c" };

      var env1 = new Simulation(15);
      var res1 = Enumerable.Range(1, 100)
        .Select(_ => env1.RandChoice(source, new[] { 0.5, 0.3, 0.2 }));

      var env2 = new Simulation(15); new Simulation(15);
      var res2 = Enumerable.Range(1, 100)
        .Select(_ => env2.RandChoice(source, new[] { 5d, 3, 2 }));

      Assert.Equal(res1, res2);
    }
  }
}
