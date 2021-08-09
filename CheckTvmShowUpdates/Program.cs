using System;
using Common_Lib;
using Web_Lib;
using DB_Lib;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CheckTvmShowUpdates
{
    class Program
    {
        static void Main()
        {
            Logger log = new("CheckTvmShowUpdates.log");
            log.Start("ImportFromPythonTVM");

            #region Get TVMaze last 24 Show Updates;

            using (WebAPI tvmapi = new(log))
            {

                log.Write("Start to API test", "Program", 0);
                HttpResponseMessage result = tvmapi.GetShowUpdateEpochs("day");  //day or week or month
                log.Write($"Result back from API call {result.StatusCode}", "Get Update Epochs", 3);

                var content = result.Content.ReadAsStringAsync().Result;
                var jsoncontent = JsonConvert.DeserializeObject<Dictionary<int, int>>(content);
                

                foreach (var kvp in jsoncontent)
                {
                    log.Write($"Key: {kvp.Key}, value: {kvp.Value}");
                    HttpResponseMessage showinfo = tvmapi.GetShow(kvp.Key);
                    log.Write(showinfo.Content.ReadAsStringAsync().Result);
                    break;
                }

                tvmapi.Dispose();
            }

            #endregion

            log.Stop();
        }
    }
}
