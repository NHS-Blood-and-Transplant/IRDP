using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Generic;

namespace NHSBT.IRDP.Plugins
{
    public class ParsedFileRow
    {
        public ParsedFileRow(String contributorCode, EntityReference owningTeam, EntityReference parentAccount, string headerRow, string rawData)
        {
            Source = new ProxyClasses.RareBloodSource()
            {
                Id = Guid.NewGuid(),
                ContributorCode = contributorCode,
                Owner = owningTeam,
                ParentAccount = parentAccount,
                HeaderRow = headerRow,
                Rawdata = rawData
            };

            AntigenSourceAssociations = new List<ProxyClasses.Antigen_SourceAssociation>();
            RaritySourceAssociations = new List<ProxyClasses.Rarity_SourceAssociation>();
            ParseExceptions = new List<ParseException>();
        }

        public ProxyClasses.RareBloodSource Source { get; }

        public List<ProxyClasses.Antigen_SourceAssociation> AntigenSourceAssociations { get; }

        public List<ProxyClasses.Rarity_SourceAssociation> RaritySourceAssociations { get; }

        public List<ParseException> ParseExceptions { get; }

        public bool IsValid
        {
            get
            {
                return ParseExceptions.Count == 0;
            }
        }
    }
}
