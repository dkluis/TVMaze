using System;
using System.Diagnostics;
using System.Net;
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
        private static HttpClient _rarbgclient = new();
        private readonly HttpClient _client = new();

        private readonly TextFileHandler _log;

        private readonly string _rarbgApiUrlPre = "https://torrentapi.org/pubapi_v2.php?mode=search&search_string='";
        private readonly string _tvmazeUrl = "https://api.tvmaze.com/";
        private readonly string _tvmazeUserUrl = "https://api.tvmaze.com/v1/user/";
        private readonly string _tvmazeSecurity;
        private HttpResponseMessage _httpResponse;
        private bool _tvmazeUrlInitialized;
        private bool _tvmazeUserUrlInitialized;

        public bool IsTimedOut;
        private readonly string _rarbgApiUrlSuf;

        public WebApi(AppInfo appinfo)
        {
            _log = appinfo.TxtFile;
            _rarbgApiUrlSuf = appinfo.RarbgToken;
            _tvmazeSecurity = appinfo.TvmazeToken;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
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

            var jobject = JObject.Parse(content);
            return jobject;
        }

#pragma warning disable CA1822 // Mark members as static
        public JArray ConvertHttpToJArray(HttpResponseMessage messsage)
#pragma warning restore CA1822 // Mark members as static
        {
            var content = messsage.Content.ReadAsStringAsync().Result;
            if (content == "")
            {
                JArray empty = new();
                return empty;
            }

            var jarray = JArray.Parse(content);
            return jarray;
        }

        public HttpClientHandler ShowRssLogin(string user, string password)
        {
            HttpClientHandler hch = new();
            hch.Credentials = new NetworkCredential(user, password);
            return hch;
        }

        #region TVMaze Show APIs

        private void PerformWaitTvmApi(string api)
        {
            var exectime = new Stopwatch();
            exectime.Start();

            var t = PerformTvmApiAsync(api);
            t.Wait();

            exectime.Stop();
            _log.Write($"TVMApi Exec time: {exectime.ElapsedMilliseconds} ms.", "", 4);

            if (IsTimedOut)
            {
                _log.Write(
                    $"TimedOut --> Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}",
                    "WebAPI Exec");
                _httpResponse = new HttpResponseMessage();
            }
            else if (_httpResponse is null)
            {
                _log.Write($"NULL --> Http Response Code is: NULL for API {_client.BaseAddress}{api}", "WebAPI Exec");
                _httpResponse = new HttpResponseMessage();
            }
            else if (!_httpResponse.IsSuccessStatusCode)
            {
                _log.Write(
                    $"Status Code --> Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}",
                    "WebAPI Exec", 4);
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
                    catch (Exception ee)
                    {
                        _log.Write($"2nd Exception: {ee.Message}", "WebAPI Async");
                    }

                    IsTimedOut = true;
                }
            }
        }

        private void PerformWaitPutTvmApiAsync(string api, int epi, string date, string type = "")
        {
            var exectime = new Stopwatch();
            exectime.Start();

            EpisodeMarking em = new(epi, date, type);
            var content = em.GetJson();
            _log.Write($"TVMaze Put Async with {epi} {date} {type} turned into {content}", "", 4);

            var t = PerformPutTvmApiAsync(api, content);
            t.Wait();

            exectime.Stop();
            _log.Write($"TVMApi Exec time: {exectime.ElapsedMilliseconds} ms.", "", 4);

            if (!_httpResponse.IsSuccessStatusCode)
            {
                _log.Write($"Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}",
                    "WebAPI Put Exec", 4);
                _httpResponse = new HttpResponseMessage();
            }
        }

        private async Task PerformPutTvmApiAsync(string api, string json)
        {
            StringContent httpcontent = new(json, Encoding.UTF8, "application/json");
            _log.Write($"json content now is {json} for api {_client.BaseAddress + api}", "WebAPI PPTAA", 4);

            try
            {
                _httpResponse = await _client.PutAsync(_client.BaseAddress + api, httpcontent).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.Write($"Exception: {e.Message} for {api}", "WebAPI Put Async");
            }
        }

        private void SetTvmaze()
        {
            if (!_tvmazeUrlInitialized)
            {
                _client.BaseAddress = new Uri(_tvmazeUrl);
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
                _client.Timeout = TimeSpan.FromSeconds(30);
                _tvmazeUrlInitialized = true;
            }
        }

        private void SetTvmazeUser()
        {
            if (!_tvmazeUserUrlInitialized)
            {
                _client.BaseAddress = new Uri(_tvmazeUserUrl);
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _client.DefaultRequestHeaders.Add("Authorization", _tvmazeSecurity);
                _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
                _client.Timeout = TimeSpan.FromSeconds(30);
                _tvmazeUserUrlInitialized = true;
            }
        }

        public HttpResponseMessage GetShow(string showname)
        {
            SetTvmaze();

            var api = $"search/shows?q={showname}";
            _log.Write($"API String = {_tvmazeUrl}{api}", "WebAPI GS", 4);

            PerformWaitTvmApi(api);

            return _httpResponse;
        }

        public HttpResponseMessage GetShow(int showid)
        {
            SetTvmaze();
            var api = $"shows/{showid}";
            PerformWaitTvmApi(api);
            _log.Write($"API String = {_tvmazeUrl}{api}", "WebAPI GS Int", 4);

            return _httpResponse;
        }

        public HttpResponseMessage GetEpisodesByShow(int showid)
        {
            SetTvmaze();
            var api = $"shows/{showid}/episodes";
            PerformWaitTvmApi(api);
            _log.Write($"API String = {_tvmazeUrl}{api}", "WebAPI GEBS", 4);

            return _httpResponse;
        }

        public HttpResponseMessage GetShowUpdateEpochs(string period)
        {
            // day, week, month option for period
            SetTvmaze();
            var api = $"updates/shows?since={period}";
            PerformWaitTvmApi(api);
            _log.Write($"API String = {_tvmazeUrl}{api}", "WebAPI GSUE", 4);

            return _httpResponse;
        }

        public HttpResponseMessage GetFollowedShows()
        {
            SetTvmazeUser();
            var api = "follows/shows";
            PerformWaitTvmApi(api);
            _log.Write($"API String = {_tvmazeUserUrl}{api}", "WebAPI GFS", 4);

            return _httpResponse;
        }

        public bool CheckForFollowedShow(int showid)
        {
            var isFollowed = false;
            SetTvmazeUser();
            var api = $"follows/shows/{showid}";
            PerformWaitTvmApi(api);
            _log.Write($"API String = {_tvmazeUserUrl}{api}", "WebAPI GFS", 4);
            if (_httpResponse.IsSuccessStatusCode) isFollowed = true;
            return isFollowed;
        }

        #endregion

        #region Tvmaze Episode APIs

        public HttpResponseMessage GetEpisode(int episodeid)
        {
            SetTvmaze();
            var api = $"episodes/{episodeid}?embed=show";
            _log.Write($"API String = {_tvmazeUrl}{api}", "WebAPI G Epi", 4);
            PerformWaitTvmApi(api);

            return _httpResponse;
        }

        public HttpResponseMessage GetEpisodeMarks(int episodeid)
        {
            SetTvmazeUser();
            var api = $"episodes/{episodeid}";
            _log.Write($"API String = {_tvmazeUserUrl}{api}", "WebAPI GM Epi", 4);
            PerformWaitTvmApi(api);

            /*
             * 0 = watched, 1 = acquired, 2 = skipped 
            */

            return _httpResponse;
        }

        public HttpResponseMessage PutEpisodeToWatched(int episodeid, string watcheddate = "")
        {
            SetTvmazeUser();
            var api = $"episodes/{episodeid}";
            if (watcheddate == "") watcheddate = DateTime.Now.ToString("yyyy-MM-dd");
            PerformWaitPutTvmApiAsync(api, episodeid, watcheddate, "Watched");

            return _httpResponse;
        }

        public HttpResponseMessage PutEpisodeToAcquired(int episodeid, string acquiredate = "")
        {
            SetTvmazeUser();
            var api = $"episodes/{episodeid}";
            if (acquiredate == "") acquiredate = DateTime.Now.ToString("yyyy-MM-dd");
            PerformWaitPutTvmApiAsync(api, episodeid, acquiredate, "Acquired");

            return _httpResponse;
        }

        public HttpResponseMessage PutEpisodeToSkipped(int episodeid, string skipdate = "")
        {
            SetTvmazeUser();
            var api = $"episodes/{episodeid}";
            if (skipdate == "") skipdate = DateTime.Now.ToString("yyyy-MM-dd");
            PerformWaitPutTvmApiAsync(api, episodeid, skipdate, "Skipped");

            return _httpResponse;
        }

        #endregion

        #region Scrape APIs

        #region RarbgAPI

        public HttpResponseMessage GetRarbgMagnets(string searchfor)
        {
            _rarbgclient = new HttpClient();
            _rarbgclient.BaseAddress = new Uri(_rarbgApiUrlPre);
            _rarbgclient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var productvalue = new ProductInfoHeaderValue("Safari", "13.0");
            _rarbgclient.DefaultRequestHeaders.UserAgent.Add(productvalue);
            var t = GetShowRarbg(searchfor);
            t.Wait();
            return _httpResponse;
        }

        public async Task GetShowRarbg(string searchfor)
        {
            try
            {
                HttpResponseMessage response = new();
                var url = GetRarbgMagnetsApi(searchfor);
                _httpResponse = await _rarbgclient.GetAsync(url).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.Write($"Exception {e.Message}", "WebAPI Rarbg", 0);
            }
        }

        private string GetRarbgMagnetsApi(string searchfor)
        {
            var api = $"{_rarbgApiUrlPre}{Common.RemoveSpecialCharsInShowName(searchfor)}{_rarbgApiUrlSuf}";
            _log.Write($"API String = {api}", "RarbgAPI", 4);
            return api;
        }

        #endregion

        #endregion
    }

    public class EpisodeMarking
    {
        public int EpisodeId;
        public int MarkedAt;
        public int Type;

        public EpisodeMarking(int epi, string date, string ty = "")
        {
            EpisodeId = epi;
            MarkedAt = Common.ConvertDateToEpoch(date);
            switch (ty)
            {
                case "Watched":
                    Type = 0;
                    break;
                case "Acquired":
                    Type = 1;
                    break;
                case "Skipped":
                    Type = 2;
                    break;
                default:
                    Type = 0;
                    break;
            }
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}