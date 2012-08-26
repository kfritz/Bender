using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bend;
using Bend.MultiUserChat;
using Bender.Common;
using Bender.Configuration;
using Bent.Common;
using Bent.Common.Extensions;

namespace Bender.Backend.Xmpp.Bend
{
    internal class BendBackend : IBackend, IObserver<XElement>
    {
        private readonly MultiObserver<MessageData> multiObserver = new MultiObserver<MessageData>();

        private readonly IConfiguration configuration;
        private readonly IXmppClient client;
        private readonly IDisposable observableSubscription;
        private readonly Jid jid;

        public BendBackend(IConfiguration configuration)
        {
            this.configuration = configuration;
            
            this.jid = new Jid(this.configuration.Jid);
            this.client = new XmppClient(new XmppTcpClientStream(this.jid, this.configuration.Password, new DnsEndPoint(this.jid.Domain, 5222)));
            this.observableSubscription = this.client.Subscribe(this);
        }

        public async Task ConnectAsync()
        {
            await Task.Run(() =>
                {
                    this.client.Connect();
                    foreach (var room in this.configuration.Rooms)
                    {
                        this.client.MultiUserChat().JoinRoom(new Jid(room), this.configuration.Name);
                    }
                });
        }

        public async Task DisconnectAsync()
        {
            await Task.Run(() => this.client.Disconnect());
        }

        public async Task SendMessageAsync(IAddress address, string body)
        {
            await Task.Run(() =>
                {
                    var xAddress = address as Address;
                    if (xAddress != null)
                    {
                        this.client.SendMessage(xAddress.Jid, xAddress.MessageType, new Automatic<CultureInfo>(), new Body(body, new Automatic<CultureInfo>(null)).AsEnumerable());
                    }
                });
        }

        public void Dispose()
        {
            this.client.Dispose();
            this.observableSubscription.Dispose();
            //this.multiObserver.OnCompleted(); // TODO: need to make sure we don't send OnCompleted twice
        }

        public IDisposable Subscribe(IObserver<MessageData> observer)
        {
            return this.multiObserver.Add(observer);
        }

        public void OnCompleted()
        {
            this.multiObserver.OnCompleted();
        }

        public void OnError(Exception error)
        {
            this.multiObserver.OnError(error);
        }

        public void OnNext(XElement value)
        {
            if (value.Is(ClientNamespace.Message))
            {
                var type = value.Attribute("type");
                if (type.IsNotNull())
                {
                    var isTypeChat = type.Value.Equals(MessageType.Chat.ToString(), StringComparison.OrdinalIgnoreCase);
                    var isTypeGroupChat = type.Value.Equals(MessageType.GroupChat.ToString(), StringComparison.OrdinalIgnoreCase);

                    if (isTypeChat || isTypeGroupChat)
                    {
                        var body = value.Element(ClientNamespace.Body);
                        if (body.IsNotNull())
                        {
                            var from = value.Attribute("from");

                            if (from.IsNotNull())
                            {
                                var fromJid = new Jid(from.Value);

                                this.multiObserver.OnNext(new MessageData(
                                    replyTo: new Address(isTypeChat ? fromJid : fromJid.Bare, isTypeChat ? MessageType.Chat : MessageType.GroupChat),
                                    senderAddress: new Address(fromJid, MessageType.Chat),
                                    senderName: isTypeChat ? fromJid.Local : fromJid.Resource,
                                    body: body.Value,
                                    isFromMyself: isTypeChat ? string.Equals(fromJid.Bare, this.jid.Bare) : String.Equals(fromJid.Resource, this.configuration.Name, StringComparison.OrdinalIgnoreCase),
                                    isHistorical: value.Element(DelayNamespace.Delay) != null,
                                    isPrivate: isTypeChat
                                ));
                            }                            
                        }
                    }
                }
            }
        }

        private class Address : IAddress
        {
            public readonly Jid Jid;
            public readonly MessageType MessageType;

            public Address(Jid jid, MessageType messageType)
            {
                this.Jid = jid;
                this.MessageType = messageType;
            }
        }
    }
}
