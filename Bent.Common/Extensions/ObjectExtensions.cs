using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bent.Common.Extensions
{
    public static class ObjectExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this T obj)
        {
            return Enumerable.Repeat(obj, 1);
        }

        public static bool Is<T, U>(this T self, U value) where T : U
        {
            return Object.Equals(self, value);
        }

        public static bool Is<T, U>(this T self, params U[] values)  where T : U
        {
            return values.Contains(self);
        }

        public static bool IsNull(this object obj)
        {
            return Object.ReferenceEquals(obj, null);
        }

        public static bool IsNotNull(this object obj)
        {
            return !obj.IsNull();
        }
    }
}
