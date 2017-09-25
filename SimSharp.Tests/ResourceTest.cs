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
      var env = new Environment(start);
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
    private IEnumerable<Event> TestResourceProc(Environment env, string name, Resource resource,
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
      var env = new Environment(start);
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
    private IEnumerable<Event> TestResourceWithUsingProc(Environment env, string name, Resource resource,
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
      var env = new Environment(start);
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
    private IEnumerable<Event> TestResourceWithUsingAndConditionProc(Environment env, string name, Resource resource,
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
      var env = new Environment(start);
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
    private IEnumerable<Event> TestResourceSlotsProc(Environment env, string name, Resource resource,
      Dictionary<string, DateTime> log) {
      using (var req = resource.Request()) {
        yield return req;
        log.Add(name, env.Now);
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    [Fact]
    public void TestResourceContinueAfterInterrupt() {
      var env = new Environment(new DateTime(2014, 4, 1));
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceContinueAfterInterruptProc(env, res));
      var proc = env.Process(TestResourceContinueAfterInterruptVictim(env, res));
      env.Process(TestResourceContinueAfterInterruptInterruptor(env, proc));
      env.Run();
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptProc(Environment env, Resource res) {
      using (var req = res.Request()) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptVictim(Environment env, Resource res) {
      var req = res.Request();
      yield return req;
      Assert.False(req.IsOk);
      env.ActiveProcess.HandleFault();
      yield return req;
      res.Release(req);
      Assert.Equal(new DateTime(2014, 4, 1, 0, 0, 1), env.Now);
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptInterruptor(Environment env, Process proc) {
      proc.Interrupt();
      yield break;
    }

    [Fact]
    public void TestResourceContinueAfterInterruptWaiting() {
      var env = new Environment(new DateTime(2014, 4, 1));
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceContinueAfterInterruptWaitingProc(env, res));
      var proc = env.Process(TestResourceContinueAfterInterruptWaitingVictim(env, res));
      env.Process(TestResourceContinueAfterInterruptWaitingInterruptor(env, proc));
      env.Run();
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptWaitingProc(Environment env, Resource res) {
      using (var req = res.Request()) {
        yield return req;
        yield return env.Timeout(TimeSpan.FromSeconds(1));
      }
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptWaitingVictim(Environment env, Resource res) {
      var req = res.Request();
      yield return req;
      Assert.False(req.IsOk);
      env.ActiveProcess.HandleFault();
      yield return env.Timeout(TimeSpan.FromSeconds(2));
      yield return req;
      res.Release(req);
      Assert.Equal(new DateTime(2014, 4, 1, 0, 0, 2), env.Now);
    }

    private IEnumerable<Event> TestResourceContinueAfterInterruptWaitingInterruptor(Environment env, Process proc) {
      proc.Interrupt();
      yield break;
    }

    [Fact]
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
      Assert.False(req.IsOk);
      Assert.True(env.ActiveProcess.HandleFault());
      // Dont wait for the resource
      res.Release(req);
      Assert.Equal(new DateTime(2014, 4, 1), env.Now);
    }

    private IEnumerable<Event> TestResourceReleaseAfterInterruptInterruptor(Environment env, Process proc) {
      proc.Interrupt();
      yield break;
    }

    [Fact]
    public void TestResourceWithCondition() {
      var env = new Environment();
      var res = new Resource(env, capacity: 1);
      env.Process(TestResourceWithConditionProc(env, res));
      env.Run();
    }
    private IEnumerable<Event> TestResourceWithConditionProc(Environment env, Resource res) {
      using (var req = res.Request()) {
        var timeout = env.Timeout(TimeSpan.FromSeconds(1));
        yield return req | timeout;
        Assert.True(req.IsOk);
        Assert.False(timeout.IsProcessed);
      }
    }

    [Fact]
    public void TestResourceWithPriorityQueue() {
      var env = new Environment(new DateTime(2014, 4, 1));
      var resource = new PriorityResource(env, capacity: 1);
      env.Process(TestResourceWithPriorityQueueProc(env, 0, resource, 2, 0));
      env.Process(TestResourceWithPriorityQueueProc(env, 2, resource, 3, 10));
      env.Process(TestResourceWithPriorityQueueProc(env, 2, resource, 3, 15)); // Test equal priority
      env.Process(TestResourceWithPriorityQueueProc(env, 4, resource, 1, 5));
      env.Run();
    }
    private IEnumerable<Event> TestResourceWithPriorityQueueProc(Environment env, int delay, PriorityResource resource, int priority, int resTime) {
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
      var env = new Environment(start);
      var res = new PreemptiveResource(env, capacity: 2);
      var log = new Dictionary<DateTime, int>();
      //                                id           d  p
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
    private IEnumerable<Event> TestPreemtiveResourceProc(int id, Environment env, PreemptiveResource res, int delay, int prio, Dictionary<DateTime, int> log) {
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
      var env = new Environment();
      var res = new PreemptiveResource(env, capacity: 1);
      env.Process(TestPreemptiveResourceTimeoutA(env, res, 1));
      env.Process(TestPreemptiveResourceTimeoutB(env, res, 0));
      env.Run();
    }
    private IEnumerable<Event> TestPreemptiveResourceTimeoutA(Environment env, PreemptiveResource res, int prio) {
      using (var req = res.Request(priority: prio, preempt: true)) {
        yield return req;
        Assert.True(env.ActiveProcess.HandleFault());
        yield return env.Timeout(TimeSpan.FromSeconds(1));
        Assert.False(env.ActiveProcess.HandleFault());
      }
    }
    private IEnumerable<Event> TestPreemptiveResourceTimeoutB(Environment env, PreemptiveResource res, int prio) {
      using (var req = res.Request(priority: prio, preempt: true)) {
        yield return req;
      }
    }

    [Fact]
    public void TestMixedPreemtion() {
      var start = new DateTime(2014, 4, 2);
      var env = new Environment(start);
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
    private IEnumerable<Event> TestMixedPreemtionProc(int id, Environment env, PreemptiveResource res, int delay, int prio, bool preempt, List<Tuple<int, int>> log) {
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
      var env = new Environment(start) {
        Logger = new StringWriter(sb)
      };
      var sto = new FilterStore(env, capacity: 1);
      env.Process(FilterStoreProducer(env, sto));
      env.Process(FilterStoreConsumerA(env, sto));
      env.Process(FilterStoreConsumerB(env, sto));
      env.Run(TimeSpan.FromSeconds(20));
      Assert.Equal(
@"4: Produce A
4: Consume A
6: Produce B
6: Consume B
10: Produce A
14: Consume A
14: Produce B
14: Consume B
18: Produce A
", sb.ToString());
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

    [Fact]
    public void TestFilterStoreGetAfterMismatch() {
      var env = new Environment(new DateTime(2014, 1, 1));
      var store = new FilterStore(env, capacity: 2);
      var proc1 = env.Process(TestFilterStoreGetAfterMismatch_Getter(env, store, 1));
      var proc2 = env.Process(TestFilterStoreGetAfterMismatch_Getter(env, store, 2));
      env.Process(TestFilterStoreGetAfterMismatch_Putter(env, store));
      env.Run();
      Assert.Equal(1, proc1.Value);
      Assert.Equal(0, proc2.Value);
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

    [Fact]
    public void TestResourceImmediateRequests() {
      /* A process must not acquire a resource if it releases it and immediately
       * requests it again while there are already other requesting processes.
       */
      var env = new Environment(new DateTime(2014, 1, 1));
      env.Process(TestResourceImmediateRequests_Parent(env));
      env.Run();
      Assert.Equal(6, env.Now.Second);
    }

    private IEnumerable<Event> TestResourceImmediateRequests_Parent(Environment env) {
      var res = new Resource(env, 1);
      var childA = env.Process(TestResourceImmediateRequests_Child(env, res));
      var childB = env.Process(TestResourceImmediateRequests_Child(env, res));
      yield return childA;
      yield return childB;

      Assert.True(new[] { 0, 2, 4 }.SequenceEqual((IList<int>)childA.Value));
      Assert.True(new[] { 1, 3, 5 }.SequenceEqual((IList<int>)childB.Value));
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

    [Fact]
    public void TestFilterCallsBestCase() {
      var env = new Environment();
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
      var env = new Environment();
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

    [Fact]
    public void TestImmediateContainer() {
      var env = new Environment();
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
      var env = new Environment();
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
      var env = new Environment();
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
      var env = new Environment();
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
      var env = new Environment();
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
      var env = new Environment();
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
      var env = new Environment();
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
  }
}
