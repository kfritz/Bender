using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bender.Backend;
using Bender.Backend.Xmpp;
using Bender.Backend.Xmpp.Bend;
using Bender.Configuration;

namespace Bender
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // TODO: We never dispose of the file stream
            var config = new Configuration.Configuration(new FileStream(@"C:\Bender\bender.config", FileMode.Open));

            var bot = new Bot(config, new BendBackend(config));

            bot.RunAsync().Wait();
        }
    }
}
