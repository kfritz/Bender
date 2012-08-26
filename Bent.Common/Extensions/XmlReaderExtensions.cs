using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Bent.Common.Extensions
{
    public static class XmlReaderExtensions
    {
        public static XName CurrentName(this XmlReader self)
        {
            return (XNamespace)self.NamespaceURI + self.LocalName;
        }
    }
}
