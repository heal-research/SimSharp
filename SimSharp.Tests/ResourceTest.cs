#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2016  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimSharp.Tests {
  [TestClass]
  public class ResourceTest {
    [TestMethod]
    public void TestResource() {
      var start = new DateTime(2014, 4, 1);
      var env = new Environment(start);
      var resource = new Resource(env, capacity: 1);

      Assert.AreEqual(1, resource.Capacity);
      Assert.AreEqual(0, resource.InUse);

      var log = new Dictionary<string, DateTime>();
      env.Process(TestResource(env, "a", resource, log));
      env.Process(TestResource(env, "b", resource, log));
      env.Run();

      Assert.AreEqual(start + TimeSpan.FromSeconds(1), log["a"]);
      Assert.AreEqual(start + TimeSpan.FromSeconds(2), log["b"]);
    }
    private IEnumerable<Event> TestResource(Environment env, string name, Resource resource,
      Dictionary<string, DateTime> log) {
      var req = resource.Request();
      yield return req;
      Assert.AreEqual(1, resource.InUse);

      yield return env.Timeout(TimeSpan.FromSeconds(1));
      resource.Release(req);

      log.Add(name, env.Now);
    }

    [TestMethod]
    public void TestResourceWithUsing() {
      var start = new DateTime(2014, 4, 1);
      var env = new Environment(start);
      var resource = new Resource(env, capacity: 1);

      Assert.AreEqual(1, resource.Capacity);
      Assert.AreEqual(0, resource.InUse);

      var log = new Dictionary<string, DateTime>();
      env.Process(TestResourceWithUsing(env, "a", resource, log));
      env.Process(TestResourceWithUsing(env, "b", resource, log));
      env.Run();

      Assert.AreEqual(start + TimeSpan.FromSeconds(1), log["a"]);
      Assert.AreEqual(start + TimeSpan.FromSeconds(2), log["b"]);
    }
    private IEnumerable<Event> TestResourceWithUsing(Environment env, string name, Resource resource,
      Dictionary<string, DateTime> log) {
      using (var req = resource.Request()) {
        yield return req;
        Assert.AreEqual(1, resource.InUse);

        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
      log.Add(name, env.Now);
    }

    [TestMethod]
    public void TestResourceWithUsingAndCondition() {
      var start = new DateTime(2016, 3, 1);
      var env = new Environment(start);
      var resource = new Resource(env, capacity: 1);

      Assert.AreEqual(1, resource.Capacity);
      Assert.AreEqual(0, resource.InUse);

      var log = new Dictionary<string, DateTime>();
      env.Process(TestResourceWithUsingAndCondition(env, "a", resource, log));
      env.Process(TestResourceWithUsingAndCondition(env, "b", resource, log));
      env.Run();

      Assert.AreEqual(0, resource.InUse);
      Assert.AreEqual(start + TimeSpan.FromSeconds(1), log["b"]);
      Assert.AreEqual(start + TimeSpan.FromSeconds(3), log["a"]);
    }
    private IEnumerable<Event> TestResourceWithUsingAndCondition(Environment env, string name, Resource resource,
      Dictionary<string, DateTime> log) {
      using (var req = resource.Request()) {
        var waitingTimeOut = env.Timeout(name == "b" ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(5));
        yield return req | waitingTimeOut;
        if (name == "a") {
          Assert.AreEqual(1, resource.InUse);
          Assert.IsTrue(req.IsProcessed);
          Assert.IsFalse(waitingTimeOut.IsProcessed);

          yield return env.Timeout(TimeSpan.FromSeconds(3));
        } else {
          Assert.AreEqual(1, resource.InUse);
          Assert.IsFalse(req.IsProcessed);
          Assert.IsTrue(waitingTimeOut.IsProcessed);
        }
      }
      log.Add(name, env.Now);
    }

    [TestMethod]
    public void TestResourceSlots() {
      var start = new DateTime(2014, 4, 1);
      var env = new Environment(start);
      var resource = new Resource(env, capacity: 3);
      var log = new Dictionary<string, DateTime>();
      for (int i = 0; i < 9; i++)
        env.Process(TestResourceSlots(env, i.ToString(), resource, log));
      env.Run();

      var expected = new Dictionary<string, int> {
        {"0", 0}, {"1", 0}, {"2", 0}, {"3", 1}, {"4", 1}, {"5", 1}, {"6", 2}, {"7", 2}, {"8", 2}
      }.ToDictionary(x => x.Key, x => start + TimeSpan.FromSeconds(x.Value));
      CollectionAssert.AreEqual(expected, log);
    }
    private IEnumerable<Event> TestResourceSlots(Environment env, string name, Resource resource,
      Dictionary<string, DateTime> log) {
      using (var req = resource.Request()) {
        yield return req;
        log.Add(name, env.Now);
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    [TestMethod]
    public void TestResourceContinueAfterInterrupt() {
      var env = new Environment(new DateTime(2014, 4, 1));
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceContinueAfterInterrupt(env, res));
      var proc = env.Process(TestResourceContinueAfterInterruptVictim(env, res));
      env.Process(TestResourceContinueAfterInterruptInterruptor(env, proc));
      env.Run();
    }

    private IEnumerable<Event> TestResourceContinueAfterInterrupt(Environment env, Resource res) {
      using (var req = res.Request()) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptVictim(Environment env, Resource res) {
      var req = res.Request();
      yield return req;
      Assert.IsFalse(req.IsOk);
      env.ActiveProcess.HandleFault();
      yield return req;
      res.Release(req);
      Assert.AreEqual(new DateTime(2014, 4, 1, 0, 0, 1), env.Now);
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptInterruptor(Environment env, Process proc) {
      proc.Interrupt();
      yield break;
    }

    [TestMethod]
    public void TestResourceContinueAfterInterruptWaiting() {
      var env = new Environment(new DateTime(2014, 4, 1));
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceContinueAfterInterruptWaiting(env, res));
      var proc = env.Process(TestResourceContinueAfterInterruptWaitingVictim(env, res));
      env.Process(TestResourceContinueAfterInterruptWaitingInterruptor(env, proc));
      env.Run();
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptWaiting(Environment env, Resource res) {
      using (var req = res.Request()) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptWaitingVictim(Environment env, Resource res) {
      var req = res.Request();
      yield return req;
      Assert.IsFalse(req.IsOk);
      env.ActiveProcess.HandleFault();
      yield return env.Timeout(TimeSpan.FromSeconds(2));
      yield return req;
      res.Release(req);
      Assert.AreEqual(new DateTime(2014, 4, 1, 0, 0, 2), env.Now);
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptWaitingInterruptor(Environment env, Process proc) {
      proc.Interrupt();
      yield break;
    }

    [TestMethod]
    public void TestResourceReleaseAfterInterrupt() {
      var env = new Environment(new DateTime(2014, 4, 1));
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceReleaseAfterInterruptBlocker(env, res));
      var victimProc = env.Process(TestResourceReleaseAfterInterruptVictim(env, res));
      env.Process(TestResourceReleaseAfterInterruptInterruptor(env, victimProc));
      env.Run();
    }

    private IEnumerable<Event> TestResourceReleaseAfterInterruptBlocker(Environment env, Resource res) {
      using (var req = res.Request()) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    private IEnumerable<Event> TestResourceReleaseAfterInterruptVictim(Environment env, Resource res) {
      var req = res.Request();
      yield return req;
      Assert.IsFalse(req.IsOk);
      Assert.IsTrue(env.ActiveProcess.HandleFault());
      // Dont wait for the resource
      res.Release(req);
      Assert.AreEqual(new DateTime(2014, 4, 1), env.Now);
    }

    private IEnumerable<Event> TestResourceReleaseAfterInterruptInterruptor(Environment env, Process proc) {
      proc.Interrupt();
      yield break;
    }

    [TestMethod]
    public void TestResourceWithCondition() {
      var env = new Environment();
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceWithCondition(env, res));
      env.Run();
    }
    private IEnumerable<Event> TestResourceWithCondition(Environment env, Resource res) {
      using (var req = res.Request()) {
        var timeout = env.Timeout(TimeSpan.FromSeconds(1));
        yield return req | timeout;
        Assert.IsTrue(req.IsOk);
        Assert.IsFalse(timeout.IsProcessed);
      }
    }

    [TestMethod]
    public void TestResourceWithPriorityQueue() {
      var env = new Environment(new DateTime(2014, 4, 1));
      var resource = new PriorityResource(env, capacity: 1);
      env.Process(TestResourceWithPriorityQueue(env, 0, resource, 2, 0));
      env.Process(TestResourceWithPriorityQueue(env, 2, resource, 3, 10));
      env.Process(TestResourceWithPriorityQueue(env, 2, resource, 3, 15)); // Test equal priority
      env.Process(TestResourceWithPriorityQueue(env, 4, resource, 1, 5));
      env.Run();
    }
    private IEnumerable<Event> TestResourceWithPriorityQueue(Environment env, int delay, PriorityResource resource, int priority, int resTime) {
      yield return env.Timeout(TimeSpan.FromSeconds(delay));
      var req = resource.Request(priority);
      yield return req;
      Assert.AreEqual(new DateTime(2014, 4, 1) + TimeSpan.FromSeconds(resTime), env.Now);
      yield return env.Timeout(TimeSpan.FromSeconds(5));
      resource.Release(req);
    }

    [TestMethod]
    public void TestPreemtiveResource() {
      var start = new DateTime(2014, 4, 1);
      var env = new Environment(start);
      var res = new PreemptiveResource(env, capacity: 2);
      var log = new Dictionary<DateTime, int>();
      //                                id           d  p
      env.Process(TestPreemtiveResource(0, env, res, 0, 1, log));
      env.Process(TestPreemtiveResource(1, env, res, 0, 1, log));
      env.Process(TestPreemtiveResource(2, env, res, 1, 0, log));
      env.Process(TestPreemtiveResource(3, env, res, 2, 2, log));
      env.Run();

      var expected = new Dictionary<int, int> {
        {5, 0}, {6, 2}, {10, 3}
      }.ToDictionary(x => start + TimeSpan.FromSeconds(x.Key), x => x.Value);
      CollectionAssert.AreEqual(expected, log);
    }
    private IEnumerable<Event> TestPreemtiveResource(int id, Environment env, PreemptiveResource res, int delay, int prio, Dictionary<DateTime, int> log) {
      yield return env.Timeout(TimeSpan.FromSeconds(delay));
      using (var req = res.Request(priority: prio, preempt: true)) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(5));
        if (!env.ActiveProcess.HandleFault())
          log.Add(env.Now, id);
      }
    }

    [TestMethod]
    public void TestPreemptiveResourceTimeout0() {
      var env = new Environment();
      var res = new PreemptiveResource(env, capacity: 1);
      env.Process(TestPreemptiveResourceTimeoutA(env, res, 1));
      env.Process(TestPreemptiveResourceTimeoutB(env, res, 0));
      env.Run();
    }
    private IEnumerable<Event> TestPreemptiveResourceTimeoutA(Environment env, PreemptiveResource res, int prio) {
      using (var req = res.Request(priority: prio, preempt: true)) {
        yield return req;
        Assert.IsTrue(env.ActiveProcess.HandleFault());
        yield return env.Timeout(TimeSpan.FromSeconds(1));
        Assert.IsFalse(env.ActiveProcess.HandleFault());
      }
    }
    private IEnumerable<Event> TestPreemptiveResourceTimeoutB(Environment env, PreemptiveResource res, int prio) {
      using (var req = res.Request(priority: prio, preempt: true)) {
        yield return req;
      }
    }

    [TestMethod]
    public void TestMixedPreemtion() {
      var start = new DateTime(2014, 4, 2);
      var env = new Environment(start);
      var res = new PreemptiveResource(env, capacity: 2);
      var log = new List<Tuple<int, int>>();
      env.Process(TestMixedPreemtion(0, env, res, 0, 1, true, log));
      env.Process(TestMixedPreemtion(1, env, res, 0, 1, true, log));
      env.Process(TestMixedPreemtion(2, env, res, 1, 0, false, log));
      env.Process(TestMixedPreemtion(3, env, res, 1, 0, true, log));
      env.Process(TestMixedPreemtion(4, env, res, 2, 2, true, log));
      env.Run();
      var expected = new List<Tuple<int, int>> {
        Tuple.Create(5, 0),
        Tuple.Create(6, 3),
        Tuple.Create(10, 2),
        Tuple.Create(11, 4)
      };
      CollectionAssert.AreEqual(expected, log);
    }
    private IEnumerable<Event> TestMixedPreemtion(int id, Environment env, PreemptiveResource res, int delay, int prio, bool preempt, List<Tuple<int, int>> log) {
      yield return env.Timeout(TimeSpan.FromSeconds(delay));
      using (var req = res.Request(priority: prio, preempt: preempt)) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(5));
        if (!env.ActiveProcess.HandleFault())
          log.Add(Tuple.Create(env.Now.Second, id));
      }
    }

    [TestMethod]
    public void TestFilterStore() {
      var start = new DateTime(1970, 1, 1, 0, 0, 0);
      var sb = new StringBuilder();
      var env = new Environment(start) {
        Logger = new StringWriter(sb)
      };
      var sto = new FilterStore(env, capacity: 1);
      env.Process(FilterStoreProducer(env, sto));
      env.Process(FilterStoreConsumerA(env, sto));
      env.Process(FilterStoreConsumerB(env, sto));
      env.Run(TimeSpan.FromSeconds(20));
      Assert.AreEqual(sb.ToString(),
@"4: Produce A
4: Consume A
6: Produce B
6: Consume B
10: Produce A
14: Consume A
14: Produce B
14: Consume B
18: Produce A
");
    }

    private static readonly object FilterStoreObjA = new object();
    private static readonly object FilterStoreObjB = new object();

    private IEnumerable<Event> FilterStoreProducer(Environment env, FilterStore sto) {
      while (true) {
        yield return env.Timeout(TimeSpan.FromSeconds(4));
        yield return sto.Put(FilterStoreObjA);
        env.Log("{0}: Produce A", env.Now.Second);
        yield return env.Timeout(TimeSpan.FromSeconds(2));
        yield return sto.Put(FilterStoreObjB);
        env.Log("{0}: Produce B", env.Now.Second);
      }
    }

    private IEnumerable<Event> FilterStoreConsumerA(Environment env, FilterStore sto) {
      while (true) {
        yield return sto.Get(x => x == FilterStoreObjA);
        env.Log("{0}: Consume A", env.Now.Second);
        yield return env.Timeout(TimeSpan.FromSeconds(10));
      }
    }

    private IEnumerable<Event> FilterStoreConsumerB(Environment env, FilterStore sto) {
      while (true) {
        yield return sto.Get(x => x == FilterStoreObjB);
        env.Log("{0}: Consume B", env.Now.Second);
        yield return env.Timeout(TimeSpan.FromSeconds(3));
      }
    }

    [TestMethod]
    public void TestFilterStoreGetAfterMismatch() {
      var env = new Environment(new DateTime(2014, 1, 1));
      var store = new FilterStore(env, capacity: 2);
      var proc1 = env.Process(TestFilterStoreGetAfterMismatch_Getter(env, store, 1));
      var proc2 = env.Process(TestFilterStoreGetAfterMismatch_Getter(env, store, 2));
      env.Process(TestFilterStoreGetAfterMismatch_Putter(env, store));
      env.Run();
      Assert.AreEqual(proc1.Value, 1);
      Assert.AreEqual(proc2.Value, 0);
    }
    private IEnumerable<Event> TestFilterStoreGetAfterMismatch_Putter(Environment env, FilterStore store) {
      yield return store.Put(2);
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      yield return store.Put(1);
    }

    private IEnumerable<Event> TestFilterStoreGetAfterMismatch_Getter(Environment env, FilterStore store, int value) {
      yield return store.Get(x => (int)x == value);
      env.ActiveProcess.Succeed(env.Now.Second);
    }

    [TestMethod]
    public void TestResourceImmediateRequests() {
      /* A process must not acquire a resource if it releases it and immediately
       * requests it again while there are already other requesting processes.
       */
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestResourceImmediateRequests_Parent(env));
      env.Run();
      Assert.AreEqual(env.Now.Second, 6);
    }

    private IEnumerable<Event> TestResourceImmediateRequests_Parent(Environment env) {
      var res = new Resource(env, 1);
      var childA = env.Process(TestResourceImmediateRequests_Child(env, res));
      var childB = env.Process(TestResourceImmediateRequests_Child(env, res));
      yield return childA;
      yield return childB;

      Assert.IsTrue(new[] { 0, 2, 4 }.SequenceEqual((IList<int>)childA.Value));
      Assert.IsTrue(new[] { 1, 3, 5 }.SequenceEqual((IList<int>)childB.Value));
    }

    private IEnumerable<Event> TestResourceImmediateRequests_Child(Environment env, Resource res) {
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

    [TestMethod]
    public void TestFilterCallsBestCase() {
      var env = new Environment();
      var store = new FilterStore(env, new object[] { 1, 2, 3 }, 3);
      var log = new List<string>();
      Func<object, bool> filterLogger = o => { log.Add(string.Format("check {0}", o)); return true; };
      env.Process(TestFilterCallsBestCaseProcess(store, filterLogger, log));
      env.Run();
      Assert.IsTrue(log.SequenceEqual(new[] { "check 1", "get 1", "check 2", "get 2", "check 3", "get 3" }));
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

    [TestMethod]
    public void TestFilterCallsWorstCase() {
      var env = new Environment();
      var store = new FilterStore(env, 4);
      var log = new List<string>();
      Func<object, bool> filterLogger = o => { log.Add(string.Format("check {0}", o)); return (int)o >= 3; };
      env.Process(TestFilterCallsWorseCaseGetProcess(store, filterLogger, log));
      env.Process(TestFilterCallsWorstCasePutProcess(store, log));
      env.Run();
      Assert.IsTrue(log.SequenceEqual(new[] {
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
      public MyContainer(Environment environment, double capacity = Double.MaxValue, double initial = 0) : base(environment, capacity, initial) { }
    }
    class MyFilterStore : FilterStore {
      public int PutQueueLength { get { return PutQueue.Count; } }
      public int GetQueueLength { get { return GetQueue.Count; } }
      public MyFilterStore(Environment environment, int capacity = Int32.MaxValue) : base(environment, capacity) { }
    }
    class MyPreemptiveResource : PreemptiveResource {
      public int RequestQueueLength { get { return RequestQueue.SelectMany(x => x.Value).Count(); } }
      public int ReleaseQueueLength { get { return ReleaseQueue.Count; } }
      public MyPreemptiveResource(Environment environment, int capacity = 1) : base(environment, capacity) { }
    }
    class MyPriorityResource : PriorityResource {
      public int RequestQueueLength { get { return RequestQueue.SelectMany(x => x.Value).Count(); } }
      public int ReleaseQueueLength { get { return ReleaseQueue.Count; } }
      public MyPriorityResource(Environment environment, int capacity = 1) : base(environment, capacity) { }
    }
    class MyResource : Resource {
      public int RequestQueueLength { get { return RequestQueue.Count; } }
      public int ReleaseQueueLength { get { return ReleaseQueue.Count; } }
      public MyResource(Environment environment, int capacity = 1) : base(environment, capacity) { }
    }
    class MyResourcePool : ResourcePool {
      public int RequestQueueLength { get { return RequestQueue.Count; } }
      public int ReleaseQueueLength { get { return ReleaseQueue.Count; } }
      public MyResourcePool(Environment environment, IEnumerable<object> items) : base(environment, items) { }
    }
    class MyStore : Store {
      public int PutQueueLength { get { return PutQueue.Count; } }
      public int GetQueueLength { get { return GetQueue.Count; } }
      public MyStore(Environment environment, int capacity = Int32.MaxValue) : base(environment, capacity) { }
    }

    [TestMethod]
    public void TestImmediateContainer() {
      var env = new Environment();
      var res = new MyContainer(env);
      Assert.AreEqual(0, res.Level);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);

      var put = res.Put(1);
      Assert.IsTrue(put.IsTriggered);
      Assert.AreEqual(1, res.Level);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);

      var get = res.Get(1);
      Assert.IsTrue(get.IsTriggered);
      Assert.AreEqual(0, res.Level);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);

      get = res.Get(1);
      Assert.IsFalse(get.IsTriggered);
      Assert.AreEqual(0, res.Level);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(1, res.GetQueueLength);

      put = res.Put(1);
      Assert.IsTrue(put.IsTriggered);
      Assert.AreEqual(1, res.Level);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(1, res.GetQueueLength);

      env.Run();
      Assert.IsTrue(get.IsTriggered);
      Assert.AreEqual(0, res.Level);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);
    }

    [TestMethod]
    public void TestImmediateFilterStore() {
      var env = new Environment();
      var res = new MyFilterStore(env);
      Assert.AreEqual(0, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);

      var put = res.Put(1);
      Assert.IsTrue(put.IsTriggered);
      Assert.AreEqual(1, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);

      var get = res.Get();
      Assert.IsTrue(get.IsTriggered);
      Assert.AreEqual(0, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);

      get = res.Get();
      Assert.IsFalse(get.IsTriggered);
      Assert.AreEqual(0, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(1, res.GetQueueLength);

      put = res.Put(1);
      Assert.IsTrue(put.IsTriggered);
      Assert.AreEqual(1, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(1, res.GetQueueLength);

      env.Run();
      Assert.IsTrue(get.IsTriggered);
      Assert.AreEqual(0, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);
    }

    [TestMethod]
    public void TestImmediatePreemptiveResource() {
      var env = new Environment();
      var res = new MyPreemptiveResource(env, capacity: 1);
      Assert.AreEqual(0, res.InUse);
      Assert.AreEqual(1, res.Remaining);
      Assert.AreEqual(0, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req1 = res.Request(1, true);
      Assert.IsTrue(req1.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(0, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req2 = res.Request(1, true);
      Assert.IsFalse(req2.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      req1.Dispose();
      Assert.IsFalse(req2.IsTriggered);
      Assert.AreEqual(0, res.InUse);
      Assert.AreEqual(1, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req3 = res.Request(1, true);
      Assert.IsTrue(req2.IsTriggered);
      Assert.IsFalse(req3.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);
    }

    [TestMethod]
    public void TestImmediatePriorityResource() {
      var env = new Environment();
      var res = new MyPriorityResource(env, capacity: 1);
      Assert.AreEqual(0, res.InUse);
      Assert.AreEqual(1, res.Remaining);
      Assert.AreEqual(0, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req1 = res.Request(1);
      Assert.IsTrue(req1.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(0, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req2 = res.Request(1);
      Assert.IsFalse(req2.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      req1.Dispose();
      Assert.IsFalse(req2.IsTriggered);
      Assert.AreEqual(0, res.InUse);
      Assert.AreEqual(1, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req3 = res.Request(1);
      Assert.IsTrue(req2.IsTriggered);
      Assert.IsFalse(req3.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);
    }

    [TestMethod]
    public void TestImmediateResource() {
      var env = new Environment();
      var res = new MyResource(env, capacity: 1);
      Assert.AreEqual(0, res.InUse);
      Assert.AreEqual(1, res.Remaining);
      Assert.AreEqual(0, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req1 = res.Request();
      Assert.IsTrue(req1.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(0, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req2 = res.Request();
      Assert.IsFalse(req2.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      req1.Dispose();
      Assert.IsFalse(req2.IsTriggered);
      Assert.AreEqual(0, res.InUse);
      Assert.AreEqual(1, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req3 = res.Request();
      Assert.IsTrue(req2.IsTriggered);
      Assert.IsFalse(req3.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);
    }

    [TestMethod]
    public void TestImmediateResourcePool() {
      var env = new Environment();
      var res = new MyResourcePool(env, new object[] { 1 });
      Assert.AreEqual(0, res.InUse);
      Assert.AreEqual(1, res.Remaining);
      Assert.AreEqual(0, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req1 = res.Request();
      Assert.IsTrue(req1.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(0, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req2 = res.Request();
      Assert.IsFalse(req2.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      req1.Dispose();
      Assert.IsFalse(req2.IsTriggered);
      Assert.AreEqual(0, res.InUse);
      Assert.AreEqual(1, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);

      var req3 = res.Request();
      Assert.IsTrue(req2.IsTriggered);
      Assert.IsFalse(req3.IsTriggered);
      Assert.AreEqual(1, res.InUse);
      Assert.AreEqual(0, res.Remaining);
      Assert.AreEqual(1, res.RequestQueueLength);
      Assert.AreEqual(0, res.ReleaseQueueLength);
    }

    [TestMethod]
    public void TestImmediateStore() {
      var env = new Environment();
      var res = new MyStore(env);
      Assert.AreEqual(0, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);

      var put = res.Put(1);
      Assert.IsTrue(put.IsTriggered);
      Assert.AreEqual(1, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);

      var get = res.Get();
      Assert.IsTrue(get.IsTriggered);
      Assert.AreEqual(0, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);

      get = res.Get();
      Assert.IsFalse(get.IsTriggered);
      Assert.AreEqual(0, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(1, res.GetQueueLength);

      put = res.Put(1);
      Assert.IsTrue(put.IsTriggered);
      Assert.AreEqual(1, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(1, res.GetQueueLength);

      env.Run();
      Assert.IsTrue(get.IsTriggered);
      Assert.AreEqual(0, res.Count);
      Assert.AreEqual(0, res.PutQueueLength);
      Assert.AreEqual(0, res.GetQueueLength);
    }
  }
}
