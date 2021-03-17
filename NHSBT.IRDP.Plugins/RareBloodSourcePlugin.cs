namespace NHSBT.IRDP.Plugins
{
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Query;
    using NHSBT.IRDP.Plugins.ProxyClasses;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class RareBloodSourcePlugin : BasePlugin
    {

        public RareBloodSourcePlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {

            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PreOperation,
                MessageName = MessageNames.Create,
                EntityName = EntityNames.nhs_rarebloodsource,
                PluginAction = PreCreateExecution
            });


            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.Create,
                EntityName = EntityNames.nhs_rarebloodsource,
                PluginAction = PostCreateExecution
            });

            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.Update,
                EntityName = EntityNames.nhs_rarebloodsource,
                PluginAction = PostUpdateExecution
            });

            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.SetState,
                EntityName = EntityNames.nhs_rarebloodsource,
                PluginAction = SetStateExecution
            });

            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.SetStateDynamicEntity,
                EntityName = EntityNames.nhs_rarebloodsource,
                PluginAction = SetStateExecution
            });
        }

        protected void SetStateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                var rareBloodSourceId = ((EntityReference)localContext.PluginExecutionContext.InputParameters["EntityMoniker"]).Id;
                var state = (OptionSetValue)localContext.PluginExecutionContext.InputParameters["State"];
                var status = (OptionSetValue)localContext.PluginExecutionContext.InputParameters["Status"];

                if (state.Value == (int)RareBloodSource.eStatus.Active)
                {
                    CheckIfSourceHasAnyContradictions(localContext.OrganizationService, rareBloodSourceId);
                }
            }
        }


        protected void PreCreateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                // Obtain the target entity from the Input Parameters.
                var targetEntity = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];

                if (targetEntity.Contains(RareBloodSource.Properties.ParentAccount) &&
                    targetEntity.Contains(RareBloodSource.Properties.ContributorCode))
                {
                    var accountId = ((EntityReference)targetEntity[RareBloodSource.Properties.ParentAccount]).Id;
                    var contributorCode = ((String)targetEntity[RareBloodSource.Properties.ContributorCode]);
                    var alternateKey = String.Format("[{0}][{1}]", accountId.ToString().ToUpper(), contributorCode);

                    //Create a unique key
                    targetEntity[RareBloodSource.Properties.AlternateKey] = alternateKey;
                }
            }
        }

        protected void PostCreateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                // Obtain the target entity from the Input Parameters.
                var targetEntity = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];

                if (targetEntity.Contains(RareBloodSource.Properties.Rawdata) &&
                    targetEntity.Contains(RareBloodSource.Properties.HeaderRow) &&
                    targetEntity.Contains(RareBloodSource.Properties.ParentAccount))
                {
                    var accountId = ((EntityReference)targetEntity[RareBloodSource.Properties.ParentAccount]).Id;
                    var headerRow = (String)targetEntity[RareBloodSource.Properties.HeaderRow];
                    var line = (String)targetEntity[RareBloodSource.Properties.Rawdata];

                    if (!String.IsNullOrEmpty(headerRow) && !String.IsNullOrEmpty(line))
                    {
                        var parsedFileRow = GetParsedFileRow(
                           localContext.OrganizationService,
                           accountId,
                           line,
                           headerRow);

                        ProcessSourceAssociations(localContext.OrganizationService, parsedFileRow, targetEntity);
                    }
                }
            }
        }


        protected void PostUpdateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                var targetEntity = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];
                var postImage = localContext.PluginExecutionContext.PostEntityImages["PostImage"];

                if (
                    targetEntity.Contains(RareBloodSource.Properties.Rawdata)
                    && targetEntity[RareBloodSource.Properties.Rawdata] != null
                )
                {

                    var accountId = ((EntityReference)postImage[RareBloodSource.Properties.ParentAccount]).Id;

                    var rawData = (String)targetEntity[RareBloodSource.Properties.Rawdata];
                    var headerRow = targetEntity.Contains(RareBloodSource.Properties.HeaderRow) ? (String)targetEntity[RareBloodSource.Properties.HeaderRow] : (String)postImage[RareBloodSource.Properties.HeaderRow];

                    if (!String.IsNullOrEmpty(headerRow) && !String.IsNullOrEmpty(rawData))
                    {
                        var parsedFileRow = GetParsedFileRow(
                           localContext.OrganizationService,
                           accountId,
                           rawData,
                           headerRow);

                        ProcessSourceAssociations(localContext.OrganizationService, parsedFileRow, targetEntity);
                    }
                }
            }
        }

        private void ProcessSourceAssociations(IOrganizationService organisationService, ParsedFileRow parsedFileRow, Entity targetEntity)
        {
            var executeMultipleRequest = new ExecuteMultipleRequest()
            {
                // Assign settings that define execution behavior: continue on error, return responses. 
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                // Create an empty organization request collection.
                Requests = new OrganizationRequestCollection()
            };

            targetEntity.Attributes[ProxyClasses.RareBloodSource.Properties.Rawdata] = null;
            targetEntity.Attributes[ProxyClasses.RareBloodSource.Properties.HeaderRow] = null;


            executeMultipleRequest.Requests.Add(
                new UpdateRequest()
                {
                    Target = targetEntity
                }
            );

            //Create, Update (set active), or Remove Rarities to align with uplaod document
            executeMultipleRequest = AlignRaritySourceAssociations(organisationService, parsedFileRow, targetEntity.ToEntityReference(), executeMultipleRequest);

            ////Implicit antigens are created and we may need to create, update and remove some to align with the upload document
            executeMultipleRequest = AlignAntigenSourceAssociations(organisationService, parsedFileRow, targetEntity.ToEntityReference(), executeMultipleRequest);



            ////Finally execute a batch of requests
            var executeMutlipleResponse = (ExecuteMultipleResponse)organisationService.Execute(executeMultipleRequest);

            if (executeMutlipleResponse.IsFaulted)
            {
                throw new InvalidPluginExecutionException("Failed to execute all of the requests in an ExecuteMultipleRequest while processing rarity/antigen associations for this Rare Blood Source");
            }
        }

        static ExecuteMultipleRequest AlignRaritySourceAssociations(IOrganizationService organisationService, ParsedFileRow parsedFileRow, EntityReference rareBloodSource, ExecuteMultipleRequest executeMultipleRequest)
        {

            //Get all the rarity asscoaiton for the source record
            var sourceRarities = Helper.GetRarityAssociationsForSource(organisationService, rareBloodSource.Id);

            //Get the set of the uplaoded rarity associations we need to create
            var raritiesToCreate = parsedFileRow.RaritySourceAssociations.Where(
                pfr => sourceRarities.All(
                    source => pfr.Rarity.Id != source.Rarity.Id
                )
            );
            //Get the set of the uplaoded rarity associations we need to update (set active)
            var raritiesToUpdate = sourceRarities.Where(
                source => parsedFileRow.RaritySourceAssociations.Any(
                    pfr => pfr.Rarity.Id == source.Rarity.Id
                )
            );
            // Get the set of the uplaoded rarity associations we need to remove (deactivate)
            var raritiesToRemove = sourceRarities.Where(
                source => parsedFileRow.RaritySourceAssociations.All(
                    pfr =>
                        pfr.Rarity.Id != source.Rarity.Id
                        && source.Status != ProxyClasses.Rarity_SourceAssociation.eStatus.Inactive
                )
            );

            //Foreach rarity association that needs creating...
            foreach (var raritySourceAssociation in raritiesToCreate)
            {
                //...set the source
                raritySourceAssociation.Source = rareBloodSource;

                executeMultipleRequest.Requests.Add(
                      new CreateRequest()
                      {
                          Target = raritySourceAssociation
                      });

            }

            var activeRarityAssocsProcessed = new List<EntityReference>();

            foreach (var raritySourceAssociation in raritiesToUpdate) // rarities to reactivate
            {
                //test whether a this rarity is already associated with this source via another association
                var isRarityAlreadyRepresented =
                    activeRarityAssocsProcessed.Count > 0
                    && activeRarityAssocsProcessed.Where(
                        reactivatedRarity =>
                        reactivatedRarity.Id == raritySourceAssociation.Rarity.Id
                    ).Count() > 0;

                //test whether this association is already active
                var isThisAssocActive = raritySourceAssociation.Status == Rarity_SourceAssociation.eStatus.Active;


                if (!isThisAssocActive && !isRarityAlreadyRepresented)
                {
                    //if we've not already activated a rarity association for this rarity 
                    //(and this one is inactive)
                    // then activate this one
                    executeMultipleRequest.Requests.Add(
                        new SetStateRequest()
                        {
                            EntityMoniker = raritySourceAssociation.ToEntityReference(),
                            State = new OptionSetValue((int)Rarity_SourceAssociation.eStatus.Active),
                            Status = new OptionSetValue((int)Rarity_SourceAssociation.eStatusReason.Active_Active)
                        }
                    );



                }
                else if (isThisAssocActive && isRarityAlreadyRepresented)
                { // if this condition is met, its a duplicate and we should deactivate
                    executeMultipleRequest.Requests.Add(
                        new SetStateRequest()
                        {
                            EntityMoniker = raritySourceAssociation.ToEntityReference(),
                            State = new OptionSetValue((int)Rarity_SourceAssociation.eStatus.Inactive),
                            Status = new OptionSetValue((int)Rarity_SourceAssociation.eStatusReason.RemovedByImport_Inactive)
                        }
                    );
                }

                //and add this rarity to the list already processed
                activeRarityAssocsProcessed.Add(raritySourceAssociation.Rarity);
            }

            foreach (var raritySourceAssociation in raritiesToRemove)
            {
                executeMultipleRequest.Requests.Add(
                   new SetStateRequest()
                   {
                       EntityMoniker = raritySourceAssociation.ToEntityReference(),
                       State = new OptionSetValue((int)Rarity_SourceAssociation.eStatus.Inactive),
                       Status = new OptionSetValue((int)Rarity_SourceAssociation.eStatusReason.RemovedByImport_Inactive)
                   });
            }

            return executeMultipleRequest;
        }

        private ExecuteMultipleRequest AlignAntigenSourceAssociations(IOrganizationService organisationService, ParsedFileRow parsedFileRow, EntityReference rareBloodSource, ExecuteMultipleRequest executeMultipleRequest)
        {
            var sourceAntigens = Helper.GetAntigenAssociationsForSource(organisationService, rareBloodSource.Id);

            //Query the records prior to any create/update/remove actions
            var antigenSourceAssociationsToCreate = parsedFileRow.AntigenSourceAssociations.Where(pfr => sourceAntigens.All(source => pfr.Antigen.Id != source.Antigen.Id));
            var antigenSourceAssociationsToUpdate = sourceAntigens.Where(source => parsedFileRow.AntigenSourceAssociations.Any(pfr => pfr.Antigen.Id == source.Antigen.Id));
            var antigenSourceAssociationsToRemove = sourceAntigens.Where(source => parsedFileRow.AntigenSourceAssociations.All(pfr => pfr.Antigen.Id != source.Antigen.Id));


            foreach (var antigenSourceAssociation in antigenSourceAssociationsToCreate)
            {
                antigenSourceAssociation.Source = rareBloodSource;
                antigenSourceAssociation.Explicit = true;

                executeMultipleRequest.Requests.Add(
                       new CreateRequest()
                       {
                           Target = antigenSourceAssociation
                       });

            }

            List<EntityReference> antigensAlreadyRepresented = new List<EntityReference>();

            foreach (var antigenSourceAssociation in antigenSourceAssociationsToUpdate)
            {
                //=========//=========//=========//=========//
                //=========  Section 1 - Gather Data 
                //=========//=========//=========//=========//
                //Finds the oldest instance of antigen associations matching the file upload

                //test whether the antigen association is active
                bool isAssociationActive = antigenSourceAssociation.Status == Antigen_SourceAssociation.eStatus.Active;

                //test whether it is explicit
                bool isAssociationExplicit = antigenSourceAssociation.Explicit.Value;

                //test whether it is implied by rarities
                bool isAssociationImpliedByRarities = CheckIfImpliedByRarities(organisationService, antigenSourceAssociation);


                //get the antigen association details from the parsed file row
                Antigen_SourceAssociation uploadedAntigenDetails = (
                        from a in parsedFileRow.AntigenSourceAssociations
                        where a.Antigen.Id == antigenSourceAssociation.Antigen.Id
                        select a
                    ).First();

                //test whether the result is already correct
                bool isResultCorrect = antigenSourceAssociation.AntigenResult == uploadedAntigenDetails.AntigenResult;

                //find the oldest existing antigen association 
                //that we can reuse (i.e. result is already matching, or the result can be amended as it's not implied) 
                Antigen_SourceAssociation oldestReusableMatchingAssociation = (
                        antigenSourceAssociationsToUpdate.Where(
                            asatu =>
                                asatu.Antigen.Id == antigenSourceAssociation.Antigen.Id

                                //where either the result is the same
                                && (asatu.AntigenResult.Value == uploadedAntigenDetails.AntigenResult.Value

                                //or it's not implied (i.e. its result can be updated)
                                || CheckIfImpliedByRarities(organisationService, asatu) == false)
                        )
                    ).FirstOrDefault();


                //=========//=========//=========//=========//=========//=========//
                //=========  Section 2 - Test the values to determine next action
                //=========//=========//=========//=========//=========//=========//

                //=== First Preference ===//
                if (oldestReusableMatchingAssociation != null
                    && antigenSourceAssociation.Id == oldestReusableMatchingAssociation.Id)
                {   //if this is the oldest association with the same result for this antigen/source combo
                    if (!isAssociationActive || !isAssociationExplicit || !isResultCorrect)
                    { //and if its either inactive or not marked as explicit or has the incorrect result
                        //then update it
                        antigenSourceAssociation.Explicit = true;
                        antigenSourceAssociation.AntigenResult = uploadedAntigenDetails.AntigenResult;
                        antigenSourceAssociation.Status = Antigen_SourceAssociation.eStatus.Active;
                        antigenSourceAssociation.StatusReason = Antigen_SourceAssociation.eStatusReason.Active_Active;

                        //and add the update to the execution queue.
                        executeMultipleRequest.Requests.Add(
                            new UpdateRequest()
                            {
                                Target = antigenSourceAssociation
                            }
                        );

                        //And record this antigen as processed
                        antigensAlreadyRepresented.Add(antigenSourceAssociation.Antigen);
                    }

                }
                else
                {   //if this is not the oldest antigen association that we can reuse


                    if (isAssociationActive && !isAssociationImpliedByRarities)
                    // if it's active
                    //and it's not implied by other rarities
                    {
                        //then it should simply be deactivated.
                        executeMultipleRequest.Requests.Add(
                        new SetStateRequest()
                        {
                            EntityMoniker = antigenSourceAssociation.ToEntityReference(),
                            State = new OptionSetValue((int)Antigen_SourceAssociation.eStatus.Inactive),
                            Status = new OptionSetValue((int)Antigen_SourceAssociation.eStatusReason.RemovedByImport_Inactive)
                        });
                    }

                    //test whether this antigen has been associated with the source already
                    bool isAntigenAlreadyRepresented = antigensAlreadyRepresented.Where(
                        newAntigens => newAntigens.Id == antigenSourceAssociation.Antigen.Id
                        ).FirstOrDefault() != null;

                    //if there is no reusable antigen association
                    //and the antigen has not already been represented against this source
                    if (oldestReusableMatchingAssociation == null
                        && isAntigenAlreadyRepresented == false)
                    {
                        //then we need to create a new antigen-source association
                        //to highlight the contradiction with the existing ones.
                        uploadedAntigenDetails.Source = rareBloodSource;
                        uploadedAntigenDetails.Explicit = true;

                        executeMultipleRequest.Requests.Add(
                               new CreateRequest()
                               {
                                   Target = uploadedAntigenDetails
                               });

                        //and record the fact that this antigen is now represented for this upload
                        antigensAlreadyRepresented.Add(antigenSourceAssociation.Antigen);
                    }
                }
            }

            foreach (var antigenSourceAssociation in antigenSourceAssociationsToRemove)
            {

                bool isImpliedByRarities = CheckIfImpliedByRarities(organisationService, antigenSourceAssociation);
                bool isActive = antigenSourceAssociation.Status == Antigen_SourceAssociation.eStatus.Active;

                if (!isImpliedByRarities && isActive)
                { //if it isn't implied, but is still active
                    //then we can just deactivate it.
                    executeMultipleRequest.Requests.Add(
                        new SetStateRequest()
                        {
                            EntityMoniker = antigenSourceAssociation.ToEntityReference(),
                            State = new OptionSetValue((int)Antigen_SourceAssociation.eStatus.Inactive),
                            Status = new OptionSetValue((int)Antigen_SourceAssociation.eStatusReason.RemovedByImport_Inactive)
                        });
                }
            }

            return executeMultipleRequest;
        }

        private bool CheckIfImpliedByRarities(IOrganizationService organisationService, Antigen_SourceAssociation antigenSourceAssociation)
        {
            var antigenIdList = new List<Guid>();

            var queryExpression = new QueryExpression()
            {
                EntityName = ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.PrimaryIdAttribute,
                                ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.Properties.Nhs_antigensourceassociationid,
                                ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.Properties.Nhs_raritysourceassociationid}),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression() {
                            Conditions =
                            {
                                new ConditionExpression(
                                    ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.Properties.Nhs_antigensourceassociationid,
                                    ConditionOperator.Equal,
                                    antigenSourceAssociation.Id
                                )
                            }
                        }
                    }
                }
            };

            var results = organisationService.RetrieveMultiple(queryExpression);

            if (results.Entities.Count() > 0)
            {
                //if there are any implying rarities for this antigen
                return true;
            }
            else
            {
                // but if there are none, then its not implied
                return false;
            }
        }


        private ParsedFileRow GetParsedFileRow(
         IOrganizationService organisationService,
         Guid accountId,
         string line,
         string headerRow)
        {
            const string COLUMN_HEADER_ID = "ID";
            const string COLUMN_HEADER_ABO_GROUP = "ABO Group";
            const string COLUMN_HEADER_DONOR_COUNT = "Donor Count";
            const string COLUMN_HEADER_FROZEN_UNIT_COUNT = "Frozen Unit Count";

            var account = new Account(organisationService.Retrieve(Account.LogicalName, accountId, new ColumnSet(new string[] { Account.Properties.OwningTeam })));
            var optionSetABO = Helper.GetOptionSet(organisationService, "nhs_abosubtypes");
            var optionSetAntigenResults = Helper.GetOptionSet(organisationService, "nhs_antigenresultsourcecontext");
            var allRarities = Helper.GetAllRarities(organisationService);
            var allAntigens = Helper.GetAllAntigens(organisationService);

            var columnHeaderValidation = new List<string>(new string[] { COLUMN_HEADER_ID, COLUMN_HEADER_ABO_GROUP, COLUMN_HEADER_DONOR_COUNT, COLUMN_HEADER_FROZEN_UNIT_COUNT });


            string[] columnHeaders = headerRow.Split(',');
            var columnData = line.Split(',');

            var contributorCode = columnData[columnHeaderValidation.IndexOf(COLUMN_HEADER_ID)].Trim().Replace(@"\""", "\"");

            var parsedFileRow = new ParsedFileRow(contributorCode, account.OwningTeam, account.ToEntityReference(), headerRow, line);

            for (int index = columnHeaderValidation.Count(); index < columnHeaders.Length; index++)
            {
                var columnHeader = columnHeaders[index].Trim();
                var columnValue = columnData[index].Trim();

                if (columnHeader.Contains("Rarity"))
                {
                    if (!String.IsNullOrEmpty(columnValue))
                    {
                        var rarity = (from r in allRarities
                                      where r.Name_Unicode == columnValue
                                      select r).FirstOrDefault();

                        //...add it to the collection
                        parsedFileRow.RaritySourceAssociations.Add(
                            new Rarity_SourceAssociation()
                            {
                                Rarity = rarity.ToEntityReference(),
                                Owner = account.OwningTeam
                            });
                    }
                }
                //Else we have a value anitgen results column so...
                else if (!String.IsNullOrEmpty(columnValue))
                {

                    ////...try to get the Antigen form the Header Column text
                    var antigen = (from a in allAntigens
                                   where a.Name_Unicode.Trim() == columnHeader
                                   select a).FirstOrDefault();

                    //Try to get the Antigen Result from column text 
                    var antigenResult = (from o in optionSetAntigenResults
                                         where o.Label.LocalizedLabels[0].Label == columnValue.ToUpper() //Check for lower case "w"
                                         select o).FirstOrDefault();

                    //...add it to the collection
                    parsedFileRow.AntigenSourceAssociations.Add(
                        new Antigen_SourceAssociation()
                        {
                            Antigen = antigen.ToEntityReference(),
                            AntigenResult_OptionSetValue = new OptionSetValue(antigenResult.Value.Value),
                            Owner = account.OwningTeam
                        });
                }
            }

            //...add it to the collection
            return parsedFileRow;
        }

        private void CheckIfSourceHasAnyContradictions(IOrganizationService organisationService, Guid sourceId)
        {
            //Todo: needs de-duplicated as exists in RaraBloodSourcePlugin also
            var matchedAntigens = new List<Antigen_SourceAssociation>();

            var queryExpression = new QueryExpression()
            {
                EntityName = Antigen_SourceAssociation.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                Antigen_SourceAssociation.PrimaryIdAttribute,
                                Antigen_SourceAssociation.Properties.Explicit,
                                Antigen_SourceAssociation.Properties.Antigen,
                                Antigen_SourceAssociation.Properties.AntigenResult,
                                Antigen_SourceAssociation.Properties.Status,
                                Antigen_SourceAssociation.Properties.StatusReason}),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {
                            Conditions =
                            {
                                new ConditionExpression(Antigen_SourceAssociation.Properties.Source, ConditionOperator.Equal, sourceId),
                                new ConditionExpression(Antigen_SourceAssociation.Properties.StatusReason, ConditionOperator.Equal, (int)Antigen_SourceAssociation.eStatusReason.Contradiction_Active)
                            }
                        }
                    }
                }
            };

            var results = organisationService.RetrieveMultiple(queryExpression).Entities;

            if (results.Count() > 0)
            {
                Helper.SetState(
                    organisationService,
                    new EntityReference(RareBloodSource.LogicalName, sourceId),
                    new OptionSetValue((int)RareBloodSource.eStatus.Inactive),
                    new OptionSetValue((int)RareBloodSource.eStatusReason.ValidationErrors_Inactive));
            }
        }
    }
}
