using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using NHSBT.IRDP.Plugins.ProxyClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NHSBT.IRDP.Plugins
{
    public partial class Helper
    {
        public static void SetState(IOrganizationService organisationService, EntityReference entityReference, OptionSetValue state, OptionSetValue statusReason)
        {
            var helper = new Helper();
            
            bool isUpdateNeeded = helper.isStatusChangeRequired(
                organisationService, 
                entityReference, 
                statusReason
            );

            if (isUpdateNeeded)
            {

                var setStatusRequest = new SetStateRequest()
                {
                    EntityMoniker = entityReference,
                    State = state,
                    Status = statusReason
                };

                //...set status to be active
                organisationService.Execute(setStatusRequest);
            }
        }

        private bool isStatusChangeRequired(IOrganizationService organisationService, EntityReference targetEntityRef, OptionSetValue targetStatusReason)
        {
            var targetEntity = organisationService.Retrieve(
                targetEntityRef.LogicalName,
                targetEntityRef.Id,
                new ColumnSet(new string[] {
                    "statuscode" //i.e. the status reason
                })
            );

            var isChangeRequired = (OptionSetValue)targetEntity.Attributes["statuscode"] != targetStatusReason;

            return isChangeRequired;
        }

        public static List<OptionMetadata> GetOptionSet(IOrganizationService organisationService, string name)
        {
            var optionSet = new List<OptionMetadata>();

            // Use the RetrieveOptionSetRequest message to retrieve  
            // a global option set by it's name.
            var retrieveOptionSetRequest = new RetrieveOptionSetRequest
            {
                Name = name
            };

            // Execute the request.
            var retrieveOptionSetResponse = (RetrieveOptionSetResponse)organisationService.Execute(retrieveOptionSetRequest);


            // Access the retrieved OptionSetMetadata.
            var retrievedOptionSetMetadata = (OptionSetMetadata)retrieveOptionSetResponse.OptionSetMetadata;

            foreach (var optionMetadata in retrievedOptionSetMetadata.Options)
            {
                optionSet.Add(optionMetadata);
            }

            // Get the current options list for the retrieved attribute.
            return optionSet;
        }

        public static Dictionary<string, string> GetEnvironmentVariables(IOrganizationService organisationService)
        {
            var environmentVariables = new Dictionary<string, string>();

            var queryExpression = new QueryExpression()
            {
                EntityName = "nhs_configentity",
                ColumnSet = new ColumnSet(new string[] {
                                "nhs_key",
                                "nhs_value" })

            };

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                environmentVariables.Add(result["nhs_key"].ToString(), result["nhs_value"].ToString());
            }

            return environmentVariables;
        }

        public static List<Rarity> GetAllRarities(IOrganizationService organisationService)
        {
            var rarities = new List<Rarity>();

            var queryExpression = new QueryExpression()
            {
                EntityName = Rarity.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                Rarity.PrimaryIdAttribute,
                                Rarity.PrimaryNameAttribute })
            };

            queryExpression.AddOrder(Rarity.PrimaryNameAttribute, OrderType.Ascending);

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                if (result[Rarity.PrimaryNameAttribute].ToString() != "[NONE]")
                    rarities.Add(new Rarity(result));
            }

            return rarities;
        }

        public static List<Antigen> GetAllAntigens(IOrganizationService organisationService)
        {
            var antigens = new List<Antigen>();

            var queryExpression = new QueryExpression()
            {
                EntityName = Antigen.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                Antigen.PrimaryIdAttribute,
                                Antigen.PrimaryNameAttribute })

            };

            queryExpression.AddOrder(Antigen.PrimaryNameAttribute, OrderType.Ascending);

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                antigens.Add(new Antigen(result));
            }

            return antigens;
        }

        public static IEnumerable<Antigen_SourceAssociation> GetAntigenAssociationsForSource(IOrganizationService organisationService, Guid sourceId)
        {
            //N.B. The related link record can be queried thus: antigenAssociation.GetAttributeValue<AliasedValue>(entityAlias + "." + nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.PrimaryIdAttribute).Value.ToString()
            var entityAlias = "implicitAssociationLink";
            var antigenSourceAssociations = new List<Antigen_SourceAssociation>();

            var queryExpression = new QueryExpression()
            {
                EntityName = Antigen_SourceAssociation.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                Antigen_SourceAssociation.PrimaryIdAttribute,
                                Antigen_SourceAssociation.PrimaryNameAttribute,
                                Antigen_SourceAssociation.Properties.Antigen,
                                Antigen_SourceAssociation.Properties.AntigenResult,
                                Antigen_SourceAssociation.Properties.Status,
                                Antigen_SourceAssociation.Properties.StatusReason,
                                Antigen_SourceAssociation.Properties.Explicit
                }),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {
                            Conditions =
                            {
                                new ConditionExpression(Antigen_SourceAssociation.Properties.Source, ConditionOperator.Equal, sourceId)
                            }
                        }
                    }
                }
            };

            var implictAssociationLink = new LinkEntity
            {
                LinkToEntityName = nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.LogicalName,
                LinkFromAttributeName = Antigen_SourceAssociation.PrimaryIdAttribute,
                LinkToAttributeName = nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.Properties.Nhs_antigensourceassociationid,
                JoinOperator = JoinOperator.LeftOuter,
                EntityAlias = entityAlias,
                Columns = new ColumnSet(new string[] { nhs_AnitenSrcAssoc_nhs_RaritySrcAssoc.PrimaryIdAttribute })
            };

            queryExpression.LinkEntities.Add(implictAssociationLink);

            queryExpression.AddOrder(Antigen_SourceAssociation.Properties.CreatedOn, OrderType.Ascending);

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                antigenSourceAssociations.Add(new Antigen_SourceAssociation(result));
            }

            return antigenSourceAssociations;
        }

        public static List<RareBloodSource> GetSourcesForAccount(IOrganizationService organisationService, Guid accountId)
        {
            var sources = new List<RareBloodSource>();

            var queryExpression = new QueryExpression()
            {
                EntityName = RareBloodSource.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                RareBloodSource.PrimaryIdAttribute,
                                RareBloodSource.PrimaryNameAttribute,
                                RareBloodSource.Properties.ContributorCode,
                                RareBloodSource.Properties.Status,
                                RareBloodSource.Properties.StatusReason}),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {
                            Conditions =
                            {
                                new ConditionExpression(RareBloodSource.Properties.ParentAccount, ConditionOperator.Equal, accountId)
                            }
                        }
                    }
                }
            };

            queryExpression.AddOrder(RareBloodSource.PrimaryNameAttribute, OrderType.Ascending);

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                sources.Add(new RareBloodSource(result));
            }

            return sources;
        }

        public static List<Task> GetActiveTasksForAccount(IOrganizationService organisationService, Guid accountId)
        {
            var activeTasks = new List<Task>();

            Entity retrievedAccount = organisationService.Retrieve(
                Account.LogicalName,
                accountId,
                new ColumnSet("owningteam")
            );

            var teamId = ((EntityReference)retrievedAccount["owningteam"]).Id;

            var queryExpression = new QueryExpression()
            {
                EntityName = Task.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                Task.PrimaryIdAttribute,
                                Task.PrimaryNameAttribute,
                                Task.Properties.ActivityStatus,
                                Task.Properties.StatusReason}),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {

                            //FilterOperator = LogicalOperator.And,
                            Conditions =
                            {
                                new ConditionExpression(Task.Properties.OwningTeam, ConditionOperator.Equal, teamId),
                                //new ConditionExpression(Task.Properties.ActivityStatus, ConditionOperator.Equal, Task.eActivityStatus.Open)
                            }
                        }
                    }
                }
            };

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                activeTasks.Add(new Task(result));
            }

            return activeTasks;
        }

        public static IEnumerable<Rarity_SourceAssociation> GetRarityAssociationsForSource(IOrganizationService organisationService, Guid sourceId)
        {
            var raritySourceAssociations = new List<Rarity_SourceAssociation>();

            var queryExpression = new QueryExpression()
            {
                EntityName = Rarity_SourceAssociation.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                Rarity_SourceAssociation.PrimaryIdAttribute,
                                Rarity_SourceAssociation.PrimaryNameAttribute,
                                Rarity_SourceAssociation.Properties.Rarity,
                                Rarity_SourceAssociation.Properties.Status,
                                Rarity_SourceAssociation.Properties.StatusReason}),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {
                            Conditions =
                            {
                                new ConditionExpression(Rarity_SourceAssociation.Properties.Source, ConditionOperator.Equal, sourceId)
                            }
                        }
                    }
                }
            };

            queryExpression.AddOrder(Rarity_SourceAssociation.Properties.CreatedOn, OrderType.Ascending);

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                raritySourceAssociations.Add(new Rarity_SourceAssociation(result));
            }

            return raritySourceAssociations;
        }

        public static List<ProxyClasses.Antigen_RarityAssociation> GetAntigensImpliedByRarity(IOrganizationService organisationService, Guid rarityId)
        {
            var implicitAntigens = new List<ProxyClasses.Antigen_RarityAssociation>();

            var queryExpression = new QueryExpression()
            {
                EntityName = ProxyClasses.Antigen_RarityAssociation.LogicalName,
                ColumnSet = new ColumnSet(new string[] {
                                ProxyClasses.Antigen_RarityAssociation.PrimaryIdAttribute,
                                ProxyClasses.Antigen_RarityAssociation.Properties.Rarity,
                                ProxyClasses.Antigen_RarityAssociation.Properties.Antigen,
                                ProxyClasses.Antigen_RarityAssociation.Properties.AntigenResult,
                                ProxyClasses.Antigen_RarityAssociation.Properties.Status,
                                ProxyClasses.Antigen_RarityAssociation.Properties.StatusReason}),
                Criteria = new FilterExpression()
                {
                    Filters =
                    {
                        new FilterExpression()
                        {
                            Conditions =
                            {
                                new ConditionExpression(ProxyClasses.Antigen_RarityAssociation.Properties.Rarity, ConditionOperator.Equal, rarityId)
                            }
                        }
                    }
                }
            };

            var results = organisationService.RetrieveMultiple(queryExpression);

            foreach (var result in results.Entities)
            {
                implicitAntigens.Add(new ProxyClasses.Antigen_RarityAssociation(result));
            }

            return implicitAntigens;
        }


    }
}
