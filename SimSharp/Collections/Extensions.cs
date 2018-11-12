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
