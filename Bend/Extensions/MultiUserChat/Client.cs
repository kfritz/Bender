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
    internal class Client : IClient
    {
        private readonly IXmppClient xmppClient;

        public Client(IXmppClient xmppClient)
        {
            this.xmppClient = xmppClient;
        }

        public IRoom JoinRoom(Jid room, string nickname)
        {
            // TODO: only return after we've actually joined the room
            var userJid = new Jid(room.Local, room.Domain, nickname);

            this.xmppClient.SendPresence(userJid, null, null, new XElement(MucNamespace.X).AsEnumerable());

            return new Room(room, userJid, xmppClient, this);
        }
    }
}
