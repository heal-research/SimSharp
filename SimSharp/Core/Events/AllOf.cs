
using System.Collections.Generic;

namespace SimSharp {
  public class AllOf : Condition {
    public AllOf(Environment environment, params Event[] events) : base(environment, events) { }
    public AllOf(Environment environment, IEnumerable<Event> events) : base(environment, events) { }

    protected override bool Evaluate() {
      return FiredEvents.Count == Events.Count;
    }
  }
}
