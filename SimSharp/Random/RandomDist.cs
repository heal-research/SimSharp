#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2014  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
  public static class RandomDist {
    private static readonly double NormalMagicConst = 4 * Math.Exp(-0.5) / Math.Sqrt(2.0);

    public static double Uniform(Random urand, double a, double b) {
      return a + (b - a) * urand.NextDouble();
    }

    public static double Triangular(Random random, double low, double high) {
      var u = random.NextDouble();
      if (u > 0.5)
        return high + (low - high) * Math.Sqrt(((1.0 - u) / 2));
      return low + (high - low) * Math.Sqrt(u / 2);
    }
    public static double Triangular(Random random, double low, double high, double mode) {
      var u = random.NextDouble();
      var c = (mode - low) / (high - low);
      if (u > c)
        return high + (low - high) * Math.Sqrt(((1.0 - u) * (1.0 - c)));
      return low + (high - low) * Math.Sqrt(u * c);
    }

    public static double Exponential(Random urand, double lambda) {
      return -Math.Log(1 - urand.NextDouble()) / lambda;
    }

    public static double Normal(Random urand, double mu, double sigma) {
      double z, zz, u1, u2;
      do {
        u1 = urand.NextDouble();
        u2 = 1 - urand.NextDouble();
        z = NormalMagicConst * (u1 - 0.5) / u2;
        zz = z * z / 4.0;
      } while (zz > -Math.Log(u2));
      return mu + z * sigma;
    }

    public static double LogNormal(Random urand, double mu, double sigma) {
      return Math.Exp(Normal(urand, mu, sigma));
    }

    public static double Cauchy(Random urand, double x0, double gamma) {
      return x0 + gamma * Math.Tan(Math.PI * (urand.NextDouble() - 0.5));
    }

    public static double Weibull(Random urand, double alpha, double beta) {
      return alpha * Math.Pow(-Math.Log(1 - urand.NextDouble()), 1 / beta);
    }
  }
}
