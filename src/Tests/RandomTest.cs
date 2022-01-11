using System;
using System.Collections.Generic;
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
      Assert.Throws<System.ArgumentException>(
        () => new EmpiricalNonUniform<string>(new[] { "a", "b" }, weights));
    }

    [Fact]
    public void RandChoiceTestContainZeroWeight() {
      var source = new[] { "a", "b", "c" };
      var dist = new EmpiricalNonUniform<string>(source, new[] { 0.7, 0.2, 0 });
      var rand = new PcgRandom();
      var res1 = Enumerable.Range(1, 100)
        .Select(_ => dist.Sample(rand));
      Assert.DoesNotContain("c", res1);
    }

    [Fact]
    public void RandChoiceTestTotalWeightMoreThanOne() {
      var source = new[] { "a", "b", "c" };
      var rand = new PcgRandom(15);
      var dist1 = new EmpiricalNonUniform<string>(source, new[] { 0.5, 0.3, 0.2 });
      var res1 = Enumerable.Range(1, 100)
        .Select(_ => dist1.Sample(rand)).ToArray();

      rand.Reinitialise(15);
      var dist2 = new EmpiricalNonUniform<string>(source, new[] { 5d, 3, 2 });
      var res2 = Enumerable.Range(1, 100)
        .Select(_ => dist2.Sample(rand)).ToArray();

      Assert.Equal(res1, res2);
    }

    [Fact]
    public void RandChoiceTests() {
      var source = new [] { "a", "b", "c", "d", "e", "f", "g" };
      var dist = new EmpiricalUniform<string>(source);
      var rand = new PcgRandom();
      for (var i = 0; i < 50; i++) {
        var env = new Simulation(i);

        Assert.Contains(env.Rand(dist), source);
        foreach (var s in env.Rand(dist, 5))
          Assert.Contains(s, source);
        Assert.Equal(4, env.Rand(dist, 4).Count());
        Assert.Equal(20, env.Rand(dist, 20).Count());
        Assert.Empty(env.Rand(dist, 0));
        Assert.Throws<ArgumentException>(() => env.Rand(dist, -1).ToList());

        Assert.Contains(EmpiricalUniform<string>.SampleOnline(rand, source), source);
        foreach (var s in EmpiricalUniform<string>.SampleOnline(rand, source, 5))
          Assert.Contains(s, source);
        Assert.Equal(4, EmpiricalUniform<string>.SampleOnline(rand, source, 4).Count());
        Assert.Equal(20, EmpiricalUniform<string>.SampleOnline(rand, source, 20).Count());
        Assert.Empty(EmpiricalUniform<string>.SampleOnline(rand, source, 0));
        Assert.Throws<ArgumentException>(() => EmpiricalUniform<string>.SampleOnline(rand, source, -1).ToList());

        Assert.Equal(source, dist.SampleNoRepetition(rand, source.Length));
        var sample = dist.SampleNoRepetition(rand,  4).ToList();
        Assert.Equal(source.Where(x => sample.Contains(x)), sample);
        Assert.Equal(5, dist.SampleNoRepetition(rand, 5).Distinct().Count());
        Assert.Empty(dist.SampleNoRepetition(rand, 0));
        Assert.Throws<ArgumentException>(() => dist.SampleNoRepetition(rand, source.Length + 1).ToList());
        Assert.Throws<ArgumentException>(() => dist.SampleNoRepetition(rand, -1).ToList());
      }
    }
  }
}
