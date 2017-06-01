using System;

namespace CLOSER_Repository_Ingester
{
    class Program
    {
        static void Main(string[] args)
        {
            string buildDirectory = null;
            string controlFile = null;
            var keepGoing = false;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-b")
                {
                    buildDirectory = args[i + 1];
                    i++;
                    continue;
                }
                if (args[i] == "-c")
                {
                    controlFile = args[i + 1];
                    i++;
                }
                if (args[i] == "-y")
                {
                    keepGoing = true;
                }
            }
            if (controlFile == null)
            {
                Console.WriteLine("No control file was specified.");
            }
            else
            {
                Console.SetWindowSize(
                    Math.Min(150, Console.LargestWindowWidth),
                    Math.Min(60, Console.LargestWindowHeight)
                    );
                Ingester ingester = new Ingester();

                ingester.Init(controlFile, keepGoing);
                Console.WriteLine("Ingester has been initalised");

                if (ingester.Prepare(buildDirectory))
                {
                    Console.WriteLine("Ingester has been prepared.");
                    //ingester.RunGlobalActions();
                    ingester.RunByGroup();
                }
                else
                {
                    Console.WriteLine("Failed to prepare build.");
                }

                Console.WriteLine("Finished. Press enter to exit");
            }

            Console.ReadLine();
        }
    }
}
