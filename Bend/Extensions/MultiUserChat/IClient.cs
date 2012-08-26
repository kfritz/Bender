using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bend;

namespace Bend.MultiUserChat
{
    public interface IClient
    {
        IRoom JoinRoom(Jid room, string nickname);
    }
}
