#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2019  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace SimSharp.Tests {

  public class ResourceTest {
    [Fact]
    public void TestResource() {
      var start = new DateTime(2014, 4, 1);
      var env = new Simulation(start);
      var resource = new Resource(env, capacity: 1);

      Assert.Equal(1, resource.Capacity);
      Assert.Equal(0, resource.InUse);

      var log = new Dictionary<string, DateTime>();
      env.Process(TestResourceProc(env, "a", resource, log));
      env.Process(TestResourceProc(env, "b", resource, log));
      env.Run();

      Assert.Equal(start + TimeSpan.FromSeconds(1), log["a"]);
      Assert.Equal(start + TimeSpan.FromSeconds(2), log["b"]);
    }
    private IEnumerable<Event> TestResourceProc(Simulation env, string name, Resource resource,
      Dictionary<string, DateTime> log) {
      var req = resource.Request();
      yield return req;
      Assert.Equal(1, resource.InUse);

      yield return env.Timeout(TimeSpan.FromSeconds(1));
      resource.Release(req);

      log.Add(name, env.Now);
    }

    [Fact]
    public void TestResourceWithUsing() {
      var start = new DateTime(2014, 4, 1);
      var env = new Simulation(start);
      var resource = new Resource(env, capacity: 1);

      Assert.Equal(1, resource.Capacity);
      Assert.Equal(0, resource.InUse);

      var log = new Dictionary<string, DateTime>();
      env.Process(TestResourceWithUsingProc(env, "a", resource, log));
      env.Process(TestResourceWithUsingProc(env, "b", resource, log));
      env.Run();

      Assert.Equal(start + TimeSpan.FromSeconds(1), log["a"]);
      Assert.Equal(start + TimeSpan.FromSeconds(2), log["b"]);
    }
    private IEnumerable<Event> TestResourceWithUsingProc(Simulation env, string name, Resource resource,
      Dictionary<string, DateTime> log) {
      using (var req = resource.Request()) {
        yield return req;
        Assert.Equal(1, resource.InUse);

        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
      log.Add(name, env.Now);
    }

    [Fact]
    public void TestResourceWithUsingAndCondition() {
      var start = new DateTime(2016, 3, 1);
      var env = new Simulation(start);
      var resource = new Resource(env, capacity: 1);

      Assert.Equal(1, resource.Capacity);
      Assert.Equal(0, resource.InUse);

      var log = new Dictionary<string, DateTime>();
      env.Process(TestResourceWithUsingAndConditionProc(env, "a", resource, log));
      env.Process(TestResourceWithUsingAndConditionProc(env, "b", resource, log));
      env.Run();

      Assert.Equal(0, resource.InUse);
      Assert.Equal(start + TimeSpan.FromSeconds(1), log["b"]);
      Assert.Equal(start + TimeSpan.FromSeconds(3), log["a"]);
    }
    private IEnumerable<Event> TestResourceWithUsingAndConditionProc(Simulation env, string name, Resource resource,
      Dictionary<string, DateTime> log) {
      using (var req = resource.Request()) {
        var waitingTimeOut = env.Timeout(name == "b" ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(5));
        yield return req | waitingTimeOut;
        if (name == "a") {
          Assert.Equal(1, resource.InUse);
          Assert.True(req.IsProcessed);
          Assert.False(waitingTimeOut.IsProcessed);

          yield return env.Timeout(TimeSpan.FromSeconds(3));
        } else {
          Assert.Equal(1, resource.InUse);
          Assert.False(req.IsProcessed);
          Assert.True(waitingTimeOut.IsProcessed);
        }
      }
      log.Add(name, env.Now);
    }

    [Fact]
    public void TestResourceSlots() {
      var start = new DateTime(2014, 4, 1);
      var env = new Simulation(start);
      var resource = new Resource(env, capacity: 3);
      var log = new Dictionary<string, DateTime>();
      for (int i = 0; i < 9; i++)
        env.Process(TestResourceSlotsProc(env, i.ToString(), resource, log));
      env.Run();

      var expected = new Dictionary<string, int> {
        {"0", 0}, {"1", 0}, {"2", 0}, {"3", 1}, {"4", 1}, {"5", 1}, {"6", 2}, {"7", 2}, {"8", 2}
      }.ToDictionary(x => x.Key, x => start + TimeSpan.FromSeconds(x.Value));
      Assert.Equal(expected, log);
    }
    private IEnumerable<Event> TestResourceSlotsProc(Simulation env, string name, Resource resource,
      Dictionary<string, DateTime> log) {
      using (var req = resource.Request()) {
        yield return req;
        log.Add(name, env.Now);
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    [Fact]
    public void TestResourceContinueAfterInterrupt() {
      var env = new Simulation(new DateTime(2014, 4, 1));
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceContinueAfterInterruptProc(env, res));
      var proc = env.Process(TestResourceContinueAfterInterruptVictim(env, res));
      env.Process(TestResourceContinueAfterInterruptInterruptor(env, proc));
      env.Run();
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptProc(Simulation env, Resource res) {
      using (var req = res.Request()) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptVictim(Simulation env, Resource res) {
      var req = res.Request();
      yield return req;
      Assert.False(req.IsOk);
      env.ActiveProcess.HandleFault();
      yield return req;
      res.Release(req);
      Assert.Equal(new DateTime(2014, 4, 1, 0, 0, 1), env.Now);
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptInterruptor(Simulation env, Process proc) {
      proc.Interrupt();
      yield break;
    }

    [Fact]
    public void TestResourceContinueAfterInterruptWaiting() {
      var env = new Simulation(new DateTime(2014, 4, 1));
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceContinueAfterInterruptWaitingProc(env, res));
      var proc = env.Process(TestResourceContinueAfterInterruptWaitingVictim(env, res));
      env.Process(TestResourceContinueAfterInterruptWaitingInterruptor(env, proc));
      env.Run();
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptWaitingProc(Simulation env, Resource res) {
      using (var req = res.Request()) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptWaitingVictim(Simulation env, Resource res) {
      var req = res.Request();
      yield return req;
      Assert.False(req.IsOk);
      env.ActiveProcess.HandleFault();
      yield return env.Timeout(TimeSpan.FromSeconds(2));
      yield return req;
      res.Release(req);
      Assert.Equal(new DateTime(2014, 4, 1, 0, 0, 2), env.Now);
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptWaitingInterruptor(Simulation env, Process proc) {
      proc.Interrupt();
      yield break;
    }

    [Fact]
    public void TestResourceReleaseAfterInterrupt() {
      var env = new Simulation(new DateTime(2014, 4, 1));
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceReleaseAfterInterruptBlocker(env, res));
      var victimProc = env.Process(TestResourceReleaseAfterInterruptVictim(env, res));
      env.Process(TestResourceReleaseAfterInterruptInterruptor(env, victimProc));
      env.Run();
    }

    private IEnumerable<Event> TestResourceReleaseAfterInterruptBlocker(Simulation env, Resource res) {
      using (var req = res.Request()) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    private IEnumerable<Event> TestResourceReleaseAfterInterruptVictim(Simulation env, Resource res) {
      var req = res.Request();
      yield return req;
      Assert.False(req.IsOk);
      Assert.True(env.ActiveProcess.HandleFault());
      // Dont wait for the resource
      res.Release(req);
      Assert.Equal(new DateTime(2014, 4, 1), env.Now);
    }

    private IEnumerable<Event> TestResourceReleaseAfterInterruptInterruptor(Simulation env, Process proc) {
      proc.Interrupt();
      yield break;
    }

    [Fact]
    public void TestResourceWithCondition() {
      var env = new Simulation();
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceWithConditionProc(env, res));
      env.Run();
    }
    private IEnumerable<Event> TestResourceWithConditionProc(Simulation env, Resource res) {
      using (var req = res.Request()) {
        var timeout = env.Timeout(TimeSpan.FromSeconds(1));
        yield return req | timeout;
        Assert.True(req.IsOk);
        Assert.False(timeout.IsProcessed);
      }
    }

    [Fact]
    public void TestResourceWithPriorityQueue() {
      var env = new Simulation(new DateTime(2014, 4, 1));
      var resource = new PriorityResource(env, capacity: 1);
      env.Process(TestResourceWithPriorityQueueProc(env, 0, resource, 2, 0));
      env.Process(TestResourceWithPriorityQueueProc(env, 2, resource, 3, 10));
      env.Process(TestResourceWithPriorityQueueProc(env, 2, resource, 3, 15)); // Test equal priority
      env.Process(TestResourceWithPriorityQueueProc(env, 4, resource, 1, 5));
      env.Run();
    }
    private IEnumerable<Event> TestResourceWithPriorityQueueProc(Simulation env, int delay, PriorityResource resource, int priority, int resTime) {
      yield return env.Timeout(TimeSpan.FromSeconds(delay));
      var req = resource.Request(priority);
      yield return req;
      Assert.Equal(new DateTime(2014, 4, 1) + TimeSpan.FromSeconds(resTime), env.Now);
      yield return env.Timeout(TimeSpan.FromSeconds(5));
      resource.Release(req);
    }

    [Fact]
    public void TestPreemtiveResource() {
      var start = new DateTime(2014, 4, 1);
      var env = new Simulation(start);
      var res = new PreemptiveResource(env, capacity: 2);
      var log = new Dictionary<DateTime, int>();
      //                                    id           d  p
      env.Process(TestPreemtiveResourceProc(0, env, res, 0, 1, log));
      env.Process(TestPreemtiveResourceProc(1, env, res, 0, 1, log));
      env.Process(TestPreemtiveResourceProc(2, env, res, 1, 0, log));
      env.Process(TestPreemtiveResourceProc(3, env, res, 2, 2, log));
      env.Run();

      var expected = new Dictionary<int, int> {
        {5, 0}, {6, 2}, {10, 3}
      }.ToDictionary(x => start + TimeSpan.FromSeconds(x.Key), x => x.Value);
      Assert.Equal(expected, log);
    }
    private IEnumerable<Event> TestPreemtiveResourceProc(int id, Simulation env, PreemptiveResource res, int delay, int prio, Dictionary<DateTime, int> log) {
      yield return env.Timeout(TimeSpan.FromSeconds(delay));
      using (var req = res.Request(priority: prio, preempt: true)) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(5));
        if (!env.ActiveProcess.HandleFault())
          log.Add(env.Now, id);
      }
    }

    [Fact]
    public void TestPreemptiveResourceTimeout0() {
      var env = new Simulation();
      var res = new PreemptiveResource(env, capacity: 1);
      env.Process(TestPreemptiveResourceTimeoutA(env, res, 1));
      env.Process(TestPreemptiveResourceTimeoutB(env, res, 0));
      env.Run();
    }
    private IEnumerable<Event> TestPreemptiveResourceTimeoutA(Simulation env, PreemptiveResource res, int prio) {
      using (var req = res.Request(priority: prio, preempt: true)) {
        yield return req;
        Assert.True(env.ActiveProcess.HandleFault());
        yield return env.Timeout(TimeSpan.FromSeconds(1));
        Assert.False(env.ActiveProcess.HandleFault());
      }
    }
    private IEnumerable<Event> TestPreemptiveResourceTimeoutB(Simulation env, PreemptiveResource res, int prio) {
      using (var req = res.Request(priority: prio, preempt: true)) {
        yield return req;
      }
    }

    [Fact]
    public void TestMixedPreemtion() {
      var start = new DateTime(2014, 4, 2);
      var env = new Simulation(start);
      var res = new PreemptiveResource(env, capacity: 2);
      var log = new List<Tuple<int, int>>();
      env.Process(TestMixedPreemtionProc(0, env, res, 0, 1, true, log));
      env.Process(TestMixedPreemtionProc(1, env, res, 0, 1, true, log));
      env.Process(TestMixedPreemtionProc(2, env, res, 1, 0, false, log));
      env.Process(TestMixedPreemtionProc(3, env, res, 1, 0, true, log));
      env.Process(TestMixedPreemtionProc(4, env, res, 2, 2, true, log));
      env.Run();
      var expected = new List<Tuple<int, int>> {
        Tuple.Create(5, 0),
        Tuple.Create(6, 3),
        Tuple.Create(10, 2),
        Tuple.Create(11, 4)
      };
      Assert.Equal(expected, log);
    }
    private IEnumerable<Event> TestMixedPreemtionProc(int id, Simulation env, PreemptiveResource res, int delay, int prio, bool preempt, List<Tuple<int, int>> log) {
      yield return env.Timeout(TimeSpan.FromSeconds(delay));
      using (var req = res.Request(priority: prio, preempt: preempt)) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(5));
        if (!env.ActiveProcess.HandleFault())
          log.Add(Tuple.Create(env.Now.Second, id));
      }
    }

    [Fact]
    public void TestFilterStore() {
      var start = new DateTime(1970, 1, 1, 0, 0, 0);
      var sb = new StringBuilder();
      var env = new Simulation(start) {
        Logger = new StringWriter(sb)
      };
      var sto = new FilterStore(env, capacity: 1);
      env.Process(FilterStoreProducer(env, sto));
      env.Process(FilterStoreConsumerA(env, sto));
      env.Process(FilterStoreConsumerB(env, sto));
      env.Run(TimeSpan.FromSeconds(20));
      Assert.Equal(string.Join(System.Environment.NewLine,
"4: Produce A",
"4: Consume A",
"6: Produce B",
"6: Consume B",
"10: Produce A",
"14: Consume A",
"14: Produce B",
"14: Consume B",
"18: Produce A", "")
, sb.ToString());
    }

    private static readonly object FilterStoreObjA = new object();
    private static readonly object FilterStoreObjB = new object();

    private IEnumerable<Event> FilterStoreProducer(Simulation env, FilterStore sto) {
      while (true) {
        yield return env.Timeout(TimeSpan.FromSeconds(4));
        yield return sto.Put(FilterStoreObjA);
        env.Log("{0}: Produce A", env.Now.Second);
        yield return env.Timeout(TimeSpan.FromSeconds(2));
        yield return sto.Put(FilterStoreObjB);
        env.Log("{0}: Produce B", env.Now.Second);
      }
    }

    private IEnumerable<Event> FilterStoreConsumerA(Simulation env, FilterStore sto) {
      while (true) {
        yield return sto.Get(x => x == FilterStoreObjA);
        env.Log("{0}: Consume A", env.Now.Second);
        yield return env.Timeout(TimeSpan.FromSeconds(10));
      }
    }

    private IEnumerable<Event> FilterStoreConsumerB(Simulation env, FilterStore sto) {
      while (true) {
        yield return sto.Get(x => x == FilterStoreObjB);
        env.Log("{0}: Consume B", env.Now.Second);
        yield return env.Timeout(TimeSpan.FromSeconds(3));
      }
    }

    [Fact]
    public void TestFilterStoreGetAfterMismatch() {
      var env = new Simulation(new DateTime(2014, 1, 1));
      var store = new FilterStore(env, capacity: 2);
      var proc1 = env.Process(TestFilterStoreGetAfterMismatch_Getter(env, store, 1));
      var proc2 = env.Process(TestFilterStoreGetAfterMismatch_Getter(env, store, 2));
      env.Process(TestFilterStoreGetAfterMismatch_Putter(env, store));
      env.Run();
      Assert.Equal(1, proc1.Value);
      Assert.Equal(0, proc2.Value);
    }
    private IEnumerable<Event> TestFilterStoreGetAfterMismatch_Putter(Simulation env, FilterStore store) {
      yield return store.Put(2);
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      yield return store.Put(1);
    }

    private IEnumerable<Event> TestFilterStoreGetAfterMismatch_Getter(Simulation env, FilterStore store, int value) {
      yield return store.Get(x => (int)x == value);
      env.ActiveProcess.Succeed(env.Now.Second);
    }

    [Fact]
    public void TestResourceImmediateRequests() {
      /* A process must not acquire a resource if it releases it and immediately
       * requests it again while there are already other requesting processes.
       */
      var env = new Simulation(new DateTime(2014, 1, 1));
      env.Process(TestResourceImmediateRequests_Parent(env));
      env.Run();
      Assert.Equal(6, env.Now.Second);
    }

    private IEnumerable<Event> TestResourceImmediateRequests_Parent(Simulation env) {
      var res = new Resource(env, 1);
      var childA = env.Process(TestResourceImmediateRequests_Child(env, res));
      var childB = env.Process(TestResourceImmediateRequests_Child(env, res));
      yield return childA;
      yield return childB;

      Assert.True(new[] { 0, 2, 4 }.SequenceEqual((IList<int>)childA.Value));
      Assert.True(new[] { 1, 3, 5 }.SequenceEqual((IList<int>)childB.Value));
    }

    private IEnumerable<Event> TestResourceImmediateRequests_Child(Simulation env, Resource res) {
      var result = new List<int>();
      for (var i = 0; i < 3; i++) {
        using (var req = res.Request()) {
          yield return req;
          result.Add(env.Now.Second);
          yield return env.Timeout(TimeSpan.FromSeconds(1));
        }
      }
      env.ActiveProcess.Succeed(result);
    }

    [Fact]
    public void TestFilterCallsBestCase() {
      var env = new Simulation();
      var store = new FilterStore(env, new object[] { 1, 2, 3 }, 3);
      var log = new List<string>();
      Func<object, bool> filterLogger = o => { log.Add(string.Format("check {0}", o)); return true; };
      env.Process(TestFilterCallsBestCaseProcess(store, filterLogger, log));
      env.Run();
      Assert.True(log.SequenceEqual(new[] { "check 1", "get 1", "check 2", "get 2", "check 3", "get 3" }));
    }

    private IEnumerable<Event> TestFilterCallsBestCaseProcess(FilterStore store, Func<object, bool> filter, List<string> log) {
      var get = store.Get(filter);
      yield return get;
      log.Add(string.Format("get {0}", get.Value));
      get = store.Get(filter);
      yield return get;
      log.Add(string.Format("get {0}", get.Value));
      get = store.Get(filter);
      yield return get;
      log.Add(string.Format("get {0}", get.Value));
    }

    [Fact]
    public void TestFilterCallsWorstCase() {
      var env = new Simulation();
      var store = new FilterStore(env, 4);
      var log = new List<string>();
      Func<object, bool> filterLogger = o => { log.Add(string.Format("check {0}", o)); return (int)o >= 3; };
      env.Process(TestFilterCallsWorseCaseGetProcess(store, filterLogger, log));
      env.Process(TestFilterCallsWorstCasePutProcess(store, log));
      env.Run();
      Assert.True(log.SequenceEqual(new[] {
        "put 0", "check 0",
        "put 1", "check 0", "check 1",
        "put 2", "check 0", "check 1", "check 2",
        "put 3", "check 0", "check 1", "check 2", "check 3", "get 3"
      }));
    }

    private IEnumerable<Event> TestFilterCallsWorstCasePutProcess(FilterStore store, List<string> log) {
      for (var i = 0; i < 4; i++) {
        log.Add(string.Format("put {0}", i));
        yield return store.Put(i);
      }
    }

    private IEnumerable<Event> TestFilterCallsWorseCaseGetProcess(FilterStore store, Func<object, bool> filterLogger, List<string> log) {
      var req = store.Get(filterLogger);
      yield return req;
      log.Add(string.Format("get {0}", req.Value));
    }

    class MyContainer : Container {
      public int PutQueueLength { get { return PutQueue.Count; } }
      public int GetQueueLength { get { return GetQueue.Count; } }
      public MyContainer(Simulation environment, double capacity = Double.MaxValue, double initial = 0) : base(environment, capacity, initial) { }
    }
    class MyFilterStore : FilterStore {
      public int PutQueueLength { get { return PutQueue.Count; } }
      public int GetQueueLength { get { return GetQueue.Count; } }
      public MyFilterStore(Simulation environment, int capacity = Int32.MaxValue) : base(environment, capacity) { }
    }
    class MyPreemptiveResource : PreemptiveResource {
      public int RequestQueueLength { get { return RequestQueue.Count; } }
      public int ReleaseQueueLength { get { return ReleaseQueue.Count; } }
      public MyPreemptiveResource(Simulation environment, int capacity = 1) : base(environment, capacity) { }
    }
    class MyPriorityResource : PriorityResource {
      public int RequestQueueLength { get { return RequestQueue.Count; } }
      public int ReleaseQueueLength { get { return ReleaseQueue.Count; } }
      public MyPriorityResource(Simulation environment, int capacity = 1) : base(environment, capacity) { }
    }
    class MyResource : Resource {
      public int RequestQueueLength { get { return RequestQueue.Count; } }
      public int ReleaseQueueLength { get { return ReleaseQueue.Count; } }
      public MyResource(Simulation environment, int capacity = 1) : base(environment, capacity) { }
    }
    class MyResourcePool : ResourcePool {
      public int RequestQueueLength { get { return RequestQueue.Count; } }
      public int ReleaseQueueLength { get { return ReleaseQueue.Count; } }
      public MyResourcePool(Simulation environment, IEnumerable<object> items) : base(environment, items) { }
    }
    class MyStore : Store {
      public int PutQueueLength { get { return PutQueue.Count; } }
      public int GetQueueLength { get { return GetQueue.Count; } }
      public MyStore(Simulation environment, int capacity = Int32.MaxValue) : base(environment, capacity) { }
    }
    class MyPriorityStore : PriorityStore {
      public int PutQueueLength { get { return PutQueue.Count; } }
      public int GetQueueLength { get { return GetQueue.Count; } }
      public object Peek { get { return Items.First.Item; } }
      public MyPriorityStore(Simulation environment, int capacity = Int32.MaxValue) : base(environment, capacity) { }
    }

    [Fact]
    public void TestImmediateContainer() {
      var env = new Simulation();
      var res = new MyContainer(env);
      Assert.Equal(0, res.Level);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      var put = res.Put(1);
      Assert.True(put.IsTriggered);
      Assert.Equal(1, res.Level);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      var get = res.Get(1);
      Assert.True(get.IsTriggered);
      Assert.Equal(0, res.Level);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      get = res.Get(1);
      Assert.False(get.IsTriggered);
      Assert.Equal(0, res.Level);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(1, res.GetQueueLength);

      put = res.Put(1);
      Assert.True(put.IsTriggered);
      Assert.Equal(1, res.Level);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(1, res.GetQueueLength);

      env.Run();
      Assert.True(get.IsTriggered);
      Assert.Equal(0, res.Level);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);
    }

    [Fact]
    public void TestImmediateFilterStore() {
      var env = new Simulation();
      var res = new MyFilterStore(env);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      var put = res.Put(1);
      Assert.True(put.IsTriggered);
      Assert.Equal(1, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      var get = res.Get();
      Assert.True(get.IsTriggered);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      get = res.Get();
      Assert.False(get.IsTriggered);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(1, res.GetQueueLength);

      put = res.Put(1);
      Assert.True(put.IsTriggered);
      Assert.Equal(1, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(1, res.GetQueueLength);

      env.Run();
      Assert.True(get.IsTriggered);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);
    }

    [Fact]
    public void TestImmediatePreemptiveResource() {
      var env = new Simulation();
      var res = new MyPreemptiveResource(env, capacity: 1);
      Assert.Equal(0, res.InUse);
      Assert.Equal(1, res.Remaining);
      Assert.Equal(0, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req1 = res.Request(1, true);
      Assert.True(req1.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(0, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req2 = res.Request(1, true);
      Assert.False(req2.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      req1.Dispose();
      Assert.False(req2.IsTriggered);
      Assert.Equal(0, res.InUse);
      Assert.Equal(1, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req3 = res.Request(1, true);
      Assert.True(req2.IsTriggered);
      Assert.False(req3.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);
    }

    [Fact]
    public void TestImmediatePriorityResource() {
      var env = new Simulation();
      var res = new MyPriorityResource(env, capacity: 1);
      Assert.Equal(0, res.InUse);
      Assert.Equal(1, res.Remaining);
      Assert.Equal(0, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req1 = res.Request(1);
      Assert.True(req1.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(0, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req2 = res.Request(1);
      Assert.False(req2.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      req1.Dispose();
      Assert.False(req2.IsTriggered);
      Assert.Equal(0, res.InUse);
      Assert.Equal(1, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req3 = res.Request(1);
      Assert.True(req2.IsTriggered);
      Assert.False(req3.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);
    }

    [Fact]
    public void TestImmediateResource() {
      var env = new Simulation();
      var res = new MyResource(env, capacity: 1);
      Assert.Equal(0, res.InUse);
      Assert.Equal(1, res.Remaining);
      Assert.Equal(0, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req1 = res.Request();
      Assert.True(req1.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(0, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req2 = res.Request();
      Assert.False(req2.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      req1.Dispose();
      Assert.False(req2.IsTriggered);
      Assert.Equal(0, res.InUse);
      Assert.Equal(1, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req3 = res.Request();
      Assert.True(req2.IsTriggered);
      Assert.False(req3.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);
    }

    [Fact]
    public void TestImmediateResourcePool() {
      var env = new Simulation();
      var res = new MyResourcePool(env, new object[] { 1 });
      Assert.Equal(0, res.InUse);
      Assert.Equal(1, res.Remaining);
      Assert.Equal(0, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req1 = res.Request();
      Assert.True(req1.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(0, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req2 = res.Request();
      Assert.False(req2.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      req1.Dispose();
      Assert.False(req2.IsTriggered);
      Assert.Equal(0, res.InUse);
      Assert.Equal(1, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);

      var req3 = res.Request();
      Assert.True(req2.IsTriggered);
      Assert.False(req3.IsTriggered);
      Assert.Equal(1, res.InUse);
      Assert.Equal(0, res.Remaining);
      Assert.Equal(1, res.RequestQueueLength);
      Assert.Equal(0, res.ReleaseQueueLength);
    }

    [Fact]
    public void TestImmediateStore() {
      var env = new Simulation();
      var res = new MyStore(env);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      var put = res.Put(1);
      Assert.True(put.IsTriggered);
      Assert.Equal(1, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      var get = res.Get();
      Assert.True(get.IsTriggered);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      get = res.Get();
      Assert.False(get.IsTriggered);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(1, res.GetQueueLength);

      put = res.Put(1);
      Assert.True(put.IsTriggered);
      Assert.Equal(1, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(1, res.GetQueueLength);

      env.Run();
      Assert.True(get.IsTriggered);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);
    }

    [Fact]
    public void TestImmediatePriorityStore() {
      var env = new Simulation();
      var res = new MyPriorityStore(env);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      var put = res.Put(1, priority: 2);
      Assert.True(put.IsTriggered);
      Assert.Equal(1, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);

      var get = res.Get();
      Assert.True(get.IsTriggered);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);
      Assert.Equal(1, (int)get.Value);

      get = res.Get();
      Assert.False(get.IsTriggered);
      Assert.Equal(0, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(1, res.GetQueueLength);

      put = res.Put(2, priority: 2);
      Assert.True(put.IsTriggered);
      Assert.Equal(1, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(1, res.GetQueueLength);

      put = res.Put(1, priority: 1);
      Assert.True(put.IsTriggered);
      Assert.Equal(2, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(1, res.GetQueueLength);

      env.Run();
      Assert.True(get.IsTriggered);
      Assert.Equal(1, res.Count);
      Assert.Equal(0, res.PutQueueLength);
      Assert.Equal(0, res.GetQueueLength);
      Assert.Equal(1, (int)get.Value);
      Assert.Equal(2, (int)res.Peek);
    }

    [Fact]
    public void TestResourcePoolUnavailable() {
      var thread = new System.Threading.Thread(TestResourcePoolUnavailableMethod);
      thread.Start();
      System.Threading.Thread.Sleep(1000);
      Assert.False(thread.IsAlive);
    }

    private void TestResourcePoolUnavailableMethod() {
      var env = new Simulation();
      var pool = new ResourcePool(env, new object[] { 0, 1 });
      env.Process(TestResourcePoolUnavailableProc(env, pool));
      env.Process(TestResourcePoolUnavailableProc(env, pool));
      env.Run();
    }

    private IEnumerable<Event> TestResourcePoolUnavailableProc(Simulation env, ResourcePool pool) {
      var req = pool.Request(o => (int)o == 0);
      yield return req;
      yield return env.TimeoutD(1);
      yield return pool.Release(req);
    }

    [Fact]
    public void TestWhenStarEventsStore() {
      var env = new Simulation();
      var store = new Store(env, 3);
      var proca = env.Process(TestWhenStarEventsStoreProcA(env, store));
      var procb = env.Process(TestWhenStarEventsStoreProcB(env, store));
      env.Run();
      Assert.Equal(7, env.NowD);
      Assert.True(proca.IsProcessed);
      Assert.True(procb.IsProcessed);
    }

    private IEnumerable<Event> TestWhenStarEventsStoreProcA(Simulation env, Store store) {
      yield return env.TimeoutD(1);
      yield return store.Put(1);
      yield return env.TimeoutD(1);
      yield return store.Put(2);
      yield return env.TimeoutD(1);
      yield return store.Put(3);
      yield return env.TimeoutD(1);
      yield return store.Get();
      yield return env.TimeoutD(1);
      yield return store.Get();
      yield return env.TimeoutD(1);
      yield return store.Get();
      yield return env.TimeoutD(1);
      yield return store.Put(1);
    }

    private IEnumerable<Event> TestWhenStarEventsStoreProcB(Simulation env, Store store) {
      yield return store.WhenEmpty();// whenempty triggers immediately if empty
      Assert.Equal(0, env.NowD);
      yield return store.WhenAny(); // whenany waits until first item is aded
      Assert.Equal(1, env.NowD);
      yield return store.WhenNew();
      Assert.Equal(2, env.NowD);
      yield return store.WhenAny(); // whenany completes immediately if any items are present
      Assert.Equal(2, env.NowD);
      yield return store.WhenFull();
      Assert.Equal(3, env.NowD);
      yield return store.WhenFull(); // whenfull triggers immediately
      Assert.Equal(3, env.NowD);
      yield return store.WhenChange(); // get triggers change
      Assert.Equal(4, env.NowD);
      yield return store.WhenEmpty();
      Assert.Equal(6, env.NowD);
      yield return store.WhenChange(); // put triggers change
      Assert.Equal(7, env.NowD);
    }

    [Fact]
    public void TestWhenStarEventsFilterStore() {
      var env = new Simulation();
      var store = new FilterStore(env, capacity: 3);
      var proca = env.Process(TestWhenStarEventsFilterStoreProcA(env, store));
      var procb = env.Process(TestWhenStarEventsFilterStoreProcB(env, store));
      env.Run();
      Assert.Equal(7, env.NowD);
      Assert.True(proca.IsProcessed);
      Assert.True(procb.IsProcessed);
    }

    private IEnumerable<Event> TestWhenStarEventsFilterStoreProcA(Simulation env, FilterStore store) {
      yield return env.TimeoutD(1);
      yield return store.Put(1);
      yield return env.TimeoutD(1);
      yield return store.Put(2);
      yield return env.TimeoutD(1);
      yield return store.Put(3);
      yield return env.TimeoutD(1);
      yield return store.Get();
      yield return env.TimeoutD(1);
      yield return store.Get();
      yield return env.TimeoutD(1);
      yield return store.Get();
      yield return env.TimeoutD(1);
      yield return store.Put(1);
    }

    private IEnumerable<Event> TestWhenStarEventsFilterStoreProcB(Simulation env, FilterStore store) {
      yield return store.WhenEmpty(); // whenempty triggers immediately if empty
      Assert.Equal(0, env.NowD);
      yield return store.WhenAny(); // whenany waits until first item is aded
      Assert.Equal(1, env.NowD);
      yield return store.WhenNew();
      Assert.Equal(2, env.NowD);
      yield return store.WhenAny(); // whenany completes immediately if any items are present
      Assert.Equal(2, env.NowD);
      yield return store.WhenFull();
      Assert.Equal(3, env.NowD);
      yield return store.WhenFull(); // whenfull triggers immediately
      Assert.Equal(3, env.NowD);
      yield return store.WhenChange(); // get triggers change
      Assert.Equal(4, env.NowD);
      yield return store.WhenEmpty();
      Assert.Equal(6, env.NowD);
      yield return store.WhenChange(); // put triggers change
      Assert.Equal(7, env.NowD);
    }

    [Fact]
    public void TestWhenStarEventsPriorityStore() {
      var env = new Simulation();
      var store = new PriorityStore(env, 3);
      var proca = env.Process(TestWhenStarEventsPriorityStoreProcA(env, store));
      var procb = env.Process(TestWhenStarEventsPriorityStoreProcB(env, store));
      env.Run();
      Assert.Equal(7, env.NowD);
      Assert.True(proca.IsProcessed);
      Assert.True(procb.IsProcessed);
    }

    private IEnumerable<Event> TestWhenStarEventsPriorityStoreProcA(Simulation env, PriorityStore store) {
      yield return env.TimeoutD(1);
      yield return store.Put(1, 3);
      yield return env.TimeoutD(1);
      yield return store.Put(2, 2);
      yield return env.TimeoutD(1);
      yield return store.Put(3, 1);
      yield return env.TimeoutD(1);
      var get = store.Get();
      yield return get;
      Assert.Equal(3, get.Value);
      yield return env.TimeoutD(1);
      get = store.Get();
      yield return get;
      Assert.Equal(2, get.Value);
      yield return env.TimeoutD(1);
      get = store.Get();
      yield return get;
      Assert.Equal(1, get.Value);
      yield return env.TimeoutD(1);
      yield return store.Put(1);
    }

    private IEnumerable<Event> TestWhenStarEventsPriorityStoreProcB(Simulation env, PriorityStore store) {
      yield return store.WhenEmpty(); // whenempty triggers immediately
      Assert.Equal(0, env.NowD);
      yield return store.WhenAny(); // whenany waits until first item is aded
      Assert.Equal(1, env.NowD);
      yield return store.WhenNew();
      Assert.Equal(2, env.NowD);
      yield return store.WhenAny(); // whenany completes immediately if any items are present
      Assert.Equal(2, env.NowD);
      yield return store.WhenFull();
      Assert.Equal(3, env.NowD);
      yield return store.WhenFull(); // whenfull triggers immediately
      Assert.Equal(3, env.NowD);
      yield return store.WhenChange(); // get triggers change
      Assert.Equal(4, env.NowD);
      yield return store.WhenEmpty();
      Assert.Equal(6, env.NowD);
      yield return store.WhenChange(); // put triggers change
      Assert.Equal(7, env.NowD);
    }

    [Fact]
    public void TestWhenStarEventsContainer() {
      var env = new Simulation();
      var container = new Container(env, 6);
      var proca = env.Process(TestWhenStarEventsContainerProcA(env, container));
      var procb = env.Process(TestWhenStarEventsContainerProcB(env, container));
      env.Run();
      Assert.Equal(8, env.NowD);
      Assert.True(proca.IsProcessed);
      Assert.True(procb.IsProcessed);
    }

    private IEnumerable<Event> TestWhenStarEventsContainerProcA(Simulation env, Container container) {
      yield return env.TimeoutD(1);
      yield return container.Put(1); // t = 1, lvl = 1
      yield return env.TimeoutD(1);
      yield return container.Put(2); // t = 2, lvl = 3
      yield return env.TimeoutD(1);
      yield return container.Put(3); // t = 3, lvl = 6
      yield return env.TimeoutD(1);
      yield return container.Get(1); // t = 4, lvl = 5
      yield return env.TimeoutD(1);
      yield return container.Get(1); // t = 5, lvl = 4
      yield return env.TimeoutD(1);
      yield return container.Get(1); // t = 6, lvl = 3
      yield return env.TimeoutD(1);
      yield return container.Get(3); // t = 7, lvl = 0
      yield return env.TimeoutD(1);
      yield return container.Put(1); // t = 8, lvl = 1
    }

    private IEnumerable<Event> TestWhenStarEventsContainerProcB(Simulation env, Container container) {
      yield return container.WhenAtLeast(3); // whenatleast waits until the level rise above limit
      Assert.Equal(2, env.NowD);
      yield return container.WhenAtLeast(3); // whenatleast triggers immediately if limit is above already
      Assert.Equal(2, env.NowD);
      yield return container.WhenAtLeast(2); // whenatleast triggers immediately if limit is above already
      Assert.Equal(2, env.NowD);
      yield return container.WhenFull();
      Assert.Equal(3, env.NowD);
      yield return container.WhenChange(); // when the next change happens  (in this case get)
      Assert.Equal(4, env.NowD);
      yield return container.WhenAtMost(3); // whenatmost waits until the level has sunk below limit
      Assert.Equal(6, env.NowD);
      yield return container.WhenAtMost(3); // whenatmost triggers immediately if limit is below already
      Assert.Equal(6, env.NowD);
      yield return container.WhenAtMost(4); // whenatmost triggers immediately if limit is below already
      Assert.Equal(6, env.NowD);
      yield return container.WhenEmpty();
      Assert.Equal(7, env.NowD);
      yield return container.WhenChange(); // when the next change happens  (in this case put)
      Assert.Equal(8, env.NowD);
    }

    [Fact]
    public void TestWhenStarEventsResourcePool() {
      var env = new Simulation();
      var pool = new ResourcePool(env, new object[] { 1, 2, 3 });
      var proca = env.Process(TestWhenStarEventsResourcePoolProcA(env, pool));
      var procb = env.Process(TestWhenStarEventsResourcePoolProcB(env, pool));
      env.Run();
      Assert.Equal(8, env.NowD);
      Assert.True(proca.IsProcessed);
      Assert.True(procb.IsProcessed);
    }

    private IEnumerable<Event> TestWhenStarEventsResourcePoolProcA(Simulation env, ResourcePool pool) {
      yield return env.TimeoutD(1);
      var req1 = pool.Request();
      yield return req1;               // t = 1, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      var req2 = pool.Request();
      yield return req2;               // t = 2, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      var req3 = pool.Request();
      yield return req3;               // t = 3, InUse = 3, Remaining = 0
      yield return env.TimeoutD(1);
      yield return pool.Release(req1); // t = 4, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      yield return pool.Release(req2); // t = 5, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      var req4 = pool.Request();
      yield return req4;               // t = 6, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      yield return pool.Release(req3); // t = 7, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      yield return pool.Release(req4); // t = 8, InUse = 0, Remaining = 3
    }

    private IEnumerable<Event> TestWhenStarEventsResourcePoolProcB(Simulation env, ResourcePool pool) {
      yield return pool.WhenAny(); // succeed immediatly
      Assert.Equal(0, env.NowD);
      yield return pool.WhenFull(); // succeed immediately
      Assert.Equal(0, env.NowD);
      yield return pool.WhenChange(); // change = request
      Assert.Equal(1, env.NowD);
      yield return pool.WhenEmpty();
      Assert.Equal(3, env.NowD);
      yield return pool.WhenEmpty(); // succeed immediately
      Assert.Equal(3, env.NowD);
      yield return pool.WhenAny();
      Assert.Equal(4, env.NowD);
      yield return pool.WhenChange(); // change = release
      Assert.Equal(5, env.NowD);
      yield return pool.WhenFull();
      Assert.Equal(8, env.NowD);
    }

    [Fact]
    public void TestWhenStarEventsResource() {
      var env = new Simulation();
      var res = new Resource(env, 3);
      var proca = env.Process(TestWhenStarEventsResourceProcA(env, res));
      var procb = env.Process(TestWhenStarEventsResourceProcB(env, res));
      env.Run();
      Assert.Equal(8, env.NowD);
      Assert.True(proca.IsProcessed);
      Assert.True(procb.IsProcessed);
    }

    private IEnumerable<Event> TestWhenStarEventsResourceProcA(Simulation env, Resource res) {
      yield return env.TimeoutD(1);
      var req1 = res.Request();
      yield return req1;              // t = 1, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      var req2 = res.Request();
      yield return req2;              // t = 2, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      var req3 = res.Request();
      yield return req3;              // t = 3, InUse = 3, Remaining = 0
      yield return env.TimeoutD(1);
      yield return res.Release(req1); // t = 4, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      yield return res.Release(req2); // t = 5, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      var req4 = res.Request();
      yield return req4;              // t = 6, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      yield return res.Release(req3); // t = 7, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      yield return res.Release(req4); // t = 8, InUse = 0, Remaining = 3
    }

    private IEnumerable<Event> TestWhenStarEventsResourceProcB(Simulation env, Resource res) {
      yield return res.WhenAny(); // succeed immediatly
      Assert.Equal(0, env.NowD);
      yield return res.WhenFull(); // succeed immediately
      Assert.Equal(0, env.NowD);
      yield return res.WhenChange(); // change = request
      Assert.Equal(1, env.NowD);
      yield return res.WhenEmpty();
      Assert.Equal(3, env.NowD);
      yield return res.WhenEmpty(); // succeed immediately
      Assert.Equal(3, env.NowD);
      yield return res.WhenAny();
      Assert.Equal(4, env.NowD);
      yield return res.WhenChange(); // change = release
      Assert.Equal(5, env.NowD);
      yield return res.WhenFull();
      Assert.Equal(8, env.NowD);
    }

    [Fact]
    public void TestWhenStarEventsPriorityResource() {
      var env = new Simulation();
      var res = new PriorityResource(env, 3);
      var proca = env.Process(TestWhenStarEventsPriorityResourceProcA(env, res));
      var procb = env.Process(TestWhenStarEventsPriorityResourceProcB(env, res));
      env.Run();
      Assert.Equal(8, env.NowD);
      Assert.True(proca.IsProcessed);
      Assert.True(procb.IsProcessed);
    }

    private IEnumerable<Event> TestWhenStarEventsPriorityResourceProcA(Simulation env, PriorityResource res) {
      yield return env.TimeoutD(1);
      var req1 = res.Request();
      yield return req1;              // t = 1, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      var req2 = res.Request();
      yield return req2;              // t = 2, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      var req3 = res.Request();
      yield return req3;              // t = 3, InUse = 3, Remaining = 0
      yield return env.TimeoutD(1);
      yield return res.Release(req1); // t = 4, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      yield return res.Release(req2); // t = 5, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      var req4 = res.Request();
      yield return req4;              // t = 6, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      yield return res.Release(req3); // t = 7, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      yield return res.Release(req4); // t = 8, InUse = 0, Remaining = 3
    }

    private IEnumerable<Event> TestWhenStarEventsPriorityResourceProcB(Simulation env, PriorityResource res) {
      yield return res.WhenAny(); // succeed immediatly
      Assert.Equal(0, env.NowD);
      yield return res.WhenFull(); // succeed immediately
      Assert.Equal(0, env.NowD);
      yield return res.WhenChange(); // change = request
      Assert.Equal(1, env.NowD);
      yield return res.WhenEmpty();
      Assert.Equal(3, env.NowD);
      yield return res.WhenEmpty(); // succeed immediately
      Assert.Equal(3, env.NowD);
      yield return res.WhenAny();
      Assert.Equal(4, env.NowD);
      yield return res.WhenChange(); // change = release
      Assert.Equal(5, env.NowD);
      yield return res.WhenFull();
      Assert.Equal(8, env.NowD);
    }

    [Fact]
    public void TestWhenStarEventsPreemptiveResource() {
      var env = new Simulation();
      var res = new PreemptiveResource(env, 3);
      var proca = env.Process(TestWhenStarEventsPreemptiveResourceProcA(env, res));
      var procb = env.Process(TestWhenStarEventsPreemptiveResourceProcB(env, res));
      env.Run();
      Assert.Equal(8, env.NowD);
      Assert.True(proca.IsProcessed);
      Assert.True(procb.IsProcessed);
    }

    private IEnumerable<Event> TestWhenStarEventsPreemptiveResourceProcA(Simulation env, PreemptiveResource res) {
      yield return env.TimeoutD(1);
      var req1 = res.Request();
      yield return req1;              // t = 1, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      var req2 = res.Request();
      yield return req2;              // t = 2, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      var req3 = res.Request();
      yield return req3;              // t = 3, InUse = 3, Remaining = 0
      yield return env.TimeoutD(1);
      yield return res.Release(req1); // t = 4, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      yield return res.Release(req2); // t = 5, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      var req4 = res.Request();
      yield return req4;              // t = 6, InUse = 2, Remaining = 1
      yield return env.TimeoutD(1);
      yield return res.Release(req3); // t = 7, InUse = 1, Remaining = 2
      yield return env.TimeoutD(1);
      yield return res.Release(req4); // t = 8, InUse = 0, Remaining = 3
    }

    private IEnumerable<Event> TestWhenStarEventsPreemptiveResourceProcB(Simulation env, PreemptiveResource res) {
      yield return res.WhenAny(); // succeed immediatly
      Assert.Equal(0, env.NowD);
      yield return res.WhenFull(); // succeed immediately
      Assert.Equal(0, env.NowD);
      yield return res.WhenChange(); // change = request
      Assert.Equal(1, env.NowD);
      yield return res.WhenEmpty();
      Assert.Equal(3, env.NowD);
      yield return res.WhenEmpty(); // succeed immediately
      Assert.Equal(3, env.NowD);
      yield return res.WhenAny();
      Assert.Equal(4, env.NowD);
      yield return res.WhenChange(); // change = release
      Assert.Equal(5, env.NowD);
      yield return res.WhenFull();
      Assert.Equal(8, env.NowD);
    }

    [Fact]
    public void TestResourcePoolCanceledRequests() {
      var env = new Simulation();
      var pool = new ResourcePool(env, new object[] { 1, 2 });
      env.Process(CancelResourcePoolProcess(env, pool));
      env.Run();
      Assert.False(pool.IsAvailable(x => x == null));
    }

    private IEnumerable<Event> CancelResourcePoolProcess(Simulation env, ResourcePool pool) {
      var req1 = pool.Request(filter: x => x is int ? (int)x == 1 : false);
      var req2 = pool.Request(filter: x => x is int ? (int)x == 2 : false);
      var req3 = pool.Request(filter: x => x is int ? (int)x == 1 : false);

      yield return req1;
      yield return req2;
      yield return pool.Release(req3); // cancel request
      yield return pool.Release(req2);
      yield return pool.Release(req1);
    }
  }
}
