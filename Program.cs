using System;
using System.Linq;
using System.Diagnostics;

namespace TorFileDownload
{
    class Program
    {
        static void Main()
        {
            Process.GetProcessesByName(Constants.TOR_PROCESS_NAME).ToList().ForEach(p => p.Kill());
            new MainProcess().Execute();
            Console.ReadKey();
        }
    }
}
