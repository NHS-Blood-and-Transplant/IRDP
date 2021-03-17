namespace NHSBT.IRDP.Plugins
{
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Query;
    using System;

    public class AccountPlugin: BasePlugin
    {
      
        public AccountPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PreOperation,
                MessageName = MessageNames.Create,
                EntityName = EntityNames.account,
                PluginAction = PreCreateExecution
            });

            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.Create,
                EntityName = EntityNames.account,
                PluginAction = PostCreateExecution
            });

            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.Update,
                EntityName = EntityNames.account,
                PluginAction = PostUpdateExecution
            });
        }

        protected void PreCreateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                // Obtain the target entity from the Input Parameters.
                var targetEntity = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];

                if (!targetEntity.Contains("nhs_team"))
                {
                    var teamName = ((string)targetEntity["name"]).ToUpper() + " TEAM";

                    var teamId = CreateTeam(localContext.OrganizationService, teamName);

                    targetEntity["nhs_team"] = new EntityReference(ProxyClasses.Team.LogicalName, teamId);
                }
            }
        }

        private Guid CreateTeam(IOrganizationService organisationService, string teamName)
        {
            var team = new ProxyClasses.Team
            {
                TeamName = teamName,
                BusinessUnit = GetBusinessUnitId(organisationService)
            };

            var teamId = organisationService.Create(team);

            var securityRoleName = "IRDP Team";

            // Retrieve a role from CRM.
            QueryExpression query = new QueryExpression
            {
                EntityName = ProxyClasses.SecurityRole.LogicalName,
                ColumnSet = new ColumnSet("roleid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                        {
                            // You would replace the condition below with an actual role
                            // name, or skip this query if you had a role id.
                            new ConditionExpression
                            {
                                AttributeName = "name",
                                Operator = ConditionOperator.Equal,
                                Values = { securityRoleName }
                            }
                        }
                }
            };

            var securityRole = (ProxyClasses.SecurityRole)organisationService.RetrieveMultiple(query).Entities[0];

            // Add the role to the team.
            organisationService.Associate(
                   ProxyClasses.Team.LogicalName,
                   teamId,
                   new Relationship("teamroles_association"),
                   new EntityReferenceCollection() { securityRole.ToEntityReference() });

            return teamId;
        }

        private EntityReference GetBusinessUnitId(IOrganizationService organisationService)
        {
            var businessUnitId = Guid.Empty;

            string fetchXML = @"<fetch distinct='false' no-lock='false' mapping='logical'>
                                    <entity name='businessunit'>
                                        <attribute name='createdon'/>   
                                    </entity>
                               </fetch>";
           
            var entityCollection = organisationService.RetrieveMultiple(new FetchExpression(fetchXML));

            //Use the first one!
            businessUnitId = (Guid)entityCollection.Entities[0].Attributes["businessunitid"];
           
            return new EntityReference("businessunit", businessUnitId);
        }



        protected void PostCreateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                var targetEntity = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];

                AssignRequest assign = new AssignRequest
                {
                    Assignee = (EntityReference)targetEntity["nhs_team"],
                    Target = targetEntity.ToEntityReference()
                };

                // Execute the Request
                localContext.OrganizationService.Execute(assign);
            }
        }
   
        protected void PostUpdateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                var targetEntity = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];
                var preImage = (Entity)localContext.PluginExecutionContext.PreEntityImages["PreImage"];

                if (targetEntity.Contains("name") && preImage.Contains("ownerid"))
                {

                    var team = (ProxyClasses.Team)localContext.OrganizationService.Retrieve(ProxyClasses.Team.LogicalName, ((EntityReference)preImage["ownerid"]).Id, new ColumnSet(new string[] { "name" }));

                    team.TeamName = targetEntity["name"].ToString().ToUpper() + " - TEAM";

                    var updateRequest = new UpdateRequest()
                    {
                        Target = team
                    };

                    // Execute the Request
                    localContext.OrganizationService.Execute(updateRequest);
                }
            }
        }
    }
}
