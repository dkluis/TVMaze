﻿using System;
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
        private readonly HttpClient client = new();
        private static HttpClient rarbgclient = new();
        private HttpResponseMessage _http_response;
        private readonly string tvmaze_url = "https://api.tvmaze.com/";
        private bool _tvmaze_url_initialized;
        private readonly string tvmaze_user_url = "https://api.tvmaze.com/v1/user/";
        private bool _tvmaze_user_url_initialized;

        private readonly string RarbgAPI_url_pre = "https://torrentapi.org/pubapi_v2.php?mode=search&search_string='";
        private string RarbgAPI_url_suf;
        private readonly string TvmazeSecurity;

        public bool isTimedOut;

        private readonly TextFileHandler log;

        public WebApi(AppInfo appinfo)
        {
            log = appinfo.TxtFile;
            RarbgAPI_url_suf = appinfo.RarbgToken;
            TvmazeSecurity = appinfo.TvmazeToken;
        }

#pragma warning disable CA1822 // Mark members as static
        public JObject ConvertHttpToJObject(HttpResponseMessage message)
#pragma warning restore CA1822 // Mark members as static
        {
            string content = message.Content.ReadAsStringAsync().Result;
            if (content == "")
            {
                JObject empty = new();
                return empty;
            }
            JObject jobject = JObject.Parse(content);
            return jobject;
        }

#pragma warning disable CA1822 // Mark members as static
        public JArray ConvertHttpToJArray(HttpResponseMessage messsage)
#pragma warning restore CA1822 // Mark members as static
        {
            string content = messsage.Content.ReadAsStringAsync().Result;
            if (content == "")
            {
                JArray empty = new();
                return empty;
            }
            JArray jarray = JArray.Parse(content);
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
            var exectime = new System.Diagnostics.Stopwatch();
            exectime.Start();

            Task t = PerformTvmApiAsync(api);
            t.Wait();

            exectime.Stop();
            log.Write($"TVMApi Exec time: {exectime.ElapsedMilliseconds} ms.", "", 4);

            if (isTimedOut)
            {
                log.Write($"TimedOut --> Http Response Code is: {_http_response.StatusCode} for API {client.BaseAddress}{api}", "WebAPI Exec", 3);
                _http_response = new HttpResponseMessage();
            }
            else if (_http_response is null)
            {
                log.Write($"NULL --> Http Response Code is: NULL for API {client.BaseAddress}{api}", "WebAPI Exec", 3);
                _http_response = new HttpResponseMessage();
            }
            else if (!_http_response.IsSuccessStatusCode)
            {
                log.Write($"Status Code --> Http Response Code is: {_http_response.StatusCode} for API {client.BaseAddress}{api}", "WebAPI Exec", 4);
                _http_response = new HttpResponseMessage();
            }
        }

        private async Task PerformTvmApiAsync(string api)
        {
            try
            {
                _http_response = await client.GetAsync(api).ConfigureAwait(false);
                isTimedOut = false;
            }
            catch (Exception e)
            {
                log.Write($"Exception: {e.Message}", "WebAPI Async", 3);
                if (e.Message.Contains(" seconds elapsing") || e.Message.Contains("Operation timed out"))
                {
                    log.Write($"Retrying Now: {api}", "WebAPI Async", 3);
                    try
                    {
                        _http_response = await client.GetAsync(api).ConfigureAwait(false);
                    }
                    catch (Exception ee) { log.Write($"2nd Exception: {ee.Message}", "WebAPI Async", 3); }
                    isTimedOut = true;
                }
            }
        }

        private void PerformWaitPutTvmApiAsync(string api, int epi, string date, string type = "")
        {
            var exectime = new System.Diagnostics.Stopwatch();
            exectime.Start();

            EpisodeMarking em = new(epi, date, type);
            string content = em.GetJson();
            log.Write($"TVMaze Put Async with {epi} {date} {type} turned into {content}", "", 4);

            Task t = PerformPutTvmApiAsync(api, content);
            t.Wait();

            exectime.Stop();
            log.Write($"TVMApi Exec time: {exectime.ElapsedMilliseconds} ms.", "", 4);

            if (!_http_response.IsSuccessStatusCode)
            {
                log.Write($"Http Response Code is: {_http_response.StatusCode} for API {client.BaseAddress}{api}", "WebAPI Put Exec", 4);
                _http_response = new HttpResponseMessage();
            }
        }

        private async Task PerformPutTvmApiAsync(string api, string json)
        {
            StringContent httpcontent = new(json, Encoding.UTF8, "application/json");
            log.Write($"json content now is {json} for api {client.BaseAddress + api}", "WebAPI PPTAA", 4);

            try { _http_response = await client.PutAsync(client.BaseAddress + api, httpcontent).ConfigureAwait(false); }
            catch (Exception e) { log.Write($"Exception: {e.Message} for {api}", "WebAPI Put Async", 3); }
        }

        private void SetTvmaze()
        {
            if (!_tvmaze_url_initialized)
            {
                client.BaseAddress = new Uri(tvmaze_url);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
                client.Timeout = TimeSpan.FromSeconds(30);
                _tvmaze_url_initialized = true;
            }
        }

        private void SetTvmazeUser()
        {
            if (!_tvmaze_user_url_initialized)
            {
                client.BaseAddress = new Uri(tvmaze_user_url);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", TvmazeSecurity);
                client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
                client.Timeout = TimeSpan.FromSeconds(30);
                _tvmaze_user_url_initialized = true;
            }
        }

        public HttpResponseMessage GetShow(string showname)
        {
            SetTvmaze();

            string api = $"search/shows?q={showname}";
            log.Write($"API String = {tvmaze_url}{api}", "WebAPI GS", 4);

            PerformWaitTvmApi(api);

            return _http_response;
        }

        public HttpResponseMessage GetShow(int showid)
        {
            SetTvmaze();
            string api = $"shows/{showid}";
            PerformWaitTvmApi(api);
            log.Write($"API String = {tvmaze_url}{api}", "WebAPI GS Int", 4);

            return _http_response;
        }

        public HttpResponseMessage GetEpisodesByShow(int showid)
        {
            SetTvmaze();
            string api = $"shows/{showid}/episodes";
            PerformWaitTvmApi(api);
            log.Write($"API String = {tvmaze_url}{api}", "WebAPI GEBS", 4);

            return _http_response;
        }

        public HttpResponseMessage GetShowUpdateEpochs(string period)
        {
            // day, week, month option for period
            SetTvmaze();
            string api = $"updates/shows?since={period}";
            PerformWaitTvmApi(api);
            log.Write($"API String = {tvmaze_url}{api}", "WebAPI GSUE", 4);

            return _http_response;
        }

        public HttpResponseMessage GetFollowedShows()
        {
            SetTvmazeUser();
            string api = $"follows/shows";
            PerformWaitTvmApi(api);
            log.Write($"API String = {tvmaze_user_url}{api}", "WebAPI GFS", 4);

            return _http_response;
        }

        public bool CheckForFollowedShow(int showid)
        {
            bool isFollowed = false;
            SetTvmazeUser();
            string api = $"follows/shows/{showid}";
            PerformWaitTvmApi(api);
            log.Write($"API String = {tvmaze_user_url}{api}", "WebAPI GFS", 4);
            if (_http_response.IsSuccessStatusCode) { isFollowed = true;  }
            return isFollowed;
        }

        #endregion

        #region Tvmaze Episode APIs

        public HttpResponseMessage GetEpisode(int episodeid)
        {
            SetTvmaze();
            string api = $"episodes/{episodeid}?embed=show";
            log.Write($"API String = {tvmaze_url}{api}", "WebAPI G Epi", 4);
            PerformWaitTvmApi(api);

            return _http_response;
        }

        public HttpResponseMessage GetEpisodeMarks(int episodeid)
        {
            SetTvmazeUser();
            string api = $"episodes/{episodeid}";
            log.Write($"API String = {tvmaze_user_url}{api}", "WebAPI GM Epi", 4);
            PerformWaitTvmApi(api);

            /*
             * 0 = watched, 1 = acquired, 2 = skipped 
            */

            return _http_response;
        }

        public HttpResponseMessage PutEpisodeToWatched(int episodeid, string watcheddate = "")
        {
            SetTvmazeUser();
            string api = $"episodes/{episodeid}";
            if (watcheddate == "") { watcheddate = DateTime.Now.ToString("yyyy-MM-dd"); }
            PerformWaitPutTvmApiAsync(api, episodeid, watcheddate, "Watched");

            return _http_response;
        }

        public HttpResponseMessage PutEpisodeToAcquired(int episodeid, string acquiredate = "")
        {
            SetTvmazeUser();
            string api = $"episodes/{episodeid}";
            if (acquiredate == "") { acquiredate = DateTime.Now.ToString("yyyy-MM-dd"); }
            PerformWaitPutTvmApiAsync(api, episodeid, acquiredate, "Acquired");

            return _http_response;
        }

        public HttpResponseMessage PutEpisodeToSkipped(int episodeid, string skipdate = "")
        {
            SetTvmazeUser();
            string api = $"episodes/{episodeid}";
            if (skipdate == "") { skipdate = DateTime.Now.ToString("yyyy-MM-dd"); }
            PerformWaitPutTvmApiAsync(api, episodeid, skipdate, "Skipped");

            return _http_response;
        }


        #endregion

        #region Scrape APIs

        #region RarbgAPI

        public HttpResponseMessage GetRarbgMagnets(string searchfor)
        {
            rarbgclient = new();
            rarbgclient.BaseAddress = new Uri(RarbgAPI_url_pre);
            rarbgclient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var productvalue = new ProductInfoHeaderValue("Safari", "13.0");
            rarbgclient.DefaultRequestHeaders.UserAgent.Add(productvalue);
            Task t = GetShowRarbg(searchfor);
            t.Wait();
            return _http_response;
        }

        public async Task GetShowRarbg(string searchfor)
        {
            try
            {
                HttpResponseMessage response = new();
                string url = GetRarbgMagnetsAPI(searchfor);
                _http_response = await rarbgclient.GetAsync(url).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Write($"Exception {e.Message}", "WebAPI Rarbg", 0);
            }
        }

        private string GetRarbgMagnetsAPI(string searchfor)
        {
            string api = $"{RarbgAPI_url_pre}{Common.RemoveSpecialCharsInShowName(searchfor)}{RarbgAPI_url_suf}";
            log.Write($"API String = {api}", "RarbgAPI", 4);
            return api;
        }

        #endregion

        #endregion

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class EpisodeMarking
    {
        public int episode_id;
        public int marked_at;
        public int type;

        public EpisodeMarking(int epi, string date, string ty = "")
        {
            episode_id = epi;
            marked_at = Common.ConvertDateToEpoch(date);
            switch (ty)
            {
                case "Watched":
                    type = 0;
                    break;
                case "Acquired":
                    type = 1;
                    break;
                case "Skipped":
                    type = 2;
                    break;
                default:
                    type = 0;
                    break;
            }

        }
        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}