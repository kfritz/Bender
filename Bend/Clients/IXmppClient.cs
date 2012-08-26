using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bent.Common;
using Bend;

namespace Bend
{
    public interface IXmppClient : IObservable<XElement>, IDisposable
    {
        Jid Jid { get; }

        void Connect();
        void Disconnect();

        void Send(XElement stanza);

        void SendMessage(Jid to, MessageType type,
            Automatic<CultureInfo> lang, IEnumerable<Body> bodies);

        void SendPresence(Jid to, PresenceType type,
            Automatic<CultureInfo> lang, IEnumerable<XElement> extendedContent);
    }
}
