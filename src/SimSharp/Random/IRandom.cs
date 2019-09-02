#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

namespace SimSharp {
  public interface IRandom {
    int Next();
    int Next(int upperBound);
    int Next(int lowerBound, int upperBound);
    double NextDouble();

    void Reinitialise(int seed);
  }
}
