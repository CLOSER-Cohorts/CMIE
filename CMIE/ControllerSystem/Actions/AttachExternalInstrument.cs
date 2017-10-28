﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;

namespace CMIE.ControllerSystem.Actions
{
    class AttachExternalInstrument : IAction
    {
        private string instrumentUserId;
        private string externalPath;

        public AttachExternalInstrument(string instrumentUserId, string externalPath) : base()
        {
            this.instrumentUserId = instrumentUserId;
            this.externalPath = externalPath;
        }

        public override void Validate()
        {
            if (!System.IO.File.Exists(this.externalPath))
            {
                throw new System.Exception("Missing file: " + this.externalPath);
            }
        }

        public override IEnumerable<IVersionable> Build(Repository repository)
        {
            /*var instrument = ws.OfType<Instrument>()
                .Where(x => x.UserIds.Count > 0)
                .FirstOrDefault( x => x.UserIds.Any(y => y.Identifier == instrumentUserId));
            Attach(instrument, externalPath);*/

            return new List<IVersionable>();
        }

        static public void Attach(Instrument instrument, string external_path)
        {
            if (instrument != default(Instrument))
            {
                instrument.ExternalInstrumentLocations.Add(new System.Uri("https://discovery.closer.ac.uk/files/" + external_path));
            }
        }
    }
}
