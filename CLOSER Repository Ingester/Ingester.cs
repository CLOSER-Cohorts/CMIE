using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Utility;

using CLOSER_Repository_Ingester.ControllerSystem;

namespace CLOSER_Repository_Ingester
{
    class Ingester
    {
        string controlFilepath;
        Controller controller;
        public Ingester() {}


        public void Init(string controlFilepath) 
        {
            MultilingualString.CurrentCulture = "en-GB";
            VersionableBase.DefaultAgencyId = "uk.closer";
            this.controlFilepath = controlFilepath;
        }

        public bool Prepare()
        {
            this.controller = new Controller(this.controlFilepath);
            try
            {
                this.controller.loadFile();
            } catch (Exception e) {
                Console.WriteLine("{0}", e);
            }
            return true;
        }

        public void Build()
        {
            foreach (Group group in this.controller.groups)
            {
                group.Build();
                foreach (string scope in group.GetAllScopes())
                {
                    Console.WriteLine(scope);
                }
            }
        }

        public void CompareWithRepository()
        {

        }

        public void Run()
        {
            if (Prepare())
            {
                Build();
            }
        }
    }
}
