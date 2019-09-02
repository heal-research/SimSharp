#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public sealed class Preempted {
    public Process By { get; private set; }
    public DateTime UsageSince { get; private set; }

    public Preempted(Process by, DateTime usageSince) {
      By = by;
      UsageSince = usageSince;
    }
  }
}
