using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLOSER_Repository_Ingester.ControllerSystem.Actions;

namespace CLOSER_Repository_Ingester.ControllerSystem
{
    class Controller
    {
        string filepath;
        public string basePath { get; set; }
        public List<Group> groups { get; private set; }

        public Controller(string filepath)
        {
            this.filepath = filepath;
            groups = new List<Group>();
        }

        public void loadFile()
        {
            loadFile(filepath);
        }

        public void loadFile(string filepath)
        {
            string[] lines = File.ReadAllLines(filepath);
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.Length == 0) continue;
                if (trimmedLine[0] == '#') continue;
                parseLine(trimmedLine);
            }
        }

        private void parseLine(string line)
        {
            string[] pieces = line.Split(new char[] { '\t' });
            Group group;

            if (pieces.Length > 1)
            {
                switch (pieces[0].ToLower())
                {
                    case "group":
                        groups.Add(new Group(pieces[1]));
                        return;

                    case "rootdir":
                        basePath = pieces[1];
                        return;

                    case "control":
                        loadFile(Path.Combine(Path.GetDirectoryName(filepath), pieces[1]));
                        return;

                    case "concepts":

                        return;

                    default:
                        group = groups.Find(x => string.Compare(x.name, pieces[1]) == 0);
                        break;
                }
                if (group != null)
                {
                    switch (pieces[0].ToLower())
                    {
                        case "instrument":
                            group.AddAction(pieces[2], new LoadInstrument(BuildFilePath(pieces[3])));
                            break;
                    }
                }
            }
        }

        private string BuildFilePath(string filepath)
        {
            var output = Path.Combine(basePath, filepath);

            output = output.Replace('/', Path.DirectorySeparatorChar);
            output = output.Replace('\\', Path.DirectorySeparatorChar);

            return output;
        }
    }
}
