using System;

namespace AIGames.Pathfinding
{
    public interface IHeapItem<T> : IComparable<T>
    {
        int HeapIndex { get; set; }
    }

    /// <summary>
    /// Binary min-heap stored in an array. Add/RemoveFirst are O(log n), Contains is O(1)
    /// because every item remembers its own index.
    /// </summary>
    public class Heap<T> where T : IHeapItem<T>
    {
        readonly T[] items;
        int count;

        public Heap(int maxHeapSize)
        {
            items = new T[maxHeapSize];
        }

        public int Count => count;

        public void Add(T item)
        {
            item.HeapIndex = count;
            items[count] = item;
            SortUp(item);
            count++;
        }

        public T RemoveFirst()
        {
            T first = items[0];
            count--;
            items[0] = items[count];
            items[0].HeapIndex = 0;
            SortDown(items[0]);
            return first;
        }

        /// <summary>Call after an item's priority got better (lower cost).</summary>
        public void UpdateItem(T item)
        {
            SortUp(item);
        }

        public bool Contains(T item)
        {
            return item.HeapIndex < count && Equals(items[item.HeapIndex], item);
        }

        void SortDown(T item)
        {
            while (true)
            {
                int left = item.HeapIndex * 2 + 1;
                int right = item.HeapIndex * 2 + 2;
                if (left >= count)
                    return;

                int swapIndex = left;
                if (right < count && items[left].CompareTo(items[right]) < 0)
                    swapIndex = right;

                if (item.CompareTo(items[swapIndex]) >= 0)
                    return;
                Swap(item, items[swapIndex]);
            }
        }

        void SortUp(T item)
        {
            while (item.HeapIndex > 0)
            {
                T parent = items[(item.HeapIndex - 1) / 2];
                if (item.CompareTo(parent) <= 0)
                    return;
                Swap(item, parent);
            }
        }

        void Swap(T a, T b)
        {
            items[a.HeapIndex] = b;
            items[b.HeapIndex] = a;
            (a.HeapIndex, b.HeapIndex) = (b.HeapIndex, a.HeapIndex);
        }
    }
}
