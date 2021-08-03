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
        string tvmaze_url = "https://api.tvmaze.com/";
        string tvm_user_url = "https://api.tvmaze.com/v1/";
        private Logger log;
        private HttpResponseMessage _gsa_response;

        public WebAPI(Logger logger)
        {
            log = logger;
        }

        #region TVMaze APIs

        public string ShowSearchAPI(string showname)
        {
            client.BaseAddress = new Uri(tvmaze_url);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string api = $"search/shows?q={showname}";
            log.Write($"API String = {api}", "WebAPI SSP", 3);
            return api;
        }

        public HttpResponseMessage GetShow(string showname)
        {
            Task t = GetShowAsync(showname);
            t.Wait();

            return _gsa_response;
        }

        private async Task GetShowAsync(string show_episode)
        {
            try
            {
                HttpResponseMessage response = new();
                _gsa_response = await client.GetAsync(ShowSearchAPI(show_episode)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Write($"Exception {e.Message}", "WebAPI GetShow", 0);
            }
        }

        #endregion

        #region RarbgAPI
        /*
        public HttpResponseMessage GetRarbgInfo(string searchURL)
        {
            WebClient wclient = new();
            wclient.Headers["User-Agent"] = "Mozilla/5.0 (Macintosh); Intel Mac OS X 10_15_0) " +
                                            "AppleWebKit/537.36 (KHTML, like Gecko) " +
                                            "Chrome/75.0.3770.100 Safari/537.36";
            
            client.BaseAddress = new Uri(searchURL);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla/5.0 (Macintosh); Intel Mac OS X 10_15_0) " +
            //    "AppleWebKit/537.36 (KHTML, like Gecko) " +
            //    "Chrome/75.0.3770.100 Safari/537.36"));
            Task t = GetShowRarbg(searchURL);
            t.Wait();
            return _gsa_response;
        }

        public async Task GetShowRarbg(string searchURL)
        {
            try
            {
                HttpResponseMessage response = new();
                _gsa_response = await client.GetAsync(searchURL).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Write($"Exception {e.Message}", "WebAPI Rarbg", 0);
            }
        }
        */
        #endregion

        public void Dispose()
        {

        }





    }
}
