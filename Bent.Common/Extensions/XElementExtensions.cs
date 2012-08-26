using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bent.Common.Extensions
{
    public static class XElementExtensions
    {
        public static bool Is(this XElement element, XName name)
        {
            return element.Name == name;
        }
    }
}
