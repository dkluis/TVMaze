using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common_Lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Web_Lib;

public class WebApi : IDisposable
{
    private const string TvmazeUrl = "https://api.tvmaze.com/";
    private const string TvmazeUserUrl = "https://api.tvmaze.com/v1/user/";

    private const string RarbgApiUrlPre = "https://torrentapi.org/pubapi_v2.php?mode=search&search_string='";
    private static HttpClient _rarbgClient = new();
    private readonly HttpClient _client = new();
    private readonly TextFileHandler _log;
    private readonly string _rarbgApiUrlSuf;
    private readonly string _tvmazeSecurity;
    private HttpResponseMessage _httpResponse = new();
    private bool _tvmazeUrlInitialized;
    private bool _tvmazeUserUrlInitialized;
    public bool IsTimedOut;

    public WebApi(AppInfo appInfo)
    {
        _log = appInfo.TxtFile;
        _rarbgApiUrlSuf = appInfo.RarbgToken;
        _tvmazeSecurity = appInfo.TvmazeToken;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public JObject ConvertHttpToJObject(HttpResponseMessage message)
    {
        var content = message.Content.ReadAsStringAsync().Result;
        if (content == "")
        {
            JObject empty = new();
            return empty;
        }

        var jObject = JObject.Parse(content);
        return jObject;
    }

    public JArray ConvertHttpToJArray(HttpResponseMessage message)
    {
        var content = message.Content.ReadAsStringAsync().Result;
        if (content == "")
        {
            JArray empty = new();
            return empty;
        }

        var jArray = JArray.Parse(content);
        return jArray;
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
        var execTime = new Stopwatch();
        execTime.Start();

        var t = PerformTvmApiAsync(api);
        t.Wait();

        execTime.Stop();
        _log.Write($"TVMApi Exec time: {execTime.ElapsedMilliseconds} ms.", "", 4);

        if (IsTimedOut)
        {
            _log.Write($"TimedOut --> Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}", "WebAPI Exec");
            _httpResponse = new HttpResponseMessage();
            Console.WriteLine("########### Aborting from PerformWaitTvmApi IsTimedOut is True ###################");
            Environment.Exit(99);
        }
        // else if (_httpResponse is null)
        // {
        //     _log.Write($"NULL --> Http Response Code is: NULL for API {_client.BaseAddress}{api}", "WebAPI Exec");
        //     _httpResponse = new HttpResponseMessage();
        //     Console.WriteLine("########### Aborting from PerformWaitTvmApi Response is Null ###################");
        //     Environment.Exit(99);
        // }
        else if (!_httpResponse.IsSuccessStatusCode)
        {
            _log.Write($"Status Code --> Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}", "WebAPI Exec", 4);
            _httpResponse = new HttpResponseMessage();
            //Console.WriteLine("########### Aborting from PerformWaitTvmApi isSuccessCode is False ###################");
            //Environment.Exit(99);
        }
    }

    private async Task PerformTvmApiAsync(string api)
    {
        try
        {
            _httpResponse = await _client.GetAsync(api).ConfigureAwait(false);
            IsTimedOut = false;
        }
        catch (Exception e)
        {
            _log.Write($"Exception: {e.Message}", "WebAPI Async", 0);
            if (e.Message.Contains(" seconds elapsing") || e.Message.Contains("Operation timed out"))
            {
                _log.Write($"Retrying Now: {api}", "WebAPI Async");
                try
                {
                    _httpResponse = await _client.GetAsync(api).ConfigureAwait(false);
                }
                catch (Exception ee)
                {
                    _log.Write($"2nd Exception: {ee.Message}", "WebAPI Async");
                    _httpResponse = new HttpResponseMessage();
                    IsTimedOut = true;
                    Console.WriteLine(
                        "########### Aborting from PerformTvmApiAsync 2nd Exception ###################");
                    Environment.Exit(99);
                }
            }
        }
        finally
        {
            _client.Dispose();
        }
    }

    private void PerformWaitPutTvmApiAsync(string api, int epi, string date, string type = "")
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        EpisodeMarking em = new(epi, date, type);
        var content = em.GetJson();
        _log.Write($"TVMaze Put Async with {epi} {date} {type} turned into {content}", "", 4);

        var t = PerformPutTvmApiAsync(api, content);
        t.Wait();

        stopwatch.Stop();
        _log.Write($"TVMApi Exec time: {stopwatch.ElapsedMilliseconds} ms.", "", 4);

        if (!_httpResponse.IsSuccessStatusCode)
        {
            _log.Write($"Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}", "WebAPI Put Exec", 4);
            _httpResponse = new HttpResponseMessage();
            //Console.WriteLine("########### Aborting from PerformPutTvmApiAsync ###################");
            //Environment.Exit(99);
        }
    }

    private async Task PerformPutTvmApiAsync(string api, string json)
    {
        StringContent stringContent = new(json, Encoding.UTF8, "application/json");
        _log.Write($"json content now is {json} for api {_client.BaseAddress + api}", "WebAPI PPTAA", 4);

        try
        {
            _httpResponse = await _client.PutAsync(_client.BaseAddress + api, stringContent).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _log.Write($"Exception: {e.Message} for {api}", "WebAPI Put Async");
        }
    }

    private void SetTvmaze()
    {
        if (!_tvmazeUrlInitialized)
        {
            _client.BaseAddress = new Uri(TvmazeUrl);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
            _client.Timeout = TimeSpan.FromSeconds(30);
            _tvmazeUrlInitialized = true;
        }
    }

    private void SetTvmazeUser()
    {
        if (!_tvmazeUserUrlInitialized)
        {
            _client.BaseAddress = new Uri(TvmazeUserUrl);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Add("Authorization", _tvmazeSecurity);
            _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
            _client.Timeout = TimeSpan.FromSeconds(30);
            _tvmazeUserUrlInitialized = true;
        }
    }

    public HttpResponseMessage GetShow(string showName)
    {
        SetTvmaze();

        var api = $"search/shows?q={showName}";
        _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GS", 4);

        PerformWaitTvmApi(api);

        return _httpResponse;
    }

    public HttpResponseMessage GetShow(int showid)
    {
        SetTvmaze();
        var api = $"shows/{showid}";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GS Int", 4);

        return _httpResponse;
    }

    public HttpResponseMessage GetEpisodesByShow(int showid)
    {
        SetTvmaze();
        var api = $"shows/{showid}/episodes";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GEBS", 4);

        return _httpResponse;
    }

    public HttpResponseMessage GetAllEpisodes()
    {
        SetTvmazeUser();
        var api = $"episodes";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GEBS", 4);

        return _httpResponse;
    }

    public HttpResponseMessage GetShowUpdateEpochs(string period)
    {
        // day, week, month option for period
        SetTvmaze();
        var api = $"updates/shows?since={period}";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GSUE", 4);

        return _httpResponse;
    }

    public HttpResponseMessage GetFollowedShows()
    {
        SetTvmazeUser();
        var api = "follows/shows";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GFS", 4);

        return _httpResponse;
    }

    public bool CheckForFollowedShow(int showid)
    {
        var isFollowed = false;
        SetTvmazeUser();
        var api = $"follows/shows/{showid}";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GFS", 4);
        if (_httpResponse.IsSuccessStatusCode) isFollowed = true;
        return isFollowed;
    }

    #endregion

    #region Tvmaze Episode APIs

    public HttpResponseMessage GetEpisode(int episodeid)
    {
        SetTvmaze();
        var api = $"episodes/{episodeid}?embed=show";
        _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI G Epi", 4);
        PerformWaitTvmApi(api);

        return _httpResponse;
    }

    public HttpResponseMessage GetEpisodeMarks(int episodeid)
    {
        SetTvmazeUser();
        var api = $"episodes/{episodeid}";
        _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GM Epi", 4);
        PerformWaitTvmApi(api);

        /*
         * 0 = watched, 1 = acquired, 2 = skipped 
        */

        return _httpResponse;
    }

    public HttpResponseMessage PutEpisodeToWatched(int episodeid, string watcheddate = "")
    {
        SetTvmazeUser();
        var api = $"episodes/{episodeid}";
        if (watcheddate == "") watcheddate = DateTime.Now.ToString("yyyy-MM-dd");
        PerformWaitPutTvmApiAsync(api, episodeid, watcheddate, "Watched");

        return _httpResponse;
    }

    public HttpResponseMessage PutEpisodeToAcquired(int episodeid, string acquiredate = "")
    {
        SetTvmazeUser();
        var api = $"episodes/{episodeid}";
        if (acquiredate == "") acquiredate = DateTime.Now.ToString("yyyy-MM-dd");
        PerformWaitPutTvmApiAsync(api, episodeid, acquiredate, "Acquired");

        return _httpResponse;
    }

    public HttpResponseMessage PutEpisodeToSkipped(int episodeid, string skipdate = "")
    {
        SetTvmazeUser();
        var api = $"episodes/{episodeid}";
        if (skipdate == "") skipdate = DateTime.Now.ToString("yyyy-MM-dd");
        PerformWaitPutTvmApiAsync(api, episodeid, skipdate, "Skipped");

        return _httpResponse;
    }

    #endregion

    #region Scrape APIs

    #region RarbgAPI

    public HttpResponseMessage GetRarbgMagnets(string searchfor)
    {
        _rarbgClient = new HttpClient();
        _rarbgClient.BaseAddress = new Uri(RarbgApiUrlPre);
        _rarbgClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var productvalue = new ProductInfoHeaderValue("Safari", "13.0");
        _rarbgClient.DefaultRequestHeaders.UserAgent.Add(productvalue);
        var t = GetShowRarbg(searchfor);
        t.Wait();
        return _httpResponse;
    }

    public async Task GetShowRarbg(string searchfor)
    {
        try
        {
            var url = GetRarbgMagnetsApi(searchfor);
            _httpResponse = await _rarbgClient.GetAsync(url).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _log.Write($"Exception {e.Message}", "WebAPI Rarbg", 0);
        }
    }

    private string GetRarbgMagnetsApi(string searchFor)
    {
        var api = $"{RarbgApiUrlPre}{Common.RemoveSpecialCharsInShowName(searchFor)}{_rarbgApiUrlSuf}";
        _log.Write($"API String = {api}", "RarbgAPI", 4);
        return api;
    }

    #endregion

    #endregion
}

[SuppressMessage("ReSharper", "NotAccessedField.Global")]
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
