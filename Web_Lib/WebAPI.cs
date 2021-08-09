using Common_Lib;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Web_Lib
{

    public class WebAPI : IDisposable
    {
        static readonly HttpClient client = new();
        static HttpClient rarbgclient = new();
        private HttpResponseMessage _http_response;
        readonly string tvmaze_url = "https://api.tvmaze.com/";
        private bool _tvmaze_url_initialized;
        readonly string tvmaze_user_url = "https://api.tvmaze.com/v1/user/";
        private bool _tvmaze_user_url_initialized;

        readonly string RarbgAPI_url_pre = "https://torrentapi.org/pubapi_v2.php?mode=search&search_string='";
        readonly string RarbgAPI_url_suf = "'&token=lnjzy73ucv&format=json_extended&app_id=lol";

        private readonly Logger log;

        public WebAPI(Logger logger)
        {
            log = logger;
        }

        #region TVMaze APIs

        private async Task PerformTvmApiAsync(string api)
        {
            try
            {
                HttpResponseMessage response = new();
                _http_response = await client.GetAsync(api).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Write($"Exception {e.Message}", "WebAPI Async", 0);
            }
        }

        private void SetTvmaze()
        {
            if (!_tvmaze_url_initialized)
            {
                client.BaseAddress = new Uri(tvmaze_url);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _tvmaze_url_initialized = true;
            }
        }

        private void SetTvmazeUser()
        {
            if (!_tvmaze_user_url_initialized)
            {
                client.BaseAddress = new Uri(tvmaze_user_url);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", "Basic RGlja0tsdWlzOkVpb1dWRVJpZDdHekpteUlQTEVCR09mUHExTm40SFdM");
                _tvmaze_user_url_initialized = true;
            }
        }

        public HttpResponseMessage GetShow(string showname)
        {
            SetTvmaze();

            string api = $"search/shows?q={showname}";
            log.Write($"API String = {api}", "WebAPI GS", 3);

            Task t = PerformTvmApiAsync(api);
            t.Wait();

            client.CancelPendingRequests();
            
            return _http_response;
        }

        public HttpResponseMessage GetShow(int showid)
        {
            SetTvmaze();

            string api = $"shows/{showid}";
            log.Write($"API String = {api}", "WebAPI GS", 3);

            Task t = PerformTvmApiAsync(api);
            t.Wait();

            return _http_response;
        }

        public HttpResponseMessage GetEpisodesByShow(int showid)
        {
            SetTvmaze();

            string api = $"shows/{showid}/episodes";
            log.Write($"API String = {api}", "WebAPI GS", 3);

            Task t = PerformTvmApiAsync(api);
            t.Wait();

            return _http_response;
        }

        public HttpResponseMessage GetShowUpdateEpochs(string period)
        {
            SetTvmaze();

            string api = $"updates/shows?since={period}";
            log.Write($"API String = {api}", "WebAPI GSUE", 3);
            
            Task t = PerformTvmApiAsync(api);
            t.Wait();

            return _http_response;
        }

        public HttpResponseMessage GetFollowedShows()
        {
            SetTvmazeUser();

            string api = $"follows/shows";
            log.Write($"API String = {api}", "WebAPI GFS", 3);

            Task t = PerformTvmApiAsync(api);
            t.Wait();

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
