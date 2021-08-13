using Newtonsoft.Json.Linq;


namespace Common_Lib
{
    public class ConvertJsonTxt
    {

        public static JArray ConvertStringToJArray(string message)
        { 
            if (message == "")
            {
                JArray empty = new();
                return empty;
            }
            JArray jarray = JArray.Parse(message);
            return jarray;
        }

    }
}
