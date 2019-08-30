using System;
using System.Collections;
using System.Collections.Generic;

namespace SimSharp {
  /// <summary>
  /// An implementation of a min-Priority Queue using a heap.  Has O(1) .Contains()!
  /// See https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/wiki/Getting-Started for more information
  /// </summary>
  /// <remarks>
  /// There are modifications so that the type is not generic anymore and can only hold values of type EventQueueNode
  /// </remarks>
  public sealed class EventQueue : IEnumerable<EventQueueNode> {
    private int _numNodes;
    private readonly EventQueueNode[] _nodes;
    private long _numNodesEverEnqueued;

    /// <summary>
    /// Instantiate a new Priority Queue
    /// </summary>
    /// <param name="maxNodes">EventQueueNodehe max nodes ever allowed to be enqueued (going over this will cause an exception)</param>
    public EventQueue(int maxNodes) {
      _numNodes = 0;
      _nodes = new EventQueueNode[maxNodes + 1];
      _numNodesEverEnqueued = 0;
    }

    /// <summary>
    /// Returns the number of nodes in the queue.  O(1)
    /// </summary>
    public int Count {
      get {
        return _numNodes;
      }
    }

    /// <summary>
    /// Returns the maximum number of items that can be enqueued at once in this queue.  Once you hit this number (ie. once Count == MaxSize),
    /// attempting to enqueue another item will throw an exception.  O(1)
    /// </summary>
    public int MaxSize {
      get {
        return _nodes.Length - 1;
      }
    }

    /// <summary>
    /// Removes every node from the queue.  O(n) (So, don't do this often!)
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void Clear() {
      Array.Clear(_nodes, 1, _numNodes);
      _numNodes = 0;
    }

    /// <summary>
    /// Returns (in O(1)!) whether the given node is in the queue.  O(1)
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public bool Contains(EventQueueNode node) {
      return (_nodes[node.QueueIndex] == node);
    }

    /// <summary>
    /// Enqueue a node - .Priority must be set beforehand!  O(log n)
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public EventQueueNode Enqueue(DateTime primaryPriority, Event @event, int secondaryPriority = 0) {
      var node = new EventQueueNode {
        PrimaryPriority = primaryPriority,
        SecondaryPriority = secondaryPriority,
        Event = @event,
        QueueIndex = ++_numNodes,
        InsertionIndex = _numNodesEverEnqueued++
      };
      _nodes[_numNodes] = node;
      CascadeUp(_nodes[_numNodes]);
      return node;
    }
    
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void Swap(EventQueueNode node1, EventQueueNode node2) {
      //Swap the nodes
      _nodes[node1.QueueIndex] = node2;
      _nodes[node2.QueueIndex] = node1;

      //Swap their indicies
      int temp = node1.QueueIndex;
      node1.QueueIndex = node2.QueueIndex;
      node2.QueueIndex = temp;
    }

    //Performance appears to be slightly better when this is NOT inlined o_O
    private void CascadeUp(EventQueueNode node) {
      //aka Heapify-up
      int parent = node.QueueIndex / 2;
      while (parent >= 1) {
        EventQueueNode parentNode = _nodes[parent];
        if (HasHigherPriority(parentNode, node))
          break;

        //Node has lower priority value, so move it up the heap
        Swap(node, parentNode); //For some reason, this is faster with Swap() rather than (less..?) individual operations, like in CascadeDown()

        parent = node.QueueIndex / 2;
      }
    }


    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void CascadeDown(EventQueueNode node) {
      //aka Heapify-down
      EventQueueNode newParent;
      int finalQueueIndex = node.QueueIndex;
      while (true) {
        newParent = node;
        int childLeftIndex = 2 * finalQueueIndex;

        //Check if the left-child is higher-priority than the current node
        if (childLeftIndex > _numNodes) {
          //This could be placed outside the loop, but then we'd have to check newParent != node twice
          node.QueueIndex = finalQueueIndex;
          _nodes[finalQueueIndex] = node;
          break;
        }

        EventQueueNode childLeft = _nodes[childLeftIndex];
        if (HasHigherPriority(childLeft, newParent)) {
          newParent = childLeft;
        }

        //Check if the right-child is higher-priority than either the current node or the left child
        int childRightIndex = childLeftIndex + 1;
        if (childRightIndex <= _numNodes) {
          EventQueueNode childRight = _nodes[childRightIndex];
          if (HasHigherPriority(childRight, newParent)) {
            newParent = childRight;
          }
        }

        //If either of the children has higher (smaller) priority, swap and continue cascading
        if (newParent != node) {
          //Move new parent to its new index.  node will be moved once, at the end
          //Doing it this way is one less assignment operation than calling Swap()
          _nodes[finalQueueIndex] = newParent;

          int temp = newParent.QueueIndex;
          newParent.QueueIndex = finalQueueIndex;
          finalQueueIndex = temp;
        } else {
          //See note above
          node.QueueIndex = finalQueueIndex;
          _nodes[finalQueueIndex] = node;
          break;
        }
      }
    }

    /// <summary>
    /// Returns true if 'higher' has higher priority than 'lower', false otherwise.
    /// Note that calling HasHigherPriority(node, node) (ie. both arguments the same node) will return false
    /// </summary>

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private bool HasHigherPriority(EventQueueNode higher, EventQueueNode lower) {
      return (higher.PrimaryPriority < lower.PrimaryPriority ||
          (higher.PrimaryPriority == lower.PrimaryPriority
            && (higher.SecondaryPriority < lower.SecondaryPriority ||
            (higher.SecondaryPriority == lower.SecondaryPriority
              && higher.InsertionIndex < lower.InsertionIndex))));
    }

    /// <summary>
    /// Removes the head of the queue (node with highest priority; ties are broken by order of insertion), and returns it.  O(log n)
    /// </summary>
    public EventQueueNode Dequeue() {
      EventQueueNode returnMe = _nodes[1];
      Remove(returnMe);
      return returnMe;
    }

    /// <summary>
    /// Returns the head of the queue, without removing it (use Dequeue() for that).  O(1)
    /// </summary>
    public EventQueueNode First {
      get {
        return _nodes[1];
      }
    }

    /// <summary>
    /// This method must be called on a node every time its priority changes while it is in the queue.  
    /// <b>Forgetting to call this method will result in a corrupted queue!</b>
    /// O(log n)
    /// </summary>

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void UpdatePriority(EventQueueNode node, DateTime primaryPriority, int secondaryPriority) {
      node.PrimaryPriority = primaryPriority;
      node.SecondaryPriority = secondaryPriority;
      OnNodeUpdated(node);
    }

    internal void OnNodeUpdated(EventQueueNode node) {
      //Bubble the updated node up or down as appropriate
      int parentIndex = node.QueueIndex / 2;
      EventQueueNode parentNode = _nodes[parentIndex];

      if (parentIndex > 0 && HasHigherPriority(node, parentNode)) {
        CascadeUp(node);
      } else {
        //Note that CascadeDown will be called if parentNode == node (that is, node is the root)
        CascadeDown(node);
      }
    }

    /// <summary>
    /// Removes a node from the queue.  Note that the node does not need to be the head of the queue.  O(log n)
    /// </summary>
    public void Remove(EventQueueNode node) {
      if (!Contains(node)) {
        return;
      }
      if (_numNodes <= 1) {
        _nodes[1] = null;
        _numNodes = 0;
        return;
      }

      //Make sure the node is the last node in the queue
      bool wasSwapped = false;
      EventQueueNode formerLastNode = _nodes[_numNodes];
      if (node.QueueIndex != _numNodes) {
        //Swap the node with the last node
        Swap(node, formerLastNode);
        wasSwapped = true;
      }

      _numNodes--;
      _nodes[node.QueueIndex] = null;

      if (wasSwapped) {
        //Now bubble formerLastNode (which is no longer the last node) up or down as appropriate
        OnNodeUpdated(formerLastNode);
      }
    }

    public IEnumerator<EventQueueNode> GetEnumerator() {
      for (int i = 1; i <= _numNodes; i++)
        yield return _nodes[i];
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    /// <summary>
    /// <b>Should not be called in production code.</b>
    /// Checks to make sure the queue is still in a valid state.  Used for testing/debugging the queue.
    /// </summary>
    public bool IsValidQueue() {
      for (int i = 1; i < _nodes.Length; i++) {
        if (_nodes[i] != null) {
          int childLeftIndex = 2 * i;
          if (childLeftIndex < _nodes.Length && _nodes[childLeftIndex] != null && HasHigherPriority(_nodes[childLeftIndex], _nodes[i]))
            return false;

          int childRightIndex = childLeftIndex + 1;
          if (childRightIndex < _nodes.Length && _nodes[childRightIndex] != null && HasHigherPriority(_nodes[childRightIndex], _nodes[i]))
            return false;
        }
      }
      return true;
    }
  }
}
