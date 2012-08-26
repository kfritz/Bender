using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bend
{
    public abstract class StanzaType : IAttribute
    {
        private readonly XAttribute attribute;

        public XAttribute Attribute
        {
            get { return new XAttribute(this.attribute); }
        }        

        protected StanzaType(string value)
        {
            this.attribute = new XAttribute("type", value);
        }

        public override string ToString()
        {
            return this.attribute.Value;
        }

        public static implicit operator XAttribute(StanzaType type)
        {
            return type.Attribute;
        }        
    }
}
