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
  public class ResourcePool {
    protected static readonly Func<object, bool> TrueFunc = _ => true;

    public int TotalCapacity { get; protected set; }
    public int Capacity { get { return Resources.Count; } }

    protected Environment Environment { get; private set; }

    protected List<ResourcePoolRequest> RequestQueue { get; private set; }
    protected List<Release> ReleaseQueue { get; private set; }
    protected List<object> Resources { get; private set; }

    public ResourcePool(Environment environment, IEnumerable<object> resources) {
      if (resources == null || !resources.Any()) throw new ArgumentException("There must be at least one resource", "resources");
      Environment = environment;
      RequestQueue = new List<ResourcePoolRequest>();
      ReleaseQueue = new List<Release>();
      Resources = new List<object>(resources);
      TotalCapacity = Resources.Count;
    }

    public virtual ResourcePoolRequest Request(Func<object, bool> filter = null) {
      var request = new ResourcePoolRequest(Environment, TriggerRelease, ReleaseCallback, filter ?? TrueFunc);
      RequestQueue.Add(request);
      DoRequest(request);
      return request;
    }

    public virtual Release Release(Request request) {
      var release = new Release(Environment, request, TriggerRequest);
      ReleaseQueue.Add(release);
      DoRelease(release);
      return release;
    }

    public virtual void ProcessRequests() {
      foreach (var @event in RequestQueue) {
        if (!@event.IsTriggered) DoRequest(@event);
      }
    }

    public virtual bool IsAvailable(Func<object, bool> filter) {
      return Resources.Any(filter);
    }

    protected virtual void ReleaseCallback(Event @event) {
      var request = @event as Request;
      if (request != null) Release(request);
    }

    protected virtual void DoRequest(ResourcePoolRequest request) {
      foreach (var o in Resources) {
        if (!request.Filter(o)) continue;
        Resources.Remove(o);
        request.Succeed(o);
        return;
      }
    }

    protected virtual void DoRelease(Release release) {
      if (release.Request.IsAlive) RequestQueue.Remove((ResourcePoolRequest)release.Request);
      if (release.Request.IsProcessed) Resources.Add(release.Request.Value);
      release.Succeed();
      if (!release.IsTriggered) ReleaseQueue.Remove(release);
    }

    protected virtual void TriggerRequest(Event @event) {
      ReleaseQueue.Remove((Release)@event);
      foreach (var requestEvent in RequestQueue) {
        if (!requestEvent.IsTriggered) DoRequest(requestEvent);
      }
    }

    protected virtual void TriggerRelease(Event @event) {
      RequestQueue.Remove((ResourcePoolRequest)@event);
      foreach (var releaseEvent in ReleaseQueue) {
        if (!releaseEvent.IsTriggered) DoRelease(releaseEvent);
        if (!releaseEvent.IsTriggered) break;
      }
    }
  }
}
