﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace JsonRpcLib
{
    /// <summary>
    /// A queue that wraps a regular generic queue but when empty will block Dequeue threads until an item is available or timeout passes.
    /// This class is thread safe.
    /// </summary>
    /// <typeparam name="T">The type of the object contained in the queue.</typeparam>
    internal sealed class BlockingQueue<T>
    {
        // The underlying queue
        private readonly List<T> _queue = new List<T>();
        // The semaphore used for blocking
        private readonly Semaphore _semaphore = new Semaphore(0, Int32.MaxValue);

        /// <summary>
        /// Enqueues an item.
        /// </summary>
        /// <param name="item">An item.</param>
        public void Enqueue(in T item)
        {
            lock (_queue)
            {
                _queue.Add(item);
            }
            _semaphore.Release();
        }

        /// <summary>
        /// Enqueues an item to the front of the queue.
        /// </summary>
        /// <param name="item">An item.</param>
        public void EnqueueFront(in T item)
        {
            lock (_queue)
            {
                _queue.Insert(0, item);
            }
            _semaphore.Release();
        }

        /// <summary>
        /// Dequeues an item. Will block if the queue is empty until an item becomes available or timeout passes.
        /// </summary>
        /// <returns>An item.</returns>
        public T Dequeue(int timeout = 5000)
        {
            if (!_semaphore.WaitOne(timeout))
            {
                return default;
            }

            T firstNode;

            lock (_queue)
            {
                if (_queue.Count == 0) return default;
                firstNode = _queue[0];
                _queue.RemoveAt(0);
            }

            return firstNode;
        }

        /// <summary>
        /// Clears the queue.
        /// </summary>
        public void Clear()
        {
            lock (_queue)
            {
                _queue.Clear();
            }
        }
    }
}
