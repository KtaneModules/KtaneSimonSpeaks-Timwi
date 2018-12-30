using System;
using System.Collections;

using Rnd = UnityEngine.Random;

namespace SimonSpeaks
{
    static class Ut
    {
        /// <summary>
        ///     Brings the elements of the given list into a random order.</summary>
        /// <typeparam name="T">
        ///     Type of the list.</typeparam>
        /// <param name="list">
        ///     List to shuffle.</param>
        /// <param name="rnd">
        ///     Random number generator, or null to use <see cref="Rnd"/>.</param>
        /// <returns>
        ///     The list operated on.</returns>
        public static T Shuffle<T>(this T list) where T : IList
        {
            if (list == null)
                throw new ArgumentNullException("list");
            for (int j = list.Count; j >= 1; j--)
            {
                int item = Rnd.Range(0, j);
                if (item < j - 1)
                {
                    var t = list[item];
                    list[item] = list[j - 1];
                    list[j - 1] = t;
                }
            }
            return list;
        }
    }
}
