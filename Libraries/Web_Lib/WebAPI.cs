using Common_Lib;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Web_Lib
{

    public class WebAPI : IDisposable
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

        private readonly TextFileHandler log;

        public WebAPI(AppInfo appinfo)
        {
            log = appinfo.TxtFile;
            RarbgAPI_url_suf = appinfo.RarbgToken;
            TvmazeSecurity = appinfo.TvmazeToken;
        }

        /*
        public dynamic ConvertHttpToJson(HttpResponseMessage message)
        {
            var jsoncontent = JsonConvert.DeserializeObject("");
            if (message is null)
            {
                return jsoncontent;
            }
            string content = message.Content.ReadAsStringAsync().Result;
            jsoncontent = JsonConvert.DeserializeObject(content);
            return jsoncontent;
        }
        */

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

        #region TVMaze APIs

        private void PerformWaitTvmApi(string api)
        {
            var exectime = new System.Diagnostics.Stopwatch();
            exectime.Start();

            Task t = PerformTvmApiAsync(api);
            t.Wait();

            exectime.Stop();
            log.Write($"TVMApi Exec time: {exectime.ElapsedMilliseconds} ms.");

            if (_http_response is null)
            {
                _http_response = new HttpResponseMessage();
            }

            if (!_http_response.IsSuccessStatusCode)
            {
                log.Write($"Http Response Code is: {_http_response.StatusCode}", "WebAPI Exec", 0);
                _http_response = new HttpResponseMessage();
            }
        }

        private async Task PerformTvmApiAsync(string api)
        {
            try
            {
                HttpResponseMessage response = new();
                _http_response = await client.GetAsync(api).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Write($"Exception: {e.Message}", "WebAPI Async", 0);
                if (e.Message.Contains(" seconds elapsing"))
                {
                    //Thread.Sleep(1000);
                    log.Write($"Retrying Now: {api}", "WebAPI Async", 0);
                    try
                    {
                        log.Write($"Retrying Now: {api}", "WebAPI Async", 0);
                        HttpResponseMessage response = new();
                        _http_response = await client.GetAsync(api).ConfigureAwait(false);
                    }
                    catch (Exception ee)
                    {

                        log.Write($"2nd Exception: {ee.Message}", "WebAPI Async", 0);

                    }
                }
            }
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
            log.Write($"API String = {tvmaze_url}{api}", "WebAPI GS", 3);

            PerformWaitTvmApi(api);

            return _http_response;
        }

        public HttpResponseMessage GetShow(int showid)
        {
            SetTvmaze();
            string api = $"shows/{showid}";
            PerformWaitTvmApi(api);
            log.Write($"API String = {tvmaze_url}{api}", "WebAPI GS", 3);

            return _http_response;
        }

        public HttpResponseMessage GetEpisodesByShow(int showid)
        {
            SetTvmaze();
            string api = $"shows/{showid}/episodes";
            PerformWaitTvmApi(api);
            log.Write($"API String = {tvmaze_url}{api}", "WebAPI GEBS", 3);

            return _http_response;
        }

        public HttpResponseMessage GetShowUpdateEpochs(string period)
        {
            SetTvmaze();
            string api = $"updates/shows?since={period}";
            PerformWaitTvmApi(api);
            log.Write($"API String = {tvmaze_url}{api}", "WebAPI GSUE", 3);

            return _http_response;
        }

        public HttpResponseMessage GetFollowedShows()
        {
            SetTvmazeUser();
            string api = $"follows/shows";
            PerformWaitTvmApi(api);
            log.Write($"API String = {tvmaze_user_url}{api}", "WebAPI GFS", 3);

            return _http_response;
        }

        #endregion

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
            string api = $"{RarbgAPI_url_pre}{Common.RemoveSpecialCharsInShowname(searchfor)}{RarbgAPI_url_suf}";
            log.Write($"API String = {api}", "RarbgAPI", 3);
            return api;
        }

        #endregion

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
