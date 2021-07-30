using System;
using System.Configuration;

namespace Common_Lib
{
    public class Common
    {
        public string ReadConfig(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "Not Found";
                Console.WriteLine(result);
                return result;
            }
            catch (ConfigurationErrorsException e)
            {
                Console.WriteLine("Error reading app settings");
                return e.BareMessage;
            }
        }

    }
}
