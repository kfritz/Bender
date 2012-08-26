using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bender.Apis.LastFm
{
    public enum LastFmMethod
    {
        [LastFmMethodName("artist.getTopTracks")]
        Artist_GetTopTracks,
        [LastFmMethodName("artist.getInfo")]
        Artist_GetInfo,
        [LastFmMethodName("artist.getSimilar")]
        Artist_GetSimilar,
        [LastFmMethodName("artist.search")]
        Artist_Search,
        [LastFmMethodName("chart.getHypedArtists")]
        Chart_GetHypedArtists,
        [LastFmMethodName("chart.getHypedTracks")]
        Chart_GetHypedTracks,
        [LastFmMethodName("chart.getTopArtists")]
        Chart_GetTopArtists,
        [LastFmMethodName("chart.getTopTracks")]
        Chart_GetTopTracks,
        [LastFmMethodName("track.getSimilar")]
        Track_GetSimilar,
    }
}
