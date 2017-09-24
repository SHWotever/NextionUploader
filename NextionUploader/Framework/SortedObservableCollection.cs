using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NextionUploader.Framework
{
    /// <summary>
    /// Extends ObservableCollection{T} with an AddSorted method to insert items in a sorted collection.
    /// </summary>
    public class SortedObservableCollection<T, TKey> : ObservableCollection<T>
    {
        /// <summary>
        /// Creates a new SortedObservableCollection instance.
        /// </summary>
        /// <param name="keySelector">The function to select the sorting key.</param>
        public SortedObservableCollection(Func<T, TKey> keySelector)
        {
            this.keySelector = keySelector;
            this.comparer = Comparer<TKey>.Default;
        }

        Func<T, TKey> keySelector;
        IComparer<TKey> comparer;

        /// <summary>
        /// Adds an item to a sorted collection.
        /// </summary>
        public void AddSorted(T item)
        {
            int i = 0;
            int j = Count - 1;

            while (i <= j)
            {
                int n = (i + j) / 2;
                int c = comparer.Compare(keySelector(item), keySelector(this[n]));

                if (c == 0) { i = n; break; }
                if (c > 0) i = n + 1;
                else j = n - 1;
            }

            Insert(i, item);
        }
    }
}
