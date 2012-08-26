using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bender.Common
{
    public static class Constants
    {
        public static class ConfigKey
        {
            public const string Name = "name";
            public const string Modules = "modules";

            public const string ModulesDirectory = "modulesdirectory";
            
            public const string XmppJid = "xmpp.jid";
            public const string XmppPassword = "xmpp.password";
            public const string XmppRooms = "xmpp.rooms";

            public const string DiffBotApiKey = "diffbot.apikey";
            public const string TumblrApiKey = "tumblr.apikey";
            public const string WolframAlphaApiKey = "wolframalpha.apikey";
            public const string LastFmApiKey = "lastfm.apikey";

            public const string WikipediaAlias = "wikipedia.alias";
        }
    }
}
