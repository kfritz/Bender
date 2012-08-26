using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Bender.Common;
using Bender.Configuration;
using Newtonsoft.Json.Linq;

namespace Bender.Module
{
    /*
     * ENHANCE: Utilize OpenGraph/OEmbed and then fallback to DiffBot
     */
    [Export(typeof(IModule))]
    public class WebPreview : IModule
    {
        // TODO: this is a pretty restrictive regex
        private static Regex regex = new Regex(@"^\s*(https?://\S+)\s*", RegexOptions.IgnoreCase);
        
        private IBackend backend;
        private string apiEndPoint;

        public void OnStart(IConfiguration config, IBackend backend)
        {
            this.backend = backend;

            var token = config[Constants.ConfigKey.DiffBotApiKey];

            this.apiEndPoint = "http://www.diffbot.com/api/article?summary&token=" + HttpUtility.UrlEncode(token);
        }

        public async void OnMessage(IMessage message)
        {
            try
            {
                if (!message.IsFromMyself && !message.IsHistorical)
                {
                    var match = regex.Match(message.FullBody);
                    if (regex.IsMatch(message.FullBody))
                    {
                        var uri = new Uri(message.FullBody);
                        if (uri.IsWellFormedOriginalString())
                        {
                            var reply = this.MakeReply(await this.QueryAsync(uri));

                            if (!String.IsNullOrWhiteSpace(reply))
                            {
                                // TODO: gotta get new lines sorted out
                                await this.backend.SendMessageAsync(message.ReplyTo, reply);
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e); // TODO: handle better
            }
        }

        private async Task<DiffBotResponse> QueryAsync(Uri uri)
        {
            var queryUrl = this.apiEndPoint + "&url=" + HttpUtility.UrlEncode(uri.ToString());

            var response = await new HttpClient().GetAsync(queryUrl);
            response.EnsureSuccessStatusCode();

            dynamic json = JObject.Parse(await response.Content.ReadAsStringAsync());

            return new DiffBotResponse((string)json.title, (string)json.author,(string)json.summary);
        }

        private string MakeReply(DiffBotResponse response)
        {
            var reply = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(response.Title))
            {
                reply.AppendFormat(@"""{0}""", response.Title);
            }

            if(!String.IsNullOrWhiteSpace(response.Author))
            {
                reply.AppendFormat(" by {0}", response.Author);
            }

            if(!string.IsNullOrWhiteSpace(response.Summary))
            {
                // TODO: break on words
                reply.AppendFormat(" — {0}", response.Summary.Length <= 300 ? response.Summary : response.Summary.Substring(0, 300) + "…");
            }

            return reply.ToString();
        }

        private class DiffBotResponse
        {
            public string Title { get; private set; }
            public string Author { get; private set; }
            public string Summary { get; private set;  }

            public DiffBotResponse(string title, string author, string summary)
            {
                this.Title = title;
                this.Author = author;
                this.Summary = summary;
            }
        }
    }
}
