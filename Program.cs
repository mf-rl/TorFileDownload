using System;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace TorFileDownload
{
    class Program
    {
        static void Main()
        {
            Console.Title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            Process.GetProcessesByName(Constants.TOR_PROCESS_NAME).ToList().ForEach(p => p.Kill());
            MainProcess.Execute();
            Console.ReadKey();
        }
    }
}
