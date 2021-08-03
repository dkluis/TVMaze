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
        static HttpClient client = new();
        private HttpResponseMessage _http_response;

        string tvmaze_url = "https://api.tvmaze.com/";
        string tvm_user_url = "https://api.tvmaze.com/v1/";

        string RarbgAPI_url_pre = "https://torrentapi.org/pubapi_v2.php?mode=search&search_string='";
        string RarbgAPI_url_suf = "'&token=lnjzy73ucv&format=json_extended&app_id=lol";

        private Logger log;

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

        public HttpResponseMessage GetRarbgMagnets(string searchURL)
        {
            Task t = GetShowRarbg(searchURL);
            t.Wait();
            return _http_response;
        }

        public async Task GetShowRarbg(string showname)
        {
            try
            {
                HttpResponseMessage response = new();
                string url = GetRarbgMagnetsAPI(showname);
                _http_response = await client.GetAsync(url).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Write($"Exception {e.Message}", "WebAPI Rarbg", 0);
            }
        }

        private string GetRarbgMagnetsAPI(string showname)
        {
            Common comm = new();
            client.BaseAddress = new Uri(RarbgAPI_url_pre);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var productvalue = new ProductInfoHeaderValue("Safari", "13.0");
            client.DefaultRequestHeaders.UserAgent.Add(productvalue);
            string api = $"{RarbgAPI_url_pre}{comm.RemoveSpecialCharsInShowname(showname)}{RarbgAPI_url_suf}";
            log.Write($"API String = {api}", "RarbgAPI", 3);
            return api;
        }

        #endregion

        public void Dispose()
        {

        }





    }
}
