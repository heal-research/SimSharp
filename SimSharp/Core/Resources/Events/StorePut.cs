using System;

namespace SimSharp {
  public class StorePut : Event {
    public DateTime Time { get; private set; }
    public Process Process { get; private set; }

    public StorePut(Environment environment, Action<Event> callback, object value)
      : base(environment) {
      if (value == null) throw new ArgumentNullException("value", "Value to put in a Store cannot be null.");
      CallbackList.Add(callback);
      Value = value;
      Time = environment.Now;
      Process = environment.ActiveProcess;
    }
  }
}
