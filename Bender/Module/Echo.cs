using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bender.Configuration;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    internal class Echo : IModule
    {
        private static Regex regex = new Regex(@"^\s*(?:say|echo)\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private IBackend backend;

        public void OnStart(IConfiguration config, IBackend backend)
        {
            this.backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            if (message.IsRelevant)
            {
                var match = regex.Match(message.Body);

                if (match.Success)
                {
                     this.backend.SendMessageAsync(message.ReplyTo, match.Groups[1].Value);
                }
            }
        }
    }
}
