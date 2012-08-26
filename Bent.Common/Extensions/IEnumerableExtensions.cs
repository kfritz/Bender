using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bent.Common.Extensions
{
    public static class IEnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> self)
        {
            return self.IsNull() || !self.Any();
        }

        public static bool IsNotNullOrEmpty<T>(this IEnumerable<T> self)
        {
            return !self.IsNullOrEmpty();
        }

        public static string ToBase64(this IEnumerable<byte> self)
        {
            return Convert.ToBase64String(self.ToArray());
        }
    }
}
