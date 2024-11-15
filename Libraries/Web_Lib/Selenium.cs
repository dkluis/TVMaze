using System;
using System.IO;
using System.Threading;
using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;

namespace Web_Lib;

public class Selenium : IDisposable
{
    public Selenium(string program)
    {
        Service.HideCommandPromptWindow = true;
        Service.SuppressInitialDiagnosticInformation = true;
        ThisProgram = program;
    }

    private string ThisProgram { get; set; }
    private HtmlDocument? HtmlDoc { get; set; } = new();
    private ChromeDriverService Service { get; } = ChromeDriverService.CreateDefaultService();
    private ChromeDriver? ChromeDriver { get; set; }
    private ChromeOptions ChromeOptions { get; } = new();
    private bool Started { get; set; }

    public void Dispose()
    {
        if (Started)
        {
            ChromeDriver!.Quit();
            ChromeDriver = null;
            Started = false;
        }

        GC.SuppressFinalize(this);
    }

    public void Start()
    {
        ChromeOptions.AddArgument("--headless");
        Service.SuppressInitialDiagnosticInformation = true;
        Service.HideCommandPromptWindow = true;
        //Service.LogPath = Path.GetTempFileName();
        ChromeOptions.AddArgument("--whitelisted-ips=''");
        ChromeOptions.AddArgument("--disable-dev-shm-usage");
        ChromeOptions.AddArgument("--disable-popup-blocking");
        ChromeOptions.AcceptInsecureCertificates = true;
        ChromeOptions.BinaryLocation = @"/Applications/Google Chrome.app";
        ChromeDriver = new ChromeDriver(Service, ChromeOptions);
        Started = true;
    }

    public void Stop()
    {
        if (!Started)
        {
            return;
        }
        ChromeDriver!.Quit();
        Thread.Sleep(6000);
        ChromeDriver = null;
        Started = false;
        Dispose();
    }

    public HtmlDocument GetPage(string url)
    {
        if (!Started)
        {
            return HtmlDoc!;
        }
        ChromeDriver!.Navigate().GoToUrl(url);
        var htmlString = ChromeDriver.PageSource;
        HtmlDoc!.LoadHtml(htmlString);

        return HtmlDoc;
    }
}
