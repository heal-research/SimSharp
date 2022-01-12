#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimSharp {
  /// <summary>
  /// This class is for convenience use. If you put in your code file
  /// using static SimSharp.Distributions;
  /// you can use in your class the static methods to quickly generate various distributions.
  /// </summary>
  public static class Distributions {
    public static Constant<T> CONST<T>(T value) => new Constant<T>(value);
    public static Uniform UNIF(double lower, double upper) => new Uniform(lower, upper);
    public static UniformTime UNIF(TimeSpan lower, TimeSpan upper) => new UniformTime(lower, upper);
    public static Triangular TRI(double lower, double upper, double mode) => new Triangular(lower, upper, mode);
    public static Triangular TRI(double lower, double upper) => new Triangular(lower, upper);
    public static TriangularTime TRI(TimeSpan lower, TimeSpan upper, TimeSpan mode) => new TriangularTime(lower, upper, mode);
    public static TriangularTime TRI(TimeSpan lower, TimeSpan upper) => new TriangularTime(lower, upper);
    public static Exponential EXP(double mean) => new Exponential(mean);
    public static ExponentialTime EXP(TimeSpan mean) => new ExponentialTime(mean);
    public static Normal N(double mu, double sigma) => new Normal(mu, sigma);
    public static NormalTime N(TimeSpan mu, TimeSpan sigma) => new NormalTime(mu, sigma);
    public static LogNormal LNORM(double mu, double sigma) => new LogNormal(N(mu, sigma));
    public static LogNormalTime LNORM(TimeSpan mu, TimeSpan sigma) => new LogNormalTime(N(mu, sigma));
    public static LogNormal LNORM2(double mean, double stddev) => new LogNormal(mean, stddev);
    public static LogNormalTime LNORM2(TimeSpan mean, TimeSpan stddev) => new LogNormalTime(mean, stddev);
    public static Cauchy CAUCHY(double x0, double gamma) => new Cauchy(x0, gamma);
    public static CauchyTime CAUCHY(TimeSpan x0, double gamma) => new CauchyTime(x0, gamma);
    public static Weibull WEI(double alpha, double beta) => new Weibull(alpha, beta);
    public static Erlang ERL(int k, double lambda) => new Erlang(k, lambda);
    public static ErlangTime ERL(int k, TimeSpan lambda) => new ErlangTime(k, lambda);
    public static BoundedContinuous POS(IDistribution<double> dist, int ntries = 100) => new BoundedContinuous(dist, lower: 0, ntries: ntries, excludeLower: true);
    public static BoundedContinuous NEG(IDistribution<double> dist, int ntries = 100) => new BoundedContinuous(dist, upper: 0, ntries: ntries, excludeUpper: true);
    public static BoundedDiscrete POS(IDistribution<int> dist, int ntries = 100) => new BoundedDiscrete(dist, lower: 0, ntries: ntries, excludeLower: true);
    public static BoundedDiscrete NEG(IDistribution<int> dist, int ntries = 100) => new BoundedDiscrete(dist, upper: 0, ntries: ntries, excludeUpper: true);
    public static BoundedTime POS(IDistribution<TimeSpan> dist, int ntries = 100) => new BoundedTime(dist, lower: TimeSpan.Zero, ntries: ntries, excludeLower: true);
    public static BoundedTime NEG(IDistribution<TimeSpan> dist, int ntries = 100) => new BoundedTime(dist, upper: TimeSpan.Zero, ntries: ntries, excludeUpper: true);
  }

  public abstract class Distribution<T> : IDistribution<T> {
    public abstract T Sample(IRandom random);

    public virtual IEnumerable<T> Sample(IRandom random, int n) {
      if (n < 0) throw new ArgumentException("must be >= 0", nameof(n));
      for (var i = 0; i < n; i++)
        yield return Sample(random);
    }
  }

  /// <summary>
  /// This class wraps a constant value as a distribution
  /// </summary>
  public class Constant<T> : Distribution<T> {
    private readonly T _value;

    public Constant(T value) {
      _value = value;
    }

    /// <summary>
    /// Returns the constant value.
    /// </summary>
    /// <returns>The constant value</returns>
    public override T Sample(IRandom random) {
      return _value;
    }
  }

  /// <summary>
  /// This class enables uniform sampling from arbitrary types of observations
  /// </summary>
  /// <typeparam name="T">The type of observation</typeparam>
  public class EmpiricalUniform<T> : Distribution<T> {
    private readonly IReadOnlyList<T> _observations;

    /// <summary>
    /// Creates a new uniform distribution over a range of observations.
    /// </summary>
    /// <remarks>
    /// This method copies the observations to an array.
    /// </remarks>
    /// <param name="observations">The observations that should be sampled</param>
    public EmpiricalUniform(IEnumerable<T> observations) {
      _observations = observations.ToArray();
    }
    /// <summary>
    /// Creates a new uniform distribution over a range of observations.
    /// </summary>
    /// <remarks>
    /// This method reuses the given array.
    /// </remarks>
    /// <param name="observations">The observations that should be sampled</param>
    public EmpiricalUniform(IReadOnlyList<T> observations) {
      _observations = observations;
    }

    /// <summary>
    /// Uniformly draws one of the observations. Complexity is O(1).
    /// </summary>
    /// <param name="random">The pseudo random number generator to use</param>
    /// <returns>One of the observations</returns>
    public override T Sample(IRandom random) {
      return _observations[random.Next(_observations.Count)];
    }

    /// <summary>
    /// This method chooses <paramref name="count"/> elements from <parameref name="source"/> such that
    /// no element is selected twice. Respectively, elements that are M times in the source may also
    /// be selected up to M times.
    /// </summary>
    /// <remarks>
    /// The method is implemented to iterate over all elements respectively until <paramref name="count"/> elements
    /// are selected. Runtime complexity for selecting M out of a list of N elements is thus O(N).
    /// 
    /// Order is preserved, the items are returned in the same relative order as they appear in <paramref name="_observations"/>.
    /// 
    /// Parameter <paramref name="count"/> can be 0 in which case the enumerable will be empty.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="count"/> is negative or when there are not enough items in <paramerf name="source"/>
    /// to choose from.
    /// </exception>
    /// <param name="random">The pseudo random number generator to use</param>
    /// <param name="count">The number of elements to choose</param>
    /// <returns>An enumeration of the elements</returns>
    public IEnumerable<T> SampleNoRepetition(IRandom random, int count) {
      if (count <= 0) {
        if (count == 0) yield break;
        else throw new ArgumentException($"parameter {nameof(count)} is negative ({count})");
      }
      var remaining = count;
      foreach (var s in _observations) {
        if (random.NextDouble() * remaining < count) {
          count--;
          yield return s;
          if (count <= 0) yield break;
        }
        remaining--;
      }
      throw new ArgumentException($"there are not enough items in {nameof(_observations)} to choose {count} from without repetition.");
    }

    /// <summary>
    /// This methods chooses a single element from <paramref name="observations"/> randomly and by only enumerating
    /// the elements.
    /// </summary>
    /// <remarks>
    /// The preferred and faster method is to create a <see cref="EmpiricalUniform{T}"/> instance and call <see cref="EmpiricalUniform{T}.Sample"/>.
    /// Use of this method should be limited to cases where it is undesirable to reserve a contiguous block of
    /// memory for <paramref name="observations"/>.
    /// 
    /// This method iterates over all elements and calls the RNG each time, thus runtime complexity is O(N).
    /// </remarks>
    /// <param name="random">The random number generator to use</param>
    /// <param name="observations">The elements to choose from</param>
    /// <returns>The chosen element</returns>
    public static T SampleOnline(IRandom random, IEnumerable<T> observations) {
      var iter = observations.GetEnumerator();
      if (!iter.MoveNext()) throw new ArgumentException($"{nameof(observations)} is empty");
      var chosen = iter.Current;
      var count = 2;
      while (iter.MoveNext()) {
        if (count * random.NextDouble() < 1) {
          chosen = iter.Current;
        }
        count++;
      }
      return chosen;
    }

    /// <summary>
    /// This methods chooses <paramref name="count"/> element from <paramref name="observations"/> randomly and by only enumerating
    /// the elements.
    /// </summary>
    /// <remarks>
    /// The preferred and faster method is to create a <see cref="EmpiricalUniform{T}"/> instance and call <see cref="Distribution{T}.Sample(IRandom, int)"/>.
    /// Use of this method should be limited to cases where it is undesirable to reserve a contiguous block of
    /// memory for <paramref name="observations"/>.
    /// However, the method itself reserves an array of length <paramref name="count"/> and uses a single pass of all elements
    /// in <parmaref name="source"/>.
    /// 
    /// Using a count of 0 is possible and will return an array of length 0.
    /// 
    /// For selecting M from a source of N elements runtime complexity is O(N*M). The random number generator will also be called M*N times.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="count"/> is negative.</exception>
    /// <param name="random">The random number generator to use</param>
    /// <param name="observations">The elements to choose from</param>
    /// <param name="count">The number of elements to choose</param>
    /// <returns>An enumeration of the chosen elements</returns>
    public static IList<T> SampleOnline(IRandom random, IEnumerable<T> observations, int count) {
      if (count <= 0) {
        if (count == 0) return new T[0];
        else throw new ArgumentException($"parameter {nameof(count)} is negative ({count})");
      }
      var iter = observations.GetEnumerator();
      if (!iter.MoveNext()) throw new ArgumentException($"{nameof(observations)} is empty");
      var chosen = new T[count];
      for (var c = 0; c < count; c++) chosen[c] = iter.Current;
      var element = 2;
      while (iter.MoveNext()) {
        for (var c = 0; c < count; c++) {
          if (element * random.NextDouble() < 1) {
            chosen[c] = iter.Current;
          }
        }
        element++;
      }
      return chosen;
    }

    /// <summary>
    /// This method chooses <paramref name="count"/> elements from <parameref name="source"/> such that
    /// no element is selected twice. Respectively, elements that are M times in the source may also
    /// be selected up to M times.
    /// </summary>
    /// <remarks>
    /// The method is implemented to iterate over all elements respectively until <paramref name="count"/> elements
    /// are selected. Runtime complexity for selecting M out of a list of N elements is thus O(N).
    /// 
    /// Order is preserved, the items are returned in the same relative order as they appear in <paramref name="observations"/>.
    /// 
    /// Parameter <paramref name="count"/> can be 0 in which case the enumerable will be empty.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="count"/> is negative or when there are not enough items in <paramerf name="source"/>
    /// to choose from.
    /// </exception>
    /// <param name="random">The random number generator to use</param>
    /// <param name="observations">The elements to choose from</param>
    /// <param name="count">The number of elements to choose</param>
    /// <returns>An enumeration of the elements</returns>
    public static IEnumerable<T> SampleOnlineNoRepetition(IRandom random, IEnumerable<T> observations, int count) {
      if (count <= 0) {
        if (count == 0) yield break;
        else throw new ArgumentException($"parameter {nameof(count)} is negative ({count})");
      }
      var remaining = count;
      foreach (var s in observations) {
        if (random.NextDouble() * remaining < count) {
          count--;
          yield return s;
          if (count <= 0) yield break;
        }
        remaining--;
      }
      throw new ArgumentException($"there are not enough items in {nameof(observations)} to choose {count} from without repetition.");
    }
  }

  /// <summary>
  /// This class enables non-uniform sampling from arbitrary types of observations.
  /// Each observation is associated with a certain weight that represents the proportionality
  /// with which it should be sampled.
  /// </summary>
  /// <typeparam name="T">The type of observation</typeparam>
  public class EmpiricalNonUniform<T> : Distribution<T> {
    private readonly IReadOnlyList<T> _observations;
    private readonly IReadOnlyList<double> _weights;
    private readonly double _totalWeight;

    /// <summary>
    /// Creates a new uniform distribution over a range of observations.
    /// </summary>
    /// <remarks>
    /// This method copies the observations to an array.
    /// </remarks>
    /// <param name="observations">The observations that should be sampled</param>
    /// <param name="weights">The weights that are associated with the observations</param>
    public EmpiricalNonUniform(IEnumerable<T> observations, IEnumerable<double> weights) : this(observations.ToArray(), weights.ToArray()) { }
    /// <summary>
    /// Creates a new uniform distribution over a range of observations.
    /// </summary>
    /// <remarks>
    /// This method reuses the given arrays. Weights have to be strictly positive.
    /// </remarks>
    /// <exception name="System.ArgumentException">Thrown when the size of observations do not match, weights are not valid numbers or the sum of all weights is less or equal than 0</exception>
    /// <param name="observations">The observations that should be sampled</param>
    /// <param name="weights">The weights that are associated with the observations</param>
    public EmpiricalNonUniform(IReadOnlyList<T> observations, IReadOnlyList<double> weights) {
      _observations = observations;
      _weights = weights;
      if (_observations.Count != _weights.Count) throw new ArgumentException($"Length of observations ({_observations.Count}) and weights ({_weights.Count}) differs.");
      _totalWeight = 0;
      foreach (var w in _weights) {
        if (w < 0) throw new ArgumentException("weights must be greater or equal than 0", nameof(weights));
        _totalWeight += w;
      }
      if (double.IsNaN(_totalWeight) || double.IsInfinity(_totalWeight))
        throw new ArgumentException("Weights are not valid", nameof(weights));
      if (_totalWeight <= 0)
        throw new ArgumentException("total weight must be greater than 0", nameof(weights));
    }

    /// <summary>
    /// Sample proportionally from the observations using the given weights.
    /// </summary>
    /// <param name="random">The pseudo random number generator to use</param>
    /// <returns>One of the observations</returns>
    public override T Sample(IRandom random) {
      var rnd = random.NextDouble();
      double aggWeight = 0;
      int idx = 0;
      foreach (var w in _weights) {
        if (w > 0) {
          aggWeight += (w / _totalWeight);
          if (rnd <= aggWeight) {
            break;
          }
        }
        idx++;
      }
      return _observations[idx];
    }

    public static T Sample(IRandom random, IList<T> source, IList<double> weights) {      
      if (source.Count != weights.Count) {
        throw new ArgumentException("source and weights must have same size");
      }

      double totalW = 0;
      foreach (var w in weights) {
        if (w < 0) {
          throw new ArgumentException("weight values must be non-negative", nameof(weights));
        }
        totalW += w;
      }

      if (double.IsNaN(totalW) || double.IsInfinity(totalW))
        throw new ArgumentException("Not a valid weight", nameof(weights));
      if (totalW == 0)
        throw new ArgumentException("total weight must be greater than 0", nameof(weights));

      var rnd = random.NextDouble();
      double aggWeight = 0;
      int idx = 0;
      foreach (var w in weights) {
        if (w > 0) {
          aggWeight += (w / totalW);
          if (rnd <= aggWeight) {
            break;
          }
        }
        idx++;
      }
      return source[idx];
    }
  }

  /// <summary>
  /// The uniform distribution on a continuos interval
  /// </summary>
  /// <remarks>
  /// Basically, this is an IDistribution wrapper around IRandom
  /// </remarks>
  public class Uniform : Distribution<double> {
    private readonly double _lower, _upper;

    public double Lower => _lower;
    public double Upper => _upper;

    public double Mean => 0.5 * (_upper - _lower);
    public double StdDev => Math.Sqrt((_upper - _lower) * (_upper - _lower) / 12.0);

    public Uniform(double lower, double upperExclusive) {
      if (lower >= upperExclusive) throw new ArgumentException("must be < upperExclusive", nameof(lower));
      _lower = lower;
      _upper = upperExclusive;
    }

    public override double Sample(IRandom random) => Sample(random, _lower, _upper);
    public static double Sample(IRandom random, double lower, double upper) => lower + (upper - lower) * random.NextDouble();
  }

  /// <summary>
  /// The uniform distribution on a continuos interval
  /// </summary>
  /// <remarks>
  /// Basically, this is an IDistribution wrapper around IRandom
  /// </remarks>
  public class UniformTime : Distribution<TimeSpan> {
    private readonly TimeSpan _lower, _upper;

    public TimeSpan Lower => _lower;
    public TimeSpan Upper => _upper;
    public TimeSpan MeanTime => TimeSpan.FromSeconds(0.5 * (_upper - _lower).TotalSeconds);
    public TimeSpan StdDevTime => TimeSpan.FromSeconds(Math.Sqrt((_upper - _lower).TotalSeconds * (_upper - _lower).TotalSeconds / 12.0));

    public UniformTime(TimeSpan lower, TimeSpan upperExclusive) {
      if (lower >= upperExclusive) throw new ArgumentException("must be < upperExclusive", nameof(lower));
      _lower = lower;
      _upper = upperExclusive;
    }

    public override TimeSpan Sample(IRandom random) => Sample(random, _lower, _upper);
    public static TimeSpan Sample(IRandom random, TimeSpan lower, TimeSpan upper) => lower + TimeSpan.FromSeconds((upper - lower).TotalSeconds * random.NextDouble());
  }

  /// <summary>
  /// The uniform distribution on a discrete interval.
  /// </summary>
  /// <remarks>
  /// Basically, this is an IDistribution wrapper around IRandom
  /// </remarks>
  public class UniformDiscrete : Distribution<int> {
    private readonly int _lower, _upper;

    public int Lower => _lower;
    public int Upper => _upper;

    public double Mean => 0.5 * (_upper - _lower);
    public double StdDev => Math.Sqrt(((_upper - _lower + 1) * (_upper - _lower + 1) - 1) / 12.0);
    public TimeSpan MeanTime => TimeSpan.FromSeconds(Mean);
    public TimeSpan StdDevTime => TimeSpan.FromSeconds(StdDev);

    public UniformDiscrete(int lower, int upperExclusive) {
      if (lower >= upperExclusive) throw new ArgumentException("must be < upperExclusive", nameof(lower));
      _lower = lower;
      _upper = upperExclusive;
    }

    public override int Sample(IRandom random) => random.Next(_lower, _upper);
    public static double Sample(IRandom random, int lower, int upper) => random.Next(lower, upper);
  }

  /// <summary>
  /// The triangular distribution has a fixed minimum and maximum and a peak that can be anywhere in between.
  /// </summary>
  public class Triangular : Distribution<double> {
    private readonly double _lower, _upper, _mode;

    public double Lower => _lower;
    public double Upper => _upper;
    public double Mode => _mode;

    public double Mean => (_lower + _upper + _mode) / 3.0;
    public double StdDev => Math.Sqrt((_lower * _lower + _upper * _upper + _mode * _mode  - _lower * _upper - _lower * _mode - _upper * _mode) / 18.0);

    public Triangular(double lower, double upper) : this(lower, upper, (upper - lower) / 2.0) { }
    public Triangular(double lower, double upper, double mode) {
      if (lower >= upper) throw new ArgumentException("must be smaller than upper", nameof(lower));
      if (mode < lower || mode > upper) throw new ArgumentException($"must be in the interval [{lower};{upper}]", nameof(mode));
      _lower = lower;
      _upper = upper;
      _mode = mode;
    }

    public override double Sample(IRandom random) => Sample(random, _lower, _upper, _mode);
    public static double Sample(IRandom random, double lower, double upper, double mode) {
      var u = random.NextDouble();
      var c = (mode - lower) / (upper - lower);
      if (u > c)
        return upper + (lower - upper) * Math.Sqrt(((1.0 - u) * (1.0 - c)));
      return lower + (upper - lower) * Math.Sqrt(u * c);
    }
    public static double Sample(IRandom random, double lower, double upper) {
      var u = random.NextDouble();
      if (u > 0.5)
        return upper + (lower - upper) * Math.Sqrt(((1.0 - u) / 2));
      return lower + (upper - lower) * Math.Sqrt(u / 2);
    }
  }
  /// <summary>
  /// The triangular distribution has a fixed minimum and maximum and a peak that can be anywhere in between.
  /// </summary>
  public class TriangularTime : Distribution<TimeSpan> {
    private readonly TimeSpan _lower, _upper, _mode;

    public TimeSpan Lower => _lower;
    public TimeSpan Upper => _upper;
    public TimeSpan Mode => _mode;

    public TimeSpan Mean => TimeSpan.FromSeconds((_lower + _upper + _mode).TotalSeconds / 3.0);
    public TimeSpan StdDev => TimeSpan.FromSeconds(Math.Sqrt((_lower.TotalSeconds * _lower.TotalSeconds + _upper.TotalSeconds * _upper.TotalSeconds + _mode.TotalSeconds * _mode.TotalSeconds  - _lower.TotalSeconds * _upper.TotalSeconds - _lower.TotalSeconds * _mode.TotalSeconds - _upper.TotalSeconds * _mode.TotalSeconds) / 18.0));

    public TriangularTime(TimeSpan lower, TimeSpan upper) : this(lower, upper, TimeSpan.FromSeconds(0.5 * (upper - lower).TotalSeconds)) { }
    public TriangularTime(TimeSpan lower, TimeSpan upper, TimeSpan mode) {
      if (lower >= upper) throw new ArgumentException("must be smaller than upper", nameof(lower));
      if (mode < lower || mode > upper) throw new ArgumentException($"must be in the interval [{lower};{upper}]", nameof(mode));
      _lower = lower;
      _upper = upper;
      _mode = mode;
    }

    public override TimeSpan Sample(IRandom random) => Sample(random, _lower, _upper, _mode);
    public static TimeSpan Sample(IRandom random, TimeSpan lower, TimeSpan upper, TimeSpan mode) {
      var u = random.NextDouble();
      var c = (mode - lower).TotalSeconds / (upper - lower).TotalSeconds;
      if (u > c)
        return upper + TimeSpan.FromSeconds((lower - upper).TotalSeconds * Math.Sqrt(((1.0 - u) * (1.0 - c))));
      return lower + TimeSpan.FromSeconds((upper - lower).TotalSeconds * Math.Sqrt(u * c));
    }
    public static TimeSpan Sample(IRandom random, TimeSpan lower, TimeSpan upper) {
      var u = random.NextDouble();
      if (u > 0.5)
        return upper + TimeSpan.FromSeconds((lower - upper).TotalSeconds * Math.Sqrt(((1.0 - u) / 2)));
      return lower + TimeSpan.FromSeconds((upper - lower).TotalSeconds * Math.Sqrt(u / 2));
    }
  }

  /// <summary>
  /// The exponential distribution is memoryless and is often used to model interarrival times.
  /// All samples that follow an exponential distribution are positive.
  /// </summary>
  public class Exponential : Distribution<double> {
    private readonly double _mean;
    public double Mean => _mean;
    public double StdDev => 1.0 / _mean;

    public Exponential(double mean) {
      if (mean <= 0) throw new ArgumentException("must be > 0", nameof(mean));
      _mean = mean;
    }

    public override double Sample(IRandom random) => Sample(random, _mean);
    public static double Sample(IRandom random, double mean) => -Math.Log(1 - random.NextDouble()) * mean;
  }

  /// <summary>
  /// The exponential distribution is memoryless and is often used to model interarrival times.
  /// All samples that follow an exponential distribution are positive.
  /// </summary>
  public class ExponentialTime : Distribution<TimeSpan> {
    private readonly TimeSpan _mean;
    public TimeSpan Mean => _mean;
    public TimeSpan StdDev => TimeSpan.FromSeconds(1.0 / _mean.TotalSeconds);

    public ExponentialTime(TimeSpan mean) {
      if (mean <= TimeSpan.Zero) throw new ArgumentException("must be > TimeSpan.Zero", nameof(mean));
      _mean = mean;
    }

    public override TimeSpan Sample(IRandom random) => Sample(random, _mean);
    public static TimeSpan Sample(IRandom random, TimeSpan mean) => TimeSpan.FromSeconds(-Math.Log(1 - random.NextDouble()) * mean.TotalSeconds);
  }

  /// <summary>
  /// The normal or Gaussian distribution is perhaps the most famous distribution.
  /// In this class, normal distributed variates are generated using the Marsaglia polar method.
  /// </summary>
  public class Normal : Distribution<double>, IStatefulDistribution<double> {
    private readonly double _mu, _sigma;
    private bool _useSpareNormal;
    private double _spareNormal;

    public double Mean => _mu;
    public double StdDev => _sigma;

    public Normal(double mu, double sigma) {
      if (sigma < 0) throw new ArgumentException("must be >= 0", nameof(sigma));
      _mu = mu;
      _sigma = sigma;
      _useSpareNormal = false;
      _spareNormal = double.NaN;
    }

    public override double Sample(IRandom random) {
      if (_sigma == 0) return _mu;
      if (_useSpareNormal) {
        _useSpareNormal = false;
        return _spareNormal * _sigma + _mu;
      } else {
        _useSpareNormal = true;
        return MarsagliaPolar(random, _mu, _sigma, out _spareNormal);
      }
    }

    public static double Sample(IRandom random, double mu, double sigma) => MarsagliaPolar(random, mu, sigma, out _);
    internal static double MarsagliaPolar(IRandom random, double mu, double sigma, out double spare) {
      double u, v, s;
      do {
        u = random.NextDouble() * 2 - 1;
        v = random.NextDouble() * 2 - 1;
        s = u * u + v * v;
      } while (s > 1 || s == 0);
      var mul = Math.Sqrt(-2.0 * Math.Log(s) / s);
      spare = v * mul;
      return mu + sigma * u * mul;
    }

    /// <summary>
    /// Normal has a state in that the Marsaglia polar method generates two random variates per call
    /// and the other variate is remembered. So the method is called only every second time that
    /// <see cref="Sample(IRandom)"/> is called.
    /// </summary>
    public void Reset() {
      _useSpareNormal = false;
    }
  }
  
  /// <summary>
  /// The normal or Gaussian distribution is perhaps the most famous distribution.
  /// In this class, normal distributed variates are generated using the Marsaglia polar method.
  /// </summary>
  public class NormalTime : Distribution<TimeSpan>, IStatefulDistribution<TimeSpan> {
    private readonly TimeSpan _mu, _sigma;
    private bool _useSpareNormal;
    private double _spareNormal;

    public TimeSpan Mean => _mu;
    public TimeSpan StdDev => _sigma;

    public NormalTime(TimeSpan mu, TimeSpan sigma) {
      if (sigma < TimeSpan.Zero) throw new ArgumentException("must be >= TimeSpan.Zero", nameof(sigma));
      _mu = mu;
      _sigma = sigma;
      _useSpareNormal = false;
      _spareNormal = 0;
    }

    public override TimeSpan Sample(IRandom random) {
      if (_sigma == TimeSpan.Zero) return _mu;
      if (_useSpareNormal) {
        _useSpareNormal = false;
        return TimeSpan.FromSeconds(_spareNormal * _sigma.TotalSeconds + _mu.TotalSeconds);
      } else {
        _useSpareNormal = true;
        return TimeSpan.FromSeconds(Normal.MarsagliaPolar(random, _mu.TotalSeconds, _sigma.TotalSeconds, out _spareNormal));
      }
    }

    public static TimeSpan Sample(IRandom random, TimeSpan mu, TimeSpan sigma) => TimeSpan.FromSeconds(Normal.MarsagliaPolar(random, mu.TotalSeconds, sigma.TotalSeconds, out _));

    /// <summary>
    /// Normal has a state in that the Marsaglia polar method generates two random variates per call
    /// and the other variate is remembered. So the method is called only every second time that
    /// <see cref="Sample(IRandom)"/> is called.
    /// </summary>
    public void Reset() {
      _useSpareNormal = false;
    }
  }

  /// <summary>
  /// The log-normal distribution has only positive samples. In this class a <see cref="Normal"/> distributed random variate is transformed.
  /// </summary>
  public class LogNormal : Distribution<double>, IStatefulDistribution<double> {
    private readonly Normal _normal;

    public double Mu => _normal.Mean;
    public double Sigma => _normal.StdDev;

    public double Mean { get; }
    public double StdDev { get; }

    /// <summary>
    /// Creates a log-normal distribution with a desired mean and standard deviation. Note that is not
    /// the same as the mu and sigma values of the log-normal distribution. If you wish to parameterize
    /// using mu and sigma, create a <see cref="Normal"/> with these parameters and pass it to the
    /// <see cref="LogNormal(Normal)"/> constructor.
    /// </summary>
    /// <param name="mean">The desired mean of the distribution</param>
    /// <param name="stdev">The desired standard deviation of the distribution</param>
    public LogNormal(double mean, double stdev) {
      if (stdev < 0) throw new ArgumentException("must be >= 0", nameof(stdev));
      Mean = mean;
      StdDev = stdev;
      var sigma = Math.Sqrt(Math.Log(stdev * stdev / (mean * mean) + 1));
      var mu = Math.Log(mean) - 0.5 * sigma * sigma;
      _normal = new Normal(mu, sigma);
    }
    /// <summary>
    /// Wraps the normal distribution into a log-normal. Note that the mean and standard deviation
    /// will change. If you want a log-normal distribution with a certain mean, consider the
    /// <see cref="LogNormal(double, double)"/> constructor instead.
    /// </summary>
    /// <param name="normal">The normal distribution to wrap.</param>
    public LogNormal(Normal normal) {
      if (normal == null) throw new ArgumentNullException(nameof(normal));
      Mean = Math.Exp(normal.Mean + (0.5 * normal.StdDev * normal.StdDev));
      StdDev = Math.Sqrt((Math.Exp(normal.StdDev * normal.StdDev) - 1) * Math.Exp(2 * normal.Mean + normal.StdDev * normal.StdDev));
      _normal = normal;
    }

    public override double Sample(IRandom random) {
      return Math.Exp(_normal.Sample(random));
    }

    public static double Sample(IRandom random, double mean, double stdev) {      
      var sigma = Math.Sqrt(Math.Log(stdev * stdev / (mean * mean) + 1));
      var mu = Math.Log(mean) - 0.5 * sigma * sigma;
      return Normal.Sample(random, mu, sigma);
    }

    /// <summary>
    /// As this method uses a normal distribution as its base, it resets the state
    /// </summary>
    public void Reset() {
      _normal.Reset();
    }
  }

  /// <summary>
  /// The log-normal distribution has only positive samples. In this class a <see cref="Normal"/> distributed random variate is transformed.
  /// </summary>
  public class LogNormalTime : Distribution<TimeSpan>, IStatefulDistribution<TimeSpan> {
    private readonly NormalTime _normal;

    public TimeSpan Mu => _normal.Mean;
    public TimeSpan Sigma => _normal.StdDev;

    public TimeSpan Mean { get; }
    public TimeSpan StdDev { get; }

    /// <summary>
    /// Creates a log-normal distribution with a desired mean and standard deviation. Note that is not
    /// the same as the mu and sigma values of the log-normal distribution. If you wish to parameterize
    /// using mu and sigma, create a <see cref="NormalTime"/> with these parameters and pass it to the
    /// <see cref="LogNormalTime(NormalTime)"/> constructor.
    /// </summary>
    /// <param name="mean">The desired mean of the distribution</param>
    /// <param name="stdev">The desired standard deviation of the distribution</param>
    public LogNormalTime(TimeSpan mean, TimeSpan stdev) {
      if (stdev < TimeSpan.Zero) throw new ArgumentException("must be >= TimeSpan.Zero", nameof(stdev));
      Mean = mean;
      StdDev = stdev;
      var sigma = Math.Sqrt(Math.Log(stdev.TotalSeconds * stdev.TotalSeconds / (mean.TotalSeconds * mean.TotalSeconds) + 1));
      var mu = Math.Log(mean.TotalSeconds) - 0.5 * sigma * sigma;
      _normal = new NormalTime(TimeSpan.FromSeconds(mu), TimeSpan.FromSeconds(sigma));
    }
    /// <summary>
    /// Wraps the normal distribution into a log-normal. Note that the mean and standard deviation
    /// will change. If you want a log-normal distribution with a certain mean, consider the
    /// <see cref="LogNormalTime(TimeSpan, TimeSpan)"/> constructor instead.
    /// </summary>
    /// <param name="normal">The normal distribution to wrap.</param>
    public LogNormalTime(NormalTime normal) {
      if (normal == null) throw new ArgumentNullException(nameof(normal));
      Mean = TimeSpan.FromSeconds(Math.Exp(normal.Mean.TotalSeconds + (0.5 * normal.StdDev.TotalSeconds * normal.StdDev.TotalSeconds)));
      StdDev = TimeSpan.FromSeconds(Math.Sqrt((Math.Exp(normal.StdDev.TotalSeconds * normal.StdDev.TotalSeconds) - 1) * Math.Exp(2 * normal.Mean.TotalSeconds + normal.StdDev.TotalSeconds * normal.StdDev.TotalSeconds)));
      _normal = normal;
    }

    public override TimeSpan Sample(IRandom random) {
      return TimeSpan.FromSeconds(Math.Exp(_normal.Sample(random).TotalSeconds));
    }

    public static TimeSpan Sample(IRandom random, TimeSpan mean, TimeSpan stdev) {      
      var sigma = Math.Sqrt(Math.Log(stdev.TotalSeconds * stdev.TotalSeconds / (mean.TotalSeconds * mean.TotalSeconds) + 1));
      var mu = Math.Log(mean.TotalSeconds) - 0.5 * sigma * sigma;
      return TimeSpan.FromSeconds(Normal.Sample(random, mu, sigma));
    }

    /// <summary>
    /// As this method uses a normal distribution as its base, it resets the state
    /// </summary>
    public void Reset() {
      _normal.Reset();
    }
  }

  /// <summary>
  /// The Cauchy distributio is similar to the <see cref="Normal" /> distribution, but has fatter tails.
  /// </summary>
  public class Cauchy : Distribution<double> {
    private readonly double _x0, _gamma;

    public double X0 => _x0;
    public double Gamma => _gamma;

    public Cauchy(double x0, double gamma) {
      if (gamma <= 0) throw new ArgumentException("must be > 0", nameof(gamma));
      _x0 = x0;
      _gamma = gamma;
    }

    public override double Sample(IRandom random) {
      return Sample(random, _x0, _gamma);
    }

    public static double Sample(IRandom random, double x0, double gamma) =>
      x0 + gamma * Math.Tan(Math.PI * (random.NextDouble() - 0.5));
  }

  /// <summary>
  /// The Cauchy distributio is similar to the <see cref="Normal" /> distribution, but has fatter tails.
  /// </summary>
  public class CauchyTime : Distribution<TimeSpan> {
    private readonly TimeSpan _x0;
    private readonly double _gamma;

    public TimeSpan X0 => _x0;
    public double Gamma => _gamma;

    public CauchyTime(TimeSpan x0, double gamma) {
      if (gamma <= 0) throw new ArgumentException("must be > 0", nameof(gamma));
      _x0 = x0;
      _gamma = gamma;
    }

    public override TimeSpan Sample(IRandom random) {
      return Sample(random, _x0, _gamma);
    }

    public static TimeSpan Sample(IRandom random, TimeSpan x0, double gamma) =>
      x0 + TimeSpan.FromSeconds(gamma * Math.Tan(Math.PI * (random.NextDouble() - 0.5)));
  }

  /// <summary>
  /// The Weibull distribution generates only positive values.
  /// </summary>
  public class Weibull : Distribution<double> {
    private readonly double _alpha, _beta;
    public double Alpha => _alpha;
    public double Beta => _beta;

    public Weibull(double alpha, double beta) {
      if (alpha <= 0) throw new ArgumentException("must be > 0", nameof(alpha));
      if (beta <= 0) throw new ArgumentException("must be > 0", nameof(beta));
      _alpha = alpha;
      _beta = beta;
    }

    public override double Sample(IRandom random) {
      return Sample(random, _alpha, _beta);
    }

    public static double Sample(IRandom random, double alpha, double beta) =>
      beta * Math.Pow(-Math.Log(1 - random.NextDouble()), 1 / alpha);
  }

  /// <summary>
  /// The Erlang distribution describes the sum of k identically distributed exponential distributions
  /// </summary>
  public class Erlang : Distribution<double> {
    private readonly int _k;
    private readonly double _lambda;

    public int K => _k;
    public double Lambda => _lambda;

    public double Mean => _k / _lambda;
    public double StdDev => Math.Sqrt(_k) / _lambda;
    
    public Erlang(int k, double lambda) {
      if (k <= 0) throw new ArgumentException("must be > 0", nameof(k));
      if (lambda <= 0) throw new ArgumentException("must be > 0", nameof(lambda));
      _k = k;
      _lambda = lambda;
    }

    public override double Sample(IRandom random) {
      return Sample(random, _k, _lambda);
    }

    public static double Sample(IRandom random, int k, double lambda) {
      var prod = 1.0;
      for (var i = 0; i < k; i++)
        prod *= (1.0 - random.NextDouble());
      return -Math.Log(prod) / lambda;
    }
  }

  /// <summary>
  /// The Erlang distribution describes the sum of k identically distributed exponential distributions
  /// </summary>
  public class ErlangTime : Distribution<TimeSpan> {
    private readonly int _k;
    private readonly TimeSpan _lambda;

    public int K => _k;
    public TimeSpan Lambda => _lambda;

    public TimeSpan Mean => TimeSpan.FromSeconds(_k / _lambda.TotalSeconds);
    public TimeSpan StdDev => TimeSpan.FromSeconds(Math.Sqrt(_k) / _lambda.TotalSeconds);
    
    public ErlangTime(int k, TimeSpan lambda) {
      if (k <= 0) throw new ArgumentException("must be > 0", nameof(k));
      if (lambda <= TimeSpan.Zero) throw new ArgumentException("must be > TimeSpan.Zero", nameof(lambda));
      _k = k;
      _lambda = lambda;
    }

    public override TimeSpan Sample(IRandom random) {
      return Sample(random, _k, _lambda);
    }

    public static TimeSpan Sample(IRandom random, int k, TimeSpan lambda) {
      var prod = 1.0;
      for (var i = 0; i < k; i++)
        prod *= (1.0 - random.NextDouble());
      return TimeSpan.FromSeconds(-Math.Log(prod) / lambda.TotalSeconds);
    }
  }

  public abstract class Bounded<T> : Distribution<T>, IRejectionSampledDistribution<T>, IStatefulDistribution<T> {
    protected readonly IDistribution<T> distribution;
    protected readonly T lower, upper;
    protected readonly int ntries;
    protected readonly bool excludeLower, excludeUpper;

    public T Lower => lower;
    public T Upper => upper;

    public Bounded(IDistribution<T> distribution, T lower, T upper,
        int ntries = 100, bool excludeLower = false, bool excludeUpper = false) {
      if (distribution == null) throw new ArgumentNullException(nameof(distribution));
      this.distribution = distribution;
      this.lower = lower;
      this.upper = upper;
      this.ntries = ntries;
      this.excludeLower = excludeLower;
      this.excludeUpper = excludeUpper;
    }

    public override T Sample(IRandom random) {
      if (TrySample(random, out var sample))
        return sample;
      var strRange = (excludeLower ? "]" : "[") + lower + ":" + upper + (excludeUpper ? "[" : "]");
      throw new InvalidOperationException($"Unable to sample a value in the interval {strRange} in {ntries} tries.");
    }

    public bool TrySample(IRandom random, out T sample) {
      for (var n = 0; n < ntries; n++) {
        var s = distribution.Sample(random);
        if (OutsideBounds(s)) continue;
        sample = s;
        return true;
      }
      sample = default(T);
      return false;
    }

    public void Reset() {
      if (distribution is IStatefulDistribution<T> d)
        d.Reset();
    }

    protected abstract bool OutsideBounds(T sample);
  }

  /// <summary>
  /// This wraps any distribution and obtains samples only within certain bounds using rejection sampling.
  /// </summary>
  public class BoundedContinuous : Bounded<double> {
    /// <summary>
    /// Creates a new bounded distribution that wraps a continuous distribution.
    /// </summary>
    /// <param name="distribution">The distribution to be wrapped</param>
    /// <param name="lower">The lower bound or -∞ if omitted</param>
    /// <param name="upper">The upper bound or ∞ if omitted</param>
    /// <param name="ntries">The number of tries during rejection sampling</param>
    /// <param name="excludeLower">Whether the lower bound is exclusive</param>
    /// <param name="excludeUpper">Whether the upper bound is exclusive</param>
    public BoundedContinuous(IDistribution<double> distribution, double lower = double.NegativeInfinity,
        double upper = double.PositiveInfinity, int ntries = 100, bool excludeLower = false, bool excludeUpper = false)
        : base(distribution, lower, upper, ntries, excludeLower, excludeUpper) {
    }
    protected override bool OutsideBounds(double s) =>
      excludeLower && s <= lower || !excludeLower && s < lower
      || excludeUpper && s >= upper || !excludeUpper && s > upper;
  }

  /// <summary>
  /// This wraps any distribution and obtains samples only within certain bounds using rejection sampling.
  /// </summary>
  public class BoundedDiscrete : Bounded<int> {
    /// <summary>
    /// Creates a new bounded distribution that wraps a continuous distribution.
    /// </summary>
    /// <param name="distribution">The distribution to be wrapped</param>
    /// <param name="lower">The lower bound or -2147483648 if omitted</param>
    /// <param name="upper">The upper bound or 2147483647 if omitted</param>
    /// <param name="ntries">The number of tries during rejection sampling</param>
    /// <param name="excludeLower">Whether the lower bound is exclusive</param>
    /// <param name="excludeUpper">Whether the upper bound is exclusive</param>
    public BoundedDiscrete(IDistribution<int> distribution, int lower = int.MinValue, int upper = int.MaxValue,
        int ntries = 100, bool excludeLower = false, bool excludeUpper = false)
        : base(distribution, lower, upper, ntries, excludeLower, excludeUpper) {
    }
    protected override bool OutsideBounds(int s) =>
      excludeLower && s <= lower || !excludeLower && s < lower
      || excludeUpper && s >= upper || !excludeUpper && s > upper;
  }

  /// <summary>
  /// This wraps any distribution and obtains samples only within certain bounds using rejection sampling.
  /// </summary>
  public class BoundedTime : Bounded<TimeSpan> {
    /// <summary>
    /// Creates a new bounded distribution that wraps a continuous distribution.
    /// </summary>
    /// <param name="distribution">The distribution to be wrapped</param>
    /// <param name="lower">The lower bound or TimeSpan.MinValue if omitted</param>
    /// <param name="upper">The upper bound or TimeSpan.MaxValue if omitted</param>
    /// <param name="ntries">The number of tries during rejection sampling</param>
    /// <param name="excludeLower">Whether the lower bound is exclusive</param>
    /// <param name="excludeUpper">Whether the upper bound is exclusive</param>
    public BoundedTime(IDistribution<TimeSpan> distribution, TimeSpan? lower = null, TimeSpan? upper = null,
        int ntries = 100, bool excludeLower = false, bool excludeUpper = false)
        : base(distribution, lower ?? TimeSpan.MinValue, upper ?? TimeSpan.MaxValue, ntries, excludeLower, excludeUpper) {
    }
    protected override bool OutsideBounds(TimeSpan s) =>
      excludeLower && s <= lower || !excludeLower && s < lower
      || excludeUpper && s >= upper || !excludeUpper && s > upper;
  }
}