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
using System.Linq;

namespace SimSharp {
  public class PreemptiveResource {

    public int Capacity { get; protected set; }

    public int InUse { get { return Users.Count; } }

    public int Remaining { get { return Capacity - InUse; } }

    protected Environment Environment { get; private set; }

    protected SortedList<int, LinkedList<PreemptiveRequest>> RequestQueue { get; private set; }
    protected Queue<Release> ReleaseQueue { get; private set; }
    protected HashSet<Request> Users { get; private set; }

    public PreemptiveResource(Environment environment, int capacity = 1) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0.", "capacity");
      Environment = environment;
      Capacity = capacity;
      RequestQueue = new SortedList<int, LinkedList<PreemptiveRequest>>();
      ReleaseQueue = new Queue<Release>();
      Users = new HashSet<Request>();
    }

    public virtual PreemptiveRequest Request(int priority = 1, bool preempt = false) {
      var request = new PreemptiveRequest(Environment, TriggerRelease, DisposeCallback, priority, preempt);
      if (!RequestQueue.ContainsKey(request.Priority))
        RequestQueue.Add(request.Priority, new LinkedList<PreemptiveRequest>());
      RequestQueue[request.Priority].AddLast(request);
      TriggerRequest();
      return request;
    }

    public virtual Release Release(PreemptiveRequest request) {
      var release = new Release(Environment, request, TriggerRequest);
      ReleaseQueue.Enqueue(release);
      TriggerRelease();
      return release;
    }

    protected void DisposeCallback(Event @event) {
      var request = @event as PreemptiveRequest;
      if (request != null) Release(request);
    }

    protected virtual void DoRequest(PreemptiveRequest request) {
      if (Users.Count >= Capacity && request.Preempt) {
        // Check if we can preempt another process
        var oldest = Users.OfType<PreemptiveRequest>().Select((r, i) => new { Request = r, Index = i })
          .OrderByDescending(x => x.Request.Priority)
          .ThenByDescending(x => x.Request.Time)
          .ThenByDescending(x => x.Request.Preempt)
          .ThenByDescending(x => x.Index)
          .First().Request;
        if (oldest.Priority > request.Priority || (oldest.Priority == request.Priority
            && (!oldest.Preempt && request.Preempt || (oldest.Preempt == request.Preempt
              && oldest.Time > request.Time)))) {
          Users.Remove(oldest);
          oldest.Process.Interrupt(new Preempted(request.Process, oldest.Time));
        }
      }
      if (Users.Count < Capacity) {
        Users.Add(request);
        request.Succeed();
      }
    }

    protected virtual void DoRelease(Release release) {
      if (!Users.Remove(release.Request)) {
        var preemptRequest = release.Request as PreemptiveRequest;
        if (preemptRequest != null) {
          var current = RequestQueue[preemptRequest.Priority].First;
          while (current != null && current.Value != release.Request)
            current = current.Next;
          if (current != null) RequestQueue[preemptRequest.Priority].Remove(current);
        }
      }
      release.Succeed();
    }

    protected virtual void TriggerRequest(Event @event = null) {
      foreach (var entry in RequestQueue) {
        var requests = entry.Value;
        var current = requests.First;
        while (current != null) {
          var request = current.Value;
          DoRequest(request);
          if (request.IsTriggered) {
            var next = current.Next;
            requests.Remove(current);
            current = next;
          } else current = current.Next;
        }
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
