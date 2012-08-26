using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bender.Configuration;
using Bender.Module;

namespace Bender.Module
{
    [Export(typeof(IModule))]
    public class Reddit : IModule
    {
        private static Regex regex = new Regex(@"^\s*reddit(\s+(.+))?\s*$", RegexOptions.IgnoreCase);
        private static Regex ultLinkRegex = new Regex(@"<br/>\s<a href=""(.+)"">\[link\]", RegexOptions.IgnoreCase);

        private Dictionary<string, HashSet<string>> seenLinks = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); // TODO: persist

        private IBackend backend;

        public void OnStart(IConfiguration config, IBackend backend)
        {
            this.backend = backend;
        }

        public async void OnMessage(IMessage message)
        {
            try 
            {
                if (message.IsRelevant)
                {
                    Match match = regex.Match(message.Body);
                    if (match.Success)
                    {
                        var subreddit = match.Groups[2].Value;
                        string url = (subreddit == String.Empty) ? "http://www.reddit.com/.rss" : String.Format("http://www.reddit.com/r/{0}.rss", subreddit);

                        var response = await new HttpClient().GetAsync(url);
                        if (!String.IsNullOrEmpty(subreddit) && (
                                response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                                response.RequestMessage.RequestUri.ToString().ToLowerInvariant() != url.ToString().ToLowerInvariant()))
                        {
                            await this.backend.SendMessageAsync(message.ReplyTo, "Sorry, couldn't find your subreddit. :-/");
                            return;
                        }
                        response.EnsureSuccessStatusCode();

                        var body = await response.Content.ReadAsStringAsync();
                        var xml = XDocument.Parse(body);

                        if (!seenLinks.ContainsKey(subreddit))
                        {
                            seenLinks[subreddit] = new HashSet<string>();
                        }

                        var messages = new List<string>();

                        foreach (var item in xml.Descendants("item"))
                        {
                            var titleEl = item.Elements("title").FirstOrDefault();
                            var linkEl = item.Elements("link").FirstOrDefault();
                            var descEl = item.Elements("description").FirstOrDefault();

                            if (titleEl != null && linkEl != null && descEl != null)
                            {
                                var title = titleEl.Value;
                                var link = linkEl.Value;
                                var ultLink = GetUltimateLink(descEl.Value);

                                if (!this.seenLinks[subreddit].Contains(link))
                                {
                                    this.seenLinks[subreddit].Add(link);

                                    messages.Add(GetMessage(title, link, ultLink));
                                }
                            }
                        }

                        if (messages.Any())
                        {
                            if (messages.Count > 3)
                            {
                                await this.backend.SendMessageAsync(message.ReplyTo, String.Format("There were {0} new stories, just going to give you the top 3.", messages.Count));
                            }

                            foreach (var m in messages.Take(3))
                            {
                                await this.backend.SendMessageAsync(message.ReplyTo, m);
                            }
                        }
                        else
                        {
                            await this.backend.SendMessageAsync(message.ReplyTo, "Sorry, nothing new. :-/");
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                Console.Error.WriteLine(ex);  // TODO: better exception handling
            }
        }

        private string GetUltimateLink(string descriptionValue)
        {
            var match = ultLinkRegex.Match(descriptionValue);
            if (match.Success) return match.Groups[1].Value;
            return null;
        }

        private string GetMessage(string title, string link, string ultLink)
        {
            return String.Format("{0}\n{1}", title,
                (String.IsNullOrEmpty(ultLink) ? link : ultLink));
        }
    }
}
