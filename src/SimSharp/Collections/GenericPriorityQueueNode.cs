#region License
/*
The MIT License (MIT)

Copyright (c) 2013 Daniel "BlueRaja" Pflughoeft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */
#endregion

namespace SimSharp {

  /// <summary>
  /// A node class for the generic priority queue
  /// </summary>
  public class GenericPriorityQueueNode {
    /// <summary>
    /// Represents the current position in the queue
    /// </summary>
    public int QueueIndex { get; internal set; }

    /// <summary>
    /// Represents the order the node was inserted in
    /// </summary>
    public long InsertionIndex { get; internal set; }
  }

  /// <summary>
  /// A node class for the generic priority queue with an explicit priority type
  /// </summary>
  /// <remarks>
  /// Original sources from https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
  /// </remarks>
  /// <typeparam name="TPriority"></typeparam>
  public class GenericPriorityQueueNode<TPriority> : GenericPriorityQueueNode {
    /// <summary>
    /// The Priority to insert this node at.  Must be set BEFORE adding a node to the queue (ideally just once, in the node's constructor).
    /// Should not be manually edited once the node has been enqueued - use queue.UpdatePriority() instead
    /// </summary>
    public TPriority Priority { get; protected internal set; }
  }
}
