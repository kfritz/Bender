using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bend;

namespace Bend
{
    public interface IXmppClientStream : IObservable<XElement>, IDisposable
    {
        Jid Jid { get; }

        void Connect();
        void Disconnect();

        void Send(XElement stanza);
    }
}
