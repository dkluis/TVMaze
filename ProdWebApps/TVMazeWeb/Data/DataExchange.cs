namespace TVMazeWeb.Data
{
    public class DataExchange
    {
        public int Episode;
        public string FunctionRequested;
        public string IntendedPage;
        public int Season;
        public string SendingPage;
        public string ShowName;

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