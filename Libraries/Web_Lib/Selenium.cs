using System;
using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;

namespace Web_Lib;

public class Selenium : IDisposable
{
    public Selenium(string program)
    {
        Service.HideCommandPromptWindow              = true;
        Service.SuppressInitialDiagnosticInformation = true;
        ThisProgram                                  = program;
    }

    private string              ThisProgram   { get; set; }
    public  HtmlDocument?       HtmlDoc       { get; set; } = new();
    private ChromeDriverService Service       { get; }      = ChromeDriverService.CreateDefaultService();
    public  ChromeDriver?       ChromeDriver  { get; set; }
    private ChromeOptions       ChromeOptions { get; } = new();
    private bool                Started       { get; set; }

    public void Dispose()
    {
        if (Started)
        {
            ChromeDriver!.Quit();
            ChromeDriver = null;
            Started      = false;
        }

        GC.SuppressFinalize(this);
    }

    public void Start()
    {
        ChromeOptions.AddArgument("--headless");
        ChromeOptions.AddArgument("--whitelisted-ips=''");
        ChromeOptions.AddArgument("--disable-dev-shm-usage");
        ChromeOptions.AddArgument("--disable-popup-blocking");
        ChromeOptions.AcceptInsecureCertificates = true;
        ChromeDriver                             = new ChromeDriver(ChromeOptions);
        Started                                  = true;
    }

    public void Stop()
    {
        if (Started)
        {
            ChromeDriver!.Quit();
            ChromeDriver = null;
            Started      = false;
        }
    }

    public HtmlDocument GetPage(string url)
    {
        if (Started)
        {
            ChromeDriver!.Navigate().GoToUrl(url);
            var htmlString = ChromeDriver.PageSource;
            HtmlDoc!.LoadHtml(htmlString);

            return HtmlDoc;
        }

        return HtmlDoc!;
    }
}
