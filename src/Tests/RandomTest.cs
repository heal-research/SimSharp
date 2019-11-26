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
  }
}
