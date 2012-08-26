using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bend.MultiUserChat
{
    public interface IRoom
    {
        void Leave();
        void SendMessage(string message);        
    }
}
