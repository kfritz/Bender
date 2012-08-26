using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bend
{
    public static class Namespaces
    {
        public static readonly XNamespace Bind = "urn:ietf:params:xml:ns:xmpp-bind";
        public static readonly XNamespace Client = "jabber:client";
        public static readonly XNamespace Delay = "urn:xmpp:delay";
        public static readonly XNamespace Muc = "http://jabber.org/protocol/muc";
        public static readonly XNamespace Sasl = "urn:ietf:params:xml:ns:xmpp-sasl";
        public static readonly XNamespace Streams = "http://etherx.jabber.org/streams";
        public static readonly XNamespace Tls = "urn:ietf:params:xml:ns:xmpp-tls";
    }

    public static class BindNamespace
    {
        public static readonly XName Bind = Namespaces.Bind + "bind";
        public static readonly XName Jid = Namespaces.Bind + "jid";
    }

    public static class ClientNamespace
    {
        public static readonly XName Body = Namespaces.Client + "body";
        public static readonly XName Iq = Namespaces.Client + "iq";
        public static readonly XName Message = Namespaces.Client + "message";
        public static readonly XName Presence = Namespaces.Client + "presence";
    }

    public static class DelayNamespace
    {
        public static readonly XName Delay = Namespaces.Delay + "delay";
    }

    public static class MucNamespace
    {
        public static readonly XName X = Namespaces.Muc + "x";
    }

    public static class SaslNamespace
    {
        public static readonly XName Auth = Namespaces.Sasl + "auth";
        public static readonly XName Mechanisms = Namespaces.Sasl + "mechanisms";
        public static readonly XName Success = Namespaces.Sasl + "success";
    }

    public static class StreamsNamespace
    {
        public static readonly XName Features = Namespaces.Streams + "features";
        public static readonly XName Stream = Namespaces.Streams + "stream";
    }

    public static class TlsNamespace
    {
        public static readonly XName Proceed = Namespaces.Tls + "proceed";
        public static readonly XName StartTls = Namespaces.Tls + "starttls";
    }

    public static class XmlNamespace
    {
        public static readonly XName Lang = XNamespace.Xml + "lang";
    }
}
