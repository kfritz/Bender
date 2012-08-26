using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bender.Configuration;

namespace Bender.Module
{
    // TODO: needs to support XMPP presence: i.e. when a user joins a room or their status becomes available
    [Export(typeof(IModule))]
    public class Pounce : IModule
    {
        private static Regex regex = new Regex(@"^\s*tell\s+(.+?)\s+that\s+(.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static IList<string> confirmations = new List<string> { "OK!", "Will do!", "Roger that!", "Sure!", "Okey dokey!" };

        private Random random = new Random();

        // TODO: need to store this permanently
        private ConcurrentDictionary<string, ConcurrentQueue<Tuple<string, string>>> messages = new ConcurrentDictionary<string, ConcurrentQueue<Tuple<string, string>>>(StringComparer.OrdinalIgnoreCase);        

        private IBackend backend;
        private IConfiguration config;

        public void OnStart(IConfiguration config, IBackend backend)
        {
            this.config = config;
            this.backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            CheckForPounces(message);
            CheckForNewPounce(message);
        }

        private void CheckForPounces(IMessage message)
        {
            if(!message.IsFromMyself && !message.IsHistorical)
            {
                if(messages.ContainsKey(message.SenderName) && messages[message.SenderName].Any())
                {
                    var pounces = messages[message.SenderName].ToList();
                    messages[message.SenderName] = new ConcurrentQueue<Tuple<string, string>>();

                    this.backend.SendMessageAsync(message.ReplyTo, String.Format("Welcome back {0}! {1}.", message.SenderName, pounces.Select(i => String.Format(@"{0} said, ""{1}""", i.Item1, i.Item2)).Aggregate((i,j) => String.Format("{0} and {1}", i, j))));
                }
            }
        }

        private void CheckForNewPounce(IMessage message)
        {
            if(message.IsRelevant)
            {
                var match = regex.Match(message.Body);

                if (match.Success)
                {
                    if (message.IsPrivate)
                    {
                        this.backend.SendMessageAsync(message.ReplyTo, "This isn't a group chat!");
                    }
                    else
                    {
                        var target = match.Groups[1].Value;
                        var msg = match.Groups[2].Value;

                        if (target.Equals(this.config.Name, StringComparison.OrdinalIgnoreCase) || target.Equals(message.SenderName, StringComparison.OrdinalIgnoreCase))
                        {
                            this.backend.SendMessageAsync(message.ReplyTo, "O_o?");
                        }
                        else
                        {
                            this.backend.SendMessageAsync(message.ReplyTo, GetRandomConfirmation());

                            if (!this.messages.ContainsKey(target))
                            {
                                this.messages[target] = new ConcurrentQueue<Tuple<string, string>>();
                            }

                            this.messages[target].Enqueue(Tuple.Create(message.SenderName, msg));
                        }
                    }
                }
            }
        }

        private string GetRandomConfirmation()
        {
            return confirmations[random.Next(confirmations.Count)];
        }
    }
}
