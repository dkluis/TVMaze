using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Common_Lib;

using DB_Lib_EF.Entities;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Log = DB_Lib_EF.Models.MariaDB.Log;

namespace Web_Lib;

public class WebApi : IDisposable
{
    private const    string              TvmazeUrl     = "https://api.tvmaze.com/";
    private const    string              TvmazeUserUrl = "https://api.tvmaze.com/v1/user/";
    private readonly HttpClient          _client       = new();
    private readonly string              _tvmazeSecurity;
    private          HttpResponseMessage _httpResponse = new();
    private          bool                _tvmazeUrlInitialized;
    private          bool                _tvmazeUserUrlInitialized;
    public           bool                IsTimedOut;
    private          string              _thisProgram;
    private const    string              ThisFunction = "WebApi";

    public WebApi(AppInfo appInfo)
    {
        _thisProgram = appInfo.Program;

        //_Torrentz2ApiUrlSuf = appInfo.Torrentz2Token;
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

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"TVMApi Exec time: {execTime.ElapsedMilliseconds} ms for {_client.BaseAddress}{api}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);

        if (IsTimedOut)
        {
            logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"TimedOut --> Http Response Code is: {_httpResponse.StatusCode} {_httpResponse.Content} for API {_client.BaseAddress}{api}",
                         Level        = 5,
                     };
            LogModel.Record(logRec);
            _httpResponse = new HttpResponseMessage();

            return;
        } else if (!_httpResponse.IsSuccessStatusCode)
        {
            logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"Http Response Code is: {_httpResponse.StatusCode} {_httpResponse.Content} for API {_client.BaseAddress}{api}",
                         Level        = 5,
                     };
            LogModel.Record(logRec);
            _httpResponse = new HttpResponseMessage();
        }

        logRec = new Log()
                 {
                     RecordedDate = DateTime.Now,
                     Program      = _thisProgram,
                     Function     = ThisFunction,
                     Message      = $"Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}",
                     Level        = 5,
                 };
        LogModel.Record(logRec);
    }

    private async Task PerformTvmApiAsync(string api)
    {
        try
        {
            _httpResponse = await _client.GetAsync(api).ConfigureAwait(false);
            IsTimedOut    = false;
        }
        catch (Exception e)
        {
            var logRec = new Log()
                         {
                             RecordedDate = DateTime.Now,
                             Program      = _thisProgram,
                             Function     = ThisFunction,
                             Message      = $"Exception: {e.Message} ::: {e.InnerException}",
                             Level        = 20,
                         };
            LogModel.Record(logRec);

            if (e.Message.Contains(" seconds elapsing") || e.Message.Contains("Operation timed out"))
            {
                logRec = new Log()
                         {
                             RecordedDate = DateTime.Now,
                             Program      = _thisProgram,
                             Function     = ThisFunction,
                             Message      = $"Retrying Now: {api}",
                             Level        = 5,
                         };
                LogModel.Record(logRec);

                try
                {
                    _httpResponse = await _client.GetAsync(api).ConfigureAwait(false);
                }
                catch (Exception ee)
                {
                    logRec = new Log()
                             {
                                 RecordedDate = DateTime.Now,
                                 Program      = _thisProgram,
                                 Function     = ThisFunction,
                                 Message      = $"2nd Exception: {ee.Message} ::: {ee.InnerException}",
                                 Level        = 20,
                             };
                    LogModel.Record(logRec);
                    _httpResponse = new HttpResponseMessage();
                    IsTimedOut    = true;
                }
            }
        }
        finally
        {
            _client.Dispose();
        }
    }

    private void PerformWaitPutTvmApiAsync(string api, int epi, string? date, string type = "")
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        EpisodeMarking em      = new(epi, date, type);
        var            content = em.GetJson();
        LogModel.Record(_thisProgram, ThisFunction, $"TVMaze Put Async with {epi} {date} {type} turned into {content}", 5);
        var t = PerformPutTvmApiAsync(api, content);
        t.Wait();

        stopwatch.Stop();

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"TVMApi Exec time: {stopwatch.ElapsedMilliseconds} ms. or {api} with {content}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);

        if (!_httpResponse.IsSuccessStatusCode)
        {
            logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}",
                         Level        = 5,
                     };
            LogModel.Record(logRec);
            _httpResponse = new HttpResponseMessage();
        }
    }

    private async Task PerformPutTvmApiAsync(string api, string json)
    {
        StringContent stringContent = new(json, Encoding.UTF8, "application/json");

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"json content now is {json} for api {_client.BaseAddress + api}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);

        try
        {
            _httpResponse = await _client.PutAsync(_client.BaseAddress + api, stringContent).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"Exception: for {api} WebAPI Put Async {e.Message} ::: {e.InnerException}",
                         Level        = 20,
                     };
            LogModel.Record(logRec);
        }
    }

    private void SetTvmaze()
    {
        if (!_tvmazeUrlInitialized)
        {
            _client.BaseAddress = new Uri(TvmazeUrl);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
            _client.Timeout       = TimeSpan.FromSeconds(30);
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
            _client.Timeout           = TimeSpan.FromSeconds(30);
            _tvmazeUserUrlInitialized = true;
        }
    }

    public HttpResponseMessage GetShow(int showid)
    {
        SetTvmaze();
        var api = $"shows/{showid}";
        PerformWaitTvmApi(api);

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);

        return _httpResponse;
    }

    public HttpResponseMessage GetEpisodesByShow(int showid)
    {
        SetTvmaze();
        var api = $"shows/{showid}/episodes";
        PerformWaitTvmApi(api);

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);

        return _httpResponse;
    }

    public HttpResponseMessage GetAllEpisodes()
    {
        SetTvmazeUser();
        var api = "episodes";
        PerformWaitTvmApi(api);

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);

        return _httpResponse;
    }

    public HttpResponseMessage GetShowUpdateEpochs(string period)
    {
        // day, week, month option for period
        SetTvmaze();
        var api = $"updates/shows?since={period}";
        PerformWaitTvmApi(api);

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);

        //var json = _httpResponse.Content.ReadAsStringAsync().Result;

        return _httpResponse;
    }

    public HttpResponseMessage GetFollowedShows()
    {
        SetTvmazeUser();
        var api = "follows/shows";
        PerformWaitTvmApi(api);

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);

        return _httpResponse;
    }

    public bool CheckForFollowedShow(int showid)
    {
        var isFollowed = false;
        SetTvmazeUser();
        var api = $"follows/shows/{showid}";
        PerformWaitTvmApi(api);

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);
        if (_httpResponse.IsSuccessStatusCode) isFollowed = true;

        return isFollowed;
    }

    #endregion

    #region Tvmaze Episode APIs

    public HttpResponseMessage GetEpisode(int episodeId)
    {
        SetTvmaze();
        var api = $"episodes/{episodeId}?embed=show";

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);
        PerformWaitTvmApi(api);

        return _httpResponse;
    }

    public HttpResponseMessage GetEpisodeMarks(int episodeId)
    {
        SetTvmazeUser();
        var api = $"episodes/{episodeId}";

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);
        PerformWaitTvmApi(api);

        /*
         * 0 = watched, 1 = acquired, 2 = skipped
         */

        return _httpResponse;
    }

    public HttpResponseMessage PutEpisodeToWatched(int episodeId, string? watchedDate = "")
    {
        SetTvmazeUser();
        var api                            = $"episodes/{episodeId}";
        if (watchedDate == "") watchedDate = DateTime.Now.ToString("yyyy-MM-dd");

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api} with watched date {watchedDate}",
                         Level        = 5,
                     };
        LogModel.Record(logRec);
        PerformWaitPutTvmApiAsync(api, episodeId, watchedDate, "Watched");

        return _httpResponse;
    }

    public HttpResponseMessage PutEpisodeToAcquired(int episodeId, string? acquireDate = "")
    {
        SetTvmazeUser();
        var api                            = $"episodes/{episodeId}";
        if (acquireDate == "") acquireDate = DateTime.Now.ToString("yyyy-MM-dd");

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api} with watched date {acquireDate}",
                         Level        = 5,
                     };
        PerformWaitPutTvmApiAsync(api, episodeId, acquireDate, "Acquired");

        return _httpResponse;
    }

    public HttpResponseMessage PutEpisodeToSkipped(int episodeId, string? skipDate = "")
    {
        SetTvmazeUser();
        var api                      = $"episodes/{episodeId}";
        if (skipDate == "") skipDate = DateTime.Now.ToString("yyyy-MM-dd");

        var logRec = new Log()
                     {
                         RecordedDate = DateTime.Now,
                         Program      = _thisProgram,
                         Function     = ThisFunction,
                         Message      = $"API String = {TvmazeUrl}{api} with watched date {skipDate}",
                         Level        = 5,
                     };
        PerformWaitPutTvmApiAsync(api, episodeId, skipDate, "Skipped");

        return _httpResponse;
    }

    #endregion
}

[SuppressMessage("ReSharper", "NotAccessedField.Global")]
public class EpisodeMarking
{
    public int episode_id;
    public int marked_at;
    public int type;

    public EpisodeMarking(int epi, string? date, string ty = "")
    {
        episode_id = epi;
        marked_at  = Common.ConvertDateToEpoch(date);

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
