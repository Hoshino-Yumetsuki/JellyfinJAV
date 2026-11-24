#pragma warning disable SA1600

namespace Tests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using JellyfinJav.Api;
    using JellyfinJav.Providers;
    using NUnit.Framework;

    public class R18WebClientTest
    {
        [Test]
        public async Task ResolveDvdCodeAndRefreshByContentId()
        {
            using var http = new HttpClient(new Handler(path => path switch
            {
                "/videos/vod/movies/detail/-/dvd_id=BBAN-613/json" => (HttpStatusCode.OK,
                    """{"content_id":"bban00613","images":{"jacket_image":{"large2":"https://example.org/cover.jpg"}}}"""),
                "/videos/vod/movies/detail/-/combined=bban00613/json" => (HttpStatusCode.OK,
                    """{"content_id":"bban00613","dvd_id":null,"title_en":" ","title_ja":"日本語","label_name_en":"bibian\n","actresses":[{"name_romaji":"Ruka Itoi"}],"release_date":"2026-11-06"}"""),
                _ => (HttpStatusCode.NotFound, string.Empty),
            }));
            var client = new R18WebClient(http);
            var code = Utility.ExtractCodeFromFilename("bban613.mkv");
            var (results, _) = await client.Search(code, default);
            var hit = results.Single();
            Assert.That(hit.Code, Is.EqualTo("BBAN-613"));
            Assert.That(hit.Id, Is.EqualTo("bban00613"));
            Assert.That(hit.Cover.AbsoluteUri, Is.EqualTo("https://example.org/cover.jpg"));

            var (video, status) = await client.Load(hit.Id, default);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(video.Value.Code, Is.EqualTo(hit.Code));
            Assert.That(video.Value.Title, Is.EqualTo("日本語"));
            Assert.That(video.Value.Studio, Is.EqualTo("bibian"));
            Assert.That(video.Value.ReleaseDate, Is.EqualTo(new DateTime(2026, 11, 6)));
            Assert.That(video.Value.Actresses, Is.EqualTo(new[] { "Ruka Itoi" }));
            Assert.That(video.Value.Genres, Is.Empty);

            var (missing, missStatus) = await client.Search("BBAN-6131", default);
            Assert.That(missing, Is.Empty);
            Assert.That(missStatus, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [TestCase(HttpStatusCode.OK, "<html>not metadata</html>", HttpStatusCode.BadGateway)]
        [TestCase(HttpStatusCode.OK, "{}", HttpStatusCode.BadGateway)]
        [TestCase(HttpStatusCode.TooManyRequests, "", HttpStatusCode.TooManyRequests)]
        public async Task InvalidSearchIsNotAMatch(HttpStatusCode upstream, string body, HttpStatusCode expected)
        {
            using var http = new HttpClient(new Handler(_ => (upstream, body)));
            var (results, status) = await new R18WebClient(http).Search("BBAN-613", default);
            Assert.That(results, Is.Empty);
            Assert.That(status, Is.EqualTo(expected));
        }

        [Test]
        [Explicit("Queries the live R18.dev service; run without database environment variables.")]
        public async Task LiveBbanLookup()
        {
            var (video, status) = await R18Client.SearchFirstWithStatus("BBAN-613");
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(video.HasValue, Is.True);
            Assert.That(video.Value.Id, Is.EqualTo("bban00613"));
            Assert.That(video.Value.Code, Is.EqualTo("BBAN-613"));
            Assert.That(video.Value.Title, Does.Contain("Group Date").IgnoreCase);
            Assert.That(video.Value.Actresses, Does.Contain("Ruka Itoi"));
            var refreshed = await R18Client.LoadVideo(video.Value.Id);
            Assert.That(refreshed, Is.EqualTo(video));
            Assert.That(await R18Client.LoadVideo("BBAN-613"), Is.EqualTo(video));
            var (missing, missStatus) = await R18Client.SearchWithStatus("BBAN-6131");
            Assert.That(missStatus, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(missing, Is.Empty);
        }

        private sealed class Handler(Func<string, (HttpStatusCode Status, string Body)> reply) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var (status, body) = reply(request.RequestUri.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
            }
        }
    }
}
