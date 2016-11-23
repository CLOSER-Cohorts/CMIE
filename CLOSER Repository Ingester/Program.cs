using System;

namespace CLOSER_Repository_Ingester
{
    class Program
    {
        static void Main(string[] args)
        {
            string buildDirectory = null;
            string controlFile = null;
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
            }
            if (controlFile == null)
            {
                Console.WriteLine("No control file was specified.");
            }
            else
            {
                Ingester ingester = new Ingester();

                ingester.Init(controlFile);

                if (ingester.Prepare(buildDirectory))
                {
                    ingester.Build();
                    ingester.CompareWithRepository();

                    Console.WriteLine("About to commit to repository, do you want to continue? (y/N)");
                    string response = Console.ReadLine().ToLower();
                    if (response[0].Equals('y'))
                    {
                        Console.Write("Committing ");
                        ingester.Commit();
                        Console.WriteLine("Done.");
                    }
                    else
                    {
                        Console.Write("No changes committed.");
                    }
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
