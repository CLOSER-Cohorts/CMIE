using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLOSER_Repository_Ingester
{
    class Program
    {
        static void Main(string[] args)
        {
            Ingester ingester = new Ingester();

            ingester.Init(@"d:\closer\development\will\ingest\control.txt");

            if (ingester.Prepare())
            {
                ingester.Build();
            }
            else
            {

            }

            Console.WriteLine("Finished. Press enter to exit");
            Console.ReadLine();
        }
    }
}
