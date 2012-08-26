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
    public class Manners : IModule
    {
        private static List<string> phrases = new List<string> { "No problem, {0}.", "You're welcome, {0}.", "Happy to help, {0}." };

        private Random random = new Random();

        private IBackend backend;
        private Regex regex;

        public void OnStart(IConfiguration config, IBackend backend)
        {
            this.backend = backend;
            this.regex = new Regex(String.Format(@"(thank.*?|^\s*ty,?(\s+.*)?)\s+{0}", config.Name), RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        public void OnMessage(IMessage message)
        {
            if(!message.IsFromMyself && !message.IsHistorical)
            {
                if(regex.IsMatch(message.FullBody))
                {
                    this.backend.SendMessageAsync(message.ReplyTo, String.Format(phrases[random.Next(phrases.Count)], message.SenderName));
                }
            }
        }
    }
}
