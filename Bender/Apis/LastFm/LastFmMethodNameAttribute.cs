using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bender.Apis.LastFm
{
    public class LastFmMethodNameAttribute : Attribute
    {
        public string Value { get; private set; }

        public LastFmMethodNameAttribute(string value)
        {
            this.Value = value;
        }
    }
}
