using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bent.Common.Text;

namespace Bent.Common.Extensions
{
    public static class StringExtensions
    {
        public static string FormatWith(this string self, object arg0)
        {
            return String.Format(self, arg0);
        }

        public static string FormatWith(this string self, params object[] args)
        {
            return String.Format(self, args);
        }

        public static byte[] ToUtf8Bytes(this string self)
        {
            return Encoding.Utf8NoBom.GetBytes(self);
        }
    }
}
