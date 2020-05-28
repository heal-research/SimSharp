#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimSharp.Samples {
  public class GantryCranes {

    private IEnumerable<Event> Crane(Simulation env, Runway runway, int @base) {
      var rand = new System.Random(@base);
      while (true) {
        var pos = 25 + rand.Next(51);
        RunwayRequest req = null;
        if (@base < pos) {
          req = runway.Request(@base, pos);
        } else {
          req = runway.Request(pos, @base);
        }
        using (req) {
          env.Log("{0}: Requesting to move from {1} to {2} and back", env.Now, @base, pos);
          yield return req;
          env.Log("{0}: Moving from {1} to {2} and back", env.Now, @base, pos);
          yield return env.Timeout(TimeSpan.FromSeconds(pos));
          env.Log("{0}: Back at {1}", env.Now, @base);
        }
      }
    }

    public void Simulate() {
      var env = new Simulation(TimeSpan.FromMinutes(1));
      env.Log("== Gantry Cranes ==");
      var runway = new Runway(env);
      env.Process(Crane(env, runway, 0));
      env.Process(Crane(env, runway, 100));
      env.Run(TimeSpan.FromMinutes(100));
    }
  }


  class RunwayRequest : Request {
    public int LowerPosition { get; private set; }
    public int HigherPosition { get; private set; }

    public RunwayRequest(Simulation environment, Action<Event> callback, Action<Event> disposeCallback, int lower, int higher)
      : base(environment, callback, disposeCallback) {
      LowerPosition = lower;
      HigherPosition = higher;
    }
  }
  // This is the crane runway
  class Runway {

    protected Simulation Environment { get; private set; }

    protected LinkedList<RunwayRequest> RequestQueue { get; private set; }
    protected Queue<Release> ReleaseQueue { get; private set; }
    protected HashSet<RunwayRequest> Users { get; private set; }
    public Runway(Simulation environment) {
      Environment = environment;
      RequestQueue = new LinkedList<RunwayRequest>();
      ReleaseQueue = new Queue<Release>();
      Users = new HashSet<RunwayRequest>();
    }

    public virtual RunwayRequest Request(int lower, int higher) {
      var request = new RunwayRequest(Environment, TriggerRelease, DisposeCallback, lower, higher);
      RequestQueue.AddLast(request);
      TriggerRequest();
      return request;
    }

    public virtual Release Release(RunwayRequest request) {
      var release = new Release(Environment, request, TriggerRequest);
      ReleaseQueue.Enqueue(release);
      TriggerRelease();
      return release;
    }

    protected virtual void DisposeCallback(Event @event) {
      var request = @event as RunwayRequest;
      if (request != null) {
        Release(request);
      }
    }

    protected virtual void DoRequest(RunwayRequest request) {
      if (!Users.Any(x => IsOverlap(x, request))) {
        Users.Add(request);
        request.Succeed();
      }
    }

    private bool IsOverlap(RunwayRequest x, RunwayRequest request) {
      return x.LowerPosition < request.LowerPosition && x.HigherPosition >= request.LowerPosition // overlap left
        || x.LowerPosition >= request.LowerPosition && x.HigherPosition <= request.HigherPosition // contained
        || x.LowerPosition < request.LowerPosition && x.HigherPosition > request.HigherPosition // contains
        || x.LowerPosition <= request.HigherPosition && x.HigherPosition > request.HigherPosition; // overlap right
    }

    protected virtual void DoRelease(Release release) {
      if (!Users.Remove((RunwayRequest)release.Request))
        throw new InvalidOperationException("Released request does not have a user.");
      release.Succeed();
    }

    protected virtual void TriggerRequest(Event @event = null) {
      while (RequestQueue.Count > 0) {
        var request = RequestQueue.First.Value;
        DoRequest(request);
        if (request.IsTriggered) {
          RequestQueue.RemoveFirst();
        } else break;
      }
    }

    protected virtual void TriggerRelease(Event @event = null) {
      while (ReleaseQueue.Count > 0) {
        var release = ReleaseQueue.Peek();
        if (release.Request.IsAlive) {
          if (!RequestQueue.Remove((RunwayRequest)release.Request))
            throw new InvalidOperationException("Failed to cancel a request.");
          release.Succeed();
          ReleaseQueue.Dequeue();
        } else {
          DoRelease(release);
          if (release.IsTriggered) {
            ReleaseQueue.Dequeue();
          } else break;
        }
      }
    }
  }
}
