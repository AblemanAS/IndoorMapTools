/********************************************************************************
Copyright 2026-present Korea Advanced Institute of Science and Technology (KAIST)

Author: Kyuho Son

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
********************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IndoorMapTools.Helper
{
    public static class CollectionExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind)
        {
            int index = 0;
            foreach(T element in self)
            {
                if(Equals(element, elementToFind)) return index;
                index++;
            }
            return -1;
        }

        public static void Sort<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector)
        {
            if(!(source != null && source.Count > 0)) return;
            if(!(keySelector.Invoke(source[0]) is IComparable || (keySelector == null && source[0] is IComparable))) return;
            QuickSort(source, 0, source.Count - 1, keySelector);
        }

        private static void QuickSort<TSource, TKey>(ObservableCollection<TSource> source, int low, int high, Func<TSource, TKey> keySelector)
        {
            if(low < high)
            {
                int pivotIndex = Partition(source, low, high, keySelector);
                QuickSort(source, low, pivotIndex - 1, keySelector);
                QuickSort(source, pivotIndex + 1, high, keySelector);
            }
        }

        private static int Partition<TSource, TKey>(ObservableCollection<TSource> source, int low, int high, Func<TSource, TKey> keySelector)
        {
            var pivot = (keySelector == null) ? (IComparable)source[high] : (IComparable)keySelector.Invoke(source[high]);
            int i = low - 1;

            for(int j = low; j < high; j++)
            {
                var curVal = (keySelector == null) ? (IComparable)source[j] : (IComparable)keySelector.Invoke(source[j]);
                if(curVal.CompareTo(pivot) < 0)
                {
                    i++;
                    Swap(source, i, j);
                }
            }
            Swap(source, i + 1, high);
            return i + 1;
        }

        private static void Swap<TSource>(ObservableCollection<TSource> source, int a, int b)
        {
            if(a == b) return;
            int min = Math.Min(a, b);
            int max = Math.Max(a, b);
            source.Move(max, min);
            source.Move(min + 1, max);
        }
    }
}
