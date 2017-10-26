using System;
using SysCon = System.Console;

using Algenta.Colectica.Model.Utility;

namespace CMIE
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new Application(args);
            if (app.Initialize())
            {
                app.Run();
                SysCon.WriteLine("CMIE has shutdown. Press any key to close.");
            }
            else
            {
                SysCon.ReadLine();
            }
            SysCon.ReadKey();
        }
    }
}
