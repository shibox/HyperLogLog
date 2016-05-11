using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog
{
    public interface IHyperLogLog<in T>
    {
        /// <summary>
        ///     Adds an element to the counted set.  Elements added multiple times will be counted only once.
        /// </summary>
        /// <param name="element">The element to add</param>
        void Add(T element);

        /// <summary>
        ///     Returns the estimated number of unique elements in the counted set
        /// </summary>
        /// <returns>The estimated count of unique elements</returns>
        ulong Count();

        /// <summary>
        ///     Gets the number of times elements were added (including duplicates)
        /// </summary>
        /// <returns>The number of times <see cref="Add"/> was called</returns>
        ulong CountAdditions { get; }
    }
}
