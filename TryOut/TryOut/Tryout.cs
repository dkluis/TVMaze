using System;
using Common_Lib;
using Entities_Lib;
using Web_Lib;

using System.Collections.Generic;
using System.IO;

namespace TryOut
{
    class TryingOut
    {
        static void Main()
        {
            string This_Program = "Trying Out";
            Console.WriteLine($"{DateTime.Now}: {This_Program} Started");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            Console.WriteLine($"{DateTime.Now}: {This_Program} Progress can be followed in {appinfo.FullPath}");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            string from = "/Volumes/HD-Data-CA-Server/PlexMedia/PlexProcessing/TVMaze/TransmissionFiles/What.If.2021.S01E04.1080p.DSNP.WEBRip.DDP5.1.Atmos.x264-FLUX";
            string fromfinal = from;
            string[] frompieces = from.Split("[");
            if (frompieces.Length == 2)
            {
                fromfinal = frompieces[0];
                Directory.Move(from, fromfinal);
            }
            string[] fileindir = Directory.GetFiles(fromfinal);

            foreach (string file in fileindir)
            {
                log.Write($"File is: {file}");
                if (file.ToLower().Contains(".ds_store")) { continue; }
                File.Move(file, $"/Volumes/HD-Data-CA-Server/PlexMedia/PlexProcessing/TVMaze/TransmissionFiles/" +
                    $"{file.Replace("/Volumes/HD-Data-CA-Server/PlexMedia/PlexProcessing/TVMaze/TransmissionFiles/What.If.2021.S01E04.1080p.DSNP.WEBRip.DDP5.1.Atmos.x264-FLUX", "")}", true);
            }





            log.Stop();
            Console.WriteLine($"{DateTime.Now}: {This_Program} Finished");
        }
    }
}