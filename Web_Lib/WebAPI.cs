using Common_Lib;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
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
            /// Add logic to check the result and turn to json before returning
            return _gsa_response;
        }

        private async Task GetShowAsync(string showname)
        {
            try
            {
                HttpResponseMessage response = new();
                _gsa_response = await client.GetAsync(ShowSearchAPI(showname)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Write($"Exception {e.Message}", "WebAPI GetShow", 0);
            }
        }

        #endregion

        public void Dispose()
        {
            this.Dispose();
        }



    }
}
