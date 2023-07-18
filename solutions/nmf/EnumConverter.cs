using NMF.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nmf
{
    public static class EnumConverter<T> where T : struct
    {
        [LensPut(typeof(EnumConverter<>), nameof(Parse))]
        public static string? ToString(T value)
        {
            return value.ToString();
        }

        public static T Parse(T current, string input)
        {
            return Enum.TryParse<T>(input, out var newValue) ? newValue : current;
        }
    }
}
