using System;

namespace SimSharp {
  public class StoreGet : Event {
    public DateTime Time { get; private set; }
    public Process Process { get; private set; }

    public StoreGet(Environment environment, Action<Event> callback)
      : base(environment) {
      CallbackList.Add(callback);
      Time = environment.Now;
      Process = environment.ActiveProcess;
    }
  }
}
