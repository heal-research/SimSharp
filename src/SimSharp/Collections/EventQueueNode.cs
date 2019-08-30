using System;

namespace SimSharp {
  public class EventQueueNode {
    /// <summary>
    /// The Priority to insert this node at. Must be set BEFORE adding a node to the queue.
    /// </summary>
    public DateTime PrimaryPriority { get; set; }

    /// <summary>
    /// An integer priority that will be used to distinguish nodes with the same
    /// <see cref="PrimaryPriority"/> and before the <see cref="InsertionIndex"/> is used.
    /// Must be set BEFORE adding a node to the queue.
    /// </summary>
    /// <remarks>
    /// A lower value means higher priority, thus int.MinValue has highest priority.
    /// </remarks>
    public int SecondaryPriority { get; set; }

    /// <summary>
    /// The event that is associated with this node.
    /// </summary>
    public Event Event { get; set; }

    /// <summary>
    /// <b>Used by the priority queue - do not edit this value.</b>
    /// Represents the order the node was inserted in
    /// </summary>
    /// <remarks>
    /// This is unique among all inserted nodes and thus represents the third and final priority.
    /// </remarks>
    public long InsertionIndex { get; set; }

    /// <summary>
    /// <b>Used by the priority queue - do not edit this value.</b>
    /// Represents the current position in the queue
    /// </summary>
    public int QueueIndex { get; set; }
  }
}
