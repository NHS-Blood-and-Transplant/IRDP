namespace NHSBT.IRDP.Plugins
{
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;

    public class ContactPlugin : BasePlugin
    {

        public ContactPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.Create,
                EntityName = EntityNames.nhs_antigensourceassociation,
                PluginAction = PostCreateExecution
            });
        }

        protected void PostCreateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {

                var targetEntity = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];
                var postImage = (Entity)localContext.PluginExecutionContext.PostEntityImages["PostImage"];

                if (postImage.Contains("parentcustomerid") && postImage["parentcustomerid"] != null)
                {
                    var owningAccountId = ((EntityReference)postImage["parentcustomerid"]).Id;

                    var account = localContext.OrganizationService.Retrieve(ProxyClasses.Account.LogicalName, owningAccountId, new ColumnSet(new string[] { "ownerid" }));

                    var assignRequest = new AssignRequest
                    {
                        Assignee = (EntityReference)account["ownerid"],
                        Target = targetEntity.ToEntityReference()
                    };

                    // Execute the Request
                    localContext.OrganizationService.Execute(assignRequest);
                }
            }
        }
    }
}
