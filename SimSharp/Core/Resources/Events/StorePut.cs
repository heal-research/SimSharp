using System;

namespace SimSharp {
  public class StorePut : Event {
    public object Item { get; protected set; }
    public DateTime Time { get; private set; }
    public Process Process { get; private set; }

    public StorePut(Environment environment, Action<Event> callback, object item)
      : base(environment) {
      Item = item;
      CallbackList.Add(callback);
      Time = environment.Now;
      Process = environment.ActiveProcess;
    }
  }
}
