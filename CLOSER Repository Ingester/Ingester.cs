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

        public void Init(string controlFilepath) 
        {
            MultilingualString.CurrentCulture = "en-GB";
            VersionableBase.DefaultAgencyId = "uk.closer";
            this.controlFilepath = controlFilepath;
        }

        public void SetBasePath(string basePath)
        {
            controller.basePath = basePath;
        }

        public bool Prepare(string basePath = null)
        {
            controller = new Controller(controlFilepath);
            controller.basePath = basePath;

            try
            {
                controller.loadFile();
            } catch (Exception e) {
                Console.WriteLine("{0}", e);
            }
            return true;
        }

        public void Build()
        {
            foreach (var group in controller.groups)
            {
                group.Build();
            }
        }

        public void CompareWithRepository()
        {
            foreach (var group in controller.groups)
            {
                group.CompareWithRepository();
            }
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
