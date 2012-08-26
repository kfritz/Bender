using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Bent.Common;
using Bent.Common.Exceptions;
using Bent.Common.Extensions;
using Bent.Common.IO;
using Bent.Common.Text;
using Bend;

namespace Bend
{
    // TODO: Make STARTTLS a module
    // TODO: Make SASL a module
    // TODO: Handle stream errors
    // TODO: Add async versions of public methods
    // TODO: Consider all sorts of nasty threading issues
    public sealed class XmppTcpClientStream : IXmppClientStream
    {
        /// <summary>
        /// Represents possible internal states of a <see cref="XmppTcpClientStream"/>
        /// </summary>
        /// <remarks>
        /// <para>
        /// At any given moment a <see cref="XmppTcpClientStream"/> exists in one
        /// of a finite number of states as represented by the <see cref="State"/>
        /// enumeration.
        /// </para>
        /// <para>
        /// As various events occur the <see cref="XmppTcpClientStream"/>
        /// can transition between one state or another. However, transitions are not
        /// arbitrary and can only occur if they are defined in the 
        /// <see cref="transitionPaths"/> dictionary.
        /// </para>
        /// <para>
        /// <see cref="State"/> enumeration values do not have explicitly assigned values
        /// and should not be persisted outside an executing program.
        /// </para>
        /// <para>
        /// The initial state of a <see cref="XmppTcpClientStream"/> is <see cref="State.Disconnected"/>.
        /// </para>
        /// </remarks>
        private enum State : byte
        {
            Connected,
            Disconnected,
            DisconnectLocal,
            DisconnectRemote,
            Disposed,
            StreamStart,
            StreamNegotiation,            
        }

        /*
         *  digraph TransitionPaths {
         *      StreamStart -> StreamNegotiation;
         *      StreamStart -> DisconnectLocal;
         *      StreamStart -> DisconnectRemote;
         *      
         *      StreamNegotiation -> StreamStart;
         *      StreamNegotiation -> Connected;
         *      StreamNegotiation -> DisconnectLocal;
         *      StreamNegotiation -> DisconnectRemote;
         *      
         *      Connected -> DisconnectLocal;
         *      Connected -> DisconnectRemote;
         *      
         *      DisconnectLocal -> DisconnectRemote;
         *      DisconnectLocal -> Disconnected;
         *      
         *      DisconnectRemote -> Disconnected;
         *      
         *      Disconnected -> StreamStart;
         *      Disconnected -> Disposed;
         *  }
         */

        private static readonly Dictionary<State, HashSet<State>> transitionPaths = new Dictionary<State, HashSet<State>>
            {
                {State.StreamStart, new HashSet<State> {
                    State.StreamNegotiation, State.DisconnectLocal, State.DisconnectRemote }},

                {State.StreamNegotiation, new HashSet<State> {
                    State.StreamStart, State.Connected, State.DisconnectLocal, State.DisconnectRemote }},

                {State.Connected, new HashSet<State> {
                    State.DisconnectLocal, State.DisconnectRemote }},
                
                {State.DisconnectLocal, new HashSet<State> {
                    State.DisconnectRemote, State.Disconnected}},

                {State.DisconnectRemote, new HashSet<State> {
                    State.Disconnected }},

                {State.Disconnected, new HashSet<State> {
                    State.StreamStart, State.Disposed }},

                {State.Disposed, new HashSet<State> { }},
            };

        private static TimeSpan disconnectingTimeout = TimeSpan.FromSeconds(5);
        private static TimeSpan whitespaceKeepAlivePeriod = TimeSpan.FromMinutes(5);

        private const string whiteSpaceKeepAliveToken = " ";

        private MultiObserver<XElement> multiObserver = new MultiObserver<XElement>();

        private Timer disconnectingTimer;
        private Timer whiteSpaceKeepAliveTimer;
        private State state = State.Disconnected;
        private ManualResetEvent connectedEvent = new ManualResetEvent(false);
        private ManualResetEvent disconnectedEvent = new ManualResetEvent(false);
        private Stack<Stream> streams = new Stack<Stream>(2);        

        private Jid originalJid;
        private string password; // TODO: don't store the password

        private EndPoint endPoint;

        private XmlReader xmlReader;
        private XmlWriter xmlWriter;

        private IDisposable observableStreamSubscription;

        private Jid boundJid;
        
        private Stream CurrentStream
        {
            get
            {
                return this.streams.Peek();
            }
        }

        public Jid Jid
        {
            get
            {
                return this.boundJid.IsNull() ? this.originalJid : this.boundJid;
            }
        }

        // TODO: Need the option to automatically determine the endpoint
        public XmppTcpClientStream(Jid jid, string password, EndPoint endPoint)
        {
            this.originalJid = jid;
            this.password = password;
            this.endPoint = endPoint;

            this.whiteSpaceKeepAliveTimer = new Timer(OnWhiteSpaceKeepAlive);
        }

        public void Connect()
        {
            this.AssertIn(State.Disconnected);

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(this.endPoint);

            this.PushStream(new NetworkStream(socket, FileAccess.ReadWrite, true));

            this.TransitionToStreamStart();

            this.connectedEvent.WaitOne();
        }

        public void Disconnect()
        {
            this.TransitionToLocalDisconnect();

            this.disconnectedEvent.WaitOne();
        }

        public void Send(XElement element)
        {
            if (this.state.Is(State.DisconnectLocal, State.DisconnectRemote))
            {   // We should be silent while we're in the process of disconnecting
                // TODO: Log that we're dropping stanzas
            }
            else
            {
                this.AssertIn(State.Connected);

                this.SendInternal(element);
            }
        }

        private T PushStream<T>(T stream) where T : Stream
        {
            this.DisposeObservableStreamSubscription();

            var observableStream = new ObservableStream(stream);

            this.observableStreamSubscription = observableStream.Subscribe(new ConsoleStreamObserver(Encoding.Utf8NoBom, ConsoleColor.Cyan, ConsoleColor.Yellow));
            
            this.streams.Push(observableStream);

            return stream;
        }

        private void SendInternal(XElement element)
        {
            element.WriteTo(this.xmlWriter);
            this.xmlWriter.Flush();

            this.ResetWhiteSpaceKeepAlive();
        }

        private async void StartReadLoopAsync()
        {
            // Keep a private reference to the reader when we start
            var reader = this.xmlReader;

            while (await reader.ReadAsync())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Depth)
                        {
                            case 0:
                                if(reader.IsEmptyElement)
                                {
                                    this.TransitionToRemoteDisconnect();
                                }
                                else
                                {
                                    if (reader.CurrentName().Is(StreamsNamespace.Stream))
                                    {
                                        // TODO: validate version number
                                    }
                                    else
                                    {
                                        throw new Exception("Server sent a weird start element."); // TODO: More sepcific exception
                                    }
                                }

                                break;
                            case 1:
                                using (var st = reader.ReadSubtree())
                                {
                                    this.ProcessIncomingElement(XElement.Load(st));
                                }
                                break;
                            default:
                                throw new ImpossibleException("Reached start of element at depth greater than 1.");
                        }
                        break;
                    case XmlNodeType.EndElement:
                        switch (reader.Depth)
                        {
                            case 0:
                                if (reader.CurrentName().Is(StreamsNamespace.Stream))
                                {
                                    this.TransitionToRemoteDisconnect();
                                }
                                else
                                {
                                    throw new Exception("Reached end of root level element that was not the stream element."); // TODO: More sepcific exception
                                }                                
                                break;
                            default:
                                throw new ImpossibleException("Reached end of element other than the root.");
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void ProcessIncomingElement(XElement stanza)
        {
            try
            {
                this.multiObserver.OnNext(stanza);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            if (stanza.Is(StreamsNamespace.Features))
            {
                this.OnStreamFeatures(stanza);
            }
            else if (stanza.Is(TlsNamespace.Proceed))
            {
                // TODO: We have two ObservableStreams, the second of which is useless
                var sslStream = this.PushStream(new SslStream(this.CurrentStream));

                sslStream.AuthenticateAsClient(this.originalJid.Domain);

                this.TransitionToStreamStart();
            }
            else if (stanza.Is(SaslNamespace.Success))
            {
                this.TransitionToStreamStart();
            }
            else if (stanza.Is(ClientNamespace.Iq))
            {
                if (stanza.Attribute("type").Value == "result" && stanza.Elements().Any(i => i.Is(BindNamespace.Bind)))
                {
                    var jidElement = stanza.Element(BindNamespace.Bind).Element(BindNamespace.Jid);

                    if (jidElement.IsNotNull())
                    {
                        this.boundJid = new Jid(jidElement.Value);
                    }
                    else
                    {
                        // TODO: error condition
                    }
                    
                    this.TransitionToConnected();
                }
            }
        }

        private void OnStreamFeatures(XElement streamFeatures)
        {
            foreach (var feature in streamFeatures.Elements())
	        {
                if (feature.Is(TlsNamespace.StartTls))
                {
                    this.SendInternal(new XElement(TlsNamespace.StartTls));

                    break;
                }
                else if (feature.Is(SaslNamespace.Mechanisms))
                {
                    var user = this.originalJid.Local.ToUtf8Bytes();
                    var pass = this.password.ToUtf8Bytes();
                    var nul = "\0".ToUtf8Bytes();

                    var message = Enumerable.Empty<byte>()
                        .Concat(user)
                        .Concat(nul)
                        .Concat(user)
                        .Concat(nul)
                        .Concat(pass).ToBase64();

                    this.SendInternal(new XElement(SaslNamespace.Auth,
                        new XAttribute("mechanism", "PLAIN"),
                        message));
                }
                else if (feature.Is(BindNamespace.Bind))
                {
                    this.SendInternal(new XElement(ClientNamespace.Iq,
                        new XAttribute("id", Guid.NewGuid().ToString()),
                        new XAttribute("type", "set"),
                        new XElement(BindNamespace.Bind)));
                }
	        }
        }

        private void ResetWhiteSpaceKeepAlive()
        {
            this.whiteSpaceKeepAliveTimer.Change(whitespaceKeepAlivePeriod, whitespaceKeepAlivePeriod);
        }

        private void OnWhiteSpaceKeepAlive(object state)
        {
            this.xmlWriter.WriteWhitespace(whiteSpaceKeepAliveToken);
            this.xmlWriter.Flush();
        }

        #region State Machine

        private void TransitionToStreamStart()
        {
            var prevState = this.AssertAndDoTransitionTo(State.StreamStart);

            if (prevState.Is(State.StreamNegotiation))
            {
                this.DisposeXmlWriter();
                this.DisposeXmlReader();
            }

            this.xmlReader = XmlReader.Create(this.CurrentStream, new XmlReaderSettings
                {
                    Async = true,
                    CloseInput = false,
                    ConformanceLevel = ConformanceLevel.Fragment,
                });

            this.xmlWriter = XmlWriter.Create(this.CurrentStream, new XmlWriterSettings
                {
                    Async = true,
                    CloseOutput = false,
                    ConformanceLevel = ConformanceLevel.Fragment,
                    Encoding = Encoding.Utf8NoBom,
                    WriteEndDocumentOnClose = false,
                });

            this.StartReadLoopAsync();

            this.xmlWriter.WriteRaw(new XDeclaration("1.0", "utf-8", null).ToString());
            this.xmlWriter.WriteStartElement("stream", "stream", Namespaces.Streams.ToString());
            this.xmlWriter.WriteAttributeString("from", originalJid.Bare.ToString()); // TODO: don't send the from header if the stream is not secure
            this.xmlWriter.WriteAttributeString("to", originalJid.Domain);
            this.xmlWriter.WriteAttributeString("version", "1.0");
            this.xmlWriter.WriteAttributeString("xml", "lang", null, "en");
            this.xmlWriter.WriteAttributeString("xmlns", null, Namespaces.Client.ToString());
            this.xmlWriter.WriteAttributeString("xmlns", "stream", null, Namespaces.Streams.ToString());
            this.xmlWriter.WriteString(String.Empty); // to force closure of opening element

            this.xmlWriter.Flush();

            this.TransitionToStreamNegotiation();
        }

        private void TransitionToStreamNegotiation()
        {
            this.AssertAndDoTransitionTo(State.StreamNegotiation);            
        }

        private void TransitionToConnected()
        {
            this.AssertAndDoTransitionTo(State.Connected);

            this.connectedEvent.Set();

            this.ResetWhiteSpaceKeepAlive();
        }

        private void TransitionToLocalDisconnect()
        {
            this.AssertAndDoTransitionTo(State.DisconnectLocal);

            this.xmlWriter.WriteEndElement();
            this.xmlWriter.Flush();

            this.disconnectingTimer = new Timer(s =>
                {
                    if (this.state.Is(State.DisconnectLocal))
                    {
                        this.TransitionToDisconnected();
                    }
                }, null, disconnectingTimeout, TimeSpan.Zero);
        }

        private void TransitionToRemoteDisconnect()
        {
            var prevState = this.AssertAndDoTransitionTo(State.DisconnectRemote);

            if (!prevState.Is(State.DisconnectLocal))
            {
                this.xmlWriter.WriteEndElement();
                this.xmlWriter.Flush();
            }

            this.TransitionToDisconnected();
        }

        private void TransitionToDisconnected()
        {
            /* TODO: Compliance: Not sure if SslStream will send TLS close_notify
             * 
             * http://xmpp.org/rfcs/rfc6120.html#streams-close
             */

            // TODO: need to reinitialize all the disposables if we reconnnect later

            this.DisposeDisconnectingTimer();
            this.DisposeWhiteSpaceKeepAliveTimer();

            this.AssertAndDoTransitionTo(State.Disconnected);

            this.DisposeXmlWriter();
            this.DisposeXmlReader();
            this.DisposeStreams();
            this.DisposeObservableStreamSubscription();

            // TODO: reset the various wait handles
            this.disconnectedEvent.Set();
        }

        private void TransitionToDisposed()
        {
            this.AssertAndDoTransitionTo(State.Disposed);

            this.DisposeXmlWriter();
            this.DisposeXmlReader();
            this.DisposeStreams();
            this.DisposeObservableStreamSubscription();

            try
            {
                this.multiObserver.OnCompleted();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
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
            if (this.state.Is(State.StreamStart, State.StreamNegotiation, State.Connected))
            {
                this.Disconnect();
            }

            this.TransitionToDisposed();
        }

        private void DisposeDisconnectingTimer()
        {
            if (this.disconnectingTimer.IsNotNull())
            {
                this.disconnectingTimer.Dispose();
                this.disconnectingTimer = null;
            }
        }

        private void DisposeWhiteSpaceKeepAliveTimer()
        {
            if (this.whiteSpaceKeepAliveTimer.IsNotNull())
            {
                this.whiteSpaceKeepAliveTimer.Dispose();
                this.whiteSpaceKeepAliveTimer = null;
            }
        }

        private void DisposeObservableStreamSubscription()
        {
            if (this.observableStreamSubscription.IsNotNull())
            {
                this.observableStreamSubscription.Dispose();
                this.observableStreamSubscription = null;
            }
        }

        private void DisposeStreams()
        {
            while (this.streams.Count != 0)
            {
                var stream = this.streams.Pop();
                if (stream.IsNotNull())
                {
                    stream.Dispose();
                }
            }
        }

        private void DisposeXmlReader()
        {
            if (this.xmlReader.IsNotNull())
            {
                try
                {
                    this.xmlReader.Dispose();
                }
                catch (InvalidOperationException e)
                {   /* TODO: Find a way to dispose of XmlReader more gracefully
                     *
                     * http://stackoverflow.com/questions/12015279/disposing-of-xmlreader-with-pending-async-read
                     */

                    if (!e.Message.Contains("An asynchronous operation is already in progress."))
                    {
                        throw;
                    }
                }

                this.xmlReader = null;
            }
        }

        private void DisposeXmlWriter()
        {
            if (this.xmlWriter.IsNotNull())
            {
                this.xmlWriter.Dispose();
                this.xmlWriter = null;
            }
        }

        #endregion

        #region Asserts

        private State AssertAndDoTransitionTo(State state)
        {
            if (!transitionPaths[this.state].Contains(state))
            {
                if (this.state == State.Disposed)
                {
                    throw new ObjectDisposedException(null);
                }
                else
                {
                    throw new InvalidOperationException("Attempted to transition from {0} to {1}.".FormatWith(this.state, state));
                }
            }

            var prevState = this.state;
            this.state = state;

            return prevState;
        }

        private void AssertIn(params State[] states)
        {
            if (!this.state.Is(states))
            {
                switch (this.state)
                {
                    case State.Disposed:
                        throw new ObjectDisposedException(null);
                    default:
                        throw new InvalidOperationException("Stream is in state {0}, expected it to be in one of the following states: {1}."
                            .FormatWith(this.state, states.Select(i => i.ToString()).Aggregate((i, j) => i + ", " + j)));
                }
            }
        }

        #endregion        
    }
}
