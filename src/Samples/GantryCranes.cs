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

    private IEnumerable<Event> AssignedMoves(Simulation env, TrackMovable crane, Track track) {
      while (true) {
        var targetPos = (int)Math.Round(env.RandUniform(5, 95));
        var order = new TrackOrder(env, crane, targetPos, TimeSpan.FromSeconds(10));
        track.Perform(new [] { order });
        yield return order.Started;
        //env.Log("{0}: {3} Moving from {1} to {2}", env.Now, crane.GetCurrentPosition(), targetPos, crane.Name);
        yield return order.PositionReached;
        //env.Log("{0}: {1} Servicing at {2}", env.Now, crane.Name, crane.GetCurrentPosition());
        yield return order;
        //env.Log("{0}: {2} Finished at {1}", env.Now, crane.GetCurrentPosition(), crane.Name);
        yield return env.TimeoutExponential(TimeSpan.FromSeconds(30));
        Console.WriteLine("AssignedMoves");
      }
    }

    private IEnumerable<Event> UnassignedMoves(Simulation env, Track track) {
      while (true) {        
        var targetPos = (int)Math.Round(env.RandUniform(5, 95));
        var order = new TrackOrder(env, targetPos, TimeSpan.FromSeconds(10));
        track.Perform(new [] { order });
        yield return order.Started;
        //env.Log("{0}: {3} Moving from {1} to {2}", env.Now, order.Assigned.GetCurrentPosition(), targetPos, order.Assigned);
        yield return order.PositionReached;
        //env.Log("{0}: {1} Servicing at {2}", env.Now, order.Assigned, order.Assigned.GetCurrentPosition());
        yield return order;
        //env.Log("{0}: {2} Finished at {1}", env.Now, order.Assigned.GetCurrentPosition(), order.Assigned);
        yield return env.TimeoutExponential(TimeSpan.FromSeconds(30));
        Console.WriteLine("UnassignedMoves");
      }
    }

    private IEnumerable<Event> AssignedChainedMoves(Simulation env, TrackMovable crane, Track track) {
      while (true) {        
        var pickupPos = (int)Math.Round(env.RandUniform(5, 95));
        var dropoffPos = (int)Math.Round(env.RandUniform(5, 95));
        while (pickupPos == dropoffPos) dropoffPos = (int)Math.Round(env.RandUniform(5, 95));

        var pickupOrder = new TrackOrder(env, crane, pickupPos, TimeSpan.FromSeconds(10));
        // the dropoff must be performed by the same crane as the pickup in direct predecessor cases
        var dropoffOrder = new TrackOrder(env, crane, dropoffPos, TimeSpan.FromSeconds(10), pickupOrder);
        track.Perform(new [] { pickupOrder, dropoffOrder });
        yield return pickupOrder.Started;
        yield return pickupOrder.PositionReached;
        yield return pickupOrder;
        yield return dropoffOrder.Started;
        if (pickupOrder.Assigned != dropoffOrder.Assigned) throw new InvalidOperationException("Assigned crane is not the same in chained move.");
        yield return dropoffOrder.PositionReached;
        yield return dropoffOrder;
        yield return env.TimeoutExponential(TimeSpan.FromSeconds(30));
        Console.WriteLine("AssignedChainedMoves");
      }
    }

    private IEnumerable<Event> UnassignedChainedMoves(Simulation env, Track track) {
      while (true) {        
        var pickupPos = (int)Math.Round(env.RandUniform(5, 95));
        var dropoffPos = (int)Math.Round(env.RandUniform(5, 95));
        while (pickupPos == dropoffPos) dropoffPos = (int)Math.Round(env.RandUniform(5, 95));

        var pickupOrder = new TrackOrder(env, pickupPos, TimeSpan.FromSeconds(10));
        var dropoffOrder = new TrackOrder(env, dropoffPos, TimeSpan.FromSeconds(10), pickupOrder);
        track.Perform(new [] { pickupOrder, dropoffOrder });
        yield return pickupOrder.Started;
        yield return pickupOrder.PositionReached;
        yield return pickupOrder;
        yield return dropoffOrder.Started;
        if (pickupOrder.Assigned != dropoffOrder.Assigned) throw new InvalidOperationException("Assigned crane is not the same in chained move.");
        yield return dropoffOrder.PositionReached;
        yield return dropoffOrder;
        yield return env.TimeoutExponential(TimeSpan.FromSeconds(30));
        Console.WriteLine("UnassignedChainedMoves");
      }
    }

    private IEnumerable<Event> SpaceTimeDrawing(Simulation env, Track track) {
      while (true) {
        var lastPos = 0.0;
        foreach (var m in track.Movables.OrderBy(x => x.GetCurrentPosition())) {
          if (m.Name == "Crane 1") Console.ForegroundColor = ConsoleColor.Red;
          else if (m.Name == "Crane 2") Console.ForegroundColor = ConsoleColor.Green;
          else if (m.Name == "Crane 3") Console.ForegroundColor = ConsoleColor.Yellow;
          var symbol = "|";
          if (m.State == TrackMovableState.Idle) symbol = "!";
          else if (m.State == TrackMovableState.Servicing) symbol = "x";
          Console.Write(symbol.PadLeft((int)(m.GetCurrentPosition() - lastPos)));
          Console.ResetColor();
          lastPos = m.GetCurrentPosition();
        }
        Console.CursorLeft = 100;
        Console.Write(env.NowD);
        var sep = " ";
        foreach (var m in track.Movables.OrderBy(x => x.GetCurrentPosition())) {
          Console.Write(sep + (int)m.GetCurrentPosition() + "->" + m.TargetPosition);
        }
        sep = " / ";
        foreach (var o in track.ActiveOrders) {
          Console.Write(sep + o.Assigned + "->" + o.Position);
          sep = " ";
        }
        Console.WriteLine();
        yield return env.Timeout(TimeSpan.FromSeconds(5));
      }
    }

    public void Simulate() {
      var env = new Simulation(42, TimeSpan.FromSeconds(1));
      env.Log("== Gantry Cranes ==");
      var cranes = Enumerable.Range(0, 3).Select(x => new TrackMovable(env, "Crane " + (x+1), x * 5, 1, 1)).ToList();
      var track = new Track(env, 100, cranes);
      foreach (var crane in cranes) {
        env.Process(AssignedMoves(env, crane, track));
        env.Process(AssignedChainedMoves(env, crane, track));
      }
      env.Process(UnassignedMoves(env, track));
      env.Process(UnassignedChainedMoves(env, track));
      env.Process(SpaceTimeDrawing(env, track));
      env.Run(TimeSpan.FromMinutes(100));
    }
  }

  public class TrackOrder : Event {
    public double Position { get; private set; }
    public TimeSpan ServiceTime { get; private set; }
    public DateTime ReleaseTime { get; private set; }
    public DateTime DueDate { get; private set; }
    public TrackMovable Required { get; private set; }
    private TrackMovable _assigned;
    public TrackMovable Assigned {
      get { return _assigned; }
      set {
        if (Required != null && value != Required) throw new ArgumentException("Cannot assign task to other movable than Required");
        _assigned = value;
      }
    }
    public HashSet<TrackOrder> Predecessors { get; private set; }
    public TrackOrder DirectPredecessor { get; private set; }

    public Event Started { get; private set; }
    public Event PositionReached { get; private set; }

    public TrackOrder(Simulation environment, double position, TrackOrder directPred = null)
      : this(environment, null, position, TimeSpan.Zero, directPred) { }
    public TrackOrder(Simulation environment, double position, TimeSpan serviceTime, TrackOrder directPred = null)
      : this(environment, null, position, serviceTime, DateTime.MaxValue, directPred) { }
    public TrackOrder(Simulation environment, double position, TimeSpan serviceTime, DateTime due, TrackOrder directPred = null)
      : this(environment, null, position, serviceTime, due, environment.Now, directPred) { }
    public TrackOrder(Simulation environment, double position, TimeSpan serviceTime, DateTime due, DateTime release, TrackOrder directPred = null)
      : this(environment, null, position, serviceTime, due, release, directPred) { }
    public TrackOrder(Simulation environment, TrackMovable required, double position, TrackOrder directPred = null)
      : this(environment, required, position, TimeSpan.Zero, directPred) { }
    public TrackOrder(Simulation environment, TrackMovable required, double position, TimeSpan serviceTime, TrackOrder directPred = null)
      : this(environment, required, position, serviceTime, DateTime.MaxValue, directPred) { }
    public TrackOrder(Simulation environment, TrackMovable required, double position, TimeSpan serviceTime, DateTime due, TrackOrder directPred = null)
      : this(environment, required, position, serviceTime, due, environment.Now, directPred) { }
    public TrackOrder(Simulation environment, TrackMovable required, double position, TimeSpan serviceTime, DateTime due, DateTime release, TrackOrder directPred = null)
      : base(environment) {
      Position = position;
      ServiceTime = serviceTime;
      ReleaseTime = release;
      DueDate = due;
      Required = _assigned = required;
      Predecessors = directPred != null ? new HashSet<TrackOrder>() { directPred } : new HashSet<TrackOrder>();
      DirectPredecessor = directPred;
      Started = new Event(Environment);
      PositionReached = new Event(environment);
      PositionReached.AddCallback(AtPosition); // this event, upon being processed / finished, calls AtPosition
    }

    public void Start() {
      if (Assigned == null) throw new InvalidOperationException("Order cannot be started without movable assigned.");
      Assigned.StartMovingDate = Environment.Now;
      Assigned.TargetPosition = Position;
      Assigned.State = TrackMovableState.Moving;
      Started.Succeed(); // Fire the started event now so that processes may react upon
      // Also after traveling, add a callback to fire the position reached event (calls AtPosition in turn)
      Environment.Timeout(Assigned.GetRemainingTime()).AddCallback(_ => { if (IsAlive) { PositionReached.Succeed(); } });
    }

    internal void Cancel() {
      Assigned.LastPosition = Assigned.GetCurrentPosition();
      Assigned.State = TrackMovableState.Idle;
      Succeed();
    }

    private void AtPosition(Event e) {
      if (!IsAlive) return; // canceled
      // Stop the movable
      Assigned.LastPosition = Assigned.GetCurrentPosition();
      Assigned.State = TrackMovableState.Servicing;
      // Begin service at the poition and finish the whole order afterwards by completing the order event
      Environment.Timeout(ServiceTime).AddCallback(_ => {
        Assigned.State = TrackMovableState.Idle;
        Succeed();
      });
    }
  }

  // This is the track along which the movables move
  public class Track {
    protected Simulation Environment { get; private set; }

    public double TrackLength { get; private set; }

    public IReadOnlyList<TrackMovable> Movables { get; private set; }
    private LinkedList<TrackOrder> _activeOrders;
    public IEnumerable<TrackOrder> ActiveOrders { get => _activeOrders; }
    private LinkedList<TrackOrder> _pendingOrders;
    public IEnumerable<TrackOrder> PendingOrders { get => _pendingOrders; }
    protected LinkedList<TrackOrder> DodgingOrders { get; private set; }

    public Track(Simulation environment, double trackLength, IEnumerable<TrackMovable> movables) {
      Environment = environment;
      _pendingOrders = new LinkedList<TrackOrder>();
      _activeOrders = new LinkedList<TrackOrder>();
      DodgingOrders = new LinkedList<TrackOrder>();
      Movables = movables.OrderBy(x => x.LastPosition).ToList();
      TrackLength = trackLength;
    }

    public virtual void Perform(IEnumerable<TrackOrder> orders) {
      foreach (var o in orders)
        _pendingOrders.AddLast(o);
      ExecuteSchedule(null);
    }

    private void ExecuteSchedule(Event e) {
      // All completed active orders are removed and predecessor lists of pending orders are updated
      RemoveCompleted(_activeOrders);
      RemoveCompleted(DodgingOrders);
      if (Movables.All(x => x.State != TrackMovableState.Idle)) {
        var evts = new List<Event>(ActiveOrders.Concat(DodgingOrders));
        if (evts.Count > 0)
          new AnyOf(Environment, evts).AddCallback(ExecuteSchedule);
        return;
      }

      var tiedMovables = Movables.Select(x => new { Movable = x, NextOrders = new HashSet<TrackOrder>(PendingOrders.Where(y => y.DirectPredecessor?.Assigned == x)) })
                                 .ToDictionary(x => x.Movable, x => x.NextOrders);

      var minReleaseTime = DateTime.MaxValue;
      var pending = _pendingOrders.First;
      var order = pending != null ? pending.Value : null;
      while (order != null) {
        var next = pending.Next;
        if (order.ReleaseTime > Environment.Now) {
          // check the smallest time until the next pending order becomes available
          if (order.ReleaseTime < minReleaseTime)
            minReleaseTime = order.ReleaseTime;
        } else if (order.Predecessors.Count == 0) { // the order could be scheduled
          var range = CraneAssignmentHeuristic(order, Movables.Where(x => x.State == TrackMovableState.Idle && tiedMovables[x].Count == 0));
          if (range.HasValue) {
            var dodge = DodgingOrders.SingleOrDefault(x => x.Assigned == order.Assigned); // we can cancel a potential dodge and do a real order instead
            dodge?.Cancel();
            // there is space to carry out the move, respectively, some movables got to dodge
            var totalInterval = range.Value;
            next = Start(pending);
            var dirRight = order.Assigned.LastPosition < order.Position;
            var hwidth = dirRight ? order.Assigned.Width / 2 : -order.Assigned.Width / 2;
            var dodgeTarget = order.Position + hwidth;
            foreach (var m2 in Movables.Where(x => x != order.Assigned && IsInWay(x, totalInterval))
                                            .OrderBy(x => Math.Abs(x.GetCurrentPosition() - totalInterval.Source)).ToList()) {
              dodge = new TrackOrder(Environment, dodgeTarget + (dirRight ? m2.Width / 2 : -m2.Width / 2));
              dodge.Assigned = m2;
              dodge.Start();
              DodgingOrders.AddLast(dodge);
              dodgeTarget += dirRight ? m2.Width : -m2.Width;
            }
          }
        }
        pending = next;
        order = pending != null ? pending.Value : null;
      }

      var events = new List<Event>(ActiveOrders.Concat(DodgingOrders));
      if (minReleaseTime < DateTime.MaxValue)
        events.Add(Environment.Timeout(minReleaseTime - Environment.Now));
      if (events.Count > 0)
        new AnyOf(Environment, events).AddCallback(ExecuteSchedule);
    }

    private void RemoveCompleted(LinkedList<TrackOrder> orders) {
      var o = orders.First;
      while (o != null) {
        if (!o.Value.IsProcessed) { // the order has not yet finished
          o = o.Next;
          continue;
        }
        // from all pending orders we remove the completed order from the list of predecessors
        var pending = _pendingOrders.First;
        while (pending != null) {
          pending.Value.Predecessors.Remove(o.Value);
          // for tasks with a direct predecessor, the next pending task must have the same movable
          if (pending.Value.DirectPredecessor == o.Value)
            pending.Value.Assigned = o.Value.Assigned;
          pending = pending.Next;
        }
        // then remove the completed order from the list of orders
        var next = o.Next;
        orders.Remove(o);
        o = next;
      }
    }

    private (double Source, double Target)? CraneAssignmentHeuristic(TrackOrder order, IEnumerable<TrackMovable> movables) {
      if (order.Assigned == null) {
        // The movable with that requires least space will complete the order
        // This can only be the movable to the immediate left or right of the order
        var leftOfOrder = movables.Where(x => x.GetCurrentPosition() <= order.Position).OrderBy(x => x.GetCurrentPosition()).LastOrDefault();
        if (leftOfOrder?.State != TrackMovableState.Idle) leftOfOrder = null;
        var rightOfOrder = movables.Where(x => x.GetCurrentPosition() > order.Position).OrderBy(x => x.GetCurrentPosition()).FirstOrDefault();
        if (rightOfOrder?.State != TrackMovableState.Idle) rightOfOrder = null;

        var fromLeftInterval = ExpandInterval(leftOfOrder?.GetCurrentPosition() ?? 0, order.Position + (leftOfOrder?.Width ?? 0) / 2, leftOfOrder);
        var fromRightInterval = ExpandInterval(rightOfOrder?.GetCurrentPosition() ?? TrackLength, order.Position - (rightOfOrder?.Width ?? 0) / 2, rightOfOrder);
        
        var leftIntervalSize = fromLeftInterval != null ? Math.Abs(fromLeftInterval.Value.Source - fromLeftInterval.Value.Target) : double.NaN;
        var rightIntervalSize = fromRightInterval != null ? Math.Abs(fromRightInterval.Value.Source - fromRightInterval.Value.Target) : double.NaN;

        if (!double.IsNaN(leftIntervalSize) && (double.IsNaN(rightIntervalSize) || leftIntervalSize < rightIntervalSize)) {
          order.Assigned = leftOfOrder;
          return fromLeftInterval;
        } else if (!double.IsNaN(rightIntervalSize)) {
          order.Assigned = rightOfOrder;
          return fromRightInterval;
        }
      } else {
        if (ActiveOrders.Any(x => x.Assigned == order.Assigned)) return null;
        return ExpandInterval(order.Assigned.GetCurrentPosition(), order.Position, order.Assigned);
      }
      return null;
    }

    private LinkedListNode<TrackOrder> Start(LinkedListNode<TrackOrder> pending) {
      pending.Value.Start();
      _activeOrders.AddLast(pending.Value);
      var next = pending.Next;
      _pendingOrders.Remove(pending);
      return next;
    }

    protected (double Source, double Target)? ExpandInterval(double source, double target, TrackMovable movable) {
      if (movable == null) return null;
      var toUpper = source < target;
      var movableInDir = Movables.Where(x => x != movable && (toUpper ? x.GetCurrentPosition() > source : x.GetCurrentPosition() < source));
      foreach (var m in movableInDir.OrderBy(x => toUpper ? x.GetCurrentPosition() : -x.GetCurrentPosition())) {
        var pos = m.GetCurrentPosition();
        if (toUpper && pos <= target || !toUpper && pos >= target) {
          if (m.State != TrackMovableState.Idle) return null; // non-idle movable blocking the range
          target += toUpper ? m.Width : -m.Width;
        } else break;
      }
      // Check the active orders if there are conflicting orders
      if (OverlapsWithActiveOrDodge(source, target, movable)) return null;
      return (source, target);
    }

    protected bool OverlapsWithActiveOrDodge(double Source, double Target, TrackMovable movable) {
      foreach (var x in ActiveOrders.Concat(DodgingOrders)) {
        //if (x.Assigned == movable) return true;
        if (x.Assigned.State == TrackMovableState.Moving) {
          var xSrc = x.Assigned.GetCurrentPosition();
          var xTgt = x.Position;
          var xRight = xSrc < xTgt;
          var hwidth = xRight ? x.Assigned.Width / 2 : -x.Assigned.Width / 2;
          xSrc -= hwidth;
          xTgt += hwidth;
          var dirRight = Source < Target;

          if (xRight ^ dirRight) { // opposite directions
            if (!xRight) (xSrc, xTgt) = (xTgt, xSrc);
            if (!dirRight) (Source, Target) = (Target, Source);

            if (xSrc < Source && xTgt > Source // overlap left
              || xSrc > Source && xTgt < Target // contained
              || xSrc < Source && xTgt > Target // contains
              || xSrc < Target && xTgt > Target) // overlap right
              return true;
          } else {
            if (xRight && xSrc < Source && Target < xTgt // going right, x to the left, but stops early
              || xRight && Source < xSrc && xTgt < Target // going right, x to the right, but goes beyond
              || !xRight && xSrc < Source && Target < xTgt // going left, x to the left, but goes beyond
              || !xRight && xSrc > Source && xTgt < Target) // going left, x to the right, but stops early
              return true;
          }
        } else if (x.Assigned.State == TrackMovableState.Servicing) {
          if (IsInWay(x.Assigned, (Source, Target)))
            return true;
        }
      }
      return false;
    }

    protected bool IsInWay(TrackMovable movable, (double Source, double Target) range) {
      var lower = Math.Min(range.Source, range.Target);
      var higher = Math.Max(range.Source, range.Target);
      return lower < movable.GetCurrentPosition() + movable.Width / 2 && movable.GetCurrentPosition() - movable.Width / 2 < higher;
    }
  }

  public enum TrackMovableState { Idle, Moving, Servicing }

  public class TrackMovable : ActiveObject<Simulation> {
    public string Name { get; private set; }
    public double LastPosition { get; internal set; }
    public DateTime StartMovingDate { get; internal set; }
    public TrackMovableState State { get; internal set; }
    public double TargetPosition { get; internal set; }

    /// <summary>
    /// The speed of the movable in units per second.
    /// </summary>
    /// <value>The speed of the movable.</value>
    public double Speed { get; private set; }
    public double Width { get; private set; }

    public double GetCurrentPosition() {
      if (State != TrackMovableState.Moving) return LastPosition;
      var time = Environment.Now - StartMovingDate;
      var dist = Speed * time.TotalSeconds;
      if (TargetPosition < LastPosition) return Math.Max(LastPosition - dist, TargetPosition);
      else return Math.Min(LastPosition + dist, TargetPosition);
    }
    public TimeSpan GetRemainingTime() {
      var dist = Math.Abs(GetCurrentPosition() - TargetPosition);
      return TimeSpan.FromSeconds(dist / Speed);
    }

    public TrackMovable(Simulation environment, string name, double initialPosition, double width, double speed)
      : base(environment) {
      Name = name;
      LastPosition = initialPosition;
      StartMovingDate = environment.Now;
      Width = width;
      Speed = speed;
      State = TrackMovableState.Idle;
    }

    public override string ToString() {
      return Name;
    }
  }
}
