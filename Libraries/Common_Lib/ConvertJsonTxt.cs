using Newtonsoft.Json.Linq;

namespace Common_Lib;

public static class ConvertJsonTxt
{
    public static JArray ConvertStringToJArray(string message)
    {
        if (message == "")
        {
            JArray empty = new();
            return empty;
        }

        var jA = JArray.Parse(message);
        return jA;
    }
    public static JObject ConvertStringToJObject(string message)
    {
        if (message == "")
        {
            JObject empty = new();
            return empty;
        }

        var jO = JObject.Parse(message);
        return jO;
    }
}
