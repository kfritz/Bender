using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bender.Apis.LastFm;
using Bender.Configuration;

namespace Bender.Module
{
    // TODO: break out classes

    [Export(typeof(IModule))]
    public class LastFm : IModule
    {
        #region Regular Expressions

        private static Regex musicRegex = new Regex(@"^\s*music\s+(.+?)\s*\.?\s*$", RegexOptions.IgnoreCase);
        private static Regex similarTrackRegex = new Regex(@"^\s*similar\s+to\s+""(.+)""\s+by\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static Regex similarArtistRegex = new Regex(@"^\s*similar\s+to\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static Regex discoveryChainArtistRegex = new Regex(@"^\s*discovery\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static Regex discoveryChainTrackRegex = new Regex(@"^\s*discovery\s+""(.+)""\s+by\s+(.+)\s*$", RegexOptions.IgnoreCase);
        private static Regex helpRegex = new Regex(@"^\s*help\s*$", RegexOptions.IgnoreCase);

        #endregion

        #region Fields

        private IBackend backend;
        private IConfiguration config;

        #endregion

        #region IModule Members

        public void OnStart(IConfiguration config, IBackend backend)
        {
            this.config = config;
            this.backend = backend;
        }

        public void OnMessage(IMessage message)
        {
            TestMusic(message);
        }

        #endregion

        #region Regex Tests

        private async void TestMusic(IMessage message)
        {
            try
            {
                if (message.IsRelevant && !message.IsHistorical)
                {
                    var musicMatch = musicRegex.Match(message.Body);
                    var musicBody = musicMatch.Groups[1].Value;
                    if (musicMatch.Success)
                    {
                        if (await TestSimilarTracks  (message, musicBody)) return;
                        if (await TestSimilarArtists (message, musicBody)) return;
                        if (await TestTrackDiscovery (message, musicBody)) return;
                        if (await TestArtistDiscovery(message, musicBody)) return;
                        if (await TestHelp           (message, musicBody)) return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task<bool> TestSimilarTracks(IMessage message, string musicBody)
        {
            var similarTrackMatch = similarTrackRegex.Match(musicBody);
            if (similarTrackMatch.Success)
            {
                string track = similarTrackMatch.Groups[1].Value;
                string artist = similarTrackMatch.Groups[2].Value;
                XDocument xml = await new LastFmClient(this.config[Common.Constants.ConfigKey.LastFmApiKey]).GetSimilarTracksAsync(artist, track);
                await backend.SendMessageAsync(message.ReplyTo, LastFmResponse.CreateSimilarTracksResponse(xml));
                return true;
            }
            return false;
        }

        private async Task<bool> TestSimilarArtists(IMessage message, string body)
        {
            var similarArtistMatch = similarArtistRegex.Match(body);
            if (similarArtistMatch.Success)
            {
                string artist = similarArtistMatch.Groups[1].Value;
                XDocument xml = await new LastFmClient(this.config[Common.Constants.ConfigKey.LastFmApiKey]).GetSimilarArtistsAsync(artist);
                await backend.SendMessageAsync(message.ReplyTo, LastFmResponse.CreateSimilarArtistsResponse(xml));
                return true;
            }
            return false;
        }

        private async Task<bool> TestTrackDiscovery(IMessage message, string body)
        {
            var discoveryChainTrackMatch = discoveryChainTrackRegex.Match(body);
            if (discoveryChainTrackMatch.Success)
            {
                string track = discoveryChainTrackMatch.Groups[1].Value;
                string artist = discoveryChainTrackMatch.Groups[2].Value;
                await backend.SendMessageAsync(message.ReplyTo, "Looking for cool stuff. Please be patient.");
                List<string> discovered = await DiscoveryChainTrackLoop(artist, track, 10);
                await backend.SendMessageAsync(message.ReplyTo, "Discovery chain:\r\n" + String.Join(" ->\r\n", discovered));
                return true;
            }
            return false;
        }

        private async Task<bool> TestArtistDiscovery(IMessage message, string body)
        {
            var discoveryChainArtistMatch = discoveryChainArtistRegex.Match(body);
            if (discoveryChainArtistMatch.Success)
            {
                string artist = discoveryChainArtistMatch.Groups[1].Value;
                await backend.SendMessageAsync(message.ReplyTo, "Looking for cool stuff. Please be patient.");
                List<string> discovered = await DiscoveryChainArtistLoop(artist, 10);
                await backend.SendMessageAsync(message.ReplyTo, "Discovery chain: " + String.Join(" -> ", discovered));
                return true;
            }
            return false;
        }

        private async Task<bool> TestHelp(IMessage message, string body)
        {
            var helpMatch = helpRegex.Match(body);
            if (helpMatch.Success)
            {
                await backend.SendMessageAsync(message.ReplyTo, LastFmResponse.CreateHelpResponse(config.Name));
                return true;
            }
            return false;
        }

        #endregion

        #region Web Service Loops

        // TODO: prevent cycles
        private async Task<List<string>> DiscoveryChainTrackLoop(string artist, string track, int iterations)
        {
            Debug.Assert(iterations <= 10);

            var discovered = new List<Tuple<string, string>>();
            var originalTrackName = new Tuple<string, string>(artist, track);
            for (int i = 0; i < iterations; i++)
            {
                XDocument xml = await new LastFmClient(this.config[Common.Constants.ConfigKey.LastFmApiKey]).GetSimilarTracksAsync(originalTrackName.Item1, originalTrackName.Item2);
                List<Tuple<string, string>> similar = LastFmXmlParser.GetSimilarTrackNames(xml, out originalTrackName, true, 1);

                if (i == 0)
                {
                    discovered.Add(originalTrackName);
                }

                if (similar.Any())
                {
                    discovered.Add(similar.First());
                }
                else
                {
                    break;
                }
            }

            return discovered.Select(x => String.Format("\"{0}\" by {1}", x.Item2, x.Item1)).ToList();
        }

        // TODO: prevent cycles
        private async Task<List<string>> DiscoveryChainArtistLoop(string artist, int iterations)
        {
            Debug.Assert(iterations <= 10);

            var discovered = new List<string>();

            string originalArtistName = artist;
            for (int i = 0; i < iterations; i++)
            {
                XDocument xml = await new LastFmClient(this.config[Common.Constants.ConfigKey.LastFmApiKey]).GetSimilarArtistsAsync(originalArtistName);
                List<string> similar = LastFmXmlParser.GetSimilarArtistNames(xml, out originalArtistName, true, 1);

                if (i == 0)
                {
                    discovered.Add(originalArtistName);
                }

                if (similar.Any())
                {
                    discovered.Add(similar.First());
                    originalArtistName = similar.First();
                }
                else
                {
                    break;
                }
            }

            return discovered;
        }

        #endregion

        #region Private Classes

        private static class LastFmXmlParser
        {
            public static List<string> GetSimilarArtistNames(XDocument xml, out string originalArtistName, bool isRandomized = true, int limit = 10)
            {
                Debug.Assert(limit > 0);

                originalArtistName = xml
                    .Descendants("similarartists").First()
                    .Attribute("artist").Value;
                
                var r = new Random();
                var names = new List<string>();
                foreach (var item in xml.Descendants("artist").OrderBy(x => isRandomized ? r.Next() : 0).Take(limit))
                {
                    names.Add(item.Element("name").Value);
                }

                return names;
            }

            public static List<Tuple<string, string>> GetSimilarTrackNames(XDocument xml, out Tuple<string, string> originalTrackName, bool isRandomized = false, int limit = 25)
            {
                Debug.Assert(limit > 0);

                var similarTracksElement = xml.Descendants("similartracks").First();
                originalTrackName = new Tuple<string, string>(
                    similarTracksElement.Attribute("artist").Value,
                    similarTracksElement.Attribute("track").Value
                );

                var r = new Random();
                var tracks = new List<Tuple<string, string>>();
                foreach (var item in xml.Descendants("track").OrderBy(x => isRandomized ? r.Next() : 0).Take(limit))
                {
                    tracks.Add(new Tuple<string,string>(
                        item.Element("artist").Element("name").Value,
                        item.Element("name").Value));
                }

                return tracks;
            }
        }

        private static class LastFmResponse
        {
            public static string CreateSimilarArtistsResponse(XDocument xml, bool isRandomized = true, int limit = 10)
            {
                string originalArtistName;
                List<string> similarArtistNames = LastFmXmlParser.GetSimilarArtistNames(xml, out originalArtistName, isRandomized, limit);

                var response = new StringBuilder();
                response
                    .Append("Similar artists to ")
                    .Append(originalArtistName)
                    .Append(": ")
                    .Append(String.Join(", ", similarArtistNames))
                    .Append(".");

                return response.ToString();
            }

            public static string CreateSimilarTracksResponse(XDocument xml, bool isRandomized = false, int limit = 25)
            {
                Tuple<string, string> originalTrackName;
                List<Tuple<string, string>> similarTrackNames = LastFmXmlParser.GetSimilarTrackNames(xml, out originalTrackName, isRandomized, limit);

                Func<Tuple<string, string>, string> toFriendly = (x) => String.Format("\"{0}\" by {1}", x.Item2, x.Item1);

                var response = new StringBuilder();
                response
                    .Append("Similar songs to ")
                    .Append(toFriendly(originalTrackName))
                    .Append(":\r\n")
                    .Append(String.Join("\r\n", similarTrackNames.Select(x => toFriendly(x))));

                return response.ToString();
            }

            public static string CreateHelpResponse(string botName)
            {
                var response = new StringBuilder();

                response.AppendLine();
                response.AppendLine(botName + " music help");
                response.AppendLine("    The help text you are currently viewing.");
                response.AppendLine();
                response.AppendLine(botName + " music similar to Rebecca Black");
                response.AppendLine("    Returns a randomized list of artists that are similar to Rebecca Black.");
                response.AppendLine();
                response.AppendLine(botName + " music similar to \"Whip My Hair\" by Willow Smith");
                response.AppendLine("    Returns a list of songs that are similar to \"Whip My Hair\" by Willow Smith, sorted by relevance.");
                response.AppendLine();
                response.AppendLine(botName + " music discovery Miley Cyrus");
                response.AppendLine("    Returns a discovery chain of artists, beginning with Miley Cyrus.");
                response.AppendLine();
                response.AppendLine(botName + " music discovery \"Ice Ice Baby\" by Vanilla Ice");
                response.AppendLine("    Returns a discovery chain of songs, beginning with \"Ice Ice Baby\" by Vanilla Ice.");
                response.AppendLine();
                response.AppendLine();
                response.AppendLine("More cool features coming soon!");

                return response.ToString();
            }
        }

        #endregion
    }
}
