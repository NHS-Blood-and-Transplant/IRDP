
namespace NHSBT.IRDP.Plugins
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NHSBT.IRDP.Plugins.ProxyClasses;

    public class AntigenSourceAssociationPlugin : BasePlugin
    {

        public AntigenSourceAssociationPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PreOperation,
                MessageName = MessageNames.Create,
                EntityName = EntityNames.nhs_antigensourceassociation,
                PluginAction = PreCreateExecution
            });

            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PreOperation,
                MessageName = MessageNames.Update,
                EntityName = EntityNames.nhs_antigensourceassociation,
                PluginAction = PreUpdateExecution
            });

            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.SetState,
                EntityName = EntityNames.nhs_antigensourceassociation,
                PluginAction = SetStateExecution
            });

            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.SetStateDynamicEntity,
                EntityName = EntityNames.nhs_antigensourceassociation,
                PluginAction = SetStateExecution
            });
        }

        protected void PreCreateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {

                var targetEntity = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];

                var sourceId = ((EntityReference)targetEntity[Antigen_SourceAssociation.Properties.Source]).Id;
                var antigenId = ((EntityReference)targetEntity[Antigen_SourceAssociation.Properties.Antigen]).Id;
                var antigenResult = (OptionSetValue)targetEntity[Antigen_SourceAssociation.Properties.AntigenResult];
                var isExplicit = targetEntity.Contains(Antigen_SourceAssociation.Properties.Explicit) && (bool)targetEntity[Antigen_SourceAssociation.Properties.Explicit];

                DuplicateAntigenCheck(
                   localContext.OrganizationService,
                   targetEntity,
                   sourceId,
                   antigenId,
                   antigenResult,
                   isExplicit);
            }
        }

        protected void PreUpdateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {

                // Obtain the target entity from the Input Parameters.
                var targetEntity = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];
                var preImage = (Entity)localContext.PluginExecutionContext.PreEntityImages["PreImage"];

                if (targetEntity.Contains(Antigen_SourceAssociation.Properties.AntigenResult))
                {

                    var sourceId = ((EntityReference)preImage[Antigen_SourceAssociation.Properties.Source]).Id;
                    var antigenId = ((EntityReference)preImage[Antigen_SourceAssociation.Properties.Antigen]).Id;
                    var antigenResult = (OptionSetValue)targetEntity[Antigen_SourceAssociation.Properties.AntigenResult];
                    var isExplicit = targetEntity.Contains(Antigen_SourceAssociation.Properties.Explicit) && (bool)targetEntity[Antigen_SourceAssociation.Properties.Explicit];

                    DuplicateAntigenCheck(
                        localContext.OrganizationService,
                        targetEntity,
                        sourceId,
                        antigenId,
                        antigenResult,
                        isExplicit);
                }
            }
        }


        protected void SetStateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                var antigenSourceAssociationId = ((EntityReference)localContext.PluginExecutionContext.InputParameters["EntityMoniker"]).Id;
                var state = (OptionSetValue)localContext.PluginExecutionContext.InputParameters["State"];
                var status = (OptionSetValue)localContext.PluginExecutionContext.InputParameters["Status"];

                var preImage = (Entity)localContext.PluginExecutionContext.PreEntityImages["PreImage"];

                var sourceId = ((EntityReference)preImage[Antigen_SourceAssociation.Properties.Source]).Id;
                var antigenId = ((EntityReference)preImage[Antigen_SourceAssociation.Properties.Antigen]).Id;
                var antigenResult = (OptionSetValue)preImage[Antigen_SourceAssociation.Properties.AntigenResult];

                

                //If the record is being deactivated then we need to check if it makes any
                // contradictory antigen associations valid.
                if (state.Value == (int)Antigen_SourceAssociation.eStatus.Inactive)
                {
                    //set the explicit flag to false
                    SetExplicit(localContext.OrganizationService, antigenSourceAssociationId, isExplicit: false);

                    //...if the record had an active>contradiction status reason...
                    if (preImage.Contains(Antigen_SourceAssociation.Properties.Source) &&
                        preImage.Contains(Antigen_SourceAssociation.Properties.Antigen) &&
                        preImage.Contains(Antigen_SourceAssociation.Properties.AntigenResult) &&
                        preImage.Contains(Antigen_SourceAssociation.Properties.Status) &&
                        preImage.Contains(Antigen_SourceAssociation.Properties.StatusReason) &&
                        ((OptionSetValue)preImage[Antigen_SourceAssociation.Properties.Status]).Value == (int)Antigen_SourceAssociation.eStatus.Active &&
                        ((OptionSetValue)preImage[Antigen_SourceAssociation.Properties.StatusReason]).Value == (int)Antigen_SourceAssociation.eStatusReason.Contradiction_Active)
                    {

                        //Get the set of matching active antigens...
                        var matchedAntigens = GetMatchedAntigens(localContext.OrganizationService, sourceId, antigenId);

                        var activeMatchedAntigens = from antigen in matchedAntigens
                                                    where antigen.StatusReason == Antigen_SourceAssociation.eStatusReason.Contradiction_Active
                                                    select antigen;

                        //If only one is returned then...
                        if (activeMatchedAntigens.Count() == 1)
                        {
                            var activeMatchedAntigen = activeMatchedAntigens.First();

                            //...set the state to  from Active/Contradiction to Active > Active

                            Helper.SetState(
                                    localContext.OrganizationService,
                                    activeMatchedAntigen.ToEntityReference(),
                                    new OptionSetValue((int)Antigen_SourceAssociation.eStatus.Active),
                                    new OptionSetValue((int)Antigen_SourceAssociation.eStatusReason.Active_Active));
                        }
                        else if (activeMatchedAntigens.Count() == 2) //error handling
                            //as UAT had a 2 instances where there were two matching antigens iwth the same result
                        {
                            var firstActiveMatchedAntigen = activeMatchedAntigens.First();
                            var secondActiveMatchedAntigen = activeMatchedAntigens.ElementAt(1);

                            if (firstActiveMatchedAntigen.AntigenResult_OptionSetValue == secondActiveMatchedAntigen.AntigenResult_OptionSetValue)
                            {   //if we've got antigen assocs with the same result
                                //then deactivate the older one as a merged duplicate
                                Helper.SetState(
                                    localContext.OrganizationService,
                                    secondActiveMatchedAntigen.ToEntityReference(),
                                    new OptionSetValue((int)Antigen_SourceAssociation.eStatus.Inactive),
                                    new OptionSetValue((int)Antigen_SourceAssociation.eStatusReason.MergedWithDuplicate_Inactive));

                            }
                        }
                    }
                }
                else //if its moving to an active state, then we need to 
                {
                    //Get the set of matching active antigens...
                    var matchedAntigens = GetMatchedAntigens(localContext.OrganizationService, sourceId, antigenId);

                    var activeMatchedAntigens = from antigen in matchedAntigens
                                                where antigen.Status == Antigen_SourceAssociation.eStatus.Active
                                                select antigen;

                    if (activeMatchedAntigens.Count() > 1)
                    {
                        foreach (var activeMatchedAntigen in activeMatchedAntigens)
                        {
                            if (activeMatchedAntigen.StatusReason != Antigen_SourceAssociation.eStatusReason.Contradiction_Active)
                            {
                                //...set this new record status to Active > Contradiction
                                Helper.SetState(
                                    localContext.OrganizationService,
                                    activeMatchedAntigen.ToEntityReference(),
                                    new OptionSetValue((int)Antigen_SourceAssociation.eStatus.Active),
                                    new OptionSetValue((int)Antigen_SourceAssociation.eStatusReason.Contradiction_Active));
                            }
                        }
                    }
                }

                CheckIfSourceHasAnyContradictions(localContext.OrganizationService, sourceId);
            }
        }

        private void DuplicateAntigenCheck(
            IOrganizationService organisationService, 
            Entity targetEntity, 
            Guid sourceId, 
            Guid antigenId, 
            OptionSetValue antigenResult,
            bool isExplicit
        ) {
            var matchedAntigens = GetMatchedAntigens(organisationService, sourceId, antigenId);

            //So if any mathcing antigens exist in any state then...
            if (matchedAntigens.Count() > 0)
            {
                //Query a direct active match
                var activeResultMatch = (from antigenAssoc in matchedAntigens
                                         where antigenAssoc.Status == Antigen_SourceAssociation.eStatus.Active
                                         && antigenAssoc.AntigenResult_OptionSetValue.Value == antigenResult.Value
                                         && antigenAssoc.Id != targetEntity.Id
                                         select antigenAssoc).FirstOrDefault();

                //If a direct active match exists...
                if (activeResultMatch != null)
                {
                    //...deactivate this targret entity (i.e. the newly created one)
                    Helper.SetState(
                        organisationService,
                        targetEntity.ToEntityReference(),
                        new OptionSetValue((int)Antigen_SourceAssociation.eStatus.Inactive),
                        new OptionSetValue((int)Antigen_SourceAssociation.eStatusReason.MergedWithDuplicate_Inactive)
                    );

                    //The record is created by a user then...
                    if (isExplicit && (!activeResultMatch.Explicit.HasValue || !activeResultMatch.Explicit.Value))
                    {
                        //...set explicit on the active match
                        SetExplicit(organisationService, activeResultMatch.Id);

                    }
                }
                else //...no active match...
                {
                    //...query for inactive match...
                    var inactiveResultMatch = (from antigen in matchedAntigens
                                               where antigen.Status == Antigen_SourceAssociation.eStatus.Inactive
                                               where antigen.AntigenResult_OptionSetValue.Value == antigenResult.Value
                                               where antigen.Id != targetEntity.Id
                                               select antigen).FirstOrDefault();

                    //If an inactive match exists then...
                    if (inactiveResultMatch != null)
                    {
                        //...deactivate the targret entity
                        targetEntity[Antigen_SourceAssociation.Properties.Status] = new OptionSetValue((int)Antigen_SourceAssociation.eStatus.Inactive);
                        targetEntity[Antigen_SourceAssociation.Properties.StatusReason] = new OptionSetValue((int)Antigen_SourceAssociation.eStatusReason.MergedWithDuplicate_Inactive);

                        //Reactivate the inactive record
                        Helper.SetState(
                            organisationService,
                            inactiveResultMatch.ToEntityReference(),
                            new OptionSetValue((int)Antigen_SourceAssociation.eStatus.Active),
                            new OptionSetValue((int)Antigen_SourceAssociation.eStatusReason.Active_Active));

                        //If the record is created by a user then...
                        if (isExplicit && (!inactiveResultMatch.Explicit.HasValue || !inactiveResultMatch.Explicit.Value))
                        {
                            SetExplicit(organisationService, inactiveResultMatch.Id);
                        }
                    }
                    else //No result match so...
                    {
                        IEnumerable<Antigen_SourceAssociation> contradictoryAntigens;

                        if (targetEntity.Id == Guid.Empty)
                        {
                            contradictoryAntigens = from antigen in matchedAntigens
                                                    where antigen.Status == Antigen_SourceAssociation.eStatus.Active
                                                    select antigen;
                        }
                        else
                        {
                            contradictoryAntigens = from antigen in matchedAntigens
                                                    where antigen.Status == Antigen_SourceAssociation.eStatus.Active
                                                    where antigen.Id != targetEntity.Id
                                                    select antigen;
                        }


                        if (contradictoryAntigens.Count() > 0)
                        {
                            targetEntity[Antigen_SourceAssociation.Properties.Status] = new OptionSetValue((int)Antigen_SourceAssociation.eStatus.Active);
                            targetEntity[Antigen_SourceAssociation.Properties.StatusReason] = new OptionSetValue((int)Antigen_SourceAssociation.eStatusReason.Contradiction_Active);

                            foreach (var contradictoryAntigen in contradictoryAntigens)
                            {
                                if (contradictoryAntigen.StatusReason != Antigen_SourceAssociation.eStatusReason.Contradiction_Active)

                                    //...set this new record status to Active > Contradiction
                                    Helper.SetState(
                                        organisationService,
                                        contradictoryAntigen.ToEntityReference(),
                                        new OptionSetValue((int)Antigen_SourceAssociation.eStatus.Active),
                                        new OptionSetValue((int)Antigen_SourceAssociation.eStatusReason.Contradiction_Active));
                            }
                        }

                        
                    }
                }
            }
            
            CheckIfSourceHasAnyContradictions(organisationService, sourceId);
        }


        private void SetExplicit(IOrganizationService organisationService, Guid antigenSourceAssociationId, bool isExplicit = true)
        {

            var antigenSourceAssociation = new Antigen_SourceAssociation();

            antigenSourceAssociation.Id = antigenSourceAssociationId;
            antigenSourceAssociation.Explicit = isExplicit;

            organisationService.Update(antigenSourceAssociation);
        }

        private void CheckIfSourceHasAnyContradictions(IOrganizationService organisationService, Guid sourceId)
        {
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
            else
            {
                Helper.SetState(
                   organisationService,
                   new EntityReference(RareBloodSource.LogicalName, sourceId),
                   new OptionSetValue((int)RareBloodSource.eStatus.Active),
                   new OptionSetValue((int)RareBloodSource.eStatusReason.Active_Active));
            }
        }
        private List<Antigen_SourceAssociation> GetMatchedAntigens(IOrganizationService organisationService, Guid sourceId, Guid antigenId)
        {
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
                                new ConditionExpression(Antigen_SourceAssociation.Properties.Antigen, ConditionOperator.Equal, antigenId)
                            }
                        }
                    }
                }
            };

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                matchedAntigens.Add(new Antigen_SourceAssociation(result));
            }

            return matchedAntigens;
        }
    }
}
