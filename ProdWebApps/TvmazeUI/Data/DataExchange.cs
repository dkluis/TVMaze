namespace TvmazeUI.Data;

public class DataExchange
{
    public string? EpisodeStateEpisode;
    public string? EpisodeStateFunction;
    public bool    EpisodeStateIncludeShowRss;
    public bool    EpisodeStateIsActive;
    public string? EpisodeStateSeason;
    public string? EpisodeStateShowName;
    public string? FunctionRequested;
    public string? IntendedPage;
    public string? LastPage;
    public string? SendingPage;
    public string? ShowName;
    public string? ShowStateFunction;
    public bool    ShowStateIsActive;
    public string? ShowStateShowName;

    public void Reset()
    {
        SendingPage  = "";
        IntendedPage = "";
        ShowName     = "";
    }

    public void ResetShowState()
    {
        ShowStateFunction = "";
        ShowStateShowName = "";
        ShowStateIsActive = false;
    }

    public void ResetEpisodeState()
    {
        EpisodeStateFunction       = "";
        EpisodeStateSeason         = "";
        EpisodeStateEpisode        = "";
        EpisodeStateShowName       = "";
        EpisodeStateIncludeShowRss = false;
        EpisodeStateIsActive       = false;
    }
}
