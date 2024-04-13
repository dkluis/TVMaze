using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common_Lib;
using DB_Lib_EF.Models.MariaDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Web_Lib.DTOs;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Web_Lib;

public class TvmApi : IDisposable
{
    private const    string              TvmazeUrl     = "https://api.tvmaze.com/";
    private const    string              TvmazeUserUrl = "https://api.tvmaze.com/v1/user/";
    private readonly HttpClient          _client       = new();
    private readonly TextFileHandler     _log;
    private readonly string              _tvmazeSecurity;
    private          bool                _disposed;
    private          HttpResponseMessage _httpResponse = new();
    private          bool                _isTimedOut;
    private          string              _thisProgram;
    private          bool                _tvmazeUrlInitialized;
    private          bool                _tvmazeUserUrlInitialized;

    public TvmApi(AppInfo appInfo)
    {
        _log         = new AppInfo("", "", "").TxtFile;
        _thisProgram = appInfo.Program;
        using var db = new TvMaze();
        _tvmazeSecurity = db.Configurations.Single(c => c.Key == "TvMazeToken").Value;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _client?.Dispose();
            _httpResponse?.Dispose();
        }

        _disposed = true;
    }

    ~TvmApi() { Dispose(false); }

    public string EpisodeMarking(int epi, string date, string ty = "")
    {
        var epiMark = new EpisodeMarkDto {EpisodeId = epi, MarkedAt = Common.ConvertDateToEpoch(date)};

        epiMark.Type = ty switch
                       {
                           "Watched" => 0, "Acquired" => 1, "Skipped" => 2, _ => 0,
                       };

        return JsonSerializer.Serialize(epiMark);
    }

    public string ShowToFollowed(int showId) { return JsonSerializer.Serialize(showId); }

    #region TVMaze Show APIs

    private void PerformWaitTvmApi(string api)
    {
        var execTime = new Stopwatch();
        execTime.Start();

        var t = PerformTvmApiAsync(api);
        t.Wait();

        execTime.Stop();
        _log.Write($"TVMApi Exec time: {execTime.ElapsedMilliseconds} ms.", "", 4);

        if (_isTimedOut)
        {
            _log.Write($"TimedOut --> Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}", "WebAPI Exec");
            _httpResponse = new HttpResponseMessage();
            Console.WriteLine("########### Aborting from PerformWaitTvmApi IsTimedOut is True ###################");
            Environment.Exit(99);
        } else if (!_httpResponse.IsSuccessStatusCode)
        {
            _log.Write($"Status Code --> Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}", "WebAPI Exec", 4);
            _httpResponse = new HttpResponseMessage();
        }
    }

    private async Task PerformTvmApiAsync(string api)
    {
        try
        {
            _httpResponse = await _client.GetAsync(api).ConfigureAwait(false);
            _isTimedOut   = false;
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
                    _isTimedOut   = true;
                    Console.WriteLine("########### Aborting from PerformTvmApiAsync 2nd Exception ###################");
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

        EpisodeMarking em      = new(epi, date, type);
        var            content = em.GetJson();
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

    private void PerformWaitPutShowTvmApiAsync(string api, int show)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        //ShowToFollowed stf     = new(show);
        var content = ""; //stf.GetJson();
        _log.Write($"TVMaze Put Async with {show} turned into {content}", "", 4);

        var t = PerformPutTvmApiAsync(api, content);
        t.Wait();

        stopwatch.Stop();
        _log.Write($"TVMApi Exec time: {stopwatch.ElapsedMilliseconds} ms.", "", 4);

        if (_httpResponse.IsSuccessStatusCode) return;

        _log.Write($"Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}", "WebAPI Put Exec", 4);
        _httpResponse = new HttpResponseMessage();
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

    private void PerformWaitDeleteShowTvmApiAsync(string api, int show)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        //ShowToFollowed stf     = new(show);
        var content = ""; //stf.GetJson();
        _log.Write($"TVMaze Put Async with {show} turned into {content}", "", 4);

        var t = PerformDeleteTvmApiAsync(api, content);
        t.Wait();

        stopwatch.Stop();
        _log.Write($"TVMApi Exec time: {stopwatch.ElapsedMilliseconds} ms.", "", 4);

        if (_httpResponse.IsSuccessStatusCode) return;

        _log.Write($"Http Response Code is: {_httpResponse.StatusCode} for API {_client.BaseAddress}{api}", "WebAPI Put Exec", 4);
        _httpResponse = new HttpResponseMessage();
    }

    private async Task PerformDeleteTvmApiAsync(string api, string json)
    {
        _log.Write($"json content now is {json} for api {_client.BaseAddress + api}", "WebAPI PPTAA", 4);

        try
        {
            _httpResponse = await _client.DeleteAsync(_client.BaseAddress + api).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _log.Write($"Exception: {e.Message} for {api}", "WebAPI Put Async");
        }
    }

    private void SetTvmaze()
    {
        if (_tvmazeUrlInitialized) return;
        _client.BaseAddress = new Uri(TvmazeUrl);

        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
        _client.Timeout       = TimeSpan.FromSeconds(30);
        _tvmazeUrlInitialized = true;
    }

    private void SetTvmazeUser()
    {
        if (_tvmazeUserUrlInitialized) return;
        _client.BaseAddress = new Uri(TvmazeUserUrl);

        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.Add("Authorization", _tvmazeSecurity);
        _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Tvmaze C# App");
        _client.Timeout           = TimeSpan.FromSeconds(30);
        _tvmazeUserUrlInitialized = true;
    }

    public HttpResponseMessage GetShow(string showName)
    {
        SetTvmaze();

        var api = $"search/shows?q={showName}";
        _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GS", 4);

        PerformWaitTvmApi(api);

        return _httpResponse;
    }

    public ShowDto? GetShow(int showId)
    {
        SetTvmaze();
        var api = $"shows/{showId}";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GS Int", 4);
        var json = _httpResponse.Content.ReadAsStringAsync().Result;

        return JsonConvert.DeserializeObject<ShowDto>(json.Replace("_links", "links").Replace("_embedded", "embedded"));

        //return ConvertHttpToJObject(_httpResponse);
    }

    public EpisodeDto[]? GetEpisodesByShow(int showId)
    {
        SetTvmaze();
        var api = $"shows/{showId}/episodes";
        PerformWaitTvmApi(TvmazeUrl + api);
        _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GEBS", 4);
        var json = _httpResponse.Content.ReadAsStringAsync().Result;

        return JsonConvert.DeserializeObject<EpisodeDto[]>(json.Replace("_links", "links").Replace("_embedded", "embedded"));
    }

    public HttpResponseMessage GetAllMyEpisodes()
    {
        SetTvmazeUser();
        var api = "episodes";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GEBS", 4);

        return _httpResponse;
    }

    public ShowEpochDto? GetShowUpdateEpochs(string period)
    {
        // day, week, month option for period
        SetTvmaze();
        var api = $"updates/shows?since={period}";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI GSUE", 4);
        var json = _httpResponse.Content.ReadAsStringAsync().Result;

        return JsonConvert.DeserializeObject<ShowEpochDto>(json);
    }

    public JArray GetFollowedShows()
    {
        SetTvmazeUser();
        var api = "follows/shows";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GFS", 4);

        return new JArray(); //ConvertHttpToJArray(_httpResponse);
    }

    public bool CheckForFollowedShow(int showId)
    {
        var isFollowed = false;
        SetTvmazeUser();
        var api = $"follows/shows/{showId}";
        PerformWaitTvmApi(api);
        _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GFS", 4);
        if (_httpResponse.IsSuccessStatusCode) isFollowed = true;

        return isFollowed;
    }

    public HttpResponseMessage PutShowToFollowed(int showId)
    {
        SetTvmazeUser();
        var api = $"follows/shows/{showId}";
        PerformWaitPutShowTvmApiAsync(api, showId);

        return _httpResponse;
    }

    public HttpResponseMessage PutShowToUnfollowed(int showId)
    {
        SetTvmazeUser();
        var api = $"follows/shows/{showId}";
        PerformWaitDeleteShowTvmApiAsync(api, showId);

        return _httpResponse;
    }

    #endregion

    #region Tvmaze Episode APIs

    public EpiShowDto? GetEpisode(int episodeid)
    {
        SetTvmaze();
        var api = $"episodes/{episodeid}?embed=show";
        _log.Write($"API String = {TvmazeUrl}{api}", "WebAPI G Epi", 4);
        PerformWaitTvmApi(api);
        var json = _httpResponse.Content.ReadAsStringAsync().Result;

        return JsonConvert.DeserializeObject<EpiShowDto>(json.Replace("_links", "links").Replace("_embedded", "embedded"));
    }

    public EpisodeMarkDto? GetEpisodeMarks(int episodeid)
    {
        SetTvmazeUser();
        var api = $"{TvmazeUserUrl}episodes/{episodeid}";
        _log.Write($"API String = {TvmazeUserUrl}{api}", "WebAPI GM Epi", 4);
        PerformWaitTvmApi(api);
        var json = _httpResponse.Content.ReadAsStringAsync().Result;

        /*
         * 0 = watched, 1 = acquired, 2 = skipped
         */
        return JsonSerializer.Deserialize<EpisodeMarkDto>(json.Replace("_links", "links").Replace("_embedded", "embedded"));
    }

    public HttpResponseMessage PutEpisodeToWatched(int episodeId, string watchedDate = "")
    {
        SetTvmazeUser();
        var api                            = $"episodes/{episodeId}";
        if (watchedDate == "") watchedDate = DateTime.Now.ToString("yyyy-MM-dd");
        PerformWaitPutTvmApiAsync(api, episodeId, watchedDate, "Watched");

        return _httpResponse;
    }

    public HttpResponseMessage PutEpisodeToAcquired(int episodeid, string acquiredate = "")
    {
        SetTvmazeUser();
        var api                            = $"episodes/{episodeid}";
        if (acquiredate == "") acquiredate = DateTime.Now.ToString("yyyy-MM-dd");
        PerformWaitPutTvmApiAsync(api, episodeid, acquiredate, "Acquired");

        return _httpResponse;
    }

    public HttpResponseMessage PutEpisodeToSkipped(int episodeid, string skipdate = "")
    {
        SetTvmazeUser();
        var api                      = $"episodes/{episodeid}";
        if (skipdate == "") skipdate = DateTime.Now.ToString("yyyy-MM-dd");
        PerformWaitPutTvmApiAsync(api, episodeid, skipdate, "Skipped");

        return _httpResponse;
    }

    #endregion
}
