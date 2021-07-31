using Common_Lib;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Web_Lib
{

    public class WebAPI
    {
        static HttpClient client = new();
        string tvmaze_url = "https://api.tvmaze.com/";
        string tvm_user_url = "https://api.tvmaze.com/v1/";
        private Logger log;

        public WebAPI(Logger logger)
        {
            log = logger;
        }


        public string ShowSearchAPI(string showname)
        {
            client.BaseAddress = new Uri(tvmaze_url);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string api = $"search/shows?q={showname}";
            log.WriteAsync($"API String = {api}", "WebAPI SSP",3);
            return api;
        }

        public async Task GetShow(string showname)
        {
            try
            {
                HttpResponseMessage response = new();
                response = await client.GetAsync(ShowSearchAPI(showname)).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    log.WriteAsync($"Response status code is {response.StatusCode}", "WebAPI", 3);
                    log.WriteAsync($"Response content is {response.Content}", "WebAPI", 3);
                }
                else
                {
                    log.WriteAsync($"Reponse was unsuccessfull {response.StatusCode}", "WebAPI", 3);
                }
            }
            catch (Exception e)
            {
                log.WriteAsync($"Exception {e.Message}", "WebAPI GetShow", 0);
            }
        }
    }




}
