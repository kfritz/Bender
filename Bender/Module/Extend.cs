using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bender.Configuration;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    internal class Extend : IModule
    {
        private static IList<string> confirmations = new List<string> { "OK!", "Will do!", "Roger that!", "Sure!", "Okey dokey!" };
        private IConfiguration configuration;
        private IBackend backend;
        private Regex regexGetDll;
        private Regex regexEnableModule;
        private Regex regexDisableModule;
        private Random random;

        public void OnStart(IConfiguration config, IBackend backend)
        {
            configuration = config;
            this.backend = backend;
            random = new Random();
            regexGetDll = new Regex(@"^\s*load library\s+(.+?)\s*$", RegexOptions.IgnoreCase);
            regexEnableModule = new Regex(@"^\s*enable module\s+(.+?)\s*$", RegexOptions.IgnoreCase);
            regexDisableModule = new Regex(@"^\s*disable module\s+(.+?)\s*$", RegexOptions.IgnoreCase);
        }

        public async void OnMessage(IMessage message)
        {
            if (message.IsRelevant)
            {
                var match = regexGetDll.Match(message.Body);
                if (match.Success)
                {
                    Exception failureException = null;
                    try
                    {
                        Uri remoteUri = new Uri(match.Groups[1].Value, UriKind.Absolute);
                        await DownloadDll(remoteUri, message.SenderName);
                        await backend.SendMessageAsync(message.ReplyTo, GetRandomConfirmation());
                    }
                    catch (Exception ex)
                    {
                        failureException = ex;
                    }
                    if (failureException != null)
                    {
                        string apology = "I can't do that right now.  I'm sorry.";
                        if (!message.ReplyTo.Equals(message.SenderAddress))
                        {
                            await backend.SendMessageAsync(message.ReplyTo, apology);
                            apology = "I'm sorry I couldn't help you just now.";
                        }
                        await backend.SendMessageAsync(message.SenderAddress, apology + "  Here's the full exception message:\n\n" + failureException.Message);
                    }
                    return;
                }
                
                match = regexEnableModule.Match(message.Body);
                if (match.Success)
                {
                    configuration.EnableModule(match.Groups[1].Value, backend);
                    await backend.SendMessageAsync(message.ReplyTo, GetRandomConfirmation());
                    return;
                }
                
                match = regexDisableModule.Match(message.Body);
                if (match.Success)
                {
                    configuration.DisableModule(match.Groups[1].Value);
                    await backend.SendMessageAsync(message.ReplyTo, GetRandomConfirmation());
                    return;
                }
            }
        }

        private async Task DownloadDll(Uri remoteUri, string from)
        {
            var response = await new HttpClient().GetAsync(remoteUri);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsByteArrayAsync();
            string path = Path.Combine(configuration.ModulesDirectoryPath, from.Replace(' ', '_') + Guid.NewGuid() + ".dll");
            File.WriteAllBytes(path, content);
            //await File.WriteAllBytesAsync(path, content);
        }

        private string GetRandomConfirmation()
        {
            return confirmations[random.Next(confirmations.Count)];
        }
    }
}
