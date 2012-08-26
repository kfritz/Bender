using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Bender.Configuration;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class Wikipedia : IModule
    {
        private static Regex regexWiki = new Regex(@"^\s*(wiki|wikipedia)\s+(.+?)\s*$", RegexOptions.IgnoreCase);
        
        private Regex regexAlias;
        private IBackend backend;

        public void OnStart(IConfiguration config, IBackend backend)
        {
            this.backend = backend;
            if (!String.IsNullOrEmpty(config[Bender.Common.Constants.ConfigKey.WikipediaAlias]))
            {
                this.regexAlias = new Regex(String.Format(@"^\s*{0},?\s+(.+?)\s*$", config[Bender.Common.Constants.ConfigKey.WikipediaAlias]), RegexOptions.IgnoreCase);
            }
        }

        public void OnMessage(IMessage message)
        {
            TestWiki(message);
        }

        private void TestWiki(IMessage message)
        {
            if (!message.IsHistorical)
            {
                if (message.IsRelevant)
                {
                    var match = regexWiki.Match(message.Body);
                    if (match.Success)
                    {
                        this.backend.SendMessageAsync(message.ReplyTo, "http://en.wikipedia.org/wiki/" + HttpUtility.UrlEncode(match.Groups[2].Value.Replace(' ', '_')));
                        return;
                    }
                }
                else if (regexAlias != null)
                {
                    var match = regexAlias.Match(message.FullBody);
                    if (match.Success)
                    {
                        this.backend.SendMessageAsync(message.ReplyTo, "http://en.wikipedia.org/wiki/" + HttpUtility.UrlEncode(match.Groups[1].Value.Replace(' ', '_')));
                        return;
                    }
                }
            }
        }
    }
}
