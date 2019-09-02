#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;

namespace SimSharp {
  public class ReverseComparer<T> : IComparer<T> where T : IComparable {
    private static readonly IComparer<T> DefaultComparer = Comparer<T>.Default;

    public int Compare(T x, T y) {
      return DefaultComparer.Compare(y, x);
    }
  }
}
