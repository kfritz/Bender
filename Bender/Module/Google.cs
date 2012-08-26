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
    public class Google : IModule
    {
        private static Regex regexGoogle = new Regex(@"^\s*google\s+(.+?)\s*$", RegexOptions.IgnoreCase);
        private static Regex regexLucky = new Regex(@"^\s*i'?m\s+feeling\s+lucky\s+(.+?)\s*$", RegexOptions.IgnoreCase);

        private IBackend backend;

        public void OnStart(IConfiguration config, IBackend backend)
        {
            this.backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            TestGoogle(message);
            TestLucky(message);
        }

        private void TestGoogle(IMessage message)
        {
            if (message.IsRelevant)
            {
                var match = regexGoogle.Match(message.Body);

                if (match.Success)
                {
                    this.backend.SendMessageAsync(message.ReplyTo, "http://lmgtfy.com/?q=" + HttpUtility.UrlEncode(match.Groups[1].Value));
                }
            }
        }

        private void TestLucky(IMessage message)
        {
            if (message.IsRelevant)
            {
                var match = regexLucky.Match(message.Body);

                if (match.Success)
                {
                    this.backend.SendMessageAsync(message.ReplyTo, "http://lmgtfy.com/?l=1&q=" + HttpUtility.UrlEncode(match.Groups[1].Value));
                }
            }
        }
    }
}
