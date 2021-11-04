using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TVMazeWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:5001", "http://192.168.142.80:5001",
                        "http://ca-server.local:5001");
                });
        }
    }
}