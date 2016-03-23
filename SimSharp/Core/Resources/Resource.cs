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
  public class Resource {

    public int Capacity { get; protected set; }

    public int InUse { get { return Users.Count; } }

    public int Remaining { get { return Capacity - InUse; } }

    protected Environment Environment { get; private set; }

    protected LinkedList<Request> RequestQueue { get; private set; }
    protected Queue<Release> ReleaseQueue { get; private set; }
    protected HashSet<Request> Users { get; private set; }

    public Resource(Environment environment, int capacity = 1) {
      if (capacity <= 0) throw new ArgumentException("Capacity must > 0.", "capacity");
      Environment = environment;
      Capacity = capacity;
      RequestQueue = new LinkedList<Request>();
      ReleaseQueue = new Queue<Release>();
      Users = new HashSet<Request>();
    }

    public virtual Request Request() {
      var request = new Request(Environment, TriggerRelease, DisposeCallback);
      RequestQueue.AddLast(request);
      TriggerRequest();
      return request;
    }

    public virtual Release Release(Request request) {
      var release = new Release(Environment, request, TriggerRequest);
      ReleaseQueue.Enqueue(release);
      TriggerRelease();
      return release;
    }

    protected virtual void DisposeCallback(Event @event) {
      var request = @event as Request;
      if (request != null) {
        Release(request);
      }
    }

    protected virtual void DoRequest(Request request) {
      if (Users.Count < Capacity) {
        Users.Add(request);
        request.Succeed();
      }
    }

    protected virtual void DoRelease(Release release) {
      if (!Users.Remove(release.Request)) {
        var current = RequestQueue.First;
        while (current != null && current.Value != release.Request)
          current = current.Next;
        if (current != null) RequestQueue.Remove(current);
      }
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
        DoRelease(release);
        if (release.IsTriggered) {
          ReleaseQueue.Dequeue();
        } else break;
      }
    }
  }
}
