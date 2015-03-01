
using System.Collections.Generic;

namespace SimSharp {
  public class AnyOf : Condition {
    public AnyOf(Environment environment, params Event[] events) : base(environment, events) { }
    public AnyOf(Environment environment, IEnumerable<Event> events) : base(environment, events) { }

    protected override bool Evaluate() {
      return FiredEvents.Count > 0 || Events.Count == 0;
    }
  }
}
