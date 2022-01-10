#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System.Collections.Generic;

namespace SimSharp {
  public interface IDistribution<T> {
    /// <summary>
    /// Samples a value from the given distribution.
    /// </summary>
    /// <remarks>
    /// This method may throw a <see cref="System.InvalidOperationException"/>, e.g. when a sample cannot be obtained.
    /// This is the case when e.g. rejection sampling is used and an accepted sample could not be produced with the given number of tries.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">Thrown when a sample cannot be obtained.</exception>
    /// <param name="random">The pseudo random number generator to use</param>
    /// <returns>The sample from the distribution</returns>
    T Sample(IRandom random);
    /// <summary>
    /// Samples a range of values from the given distribution or draws values from observations with repetition.
    /// </summary>
    /// <param name="random">The pseudo random number generator to use</param>
    /// <param name="n">The number of samples that should be obtained</param>
    /// <returns>An array of length <paramref name="n"/></returns>
    IEnumerable<T> Sample(IRandom random, int n);
  }

  public interface IRejectionSampledDistribution<T> : IDistribution<T> {
    /// <summary>
    /// Try to obtain a sample, which may fail due to the use of rejection sampling.
    /// </summary>
    /// <param name="random">The pseudo random number generator to use</param>
    /// <param name="sample">The sample if sampling was successful.</param>
    /// <returns>Whether or not the sampling was successful.</returns>
    bool TrySample(IRandom random, out T sample);
  }
}