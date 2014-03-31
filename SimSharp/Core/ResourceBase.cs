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

using System.Collections.Generic;

namespace SimSharp {
  public abstract class ResourceBase {

    protected readonly int MaxCapacity;
    protected Environment Environment { get; private set; }
    protected List<Request> Users { get; private set; }

    protected ResourceBase(Environment environment, int capacity = 1) {
      MaxCapacity = capacity;
      Environment = environment;
      Users = new List<Request>();
    }

    public virtual Request Request(int priority = 1, bool preempt = false) {
      var request = new Request(Environment, TriggerRelease, ReleaseCallback, priority, preempt);
      AddRequest(request);
      Request(request);
      return request;
    }

    public virtual Release Release(Request request) {
      var release = new Release(Environment, request, TriggerRequest);
      AddRelease(release);
      Release(release);
      return release;
    }

    protected void ReleaseCallback(Event @event) {
      var request = @event as Request;
      if (request != null) Release(request);
    }

    protected virtual void Request(Request request) {
      if (Users.Count < MaxCapacity) {
        Users.Add(request);
        request.Succeed();
      }
    }

    protected virtual void Release(Release release) {
      if (!release.Request.IsTriggered) RemoveRequest(release.Request);
      Users.Remove(release.Request);
      release.Succeed();
      if (!release.IsTriggered) RemoveRelease(release);
    }

    protected virtual void TriggerRequest(Event @event) {
      RemoveRelease((Release)@event);
      foreach (var requestEvent in Requests) {
        if (!requestEvent.IsTriggered) Request(requestEvent);
        if (!requestEvent.IsTriggered) break;
      }
    }

    protected virtual void TriggerRelease(Event @event) {
      RemoveRequest((Request)@event);
      foreach (var releaseEvent in Releases) {
        if (!releaseEvent.IsTriggered) Release(releaseEvent);
        if (!releaseEvent.IsTriggered) break;
      }
    }

    protected abstract IEnumerable<Request> Requests { get; }
    protected abstract IEnumerable<Release> Releases { get; }

    protected abstract void AddRequest(Request request);
    protected abstract void RemoveRequest(Request request);
    protected abstract void AddRelease(Release release);
    protected abstract void RemoveRelease(Release release);
  }
}
