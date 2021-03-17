namespace NHSBT.IRDP.Plugins
{
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Query;
    using NHSBT.IRDP.Plugins.ProxyClasses;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RareBloodSourceImportPlugin : BasePlugin
    {
      
        public RareBloodSourceImportPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PreOperation,
                MessageName = MessageNames.Create,
                EntityName = EntityNames.nhs_rarebloodsourceimport,
                PluginAction = PreCreateExecution
            });
        }

        protected void PreCreateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                // Obtain the target entity from the Input Parameters.
                var targetImportEntity = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];

                if ((targetImportEntity.Contains(RareBloodSourceImport.Properties.Create) &&
                    (bool)targetImportEntity[RareBloodSourceImport.Properties.Create]) ||
                    (targetImportEntity.Contains(RareBloodSourceImport.Properties.Update) &&
                    (bool)targetImportEntity[RareBloodSourceImport.Properties.Update]))

                {
                    var account = (EntityReference)targetImportEntity[RareBloodSourceImport.Properties.ParentAccount];
                    var headerRow = (string)targetImportEntity[RareBloodSourceImport.Properties.HeaderRow];
                    var dataRow = (string)targetImportEntity[RareBloodSourceImport.Properties.DataRow];

                    if (!String.IsNullOrEmpty(headerRow) && !String.IsNullOrEmpty(dataRow))
                    {
                        var targetRareBloodSource = GetRareBloodSourceObject(
                           localContext.OrganizationService,
                           account.Id,
                           headerRow,
                           dataRow);

                        if (targetImportEntity.Contains(RareBloodSourceImport.Properties.Create) && (bool)targetImportEntity[RareBloodSourceImport.Properties.Create])
                        { //If it is a create record
                            localContext.OrganizationService.Create(targetRareBloodSource);
                        }
                        else
                        { //if it is an update record
                            //then do the update
                            localContext.OrganizationService.Update(targetRareBloodSource);

                            //and ensure that the record is also active.
                            var setStateRequest = new SetStateRequest()
                            {
                                EntityMoniker = targetRareBloodSource.ToEntityReference(),
                                State = new OptionSetValue((int)RareBloodSource.eStatus.Active),
                                Status = new OptionSetValue((int)RareBloodSource.eStatusReason.Active_Active)
                            };
                            
                            localContext.OrganizationService.Execute(setStateRequest);
                        }
                    }
                   
                }
                else if (targetImportEntity.Contains(RareBloodSourceImport.Properties.RareBloodSource) &&
                        targetImportEntity.Contains(RareBloodSourceImport.Properties.Deactivate) &&
                        (bool)targetImportEntity[RareBloodSourceImport.Properties.Deactivate])
                {
                    var setStateRequest = new SetStateRequest()
                    {
                        EntityMoniker = (EntityReference)targetImportEntity[RareBloodSourceImport.Properties.RareBloodSource],
                        State = new OptionSetValue((int)RareBloodSource.eStatus.Inactive),
                        Status = new OptionSetValue((int)RareBloodSource.eStatusReason.DeactivatedViaBulkUpload_Inactive)
                    };

                    localContext.OrganizationService.Execute(setStateRequest);
                }
            }
        }


        private RareBloodSource GetRareBloodSourceObject(
         IOrganizationService organisationService,
         Guid accountId,
         string headerRow,
         string dataRow)
        {
            const string COLUMN_HEADER_ID = "ID";
            const string COLUMN_HEADER_ABO_GROUP = "ABO Group";
            const string COLUMN_HEADER_DONOR_COUNT = "Donor Count";
            const string COLUMN_HEADER_FROZEN_UNIT_COUNT = "Frozen Unit Count";

            var columnHeaderValidation = new List<string>(new string[] { COLUMN_HEADER_ID, COLUMN_HEADER_ABO_GROUP, COLUMN_HEADER_DONOR_COUNT, COLUMN_HEADER_FROZEN_UNIT_COUNT });

            var account = new Account(organisationService.Retrieve(Account.LogicalName, accountId, new ColumnSet(new string[] { Account.Properties.OwningTeam })));
            var optionSetABO = Helper.GetOptionSet(organisationService, "nhs_abosubtypes");
            var accountSources = Helper.GetSourcesForAccount(organisationService, account.Id);

            string[] columnHeaders = headerRow.Split(',');
            var columnData = dataRow.Split(',');

            var contributorCode = columnData[columnHeaderValidation.IndexOf(COLUMN_HEADER_ID)].Trim().Replace(@"\""", "\"");

            var parsedFileRow = new ParsedFileRow(contributorCode, account.OwningTeam, account.ToEntityReference(), headerRow, dataRow);

            var matchedAccountSource = (from accountSource in accountSources
                                        where accountSource.ContributorCode == parsedFileRow.Source.ContributorCode
                                        select accountSource).FirstOrDefault();

            //If the Rare Blood Source exists...
            if (matchedAccountSource != null)
            {
                parsedFileRow.Source.Id = matchedAccountSource.Id;
            }

            var bloodType = columnData[columnHeaderValidation.IndexOf(COLUMN_HEADER_ABO_GROUP)].Trim();
            var bloodTypeOption = (from o in optionSetABO
                                   where o.Label.LocalizedLabels[0].Label == bloodType
                                   select o).FirstOrDefault();

            parsedFileRow.Source.ABOType = (RareBloodSource.eABOSub_types)bloodTypeOption.Value.Value;

            parsedFileRow.Source.LastReviewedOn = DateTime.Now;

            var donorCountValue = columnData[columnHeaderValidation.IndexOf(COLUMN_HEADER_DONOR_COUNT)];
            int donorCount = 0;

            if (int.TryParse(donorCountValue, out donorCount) || donorCount < 0)
            {
                parsedFileRow.Source.DonorCount = donorCount;
                parsedFileRow.Source.SourceType = donorCount == 1 ? false : true;
            }

            var frozenUnitCountValue = columnData[columnHeaderValidation.IndexOf(COLUMN_HEADER_FROZEN_UNIT_COUNT)];
            int frozenUnitCount = 0;

            if (int.TryParse(frozenUnitCountValue, out frozenUnitCount) || frozenUnitCount < 0)
            {
               
                parsedFileRow.Source.FrozenUnitCount = frozenUnitCount;
            }

            //...and return it
            return parsedFileRow.Source;
        }
    }
}
