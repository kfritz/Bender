using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bent.Common.Text
{
    public static class Encoding
    {
        public static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
    }
}
