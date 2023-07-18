using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nmf
{
    internal static class ArrayHelper
    {
        public static T[] Add<T>(this T[]? array, T item)
        {
            if (array == null)
            {
                return new T[] { item };
            }
            var newArray = new T[array.Length + 1];
            Array.Copy(array, newArray, array.Length);
            newArray[array.Length] = item;
            return newArray;
        }
    }
}
