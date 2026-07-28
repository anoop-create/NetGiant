using System;
using System.Collections.Generic;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Default value is decimal is null value
        /// </summary>
        public static decimal IsNull(this decimal? value, decimal defaultValue)
        {
            decimal returnValue;

            if (value == null)
            {
                returnValue = defaultValue;
            }
            else
            {
                returnValue = value ?? 0;
            }

            return returnValue;
        }

        /// <summary>
        /// Default value is int is null value
        /// </summary>
        public static int IsNull(this int? value, int defaultValue)
        {
            var returnValue = 0;

            if (value == null)
            {
                returnValue = defaultValue;
            }
            else
            {
                returnValue = value ?? 0;
            }

            return returnValue;
        }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int size)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return YieldChunkElements(enumerator, size);
                }
            }
        }

        private static IEnumerable<T> YieldChunkElements<T>(IEnumerator<T> source, int size)
        {
            int count = 0;
            do
            {
                yield return source.Current;
            } while (++count < size && source.MoveNext());
        }
    }
}
