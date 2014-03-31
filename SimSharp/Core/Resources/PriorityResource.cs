#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2014  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
  public class PriorityResource {

    public int Capacity { get; protected set; }

    protected Environment Environment { get; private set; }

    protected SortedList<int, List<PriorityRequest>> RequestQueue { get; private set; }
    protected List<Release> ReleaseQueue { get; private set; }
    protected List<Request> Users { get; private set; }

    public PriorityResource(Environment environment, int capacity = 1) {
      if (capacity <= 0) throw new ArgumentException("Capacity must be > 0.", "capacity");
      Environment = environment;
      Capacity = capacity;
      RequestQueue = new SortedList<int, List<PriorityRequest>>();
      ReleaseQueue = new List<Release>();
      Users = new List<Request>();
    }

    public virtual PriorityRequest Request(int priority = 1) {
      var request = new PriorityRequest(Environment, TriggerRelease, ReleaseCallback, priority);
      if (RequestQueue.ContainsKey(request.Priority)) RequestQueue[request.Priority].Add(request);
      else RequestQueue.Add(request.Priority, new List<PriorityRequest>() { request });
      Request(request);
      return request;
    }

    public virtual Release Release(PriorityRequest request) {
      var release = new Release(Environment, request, TriggerRequest);
      ReleaseQueue.Add(release);
      Release(release);
      return release;
    }

    protected void ReleaseCallback(Event @event) {
      var request = @event as PriorityRequest;
      if (request != null) Release(request);
    }

    protected virtual void Request(Request request) {
      if (Users.Count < Capacity) {
        Users.Add(request);
        request.Succeed();
      }
    }

    protected virtual void Release(Release release) {
      if (!release.Request.IsTriggered) {
        var prioRequest = release.Request as PriorityRequest;
        if (prioRequest == null) throw new ArgumentException("Must remove a PriorityRequest from a PriorityResource.", "release");
        RequestQueue[prioRequest.Priority].Remove(prioRequest);
      }
      Users.Remove(release.Request);
      release.Succeed();
      if (!release.IsTriggered) ReleaseQueue.Remove(release);
    }

    protected virtual void TriggerRequest(Event @event) {
      ReleaseQueue.Remove((Release)@event);
      foreach (var requestEvent in RequestQueue.SelectMany(x => x.Value)) {
        if (!requestEvent.IsTriggered) Request(requestEvent);
        if (!requestEvent.IsTriggered) break;
      }
    }

    protected virtual void TriggerRelease(Event @event) {
      var prioRequest = @event as PriorityRequest;
      if (prioRequest == null) throw new ArgumentException("Must remove a PriorityRequest from a PriorityResource.", "event");
      RequestQueue[prioRequest.Priority].Remove(prioRequest);

      foreach (var releaseEvent in ReleaseQueue) {
        if (!releaseEvent.IsTriggered) Release(releaseEvent);
        if (!releaseEvent.IsTriggered) break;
      }
    }
  }
}
