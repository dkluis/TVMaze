using Common_Lib;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
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

        #region Web Scrapping

        public List<string> TestWebScrap()
        {
            string html = @"https://eztv.re/search/dcs-legends-of-tomorrow-s06e01";
            HtmlWeb web = new HtmlWeb();
            List<string> magnets = new();

            HtmlDocument htmlDoc = web.Load(html);
            //log.Write($"HTML Doc: {htmlDoc.ParsedText}", "WebScrape", 0, false);

            HtmlNodeCollection table = htmlDoc.DocumentNode.SelectNodes("//td/a");
            foreach (var node in table)
            {
                if (node.Attributes["href"].Value.Contains("magnet:"))
                {
                    magnets.Add(node.Attributes["href"].Value);
                    log.Write($"Attribute HREF " + node.Attributes["href"].Value, "Test Web Scrap", 0);
                }
            }
            return magnets;
        }

        #endregion
    }




}
