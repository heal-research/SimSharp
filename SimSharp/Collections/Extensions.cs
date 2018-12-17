#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2018  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
using System.Collections.Generic;
using System.Linq;

namespace SimSharp {
  public static class Extensions {
    public static IEnumerable<T> MaxItems<T>(this IEnumerable<T> source, Func<T, IComparable> selector) {
      var enumerator = source.GetEnumerator();
      if (!enumerator.MoveNext()) return Enumerable.Empty<T>();
      var item = enumerator.Current;
      var max = selector(item);
      var result = new List<T> { item };
      
      while (enumerator.MoveNext()) {
        item = enumerator.Current;
        var comparable = selector(item);
        var comparison = comparable.CompareTo(max);
        if (comparison > 0) {
          result.Clear();
          result.Add(item);
          max = comparable;
        } else if (comparison == 0) {
          result.Add(item);
        }
      }
      return result;
    }
  }
}
