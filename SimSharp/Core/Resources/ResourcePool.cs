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
  public class ResourcePool {
    protected static readonly Func<object, bool> TrueFunc = _ => true;

    public int Capacity { get; protected set; }

    public int InUse { get { return Capacity - Remaining; } }

    public int Remaining { get { return Resources.Count; } }

    protected Environment Environment { get; private set; }

    protected LinkedList<ResourcePoolRequest> RequestQueue { get; private set; }
    protected Queue<Release> ReleaseQueue { get; private set; }
    protected List<object> Resources { get; private set; }

    public ResourcePool(Environment environment, IEnumerable<object> resources) {
      Environment = environment;
      if (resources == null) throw new ArgumentNullException("resources");
      Resources = new List<object>(resources);
      Capacity = Resources.Count;
      if (Capacity == 0) throw new ArgumentException("There must be at least one resource", "resources");
      RequestQueue = new LinkedList<ResourcePoolRequest>();
      ReleaseQueue = new Queue<Release>();
    }

    public virtual bool IsAvailable(Func<object, bool> filter) {
      return Resources.Any(filter);
    }

    public virtual ResourcePoolRequest Request(Func<object, bool> filter = null) {
      var request = new ResourcePoolRequest(Environment, TriggerRelease, DisposeCallback, filter ?? TrueFunc);
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
      Resources.Add(release.Request.Value);
      release.Succeed();
    }

    protected virtual void TriggerRequest(Event @event = null) {
      var current = RequestQueue.First;
      while (current != null) {
        var request = current.Value;
        DoRequest(request);
        if (request.IsTriggered) {
          var next = current.Next;
          RequestQueue.Remove(current);
          current = next;
        }
        if (Resources.Count == 0) break;
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
