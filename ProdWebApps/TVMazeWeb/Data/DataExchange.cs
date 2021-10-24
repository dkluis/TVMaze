using System;
namespace TVMazeWeb.Data
{
    public class DataExchange
    {
        public string SendingPage;
        public string IntendedPage;
        public string FunctionRequested;
        public string ShowName;
        public int Season;
        public int Episode;

        public void Reset()
        {
            SendingPage = "";
            IntendedPage = "";
            ShowName = "";
            Season = 0;
            Episode = 0;
        }
    }
}
