using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;

namespace CMIE.ControllerSystem.Actions
{
    class AttachExternalAids : IAction
    {
        private string[] fileEntries;
        private string urlDir;
        private string dirName;
        private string ccsName;

        public AttachExternalAids(string ccsName, string urlDir, string dirName)
        {
            this.urlDir = urlDir;
            this.dirName = dirName;
            this.ccsName = ccsName;
            this.fileEntries = System.IO.Directory.GetFiles(dirName);
        }

        public override void Validate()
        {
            if (!System.IO.Directory.Exists(this.dirName))
            {
                throw new System.Exception("Missing folder: " + this.dirName);
            }
        }

        public override IEnumerable<IVersionable> Build(Repository repository)
        {
            /*var qcs = ws.OfType<ControlConstructScheme>().Single(x => x.ItemName.Best == ccsName).GetChildren().OfType<QuestionActivity>().ToList();

            foreach (var fileName in fileEntries)
            {
                string[] fileNamePieces = fileName.Split(new char[] { '\\' });
                string[] parts = fileNamePieces.Last().Split(new char[] { '.' });
                string qc = parts[0];
                string format = parts[1].ToLower();

                var questionConstruct = qcs.Single(x => string.Compare(x.ItemName.Best, qc, ignoreCase: true) == 0);
                var aid = new OtherMaterial();
                aid.MaterialType = "image";
                aid.MimeType = "image/png";
                aid.UrlReference = new System.Uri("https://discovery.closer.ac.uk/external_aids/" + urlDir + "/" + fileNamePieces.Last());
                string label = "";
                if (questionConstruct.Question != null)
                {
                    questionConstruct.Question.ExternalAids.Add(aid);
                    label = questionConstruct.Question.Label.Best;
                }
                else if (questionConstruct.QuestionGrid != null)
                {
                    questionConstruct.QuestionGrid.ExternalAids.Add(aid);
                    label = questionConstruct.QuestionGrid.Label.Best;
                }
                aid.DublinCoreMetadata.Title.SetStringForDefaultAudience("en-GB", label);
            }*/

            return new List<IVersionable>();
        }
    }
}
