using SysCon = System.Console;

namespace CMIE
{
    internal class Program
    {
        private static void Main(string[] args)
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
