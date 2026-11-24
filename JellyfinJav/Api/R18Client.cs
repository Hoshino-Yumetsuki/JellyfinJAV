namespace JellyfinJav.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Looks up videos on R18.dev.</summary>
    public static class R18Client
    {
        private static readonly R18WebClient Web = new(new HttpClient { Timeout = TimeSpan.FromSeconds(15) });

        /// <summary>Searches for a video by jav code.</summary>
        public static async Task<IEnumerable<VideoResult>> Search(string searchCode)
        {
            var (results, _) = await SearchWithStatus(searchCode).ConfigureAwait(false);
            return results;
        }

        /// <summary>Searches by DVD code and returns the provider ID for loading details.</summary>
        public static Task<(IEnumerable<VideoResult> Results, HttpStatusCode StatusCode)> SearchWithStatus(string searchCode, CancellationToken token = default) =>
            Web.Search(searchCode, token);

        /// <summary>Returns the first matching video.</summary>
        public static async Task<Video?> SearchFirst(string searchCode)
        {
            var (video, _) = await SearchFirstWithStatus(searchCode).ConfigureAwait(false);
            return video;
        }

        /// <summary>Resolves a DVD code and loads the video's details.</summary>
        public static async Task<(Video? Video, HttpStatusCode StatusCode)> SearchFirstWithStatus(string searchCode, CancellationToken token = default)
        {
            var (results, status) = await Web.Search(searchCode, token).ConfigureAwait(false);
            var result = results.FirstOrDefault();
            return result.Id is null ? (null, status) : await Web.Load(result.Id, token, result.Code).ConfigureAwait(false);
        }

        /// <summary>Loads a video by its R18 content ID.</summary>
        public static async Task<Video?> LoadVideo(string id)
        {
            var (video, _) = await LoadVideoWithStatus(id).ConfigureAwait(false);
            return video;
        }

        /// <summary>Loads by R18 content ID; the manual provider-ID field also accepts a DVD code.</summary>
        public static Task<(Video? Video, HttpStatusCode StatusCode)> LoadVideoWithStatus(string id, CancellationToken token = default)
        {
            return !string.IsNullOrWhiteSpace(id) && Regex.IsMatch(id.Trim(), @"^[A-Za-z]{2,10}-[0-9]{2,6}$")
                ? SearchFirstWithStatus(id, token) : Web.Load(id, token);
        }
    }
}
