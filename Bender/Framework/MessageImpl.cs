using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bender.Common;

namespace Bender.Framework
{
    internal class MessageImpl : IMessage
    {
        public IAddress ReplyTo { get; private set; }
        public IAddress SenderAddress { get; private set; }
        public string SenderName { get; private set; }

        public bool IsAddressedAtMe { get; private set; }
        public bool IsFromMyself { get; private set; }
        public bool IsHistorical { get; private set; }
        public bool IsPrivate { get; private set; }        

        public string Body { get; private set; }
        public string DirectedBody { get; private set; }
        public string FullBody  { get; private set; }

        public bool IsRelevant
        {
            get { return !this.IsFromMyself && !this.IsHistorical && (this.IsAddressedAtMe || this.IsPrivate); }
        }

        public MessageImpl(MessageData message, string directedBody, bool isAddressedAtMe)
        {
            this.ReplyTo = message.ReplyTo;
            this.SenderAddress = message.SenderAddress;
            this.SenderName = message.SenderName;

            this.IsAddressedAtMe = isAddressedAtMe;
            this.IsFromMyself = message.IsFromMyself;
            this.IsHistorical = message.IsHistorical;
            this.IsPrivate = message.IsPrivate;

            this.Body = isAddressedAtMe ? directedBody : message.Body;
            this.DirectedBody = directedBody;
            this.FullBody = message.Body;            
        }        
    }
}
