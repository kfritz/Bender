using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bend
{
    // http://xmpp.org/rfcs/rfc6121.html#presence-syntax-type
    public class PresenceType : StanzaType
    {
        public static readonly PresenceType Error = new PresenceType("error");
        public static readonly PresenceType Probe = new PresenceType("probe");
        public static readonly PresenceType Subscribe = new PresenceType("subscribe");
        public static readonly PresenceType Subscribed = new PresenceType("subscribed");
        public static readonly PresenceType Unavailable = new PresenceType("unavailable");
        public static readonly PresenceType Unsubscribe = new PresenceType("unsubscribe");
        public static readonly PresenceType Unsubscribed = new PresenceType("unsubscribed");

        private PresenceType(string value)
            : base(value) { }
    }
}
