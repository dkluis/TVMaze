using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Common_Lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Web_Lib
{

    public class WebApi : IDisposable
    {
        private readonly HttpClient _client = new();
        private static HttpClient _rarbgClient = new();
        private HttpResponseMessage _httpResponse;
        private const string TvmazeUrl = "https://api.tvmaze.com/";
        private bool _tvmazeUrlInitialized;
        private const string TvmazeUserUrl = "https://api.tvmaze.com/v1/user/";
        private bool _tvmazeUserUrlInitialized;
        private const string RarbgApiUrlPre = "https://torrentapi.org/pubapi_v2.php?mode=search&search_string='";
        private readonly string _rarbgApiUrlSuf;
        private readonly string _tvmazeSecurity;
        public bool IsTimedOut;
        private readonly TextFileHandler _log;

        public WebApi(AppInfo appInfo)
        {
            _log = appInfo.TxtFile;
            _rarbgApiUrlSuf = appInfo.RarbgToken;
            _tvmazeSecurity = appInfo.TvmazeToken;
        }

#pragma warning disable CA1822 // Mark members as static
        public JObject ConvertHttpToJObject(HttpResponseMessage message)
#pragma warning restore CA1822 // Mark members as static
        {
            var content = message.Content.ReadAsStringAsync().Result;
            if (content == "")
            {
                JObject empty = new();
                return empty;
            }
            var jO = JObject.Parse(content);
            return jO;
        }

#pragma warning disable CA1822 // Mark members as static
        public JArray ConvertHttpToJArray(HttpResponseMessage message)
#pragma warning restore CA1822 // Mark members as static
        {
            string content = message.Content.ReadAsStringAsync().Result;
            if (content == "")
            {
                JArray empty = new();
                return empty;
            }
            var jA = JArray.Parse(content);
            return jA;
        }

        private void PerformWaitTvmApi(string api)
        {
            var execTime = new System.Diagnostics.Stopwatch();
            execTime.Start();

            var t = PerformTvmApiAsync(api);
            t.Wait();

            execTime.Stop();
            _log.Write($"TVMApi Exec time: {execTime.ElapsedMilliseconds} ms.", "", 4);

            if (IsTimedOut)
            {
                _log.Write($"TimedOut --> Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}", "WebAPI Exec");
                _httpResponse = new HttpResponseMessage();
            }
            else if (_httpResponse is null)
            {
                _log.Write($"NULL --> Http Response Code is: NULL for API {_client.BaseAddress}{api}", "WebAPI Exec");
                _httpResponse = new HttpResponseMessage();
            }
            else if (!_httpResponse.IsSuccessStatusCode)
            {
                _log.Write($"Status Code --> Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}", "WebAPI Exec", 4);
                _httpResponse = new HttpResponseMessage();
            }
        }

        private async Task PerformTvmApiAsync(string api)
        {
            try
            {
                _httpResponse = await _client.GetAsync(api).ConfigureAwait(false);
                IsTimedOut = false;
            }
            catch (Exception e)
            {
                _log.Write($"Exception: {e.Message}", "WebAPI Async");
                if (e.Message.Contains(" seconds elapsing") || e.Message.Contains("Operation timed out"))
                {
                    _log.Write($"Retrying Now: {api}", "WebAPI Async");
                    try
                    {
                        _httpResponse = await _client.GetAsync(api).ConfigureAwait(false);
                    }
                    catch (Exception ee) { _log.Write($"2nd Exception: {ee.Message}", "WebAPI Async"); }
                    IsTimedOut = true;
                }
            }
        }

        private void PerformWaitPutTvmApiAsync(string api, int epi, string date, string type = "")
        {
            var execTime = new System.Diagnostics.Stopwatch();
            execTime.Start();

            EpisodeMarking em = new(epi, date, type);
            var content = em.GetJson();
            _log.Write($"TVMaze Put Async with {epi} {date} {type} turned into {content}", "", 4);
            var t = PerformPutTvmApiAsync(api, content);
            t.Wait();
            execTime.Stop();
            _log.Write($"TVMApi Exec time: {execTime.ElapsedMilliseconds} ms.", "", 4);
            if (_httpResponse.IsSuccessStatusCode) return;
            _log.Write($"Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}", "WebAPI Put Exec", 4);
            _httpResponse = new HttpResponseMessage();
        }

        private async Task PerformPutTvmApiAsync(string api, string json)
        {
            StringContent httpContent = new(json, Encoding.UTF8, "application/json");
            _log.Write($"json content now is {json} for api {_client.BaseAddress + api}", "WebAPI PPTAA", 4);
            try { _httpResponse = await _client.PutAsync(_client.BaseAddress + api, httpContent).ConfigureAwait(false); }
            catch (Exception e) { _log.Write($"Exception: {e.Message} for {api}", "WebAPI Put Async"); }
        }

        private void SetTvmaze()
        {
            if (_tvmazeUrlInitialized) return;
            _client.BaseAddress = new Uri(TvmazeUrl);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
            _client.Timeout = TimeSpan.FromSeconds(30);
            _tvmazeUrlInitialized = true;
        }

        private void SetTvmazeUser()
        {
            if (!_tvmazeUserUrlInitialized)
            {
                _client.BaseAddress = new Uri(TvmazeUserUrl);
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _client.DefaultRequestHeaders.Add("Authorization", _tvmazeSecurity);
                _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
                _client.Timeout = TimeSpan.FromSeconds(30);
                _tvmazeUserUrlInitialized = true;
            }
        }

        public HttpResponseMessage GetShow(string showName)
        {
            SetTvmaze();

            string api = $"search/shows?q={showName}";
            _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GS", 4);

            PerformWaitTvmApi(api);

            return _httpResponse;
        }

        public HttpResponseMessage GetShow(int showId)
        {
            SetTvmaze();
            string api = $"shows/{showId}";
            PerformWaitTvmApi(api);
            _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GS Int", 4);

            return _httpResponse;
        }

        public HttpResponseMessage GetEpisodesByShow(int showId)
        {
            SetTvmaze();
            var api = $"shows/{showId}/episodes";
            PerformWaitTvmApi(api);
            _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GEBS", 4);
            return _httpResponse;
        }

        public HttpResponseMessage GetShowUpdateEpochs(string period)
        {
            // day, week, month option for period
            SetTvmaze();
            var api = $"updates/shows?since={period}";
            PerformWaitTvmApi(api);
            _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GSUE", 4);
            return _httpResponse;
        }

        public HttpResponseMessage GetFollowedShows()
        {
            SetTvmazeUser();
            var api = $"follows/shows";
            PerformWaitTvmApi(api);
            _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GFS", 4);
            return _httpResponse;
        }

        public bool CheckForFollowedShow(int showId)
        {
            var isFollowed = false;
            SetTvmazeUser();
            var api = $"follows/shows/{showId}";
            PerformWaitTvmApi(api);
            _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GFS", 4);
            if (_httpResponse.IsSuccessStatusCode) { isFollowed = true; }
            return isFollowed;
        }

        public HttpResponseMessage GetEpisode(int episodeId)
        {
            SetTvmaze();
            var api = $"episodes/{episodeId}?embed=show";
            _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI G Epi", 4);
            PerformWaitTvmApi(api);

            return _httpResponse;
        }

        public HttpResponseMessage GetEpisodeMarks(int episodeId)
        {
            SetTvmazeUser();
            string api = $"episodes/{episodeId}";
            _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GM Epi", 4);
            PerformWaitTvmApi(api);
            /*
             * 0 = watched, 1 = acquired, 2 = skipped 
            */
            return _httpResponse;
        }

        public HttpResponseMessage PutEpisodeToWatched(int episodeId, string watchedDate = "")
        {
            SetTvmazeUser();
            var api = $"episodes/{episodeId}";
            if (watchedDate == "") { watchedDate = DateTime.Now.ToString("yyyy-MM-dd"); }
            PerformWaitPutTvmApiAsync(api, episodeId, watchedDate, "Watched");
            return _httpResponse;
        }

        public HttpResponseMessage PutEpisodeToAcquired(int episodeId, string acquireDate = "")
        {
            SetTvmazeUser();
            var api = $"episodes/{episodeId}";
            if (acquireDate == "") { acquireDate = DateTime.Now.ToString("yyyy-MM-dd"); }
            PerformWaitPutTvmApiAsync(api, episodeId, acquireDate, "Acquired");
            return _httpResponse;
        }

        public HttpResponseMessage GetRarbgMagnets(string searchFor)
        {
            _rarbgClient = new();
            _rarbgClient.BaseAddress = new Uri(RarbgApiUrlPre);
            _rarbgClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var productValue = new ProductInfoHeaderValue("Safari", "13.0");
            _rarbgClient.DefaultRequestHeaders.UserAgent.Add(productValue);
            var t = GetShowRarbg(searchFor);
            t.Wait();
            return _httpResponse;
        }

        private async Task GetShowRarbg(string searchFor)
        {
            try
            {
                var url = GetRarbgMagnetsApi(searchFor);
                _httpResponse = await _rarbgClient.GetAsync(url).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.Write($"Exception {e.Message}", "WebAPI Rarbg", 0);
            }
        }

        private string GetRarbgMagnetsApi(string searchFor)
        {
            var api = $"{RarbgApiUrlPre}{Common.RemoveSpecialCharsInShowName(searchFor)}{_rarbgApiUrlSuf}";
            _log.Write($"API String = {api}", "RarbgAPI", 4);
            return api;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class EpisodeMarking
    {
        private int _episodeId;
        private int _markedAt;
        private int _type;

        public EpisodeMarking(int epi, string date, string type = "")
        {
            _episodeId = epi;
            _markedAt = Common.ConvertDateToEpoch(date);
            _type = type switch
            {
                "Watched" => 0,
                "Acquired" => 1,
                "Skipped" => 2,
                _ => 0
            };
        }
        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
