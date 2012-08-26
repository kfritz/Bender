using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bent.Common;
using Bent.Common.Extensions;
using Bend;

namespace Bend.MultiUserChat
{
    internal class Room : IRoom
    {
        private readonly Jid roomJid;
        private readonly Jid userJid;
        private readonly IXmppClient xmppClient;
        private readonly IClient client;        

        public Room(Jid room, Jid userJid, IXmppClient xmppClient, IClient client)
        {
            this.roomJid = room;
            this.userJid = userJid;
            this.xmppClient = xmppClient;
            this.client = client;
        }

        public void Leave()
        {
            this.xmppClient.SendPresence(this.userJid, PresenceType.Unavailable, null, null);
        }

        public void SendMessage(string message)
        {
            this.xmppClient.SendMessage(this.roomJid,
                type: MessageType.GroupChat,
                lang: new Automatic<CultureInfo>(),
                bodies: new Body(message, new Automatic<CultureInfo>()).AsEnumerable());
        }        
    }
}
