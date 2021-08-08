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
        // readonly string tvm_user_url = "https://api.tvmaze.com/v1/";

        readonly string RarbgAPI_url_pre = "https://torrentapi.org/pubapi_v2.php?mode=search&search_string='";
        readonly string RarbgAPI_url_suf = "'&token=lnjzy73ucv&format=json_extended&app_id=lol";

        private readonly Logger log;
        // private readonly Common common = new();

        public WebAPI(Logger logger)
        {
            log = logger;
        }

        #region TVMaze APIs

        public HttpResponseMessage GetShow(string showname)
        {
            Task t = GetShowAsync(showname);
            t.Wait();

            return _http_response;
        }

        private async Task GetShowAsync(string show_episode)
        {
            try
            {
                HttpResponseMessage response = new();
                _http_response = await client.GetAsync(ShowSearchAPI(show_episode)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Write($"Exception {e.Message}", "WebAPI GetShow", 0);
            }
        }

        private string ShowSearchAPI(string showname)
        {
            client.BaseAddress = new Uri(tvmaze_url);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string api = $"search/shows?q={showname}";
            log.Write($"API String = {api}", "WebAPI SSP", 3);
            return api;
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
