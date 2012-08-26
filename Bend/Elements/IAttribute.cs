using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bend
{
    public interface IAttribute
    {
        XAttribute Attribute { get; }
    }
}
