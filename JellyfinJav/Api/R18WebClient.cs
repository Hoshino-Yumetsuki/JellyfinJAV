namespace JellyfinJav.Api
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class R18WebClient(HttpClient httpClient)
    {
        public async Task<(IEnumerable<VideoResult> Results, HttpStatusCode StatusCode)> Search(string code, CancellationToken token)
        {
            var (json, status) = await GetJson("dvd_id", code, token).ConfigureAwait(false);
            if (json is null || Text(json["content_id"]) is not string id)
            {
                return (Array.Empty<VideoResult>(), json is null ? status : HttpStatusCode.BadGateway);
            }

            var jacket = (json["images"] as JsonObject)?["jacket_image"] as JsonObject;
            Uri.TryCreate(Text(jacket?["large2"]), UriKind.Absolute, out var cover);
            return (new[] { new VideoResult { Id = id, Code = code.Trim().ToUpperInvariant(), Cover = cover } }, status);
        }

        public async Task<(Video? Video, HttpStatusCode StatusCode)> Load(string id, CancellationToken token, string? searchCode = null)
        {
            var (json, status) = await GetJson("combined", id, token).ConfigureAwait(false);
            if (json is null)
            {
                return (null, status);
            }

            var contentId = Text(json["content_id"]);
            var title = Text(json["title_en"]) ?? Text(json["title_ja"]);
            if (contentId is null || !contentId.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase) || title is null)
            {
                return (null, HttpStatusCode.BadGateway);
            }

            // Some digital releases have no dvd_id, even when found by DVD code.
            var code = Text(json["dvd_id"]) ?? searchCode ?? CodeFromContentId(contentId);
            var cover = Text(json["jacket_full_url"]);
            var releaseDate = DateTime.TryParseExact(Text(json["release_date"]), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : (DateTime?)null;
            return (new Video(contentId, code, title, Names(json["actresses"], "name_romaji"), Names(json["categories"], "name_en"),
                Text(json["label_name_en"]) ?? Text(json["maker_name_en"]), Text(json["jacket_thumb_url"]), cover, releaseDate), status);
        }

        private static string CodeFromContentId(string id)
        {
            var match = Regex.Match(id, @"^([a-zA-Z]+)00([0-9]{3})$");
            return match.Success ? $"{match.Groups[1].Value.ToUpperInvariant()}-{match.Groups[2].Value}" : id;
        }

        private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text) ? text.Trim() : null;

        private static string[] Names(JsonNode? node, string field) => node is JsonArray items
            ? items.OfType<JsonObject>().Select(item => Text(item[field])).OfType<string>().ToArray() : Array.Empty<string>();

        private async Task<(JsonObject? Json, HttpStatusCode StatusCode)> GetJson(string field, string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return (null, HttpStatusCode.NotFound);
            }

            var url = $"https://r18.dev/videos/vod/movies/detail/-/{field}={Uri.EscapeDataString(value.Trim())}/json";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("JellyfinJav/1.0");
                request.Headers.Accept.ParseAdd("application/json");
                using var response = await httpClient.SendAsync(request, token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return (null, response.StatusCode);
                }

                using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                var json = await JsonNode.ParseAsync(stream, cancellationToken: token).ConfigureAwait(false);
                return json is JsonObject obj ? (obj, response.StatusCode) : (null, HttpStatusCode.BadGateway);
            }
            catch (JsonException)
            {
                return (null, HttpStatusCode.BadGateway);
            }
            catch (HttpRequestException ex)
            {
                return (null, ex.StatusCode ?? HttpStatusCode.ServiceUnavailable);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                return (null, HttpStatusCode.GatewayTimeout);
            }
        }
    }
}
