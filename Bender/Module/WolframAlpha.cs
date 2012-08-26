using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bender.Apis.WolframAlpha;
using Bender.Common;
using Bender.Configuration;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class WolframAlpha : IModule
    {
        private static Regex regex = new Regex(@"\?\s*$", RegexOptions.IgnoreCase);

        private IConfiguration config;
        private IBackend backend;

        public void OnStart(IConfiguration config, IBackend backend)
        {
            this.config = config;
            this.backend = backend;
        }

        public async void OnMessage(IMessage message)
        {
            if (message.IsRelevant)
            {
                if (regex.IsMatch(message.Body))
                {
                    string answer = null;

                    var response = await new WolframAlphaClient(this.config[Constants.ConfigKey.WolframAlphaApiKey]).QueryAsync(message.Body, Format.PlainText);

                    var resultPods = response.Descendants("pod").Where(i => String.Equals((string)i.Attribute("id"), "result", StringComparison.OrdinalIgnoreCase));
                    if (resultPods.Any())
                    {
                        var primary = resultPods.Where(i => String.Equals((string)i.Attribute("primary"), "true", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (primary != null)
                        {
                            var plaintext = primary.Descendants("plaintext").FirstOrDefault();
                            
                            if (plaintext != null)
                                answer = plaintext.Value;
                        }
                        else
                        {
                            var plaintext = resultPods.First().Descendants("plaintext").FirstOrDefault();

                            if (plaintext != null)
                                answer = plaintext.Value;
                        }
                    }
                    else
                    {
                        var notableFacts = response.Descendants("pod").Where(i => ((string)i.Attribute("id")).StartsWith("NotableFacts", StringComparison.OrdinalIgnoreCase));

                        if (notableFacts.Any())
                        {
                            var plaintext = notableFacts.First().Descendants("plaintext").FirstOrDefault();

                            if (plaintext != null)
                                answer = plaintext.Value;
                        }
                        else
                        {
                            var basicInformation = response.Descendants("pod").Where(i => ((string)i.Attribute("id")).StartsWith("BasicInformation", StringComparison.OrdinalIgnoreCase));

                            if (basicInformation.Any())
                            {
                                var plaintext = basicInformation.First().Descendants("plaintext").FirstOrDefault();

                                if (plaintext != null)
                                    answer = plaintext.Value;
                            }
                            else
                            {
                                // hail mary
                                var all = new StringBuilder();
                                foreach (var plaintext in response.Descendants("pod").Where(i => !string.Equals((string)i.Attribute("id"), "input", StringComparison.OrdinalIgnoreCase)).SelectMany(i => i.Descendants("plaintext")))
                                {
                                    all.AppendLine(plaintext.Value);
                                }

                                if (all.Length > 0)
                                    answer = all.ToString();
                            }
                        }                        
                    }

                    if(answer != null)
                    {
                        await this.backend.SendMessageAsync(message.ReplyTo, answer);
                    }
                    else
                    {
                        await this.backend.SendMessageAsync(message.ReplyTo, @"/me ¯\_(ツ)_/¯");
                    }
                }
            }
        }
    }
}
