using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bent.Common;
using Bent.Common.Extensions;
using Bend;

namespace Bend
{
    public sealed class XmppClient : IXmppClient, IObserver<XElement>
    {
        private const string exceptionConnected = "Session is already connected.";
        private const string exceptionNotConnected = "Session is not connected.";

        private MultiObserver<XElement> multiObserver = new MultiObserver<XElement>();

        private bool connected;
        private bool disposed;

        private IXmppClientStream clientStream;

        public Jid Jid
        {
            get { return this.clientStream.Jid; }
        }

        public XmppClient(IXmppClientStream clientStream)
        {
            this.clientStream = clientStream;
            this.clientStream.Subscribe(this);
        }

        #region Public API

        public void Connect()
        {
            this.AssertNotDisposed();
            this.AssertNotConnected();

            this.clientStream.Connect();

            this.SendPresenceInternal(null, null, null, null, false);

            this.connected = true;
        }

        public void Disconnect()
        {
            this.AssertNotDisposed();
            this.AssertConnected();

            this.clientStream.Disconnect();

            this.connected = false;
        }

        public void Send(XElement stanza)
        {
            this.SendInternal(stanza);
        }        

        public void SendMessage(Jid to, MessageType type,
            Automatic<CultureInfo> lang, IEnumerable<Body> bodies)
        {
            this.SendMessageInternal(to, type, lang, bodies, true);
        }

        public void SendPresence(Jid to, PresenceType type,
            Automatic<CultureInfo> lang, IEnumerable<XElement> extendedContent)
        {
            this.SendPresenceInternal(to, type, lang, extendedContent, true);
        }

        #endregion

        #region Private API

        private void SendInternal(XElement stanza, bool check = true)
        {
            if (check)
            {
                this.AssertNotDisposed();
                this.AssertConnected();
            }

            this.clientStream.Send(stanza);
        }

        private void SendMessageInternal(Jid to, MessageType type,
            Automatic<CultureInfo> lang, IEnumerable<Body> bodies,
            bool check)
        {
            var stanza = new XElement(ClientNamespace.Message, new XAttribute("id", this.GenerateId()));

            stanza.Add(new XAttribute("to", to));

            if (type.IsNotNull())
            {
                stanza.Add((XAttribute)type);
            }

            if (!lang.HasValue || lang.Value.IsNotNull())
            {
                stanza.Add(new XAttribute(XmlNamespace.Lang, lang.ValueOr(CultureInfo.CurrentCulture)));
            }

            if (bodies.IsNotNullOrEmpty())
            {
                stanza.Add(bodies.Select(i => (XElement)i));
            }

            this.SendInternal(stanza, check);
        }

        private void SendPresenceInternal(Jid to, PresenceType type,
            Automatic<CultureInfo> lang, IEnumerable<XElement> extendedContent,
            bool check)
        {
            var stanza = new XElement(ClientNamespace.Presence, new XAttribute("id", this.GenerateId()));

            if (to.IsNotNull())
            {
                stanza.Add(new XAttribute("to", to));
            }

            if (type.IsNotNull())
            {
                stanza.Add((XAttribute)type);
            }

            if (!lang.HasValue || lang.Value.IsNotNull())
            {
                stanza.Add(new XAttribute(XmlNamespace.Lang, lang.ValueOr(CultureInfo.CurrentCulture)));
            }

            if (extendedContent.IsNotNullOrEmpty())
            {
                stanza.Add(extendedContent);
            }

            this.SendInternal(stanza, check);
        }

        private string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }

        #endregion

        #region Observable

        public IDisposable Subscribe(IObserver<XElement> observer)
        {
            return this.multiObserver.Add(observer);
        }

        #endregion

        #region Disposable

        public void Dispose()
        {
            this.DisposeClientStream();

            this.disposed = true;
        }

        private void DisposeClientStream()
        {
            if (this.clientStream.IsNotNull())
            {
                this.clientStream.Dispose();
                this.clientStream = null;
            }
        }

        #endregion

        #region Asserts

        private void AssertNotDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(null);
            }
        }

        private void AssertConnected()
        {
            if (!this.connected)
            {
                throw new InvalidOperationException(exceptionNotConnected);
            }
        }

        private void AssertNotConnected()
        {
            if (this.connected)
            {
                throw new InvalidOperationException(exceptionConnected);
            }
        }

        #endregion                
    
        public void OnCompleted()
        {
            try
            {
                this.multiObserver.OnCompleted();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public void OnError(Exception error)
        {
            try
            {
                this.multiObserver.OnError(error);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public void OnNext(XElement value)
        {
            try
            {
                this.multiObserver.OnNext(value);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
