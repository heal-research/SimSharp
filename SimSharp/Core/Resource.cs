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
  public class Resource : ResourceBase {

    protected List<Request> RequestQueue { get; private set; }
    protected List<Release> ReleaseQueue { get; private set; }

    public Resource(Environment environment, int capacity = 1)
      : base(environment, capacity) {
      RequestQueue = new List<Request>();
      ReleaseQueue = new List<Release>();
    }

    protected override IEnumerable<Request> Requests {
      get { return RequestQueue; }
    }

    protected override IEnumerable<Release> Releases {
      get { return ReleaseQueue; }
    }

    protected override void AddRequest(Request request) {
      RequestQueue.Add(request);
    }

    protected override void RemoveRequest(Request request) {
      RequestQueue.Remove(request);
    }

    protected override void AddRelease(Release release) {
      ReleaseQueue.Add(release);
    }

    protected override void RemoveRelease(Release release) {
      ReleaseQueue.Remove(release);
    }
  }
}
