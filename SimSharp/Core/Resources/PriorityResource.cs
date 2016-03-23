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

namespace SimSharp {
  public class PriorityResource {

    public int Capacity { get; protected set; }

    public int InUse { get { return Users.Count; } }

    public int Remaining { get { return Capacity - InUse; } }

    protected Environment Environment { get; private set; }

    protected SortedList<int, LinkedList<PriorityRequest>> RequestQueue { get; private set; }
    protected Queue<Release> ReleaseQueue { get; private set; }
    protected HashSet<Request> Users { get; private set; }

    public PriorityResource(Environment environment, int capacity = 1) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0.", "capacity");
      Environment = environment;
      Capacity = capacity;
      RequestQueue = new SortedList<int, LinkedList<PriorityRequest>>();
      ReleaseQueue = new Queue<Release>();
      Users = new HashSet<Request>();
    }

    public virtual PriorityRequest Request(int priority = 1) {
      var request = new PriorityRequest(Environment, TriggerRelease, DisposeCallback, priority);
      if (!RequestQueue.ContainsKey(priority))
        RequestQueue.Add(priority, new LinkedList<PriorityRequest>());
      RequestQueue[priority].AddLast(request);
      TriggerRequest();
      return request;
    }

    public virtual Release Release(PriorityRequest request) {
      var release = new Release(Environment, request, TriggerRequest);
      ReleaseQueue.Enqueue(release);
      TriggerRelease();
      return release;
    }

    protected void DisposeCallback(Event @event) {
      var request = @event as PriorityRequest;
      if (request != null) Release(request);
    }

    protected virtual void DoRequest(Request request) {
      if (Users.Count < Capacity) {
        Users.Add(request);
        request.Succeed();
      }
    }

    protected virtual void DoRelease(Release release) {
      if (!Users.Remove(release.Request)) {
        var prioRequest = release.Request as PriorityRequest;
        if (prioRequest != null) {
          var current = RequestQueue[prioRequest.Priority].First;
          while (current != null && current.Value != release.Request)
            current = current.Next;
          if (current != null) RequestQueue[prioRequest.Priority].Remove(current);
        }
      }
      release.Succeed();
    }

    protected virtual void TriggerRequest(Event @event = null) {
      foreach (var entry in RequestQueue) {
        var cascade = false;
        var requests = entry.Value;
        while (requests.Count > 0) {
          var req = requests.First.Value;
          DoRequest(req);
          if (req.IsTriggered) {
            requests.RemoveFirst();
          } else {
            cascade = true;
            break;
          }
        }
        if (cascade) break;
      }
    }

    protected virtual void TriggerRelease(Event @event = null) {
      while (ReleaseQueue.Count > 0) {
        var release = ReleaseQueue.Peek();
        DoRelease(release);
        if (release.IsTriggered) {
          ReleaseQueue.Dequeue();
        } else break;
      }
    }
  }
}
