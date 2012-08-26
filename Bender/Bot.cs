using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bender.Common;
using Bender.Configuration;
using Bender.Framework;
using Bender.Module;

namespace Bender
{
    public class Bot : IObserver<MessageData>
    {
        private ManualResetEvent done = new ManualResetEvent(false);

        private IConfiguration config;
        private IBackend backend;

        private Regex regexDirected;
      
        public Bot(IConfiguration config, IBackend backend)
        {
            this.config = config;
            this.backend = backend;

            this.regexDirected = new Regex(string.Format(@"^\s*@?{0}(?:,\s*|:\s*|\s+)(.+)$", this.config.Name), RegexOptions.IgnoreCase | RegexOptions.Singleline);

            this.config.Start(this.backend);
        }

        public async Task RunAsync()
        {
            using (this.done)
            using (this.backend.Subscribe(this))
            {
                await this.backend.ConnectAsync();

                done.WaitOne();
            }
        }

        void IObserver<MessageData>.OnCompleted()
        {
            done.Set();
        }

        void IObserver<MessageData>.OnError(Exception error)
        {
            Console.Error.WriteLineAsync(error.ToString());
        }

        void IObserver<MessageData>.OnNext(MessageData value)
        {
            var matchDirected = regexDirected.Match(value.Body);

            var message = new MessageImpl(value, matchDirected.Success ? matchDirected.Groups[1].Value : null,
                isAddressedAtMe: matchDirected.Success);

            Parallel.ForEach(this.config.Modules, p =>
                {
                    try
                    {
                        p.OnMessage(message);
                    }
                    catch(Exception e)
                    {   // TODO: Bot: Handle plugin errors
                        Console.Error.WriteLineAsync(e.ToString());
                    }
                });
        }
    }
}
