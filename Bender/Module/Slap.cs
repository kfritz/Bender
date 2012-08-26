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
    class Slap : IModule
    {
        private static Regex regex = new Regex(@"^\s*slap\s+(.+?)\s*$", RegexOptions.IgnoreCase);
        private static Regex regexJenna = new Regex("nice.+but", RegexOptions.IgnoreCase);

        private IBackend backend;

        public void OnStart(IConfiguration config, IBackend backend)
        {
            this.backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            Normal(message);
            Jenna(message);
        }

        private void Normal(IMessage message)
        {
            if (message.IsRelevant)
            {
                var match = regex.Match(message.Body);

                if (match.Success)
                {
                    var target = match.Groups[1].Value;

                    if (target.ToLowerInvariant().Contains("dwayne"))
                    {
                        this.backend.SendMessageAsync(message.ReplyTo, String.Format("/me turns around and slaps {0} with a large trout!", message.SenderName));
                    }
                    else
                    {
                        this.backend.SendMessageAsync(message.ReplyTo, String.Format("/me slaps {0} with a large trout!", target));
                    }
                }
            }
        }

        private void Jenna(IMessage message)
        {
            if(!message.IsFromMyself && !message.IsHistorical)
            {
                if(message.SenderName.ToLowerInvariant().StartsWith("jenna") || message.SenderName.ToLowerInvariant().StartsWith("jmh"))
                {
                    if(regexJenna.IsMatch(message.FullBody))
                    {
                        this.backend.SendMessageAsync(message.ReplyTo, "Jenna! (╯°□°）╯︵ ┻━┻");
                    }
                }
            }
        }
    }
}
