using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bend
{
    // http://xmpp.org/rfcs/rfc6121.html#message-syntax-type
    public class MessageType : StanzaType
    {
        public static readonly MessageType Chat = new MessageType("chat");
        public static readonly MessageType Error = new MessageType("error");
        public static readonly MessageType GroupChat = new MessageType("groupchat");
        public static readonly MessageType Headline = new MessageType("headline");
        public static readonly MessageType Normal = new MessageType("normal");

        private MessageType(string value)
            : base(value) { }
    }
}
