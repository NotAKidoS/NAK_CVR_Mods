namespace NAK.BetterContentLoading;

public class ConcurrentPriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private readonly object _lock = new();
    private readonly SortedDictionary<TPriority, Queue<TElement>> _queues = new();

    public void Enqueue(TElement item, TPriority priority)
    {
        lock (_lock)
        {
            if (!_queues.TryGetValue(priority, out var queue))
            {
                queue = new Queue<TElement>();
                _queues[priority] = queue;
            }
            queue.Enqueue(item);
        }
    }

    public bool TryDequeue(out TElement item, out TPriority priority)
    {
        lock (_lock)
        {
            if (_queues.Count == 0)
            {
                item = default;
                priority = default;
                return false;
            }

            var firstQueue = _queues.First();
            priority = firstQueue.Key;
            var queue = firstQueue.Value;
            item = queue.Dequeue();

            if (queue.Count == 0)
                _queues.Remove(priority);

            return true;
        }
    }
}