namespace SimSharp {
  public class PcgRandom : IRandom {
    private Pcg pcg;

    public PcgRandom() {
      pcg = new Pcg();
    }

    public PcgRandom(int seed) {
      pcg = new Pcg(seed);
    }
    public int Next() {
      return pcg.Next();
    }

    public int Next(int upperBound) {
      return pcg.Next(upperBound);
    }

    public int Next(int lowerBound, int upperBound) {
      return pcg.Next(lowerBound, upperBound);
    }

    public double NextDouble() {
      return pcg.NextDouble();
    }

    public void Reinitialise(int seed) {
      pcg = new Pcg(seed);
    }
  }
}
