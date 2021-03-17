
using Microsoft.Xrm.Sdk.Metadata;

namespace NHSBT.IRDP.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using NHSBT.IRDP.Plugins.ProxyClasses;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Web.UI;
    using System.Web.Util;

    public class RaritySourceAssociationPlugin : BasePlugin
    {

        public RaritySourceAssociationPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.Create,
                EntityName = EntityNames.nhs_raritysourceassociation,
                PluginAction = PostCreateExecution
            });

            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.SetState,
                EntityName = EntityNames.nhs_raritysourceassociation,
                PluginAction = SetStateExecution
            });

            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.SetStateDynamicEntity,
                EntityName = EntityNames.nhs_raritysourceassociation,
                PluginAction = SetStateExecution
            });
        
        }

        protected void PostCreateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                var organisationService = localContext.OrganizationService;
                var newEntity = new ProxyClasses.Rarity_SourceAssociation((Entity)localContext.PluginExecutionContext.InputParameters["Target"]);

                var targetEntity = DeduplicateRarityAssociation(localContext, newEntity);

                if (targetEntity == newEntity)
                {
                    AddImplicitAntigens(organisationService, newEntity);
                }  
            }
        }


        protected void SetStateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                var raritySourceAssociationId = ((EntityReference)localContext.PluginExecutionContext.InputParameters["EntityMoniker"]).Id;
                var raritySourceAssoc = GetRarityAssocFromGuid(localContext.OrganizationService, raritySourceAssociationId);
                var state = (OptionSetValue)localContext.PluginExecutionContext.InputParameters["State"];

                bool isBeingActivated = state.Value == (int)ProxyClasses.Rarity_SourceAssociation.eStatus.Active;

                //If the record is being activated then...
                if (isBeingActivated)
                {    
                    //deduplicate rarity association against the pre-existing ones
                    var deduplicatedRaritySourceAssoc = DeduplicateRarityAssociation(localContext, raritySourceAssoc);
                    
                    // if the deduped source is the same as the subject of the plugin
                    if (deduplicatedRaritySourceAssoc.Id == raritySourceAssoc.Id)
                    {
                        //then add the implicit antigens to the subject rarity association
                        AddImplicitAntigens(localContext.OrganizationService, raritySourceAssoc);
                    }
                    
                } else //if it's being deactivated
                { //then we can remove any residual association
                    RemoveImplicitAntigens(localContext.OrganizationService, raritySourceAssoc);
                }
            }
        }


        private void AddImplicitAntigens(IOrganizationService organisationService, Rarity_SourceAssociation raritySourceAssoc)
        {

            var sourceId = raritySourceAssoc.Source.Id;
            var rarityId = raritySourceAssoc.Rarity.Id;

            var targetAntigensForRarity = Helper.GetAntigensImpliedByRarity(organisationService, rarityId);

            var currentlyAssociatedAntigens = GetAssociatedSourceAntigens(organisationService, sourceId);

            var intersectEntitiesToBeCreated = new EntityReferenceCollection();

            foreach (var implicitAntigen in targetAntigensForRarity)
            {
                var oldestActiveMatchSameResult = (from associatedAntigen in currentlyAssociatedAntigens
                                                      where associatedAntigen.Antigen.Id == implicitAntigen.Antigen.Id
                                                      && associatedAntigen.AntigenResult_OptionSetValue.Value == implicitAntigen.AntigenResult_OptionSetValue.Value
                                                      select associatedAntigen).FirstOrDefault();

                var oldestMatchNotExplicitOrImplied = (from associatedAntigen in currentlyAssociatedAntigens
                                                        where associatedAntigen.Antigen.Id == implicitAntigen.Antigen.Id
                                                        && associatedAntigen.Explicit == false
                                                        && IsAntigenAssocImpliedByRarityAssoc(organisationService, associatedAntigen, raritySourceAssoc) == false
                                                        select associatedAntigen).FirstOrDefault();

                if (oldestActiveMatchSameResult != null)
                { //If this is the oldest association for that antigen with the same result
                    var isAlreadyLinkedWithRarity = IsAntigenAssocImpliedByRarityAssoc(
                        organisationService,
                        oldestActiveMatchSameResult, 
                        raritySourceAssoc);
                    var isAlreadyActive = oldestActiveMatchSameResult.Status == Antigen_SourceAssociation.eStatus.Active;

                    if (!isAlreadyLinkedWithRarity)
                    { //if its not already linked, then do so
                        intersectEntitiesToBeCreated.Add(oldestActiveMatchSameResult.ToEntityReference());
                    }

                    if (!isAlreadyActive)
                    { // and if its not active, then make it so
                      Helper.SetState(
                            organisationService,
                            oldestActiveMatchSameResult,
                            new OptionSetValue((int)ProxyClasses.Antigen_SourceAssociation.eStatus.Active),
                            new OptionSetValue((int)ProxyClasses.Antigen_SourceAssociation.eStatusReason.Active_Active));
                    }    
                }
                else if (oldestMatchNotExplicitOrImplied != null)
                //Otherwise, find the oldest antigen association not explicit or implied, with any result/status
                {
                    var isAlreadyActive = oldestActiveMatchSameResult.Status == Antigen_SourceAssociation.eStatus.Active;
                    var isAlreadyLinkedWithRarity = IsAntigenAssocImpliedByRarityAssoc(
                        organisationService,
                        oldestMatchNotExplicitOrImplied,
                        raritySourceAssoc);

                    if (!isAlreadyActive)
                    { //if its not already active
                        // then Activate the record
                        Helper.SetState(
                            organisationService,
                            oldestMatchNotExplicitOrImplied.ToEntityReference(),
                            new OptionSetValue((int)ProxyClasses.Antigen_RarityAssociation.eStatus.Active),
                            new OptionSetValue((int)ProxyClasses.Antigen_RarityAssociation.eStatusReason.Active_Active));
                    }

                    if (!isAlreadyLinkedWithRarity)
                    { //if its not already linked, then do so
                        intersectEntitiesToBeCreated.Add(oldestMatchNotExplicitOrImplied.ToEntityReference());
                    }

                    if (
                        (implicitAntigen.AntigenResult == ProxyClasses.Antigen_RarityAssociation.eAntigenResult_RarityContext.Present 
                        && oldestMatchNotExplicitOrImplied.AntigenResult != ProxyClasses.Antigen_SourceAssociation.eAntigenResult_SourceContext.Present) 
                        ||(implicitAntigen.AntigenResult == ProxyClasses.Antigen_RarityAssociation.eAntigenResult_RarityContext.Absent 
                        && oldestMatchNotExplicitOrImplied.AntigenResult != ProxyClasses.Antigen_SourceAssociation.eAntigenResult_SourceContext.Absent))
                    { //and if the antigen result doesn't match
                        //then update it
                        oldestMatchNotExplicitOrImplied.AntigenResult = 
                            (implicitAntigen.AntigenResult == ProxyClasses.Antigen_RarityAssociation.eAntigenResult_RarityContext.Present) 
                            ? ProxyClasses.Antigen_SourceAssociation.eAntigenResult_SourceContext.Present 
                            : ProxyClasses.Antigen_SourceAssociation.eAntigenResult_SourceContext.Absent;

                        organisationService.Update(oldestMatchNotExplicitOrImplied);
                    }
                }
                else //no matching Antigen Source Association exists so...
                { //we'll create a new association
                    var implicitAntigenSourceAssociation = new ProxyClasses.Antigen_SourceAssociation() 
                    {
                        Source = new EntityReference(ProxyClasses.RareBloodSource.LogicalName, sourceId),
                        Antigen = implicitAntigen.Antigen,
                        AntigenResult = implicitAntigen.AntigenResult == ProxyClasses.Antigen_RarityAssociation.eAntigenResult_RarityContext.Present ? ProxyClasses.Antigen_SourceAssociation.eAntigenResult_SourceContext.Present : ProxyClasses.Antigen_SourceAssociation.eAntigenResult_SourceContext.Absent
                    };
                    var implicitAntigenSourceAssociationId = organisationService.Create(implicitAntigenSourceAssociation);

                    //and we'll link it to the rarity association
                    intersectEntitiesToBeCreated.Add(new EntityReference(ProxyClasses.Antigen_SourceAssociation.LogicalName, implicitAntigenSourceAssociationId));
                }
            }

            if (intersectEntitiesToBeCreated.Count() > 0)
            {
                //...we need to link this rarity assocation to the antigen associations  
                organisationService.Associate(
                    ProxyClasses.Rarity_SourceAssociation.LogicalName,
                    raritySourceAssoc.Id,
                    new Relationship("nhs_AntigenSrcAssoc_nhs_RaritySrcAssoc"),
                    intersectEntitiesToBeCreated);
            }
            

        }

        private bool IsAntigenAssocImpliedByRarityAssoc (IOrganizationService organisationService, Antigen_SourceAssociation antigenSourceAssociation, Rarity_SourceAssociation raritySourceAssociation)
        {

            var queryExpression = new QueryExpression()
            {
                EntityName = ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.PrimaryIdAttribute,
                                }),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {
                            Conditions =
                            {
                                new ConditionExpression(
                                    ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.Properties.Nhs_antigensourceassociationid, 
                                    ConditionOperator.Equal, 
                                    antigenSourceAssociation.Antigen_SourceAssociationId),
                                new ConditionExpression(
                                    ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.Properties.Nhs_raritysourceassociationid,
                                    ConditionOperator.Equal,
                                    raritySourceAssociation.Rarity_SourceAssociationId)
                            }
                        }
                    }
                }
            };

            var results = organisationService.RetrieveMultiple(queryExpression);

            if (results.Entities.Count > 0)
            { return true; }
            else
            { return false; }            
        }

        private void RemoveImplicitAntigens(IOrganizationService organisationService, ProxyClasses.Rarity_SourceAssociation raritySourceAssoc)
        {
            Guid sourceId = raritySourceAssoc.Rarity.Id;
            Guid rarityAssocId = raritySourceAssoc.Id;
            
            var antigenAssocsToBeDisassociated = new EntityReferenceCollection();

            //...get all the antigen assocaitions implied by this rarity
            var matchingRarityIntersectRecords = GetAllSourceIntersectRecordsForRarity(organisationService, rarityAssocId);

            //For each antigen assoc implied by this rarity assoc
            foreach (nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc rarityIntersectRecord in matchingRarityIntersectRecords)
            {
                var antigenAssocId = rarityIntersectRecord.Nhs_antigensourceassociationid;

                //Get the antigen association as an entity reference
                var antigenAssocEntityRef = new EntityReference(
                    Antigen_SourceAssociation.LogicalName,
                    (Guid)antigenAssocId);

                var matchingAntigenIntersectRecords = GetAllSourceIntersectRecordsForAntigen(organisationService, antigenAssocId);

                // Add the intersect entity to the list of relationships to be deleted
                antigenAssocsToBeDisassociated.Add(antigenAssocEntityRef);

                //Test whether this antigen association is implied by other rarities
                //i.e. work out if any other rarities imply this same antigen source association
                var isAntigenImpliedByOtherRarities = (from antigenIntersectRecord in matchingAntigenIntersectRecords
                                            where antigenIntersectRecord.Nhs_antigensourceassociationid == rarityIntersectRecord.Nhs_antigensourceassociationid //its for the same antigen assoc
                                            && antigenIntersectRecord.Nhs_AnitenSrcAssoc_nhs_RaritySrcAssocId != rarityIntersectRecord.Nhs_AnitenSrcAssoc_nhs_RaritySrcAssocId //but a implied by a different rarity
                                            select antigenIntersectRecord).Count() > 0;


                // check if the antigen source association is explicit
                var isExplicit = (bool)rarityIntersectRecord.GetAttributeValue<AliasedValue>("antigenSourceAssoc.nhs_isexplicit").Value;
                
                //If there are no other rarities implying this antigen assoc
                // and the antigen association is also not explicit.
                if (!isAntigenImpliedByOtherRarities && !isExplicit )
                {
                    //Deactivate the antigen source association
                    Helper.SetState(
                        organisationService,
                        antigenAssocEntityRef,
                        new OptionSetValue((int)ProxyClasses.Antigen_SourceAssociation.eStatus.Inactive),
                        new OptionSetValue((int)ProxyClasses.Antigen_SourceAssociation.eStatusReason.Inactive_Inactive));
                }

            }
            if (antigenAssocsToBeDisassociated.Count() > 0)
            {
                //...we need to unlink this any deactivated rarity assocations from their respective antigen associations  
                organisationService.Disassociate(
                    ProxyClasses.Rarity_SourceAssociation.LogicalName,
                    raritySourceAssoc.Id,
                    new Relationship("nhs_AntigenSrcAssoc_nhs_RaritySrcAssoc"),
                    antigenAssocsToBeDisassociated); 
            }
        }

        private List<ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc> GetAllSourceIntersectRecordsForRarity(IOrganizationService organisationService, Guid rarityAssocId) {

            var intersctRecords = new List<ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc>();

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
                                    ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.Properties.Nhs_raritysourceassociationid,
                                    ConditionOperator.Equal,
                                    rarityAssocId
                                )
                            }
                        } 
                    }
                }
            };

            queryExpression.AddLink(
                ProxyClasses.Antigen_SourceAssociation.LogicalName,
                ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.Properties.Nhs_antigensourceassociationid,
                ProxyClasses.Antigen_SourceAssociation.PrimaryIdAttribute,
                JoinOperator.Inner);

            queryExpression.LinkEntities[0].Columns.AddColumns(
                ProxyClasses.Antigen_SourceAssociation.Properties.Antigen_SourceAssociationId,
                ProxyClasses.Antigen_SourceAssociation.Properties.Explicit
            );

            queryExpression.LinkEntities[0].EntityAlias = "antigenSourceAssoc";


            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                intersctRecords.Add(new ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc(result));
            }


            return intersctRecords;
        }

        private List<ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc> GetAllSourceIntersectRecordsForAntigen(IOrganizationService organisationService, Guid antigenAssocId)
        {

            var intersctRecords = new List<ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc>();

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
                                    antigenAssocId
                                )
                            }
                        }
                    }
                }
            };

            queryExpression.AddLink(
                ProxyClasses.Antigen_SourceAssociation.LogicalName,
                ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.Properties.Nhs_antigensourceassociationid,
                ProxyClasses.Antigen_SourceAssociation.PrimaryIdAttribute,
                JoinOperator.Inner);

            queryExpression.LinkEntities[0].Columns.AddColumns(
                ProxyClasses.Antigen_SourceAssociation.Properties.Antigen_SourceAssociationId,
                ProxyClasses.Antigen_SourceAssociation.Properties.Explicit
            );

            queryExpression.LinkEntities[0].EntityAlias = "antigenSourceAssoc";


            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                intersctRecords.Add(new ProxyClasses.nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc(result));
            }


            return intersctRecords;
        }

        private ProxyClasses.Rarity_SourceAssociation DeduplicateRarityAssociation(LocalPluginContext<Entity> localContext, ProxyClasses.Rarity_SourceAssociation subjectRarityAssociation)
        {
            // get all the pre-existing associations between this source and rarity
           
            var sourceId = subjectRarityAssociation.Source.Id;
            var rarityId = subjectRarityAssociation.Rarity.Id;

            var allMatchedAssocs = GetMatchedRarityAssocs(localContext.OrganizationService, sourceId, rarityId);

            // if there only is on matching rarity, then no need to continue with the deduplication
            if (allMatchedAssocs.Count == 1) 
            { //just return the subject association
                return subjectRarityAssociation; 
            }
            else
            {
                //Work out which is the older matching association
                var oldestMatchingRarityAssociation = (
                    from matchedRarityAssoc in allMatchedAssocs
                    orderby matchedRarityAssoc.CreatedOn ascending
                    select matchedRarityAssoc).FirstOrDefault();

                //Test whether the oldest record is still active
                var isOldestActive = oldestMatchingRarityAssociation.Status == Rarity_SourceAssociation.eStatus.Active;

                //detect any other stray matching associations that are still active
                //shouldn't happen - but helps recover from an error state
                var otherActiveMatchingRarityAssocsNotOldest = (
                    from matchedRarityAssoc in allMatchedAssocs
                    where matchedRarityAssoc.Id != oldestMatchingRarityAssociation.Id
                    where matchedRarityAssoc.Status == Rarity_SourceAssociation.eStatus.Active
                    select matchedRarityAssoc);

                //If the oldest is not active,
                if (!isOldestActive)
                {
                    //Then reactivate the oldest record
                    Helper.SetState(
                        localContext.OrganizationService,
                        oldestMatchingRarityAssociation.ToEntityReference(),
                        new OptionSetValue((int)ProxyClasses.Rarity_SourceAssociation.eStatus.Active),
                        new OptionSetValue((int)ProxyClasses.Rarity_SourceAssociation.eStatusReason.Active_Active));
                }

                //Loop through other matching associations that are still active
                foreach (var redundantRarityAssoc in otherActiveMatchingRarityAssocsNotOldest)
                {
                    //and deactivate them, as they are redundant
                    Helper.SetState(
                        localContext.OrganizationService,
                        redundantRarityAssoc.ToEntityReference(),
                        new OptionSetValue((int)ProxyClasses.Rarity_SourceAssociation.eStatus.Inactive),
                        new OptionSetValue((int)ProxyClasses.Rarity_SourceAssociation.eStatusReason.MergedWithDuplicate_Inactive));
                }

                return oldestMatchingRarityAssociation;
            }
        }


        private List<ProxyClasses.Rarity_SourceAssociation> GetMatchedRarityAssocs(
            IOrganizationService organisationService, 
            Guid sourceId, 
            Guid rarityId
        ) {
            var matchedRarityAssocs = new List<ProxyClasses.Rarity_SourceAssociation>();

            var queryExpression = new QueryExpression()
            {
                EntityName = ProxyClasses.Rarity_SourceAssociation.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                    ProxyClasses.Rarity_SourceAssociation.PrimaryIdAttribute,
                    ProxyClasses.Rarity_SourceAssociation.Properties.Rarity,
                    ProxyClasses.Rarity_SourceAssociation.Properties.Source,
                    ProxyClasses.Rarity_SourceAssociation.Properties.Status,
                    ProxyClasses.Rarity_SourceAssociation.Properties.StatusReason}),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {
                            Conditions =
                            {
                                new ConditionExpression(ProxyClasses.Rarity_SourceAssociation.Properties.Source, ConditionOperator.Equal, sourceId),
                                new ConditionExpression(ProxyClasses.Rarity_SourceAssociation.Properties.Rarity, ConditionOperator.Equal, rarityId)
                                //,new ConditionExpression(ProxyClasses.Rarity_SourceAssociation.Properties.Rarity_SourceAssociationId, ConditionOperator.NotEqual, targetRarityAssoc.Id)
                            }
                        }
                    }
                }
            };

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                matchedRarityAssocs.Add(new ProxyClasses.Rarity_SourceAssociation(result));
                
            }

            return matchedRarityAssocs;
        }


        private List<ProxyClasses.Antigen_SourceAssociation> GetAssociatedSourceAntigens(IOrganizationService organisationService, Guid sourceId)
        {
            var associatedSourceAntigens = new List<ProxyClasses.Antigen_SourceAssociation>();

            var queryExpression = new QueryExpression()
            {
                EntityName = ProxyClasses.Antigen_SourceAssociation.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                ProxyClasses.Antigen_SourceAssociation.PrimaryIdAttribute,
                                ProxyClasses.Antigen_SourceAssociation.Properties.Source,
                                ProxyClasses.Antigen_SourceAssociation.Properties.Antigen,
                                ProxyClasses.Antigen_SourceAssociation.Properties.AntigenResult,
                                ProxyClasses.Antigen_SourceAssociation.Properties.Status,
                                ProxyClasses.Antigen_SourceAssociation.Properties.StatusReason}),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {
                            Conditions =
                            {
                                new ConditionExpression(ProxyClasses.Antigen_SourceAssociation.Properties.Source, ConditionOperator.Equal, sourceId)
                            }
                        }
                    }
                }
            };

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                associatedSourceAntigens.Add(new ProxyClasses.Antigen_SourceAssociation(result));
            }

            return associatedSourceAntigens;
        }

        private Guid GetRarityIdForAssociation(IOrganizationService organisationService, Guid rarityAssocId)
        {
            var entityName = ProxyClasses.Rarity_SourceAssociation.LogicalName;
            var Guid = rarityAssocId;
            var ColumnSet = new ColumnSet(new string[] {
                            ProxyClasses.Rarity_SourceAssociation.Properties.Rarity
            });
        
            var result = (ProxyClasses.Rarity_SourceAssociation)organisationService.Retrieve(entityName,Guid,ColumnSet);

            return result.Rarity.Id;
        }

        private ProxyClasses.Rarity_SourceAssociation GetRarityAssocFromGuid(IOrganizationService organisationService, Guid rarityAssocId) 
        {
            var entityName = ProxyClasses.Rarity_SourceAssociation.LogicalName;
            var Guid = rarityAssocId;
            var ColumnSet = new ColumnSet(new string[] {
                            ProxyClasses.Rarity_SourceAssociation.Properties.Rarity_SourceAssociationId,
                            ProxyClasses.Rarity_SourceAssociation.Properties.Rarity,
                            ProxyClasses.Rarity_SourceAssociation.Properties.Source,
                            ProxyClasses.Rarity_SourceAssociation.Properties.Status,
                            ProxyClasses.Rarity_SourceAssociation.Properties.StatusReason
            });

            var result = (ProxyClasses.Rarity_SourceAssociation)organisationService.Retrieve(entityName, Guid, ColumnSet);

            return result;
        }
    }
}

