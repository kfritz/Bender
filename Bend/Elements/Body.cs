using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bent.Common;
using Bent.Common.Extensions;

namespace Bend
{
    public sealed class Body : IElement
    {
        private readonly XElement element;

        public Body(string value, Automatic<CultureInfo> lang )
        {
            this.element = new XElement(ClientNamespace.Body);

            if (!lang.HasValue || lang.Value.IsNotNull())
            {
                this.element.Add(new XAttribute(XmlNamespace.Lang, lang.ValueOr(CultureInfo.CurrentCulture)));
            }

            if (value.IsNotNull())
            {
                this.element.Add(value);
            }
        }

        public XElement Element
        {
            get {  return new XElement(this.element); }
        }

        public static implicit operator XElement(Body body)
        {
            return body.Element;
        }
    }
}
