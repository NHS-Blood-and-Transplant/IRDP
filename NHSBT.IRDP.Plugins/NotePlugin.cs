namespace NHSBT.IRDP.Plugins
{
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using NHSBT.IRDP.Plugins.ProxyClasses;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class NotePlugin : BasePlugin
    {
        /// <summary>
        /// This plugin reads in a CSV file of a specific format. The document will be validated for file errors, if one is found the whole import is rejected. 
        /// It then validates for row errors, if any are found Tasks are created regarding each failed row validation. Once the validation is successful the data 
        /// is passed to the out-of-the-box (OOTB) file import ulility to create a RareBloodSourceImport record. The plugin for this entity parses the valid raw data 
        /// and handles the create/update requests for Rare Blood Source records and Rarity/Antigen Asscoiatin records. It has been designed this way to get around 
        /// the 120 second plugin execution limitation and the fact that the OOTB file import utility cannot handle upserts without having previously exported the data
        /// file out of Dynamics 365 CE.
        /// </summary>
        /// <param name="unsecureConfig"></param>
        /// <param name="secureConfig"></param>


        public NotePlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            // Register for any specific events by instantiating a new instance of the 'PluginEvent' class and registering it
            base.RegisteredEvents.Add(new PluginEvent()
            {
                Stage = eStage.PostOperation,
                MessageName = MessageNames.Create,
                EntityName = EntityNames.annotation,
                PluginAction = PostCreateExecution
            });
        }

        protected void PostCreateExecution(IServiceProvider serviceProvider)
        {
            // Use a 'using' statement to dispose of the service context properly
            // To use a specific early bound entity replace the 'Entity' below with the appropriate class type
            using (var localContext = new LocalPluginContext<Entity>(serviceProvider))
            {
                // Obtain the target entity from the Input Parameters.
                var targetEntity = new Note((Entity)localContext.PluginExecutionContext.InputParameters["Target"]);
                //var postImage = new Note((Entity)localContext.PluginExecutionContext.PostEntityImages["PostImage"]);

                if (targetEntity.Document != string.Empty && targetEntity.Regarding.LogicalName == Account.LogicalName)
                {
                    var environmentVariables = Helper.GetEnvironmentVariables(localContext.OrganizationService);

                    if (!environmentVariables.ContainsKey("Impersonation User Id"))
                    {
                        throw new InvalidPluginExecutionException("NHSBT Note Plugin Exception: Failed to find environment variable 'Impersonation User Id'. A System Administrator needs to create a Config Entity record with the user id of a user with persmission to read the User entity.");
                    }

                    var userName = GetUserNameByImpersonation(serviceProvider, localContext.PluginExecutionContext.InitiatingUserId, new Guid(environmentVariables["Impersonation User Id"]));

                    ProcessAttachment(localContext.OrganizationService, localContext.PluginExecutionContext.InitiatingUserId, targetEntity, userName);
                }
            }
        }

        private string GetUserNameByImpersonation(IServiceProvider serviceProvider, Guid userId, Guid impersonationUserId)
        {

            var userName = string.Empty;

            IOrganizationServiceFactory organisationServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService organisationService = organisationServiceFactory.CreateOrganizationService(impersonationUserId);

            var user = organisationService.Retrieve("systemuser", userId, new ColumnSet("fullname"));
            userName = user.GetAttributeValue<string>("fullname");

            return userName;
        }

        private void ProcessAttachment(IOrganizationService organisationService, Guid userId, Note note, string userName)
        {
            List<ParsedFileRow> parsedFileRows;

            var account = new Account(organisationService.Retrieve(Account.LogicalName, note.Regarding.Id, new ColumnSet(new string[] { Account.Properties.OwningTeam, Account.PrimaryNameAttribute })));

            ClearPreviousActiveTasks(organisationService, account.Id);

            if (TryParseFile(
                organisationService,
                Convert.FromBase64String(note.Document),
                note.FileName,
                userName,
                DateTime.Now,
                note.Regarding,
                account,
                out parsedFileRows))
            {
                var accountSources = Helper.GetSourcesForAccount(organisationService, account.Id);
                var regex = new Regex("^[0-9]*$");

                var validFileRows = from parsedFileRow in parsedFileRows
                                    where parsedFileRow.IsValid
                                    select parsedFileRow;

                var sourcesToCreate = validFileRows.Where(vfr => accountSources.All(
                    source =>
                        source.ContributorCode.TrimStart(new Char[] { '0' }) != vfr.Source.ContributorCode.TrimStart(new Char[] { '0' })
                    )
                );

                List<ParsedFileRow> sourcesToCreateList = sourcesToCreate.Cast<ParsedFileRow>().ToList();

                var sourcesToUpdate = validFileRows.Where(vfr => accountSources.Any(
                    source =>
                        source.ContributorCode.TrimStart(new Char[] { '0' }) == vfr.Source.ContributorCode.TrimStart(new Char[] { '0' })
                    )
                );

                var sourcesToRemove = accountSources.Where(source => validFileRows.All(
                        vfr => (
                            vfr.Source.ContributorCode.TrimStart(new Char[] { '0' }) != source.ContributorCode.TrimStart(new Char[] { '0' })
                        )
                    )
                );

                var fileData = GetFormattedFileData(note.Regarding.Id, sourcesToCreate, sourcesToUpdate, sourcesToRemove, accountSources);

                ImportRecords(organisationService, userId, account.AccountName, fileData.ToString(), note.FileName);

                HandleRowExceptions(organisationService, parsedFileRows, accountSources, account.OwningTeam);
            }
        }

        private void ClearPreviousActiveTasks(IOrganizationService organisationService, Guid accountId)
        {
            var activeTasks = Helper.GetActiveTasksForAccount(organisationService, accountId);

            foreach (var activeTask in activeTasks)
            {
                Helper.SetState(
                    organisationService,
                    activeTask.ToEntityReference(),
                    new OptionSetValue((int)Task.eActivityStatus.Canceled),
                    new OptionSetValue((int)Task.eStatusReason.Canceled_Canceled));
            }
        }

        private string GetFormattedFileData(Guid accountId, IEnumerable<ParsedFileRow> sourcesToCreate, IEnumerable<ParsedFileRow> sourcesToUpdate, IEnumerable<RareBloodSource> sourcesToRemove, List<RareBloodSource> accountSources)
        {
            var fileData = new StringBuilder();
            var lineData = new StringBuilder();


            fileData.AppendLine("'nhs_rarebloodsource','nhs_parentaccount','nhs_headerrow','nhs_datarow','nhs_iscreate','nhs_isupdate','nhs_isdeactivate'");

            foreach (var parsedFileRow in sourcesToCreate)
            {
                lineData.Clear();

                lineData.Append("'',");
                lineData.Append("'" + accountId.ToString() + "',");
                lineData.Append("'" + parsedFileRow.Source.HeaderRow + "',");
                lineData.Append("'" + parsedFileRow.Source.Rawdata + "',");
                lineData.Append("'Yes',");
                lineData.Append("'',");
                lineData.Append("''");

                fileData.AppendLine(lineData.ToString());
            }

            //...for each parsed file row...
            List<RareBloodSource> sourceContributorCodesAlreadyRepresented = new List<RareBloodSource>();

            foreach (var parsedFileRow in sourcesToUpdate)
            {
                var regex = new Regex("^[0-9]*$");
                var rareBloodSource = accountSources.Where(source => (parsedFileRow.Source.ContributorCode == source.ContributorCode || (regex.IsMatch(source.ContributorCode) && regex.IsMatch(parsedFileRow.Source.ContributorCode) && Convert.ToInt32(source.ContributorCode) == Convert.ToInt32(parsedFileRow.Source.ContributorCode))));

                //test to see if a source with this contirbutor code has already been updated
                // (happens if there are sources with duplicate IDs within that account/institution)
                bool isSourceContributorCodeAlreadyRepresented = sourceContributorCodesAlreadyRepresented.Where(
                    sourceAlreadyRepresented => sourceAlreadyRepresented.ContributorCode == parsedFileRow.Source.ContributorCode
                    ).FirstOrDefault() != null;

                if (!isSourceContributorCodeAlreadyRepresented)
                {
                    lineData.Clear();

                    lineData.Append("'" + rareBloodSource.FirstOrDefault().Id.ToString() + "',");
                    lineData.Append("'" + accountId.ToString() + "',");
                    lineData.Append("'" + parsedFileRow.Source.HeaderRow + "',");
                    lineData.Append("'" + parsedFileRow.Source.Rawdata + "',");
                    lineData.Append("'',");
                    lineData.Append("'Yes',");
                    lineData.Append("''");

                    fileData.AppendLine(lineData.ToString());

                    sourceContributorCodesAlreadyRepresented.Add(parsedFileRow.Source);
                }
            }

            //For all those Rare Blood Srouces that require removal...
            foreach (var rareBloodSource in sourcesToRemove)
            {
                if (rareBloodSource.Status == ProxyClasses.RareBloodSource.eStatus.Active
                    || rareBloodSource.StatusReason == ProxyClasses.RareBloodSource.eStatusReason.ValidationErrors_Inactive)
                {
                    lineData.Clear();

                    lineData.Append("'" + rareBloodSource.RareBloodSourceId.ToString() + "',");
                    lineData.Append("'" + accountId.ToString() + "',");
                    lineData.Append("'',");
                    lineData.Append("'',");
                    lineData.Append("'',");
                    lineData.Append("'',");
                    lineData.Append("'Yes'");

                    fileData.AppendLine(lineData.ToString());
                }
                
            }

            return fileData.ToString();
        }

       
        private void ImportRecords(IOrganizationService organisationService, Guid userId, string accountName, string fileData, string fileName)
        {
            // Create an import map.
            var importMapId = organisationService.Create(
                new DataMap()
                {
                    MapName = "Import Map " + DateTime.Now.Ticks.ToString(),
                    Source = fileName,
                    Description = "Rare Blood Source import - " + accountName,
                    EntitiesPerFile = DataMap.eEntitiesPerFile.SingleEntityPerFile,
                    EntityState = EntityState.Created
                });


            #region Column Rare Blood Source

            // Create the mapping.
            var columnMappingRareBloodSourceId = organisationService.Create(
                new ColumnMapping()
                {
                    // Set source properties.
                    SourceAttribute = RareBloodSourceImport.Properties.RareBloodSource,
                    SourceEntityName = "Rare Blood Source Import",

                    // Set target properties.
                    TargetAttribute = RareBloodSourceImport.Properties.RareBloodSource,
                    TargetEntity = RareBloodSourceImport.LogicalName,

                    // Relate this column mapping with the data map.
                    DataMapID = new EntityReference(DataMap.LogicalName, importMapId),

                    // Force this column to be processed.
                    ProcessCode = ColumnMapping.eProcessCode.Process
                });

            // Because we created a column mapping of type lookup, we need to specify lookup details in a lookupmapping.
            // One lookupmapping will be for the parent account, and the other for the current record.
            // This lookupmapping is important because without it the current record
            // cannot be used as the parent of another record.

            // Create a lookup mapping to the parent account.  
            var lookupMappingRareBloodSource = new LookupMapping()
            {
                // Relate this mapping with its parent column mapping.
                ColumnMappingId = new EntityReference(ColumnMapping.LogicalName, columnMappingRareBloodSourceId),

                // Force this column to be processed.
                ProcessCode = LookupMapping.eProcessCode.Process,

                // Set the lookup for a Rare Blood Source entity by its name attribute.
                LookupFieldName = RareBloodSource.PrimaryIdAttribute,
                LookupEntityName = RareBloodSource.LogicalName,
                LookupSource = LookupMapping.eLookupSource.System
            };

            // Create the lookup mapping.
            organisationService.Create(lookupMappingRareBloodSource);

            #endregion

            #region Column Parent Account

            // Create the mapping.
            var columnMappingParentAccountId = organisationService.Create(
                new ColumnMapping()
                {
                    // Set source properties.
                    SourceAttribute = RareBloodSourceImport.Properties.ParentAccount,
                    SourceEntityName = "Rare Blood Source Import",

                    // Set target properties.
                    TargetAttribute = RareBloodSourceImport.Properties.ParentAccount,
                    TargetEntity = RareBloodSourceImport.LogicalName,

                    // Relate this column mapping with the data map.
                    DataMapID = new EntityReference(DataMap.LogicalName, importMapId),

                    // Force this column to be processed.
                    ProcessCode = ColumnMapping.eProcessCode.Process
                });

            // Because we created a column mapping of type lookup, we need to specify lookup details in a lookupmapping.
            // One lookupmapping will be for the parent account, and the other for the current record.
            // This lookupmapping is important because without it the current record
            // cannot be used as the parent of another record.

            // Create a lookup mapping to the parent account.  
            var lookupMappingParentAccount = new LookupMapping()
            {
                // Relate this mapping with its parent column mapping.
                ColumnMappingId = new EntityReference(ColumnMapping.LogicalName, columnMappingParentAccountId),

                // Force this column to be processed.
                ProcessCode = LookupMapping.eProcessCode.Process,

                // Set the lookup for a Rare Blood Source entity by its name attribute.
                LookupFieldName = Account.PrimaryIdAttribute,
                LookupEntityName = Account.LogicalName,
                LookupSource = LookupMapping.eLookupSource.System
            };

            // Create the lookup mapping.
            organisationService.Create(lookupMappingParentAccount);

            #endregion

            #region Column Header Row

            // Create a column mapping for a 'text' type field.
            var columnMappingHeaderRow = new ColumnMapping()
            {
                // Set source properties.
                SourceAttribute = RareBloodSourceImport.Properties.HeaderRow,
                SourceEntityName = "Rare Blood Source Import",

                // Set target properties.
                TargetAttribute = RareBloodSourceImport.Properties.HeaderRow,
                TargetEntity = RareBloodSourceImport.LogicalName,

                // Relate this column mapping with the data map.
                DataMapID = new EntityReference(DataMap.LogicalName, importMapId),

                // Force this column to be processed.
                ProcessCode = ColumnMapping.eProcessCode.Process
            };

            // Create the mapping.
            organisationService.Create(columnMappingHeaderRow);

            #endregion

            #region Column Data Row

            // Create a column mapping for a 'text' type field.
            var columnMappingDataRow = new ColumnMapping()
            {
                // Set source properties.
                SourceAttribute = RareBloodSourceImport.Properties.DataRow,
                SourceEntityName = "Rare Blood Source Import",

                // Set target properties.
                TargetAttribute = RareBloodSourceImport.Properties.DataRow,
                TargetEntity = RareBloodSourceImport.LogicalName,

                // Relate this column mapping with the data map.
                DataMapID = new EntityReference(DataMap.LogicalName, importMapId),

                // Force this column to be processed.
                ProcessCode = ColumnMapping.eProcessCode.Process
            };

            // Create the mapping.
            organisationService.Create(columnMappingDataRow);

            #endregion

            #region Column Create
            // Create a column mapping for a 'picklist' type field
            var columnMappingCreate = new ColumnMapping()
            {
                // Set source properties
                SourceAttribute = RareBloodSourceImport.Properties.Create,
                SourceEntityName = "Rare Blood Source Import",

                // Set target properties
                TargetAttribute = RareBloodSourceImport.Properties.Create,
                TargetEntity = RareBloodSourceImport.LogicalName,

                // Relate this column mapping with its parent data map
                DataMapID = new EntityReference(DataMap.LogicalName, importMapId),

                // Force this column to be processed
                ProcessCode = ColumnMapping.eProcessCode.Process
            };

            // Create the mapping
            Guid columnMappingCreateId = organisationService.Create(columnMappingCreate);

            // Create the mapping
            organisationService.Create(new ListValueMapping()
            {
                SourceValue = "Yes",
                TargetValue = 1,

                // Relate this column mapping with its column mapping data map
                ColumnMappingId = new EntityReference(ColumnMapping.LogicalName, columnMappingCreateId),

                // Force this column to be processed
                ProcessCode = ListValueMapping.eProcessCode.Process
            });

            #endregion

            #region Column Update
            // Create a column mapping for a 'picklist' type field
            var columnMappingUpdate = new ColumnMapping()
            {
                // Set source properties
                SourceAttribute = RareBloodSourceImport.Properties.Update,
                SourceEntityName = "Rare Blood Source Import",

                // Set target properties
                TargetAttribute = RareBloodSourceImport.Properties.Update,
                TargetEntity = RareBloodSourceImport.LogicalName,

                // Relate this column mapping with its parent data map
                DataMapID = new EntityReference(DataMap.LogicalName, importMapId),

                // Force this column to be processed
                ProcessCode = ColumnMapping.eProcessCode.Process
            };

            // Create the mapping
            Guid columnMappingUpdateId = organisationService.Create(columnMappingUpdate);

            // Create the mapping
            organisationService.Create(new ListValueMapping()
            {
                SourceValue = "Yes",
                TargetValue = 1,

                // Relate this column mapping with its column mapping data map
                ColumnMappingId = new EntityReference(ColumnMapping.LogicalName, columnMappingUpdateId),

                // Force this column to be processed
                ProcessCode = ListValueMapping.eProcessCode.Process
            });

            #endregion

            #region Column Deactivate
            // Create a column mapping for a 'picklist' type field
            var columnMappingDeactivate = new ColumnMapping()
            {
                // Set source properties
                SourceAttribute = RareBloodSourceImport.Properties.Deactivate,
                SourceEntityName = "Rare Blood Source Import",

                // Set target properties
                TargetAttribute = RareBloodSourceImport.Properties.Deactivate,
                TargetEntity = RareBloodSourceImport.LogicalName,

                // Relate this column mapping with its parent data map
                DataMapID = new EntityReference(DataMap.LogicalName, importMapId),

                // Force this column to be processed
                ProcessCode = ColumnMapping.eProcessCode.Process
            };

            // Create the mapping
            Guid columnMappingDeactivateId = organisationService.Create(columnMappingDeactivate);

            // Create the mapping
            organisationService.Create(new ListValueMapping()
            {
                SourceValue = "Yes",
                TargetValue = 1,

                // Relate this column mapping with its column mapping data map
                ColumnMappingId = new EntityReference(ColumnMapping.LogicalName, columnMappingDeactivateId),

                // Force this column to be processed
                ProcessCode = ListValueMapping.eProcessCode.Process
            });

            #endregion

            // Create Import
            var import = new DataImport()
            {
                // IsImport is obsolete; use ModeCode to declare Create or Update.
                Mode = DataImport.eMode.Create,
                ImportName = "Rare Blood Source import - " + accountName
            };

            var importId = organisationService.Create(import);

            // Create Import File.
            var importFile = new ImportSourceFile()
            {
                Content = fileData,
                ImportName = "Rare Blood Source import - " + accountName,
                IsFirstRowHeader = true,
                DataMap = new EntityReference(DataMap.LogicalName, importMapId),
                UseSystemMap = false,
                Source = fileName,
                SourceEntity = "Rare Blood Source Import",
                TargetEntity = RareBloodSourceImport.LogicalName,
                ImportJobID = new EntityReference(DataImport.LogicalName, importId),
                EnableDuplicateDetection = false,
                FieldDelimiter = ImportSourceFile.eFieldDelimiter.Comma,
                DataDelimiter = ImportSourceFile.eDataDelimiter.SingleQuote,
                ProcessCode = ImportSourceFile.eProcessCode.Process,
                RecordsOwner =  new EntityReference(User.LogicalName, userId)
            };

            Guid importFileId = organisationService.Create(importFile);

            // Parse the import file.
            var parseImportRequest = new ParseImportRequest()
            {
                ImportId = importId
            };

            var parseImportResponse = (ParseImportResponse)organisationService.Execute(parseImportRequest);

            // Transform the import
            var transformImportRequest = new TransformImportRequest()
            {
                ImportId = importId
            };

            var transformImportResponse = (TransformImportResponse)organisationService.Execute(transformImportRequest);

            // Upload the records.
            var importRequest = new ImportRecordsImportRequest()
            {
                ImportId = importId
            };

            var importResponse = (ImportRecordsImportResponse)organisationService.Execute(importRequest);
        }

      
        private void HandleRowExceptions(IOrganizationService organisationService, IEnumerable<ParsedFileRow> parsedFileRows, IEnumerable<RareBloodSource> accountSources, EntityReference accountOwningTeam)
        {
            ExecuteMultipleRequest executeMultipleRequest = null;

            var invalidFileRows = from parsedFileRow in parsedFileRows
                                  where !parsedFileRow.IsValid
                                  select parsedFileRow;

            foreach (var invalidFileRow in invalidFileRows)
            {
                var matchedAccountSource = (from accountSource in accountSources
                                            where accountSource.ContributorCode == invalidFileRow.Source.ContributorCode
                                            select accountSource).FirstOrDefault();


                foreach (var parseException in invalidFileRow.ParseExceptions)
                {
                    if (matchedAccountSource != null) // if there is a source with this contributor code alread
                    {   // the the task should be created regarding that source
                        parseException.Task.Regarding = matchedAccountSource.ToEntityReference();
                    }   // but if not, just leave it to be created against the account.

                    var createRequest = new CreateRequest()
                    {
                        Target = parseException.Task
                    };

                    executeMultipleRequest = HandleExceuteMulitpleRequest(organisationService, executeMultipleRequest, createRequest);
                }
            }

            if(executeMultipleRequest != null && executeMultipleRequest.Requests.Count > 0)

                organisationService.Execute(executeMultipleRequest);
        }

        public ExecuteMultipleRequest HandleExceuteMulitpleRequest(IOrganizationService organisationService, ExecuteMultipleRequest executeMultipleRequest, OrganizationRequest organisationRequest)
        {
            if (executeMultipleRequest == null)
            {
                executeMultipleRequest = new ExecuteMultipleRequest()
                {
                    // Assign settings that define execution behavior: continue on error, return responses. 
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = false
                    },
                    // Create an empty organization request collection.
                    Requests = new OrganizationRequestCollection()
                };
            }

            if (organisationRequest != null)
            {
                if (executeMultipleRequest.Requests.Count < 1000)
                {
                    executeMultipleRequest.Requests.Add(organisationRequest);
                }
                else
                {
                    organisationService.Execute(executeMultipleRequest);

                    executeMultipleRequest = null;
                }
            }

            return executeMultipleRequest;
        }

        private bool TryParseFile(
            IOrganizationService organisationService,
            byte[] fileContent,
            string fileName,
            string createdByName,
            DateTime createdOn,
            EntityReference regarding,
            Account account,
            out List<ParsedFileRow> parsedFileRows
            )
        {
            const string COLUMN_HEADER_ID = "ID";
            const string COLUMN_HEADER_ABO_GROUP = "ABO Group";
            const string COLUMN_HEADER_DONOR_COUNT = "Donor Count";
            const string COLUMN_HEADER_FROZEN_UNIT_COUNT = "Frozen Unit Count";

            var success = false;
            var columnHeaderValidation = new List<string>(new string[] { COLUMN_HEADER_ID, COLUMN_HEADER_ABO_GROUP, COLUMN_HEADER_DONOR_COUNT, COLUMN_HEADER_FROZEN_UNIT_COUNT });
            var optionSetABO = Helper.GetOptionSet(organisationService, "nhs_abosubtypes");
            var optionSetAntigenResults = Helper.GetOptionSet(organisationService, "nhs_antigenresultsourcecontext");
            var allRarities = Helper.GetAllRarities(organisationService);
            var allAntigens = Helper.GetAllAntigens(organisationService);
            var memoryStream = new MemoryStream(fileContent);

            parsedFileRows = new List<ParsedFileRow>();
            
            using (StreamReader streamReader = new StreamReader(memoryStream))
            {
                string headerRow = streamReader.ReadLine();
                
                success = (ValidateFile(
                    organisationService,
                    fileName,
                    headerRow,
                    createdByName,
                    createdOn,
                    account.ToEntityReference(),
                    account.OwningTeam,
                    columnHeaderValidation,
                    allAntigens));

                if (success)
                {
                    parsedFileRows = GetParsedFileRows(
                        organisationService,
                        streamReader,
                        headerRow,
                        createdByName,
                        createdOn,
                        account.ToEntityReference(),
                        account.OwningTeam,
                        columnHeaderValidation,
                        fileName,
                        optionSetABO,
                        optionSetAntigenResults,
                        allRarities,
                        allAntigens);
                }
            }

            return success;
        }

        private bool ValidateFile(
            IOrganizationService organisationService,
            string fileName,
            string headerRow,
            string createdByName,
            DateTime createdOn,
            EntityReference account,
            EntityReference accountOwningTeam,
            List<string> columnHeaderValidation,
            List<Antigen> allAntigens)
        {
            var success = false;
            ParseException parseException = null;

            string[] columnHeaders = headerRow.Split(',');

            //IRDP-178: AC1 Reject filename without csv extension
            if (!fileName.EndsWith(".csv"))
            {
                parseException =
                    new ParseException(
                        new Task()
                        {
                            Regarding = account,
                            Owner = accountOwningTeam,
                            Subject = "File Error: Invalid File Extension",
                            Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2} but was rejected as it does not have the CSV extension.",
                                fileName,
                                createdByName,
                                createdOn.ToString(new CultureInfo("en-GB")))
                        });
            }

            //If no File Validation exception has occurred until now...
            if (parseException == null)
            {
                if (columnHeaders.Length < columnHeaderValidation.Count()) //Bare minimum!?
                {
                    //IRDP-178: AC2 - Reject files which do not contain CSV data ?
                    parseException =
                        new ParseException(
                            new Task()
                            {
                                Regarding = account,
                                Owner = accountOwningTeam,
                                Subject = "File Error: Failed to read CSV file",
                                Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2} but was rejected as it does not contain CSV data. Could not read header row.",
                                    fileName,
                                    createdByName,
                                    createdOn.ToString(new CultureInfo("en-GB")))
                            });
                }

                //If no File Validation exception has occurred until now...
                if (parseException == null)
                {
                    for (int index = columnHeaderValidation.Count(); index < columnHeaders.Length; index++)
                    {
                        var columnHeader = columnHeaders[index].Trim();

                        if (!columnHeader.Contains("Rarity"))
                        {
                            //...try to get the Antigen form the Header Column text
                            var antigen = (from a in allAntigens
                                           where a.Name_Unicode.Trim() == columnHeader.Trim()
                                           select a).FirstOrDefault();


                            //IRDP-178: AC3 - Reject files with invalid Antigen Names
                            if (antigen == null)
                            {
                                parseException =
                                    new ParseException(
                                        new Task()
                                        {
                                            Regarding = account,
                                            Owner = accountOwningTeam,
                                            Subject = "File Error: Invalid Content",
                                            Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2}, but was rejected as it contains a Antigen column labelled {3} and no Antigen with this name could be found in the system. Note: this match is case sensitive.",
                                                fileName,
                                                createdByName,
                                                createdOn.ToString(new CultureInfo("en-GB")),
                                                columnHeader)
                                        });

                                break;
                            }
                        }
                    }
                }

                //If no File Validation exception has occurred until now...
                if (parseException == null)
                {
                    //...for each column header that needs to be always present...
                    for (int index = 0; index < columnHeaderValidation.Count(); index++)
                    {
                        var columnHeader = columnHeaders[index].Trim();

                        //IRDP-178: AC4A - Reject Files with Invalid Column Headings A-D
                        if (columnHeader != columnHeaderValidation[index].Trim())
                        {

                            parseException =
                                new ParseException(
                                    new Task()
                                    {
                                        Regarding = account,
                                        Owner = accountOwningTeam,
                                        Subject = "File Error: Invalid Content",
                                        Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2} but was rejected as the first 4 columns are not labelled 'ID', 'ABO Group', 'Donor Count', 'Frozen Unit Count'.",
                                            fileName,
                                            createdByName,
                                            createdOn.ToString(new CultureInfo("en-GB")),
                                            columnHeader)
                                    });

                            break;
                        }
                    }
                }

                //If no File Validation exception has occurred until now...
                if (parseException == null)
                {
                    var isRarityColumn = false;

                    for (int index = columnHeaderValidation.Count(); index < columnHeaders.Length; index++)
                    {
                        if (columnHeaders[index].Contains("Rarity"))
                        {
                            isRarityColumn = true;

                            break;
                        }
                    }

                    //IRDP-178: AC4B - File Error: No Rarity Column(s) Found
                    if (!isRarityColumn)
                    {
                        parseException =
                            new ParseException(
                                new Task()
                                {
                                    Regarding = account,
                                    Owner = accountOwningTeam,
                                    Subject = "File Error: Invalid Content",
                                    Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2}, but was rejected as it does not contain at least one column containing the word 'Rarity'.",
                                        fileName,
                                        createdByName,
                                        createdOn.ToString(new CultureInfo("en-GB")))
                                });
                    }
                }
            }

            if (parseException != null)
            {
                organisationService.Create(parseException.Task);
            }
            else
            {
                success = true;
            }

            return success;
        }

        private List<ParsedFileRow> GetParsedFileRows(
            IOrganizationService organisationService,
            StreamReader streamReader,
            string headerRow,
            string createdByName,
            DateTime createdOn,
            EntityReference account,
            EntityReference accountOwningTeam,
            List<string> columnHeaderValidation,
            string fileName,
            List<OptionMetadata> optionSetABO,
            List<OptionMetadata> optionSetAntigenResults,
            List<Rarity> allRarities,
            List<Antigen> allAntigens)
        {
            const string COLUMN_HEADER_ID = "ID";
            const string COLUMN_HEADER_ABO_GROUP = "ABO Group";
            const string COLUMN_HEADER_DONOR_COUNT = "Donor Count";
            const string COLUMN_HEADER_FROZEN_UNIT_COUNT = "Frozen Unit Count";

            var parsedFileRows = new List<ParsedFileRow>();

            ParsedFileRow parsedFileRow = null;
            string rawData;
            string[] columnHeaders = headerRow.Split(',');
            

            try
            {
                //Start parsing rows...
                while ((rawData = streamReader.ReadLine()) != null)
                {
                    var columnData = rawData.Split(',');
                    var contributorCode = columnData[columnHeaderValidation.IndexOf(COLUMN_HEADER_ID)].Trim();
                    parsedFileRow = new ParsedFileRow(contributorCode, accountOwningTeam, account, headerRow, rawData);


                    var regarding = account; //set to the account for now - will be updated if we find a rare blood source.

                    if (contributorCode.Contains('"') || contributorCode.Contains("'") || contributorCode.Contains(","))
                    {
                        parsedFileRow.ParseExceptions.Add(
                           new ParseException(
                               new Task()
                               {
                                   Regarding = regarding, 
                                   Owner = accountOwningTeam,
                                   Subject = "Donor/Product Row Error: Invalid characters in Contributor Code",
                                   Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2}. A row was rejected as the Contributor Code contained an invalid character (double quotes, single quotes and commas are not allowed).",
                                       fileName,
                                       createdByName,
                                       createdOn.ToString(new CultureInfo("en-GB")))
                               }));
                 
                    }
                    
                    //If we processing a duplicate row... 
                    var duplicateParsedFileRow = (from b in parsedFileRows
                                                  where b.Source.ContributorCode == parsedFileRow.Source.ContributorCode
                                                  select b).FirstOrDefault();

                    //IRDP-179: AC1 -  Reject Rows with Duplicate IDs
                    if (duplicateParsedFileRow != null)
                    {
                        parsedFileRow.ParseExceptions.Add(
                          new ParseException(
                              new Task()
                              {
                                  Regarding = regarding,
                                  Owner = accountOwningTeam,
                                  Subject = "Donor/Product Row Error: Duplicate Donor ID",
                                  Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2}. Row with ID {3} was rejected as the file contained multiple rows with this ID. This donor/product line has been deactivated.",
                                      fileName,
                                      createdByName,
                                      createdOn.ToString(new CultureInfo("en-GB")),
                                      parsedFileRow.Source.ContributorCode)
                              }));
                    }
                    

                    var bloodType = columnData[columnHeaderValidation.IndexOf(COLUMN_HEADER_ABO_GROUP)].Trim();
                    var bloodTypeOption = (from o in optionSetABO
                                           where o.Label.LocalizedLabels[0].Label == bloodType
                                           select o).FirstOrDefault();

                    //IRDP-179: AC2 - Reject Row with Invalid ABO group
                    if (bloodTypeOption == null)
                    {
                        parsedFileRow.ParseExceptions.Add(
                            new ParseException(
                                new Task()
                                {
                                    Regarding = regarding,
                                    Owner = accountOwningTeam,
                                    Subject = "Donor/Product Row Error: ABO Group Not Recognised",
                                    Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2}. Row with ID {3} was rejected as it contained an invalid ABO group value ({4}).",
                                        fileName,
                                        createdByName,
                                        createdOn.ToString(new CultureInfo("en-GB")),
                                        parsedFileRow.Source.ContributorCode,
                                        bloodType)
                                }));
                    }

                    var donorCountValue = columnData[columnHeaderValidation.IndexOf(COLUMN_HEADER_DONOR_COUNT)];
                    int donorCount = 0;

                    //IRDP-179: AC3 - Reject Row with Invalid Donor Count group
                    if (!int.TryParse(donorCountValue, out donorCount) || donorCount < 0)
                    {
                        parsedFileRow.ParseExceptions.Add(
                            new ParseException(
                                new Task()
                                {
                                    Regarding = regarding,
                                    Owner = accountOwningTeam,
                                    Subject = "Donor/Product Row Error: Invalid Donor Count",
                                    Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2}. Row with ID {3} was rejected as it contained an invalid value ({4}) for Donor Count.",
                                        fileName,
                                        createdByName,
                                        createdOn.ToString(new CultureInfo("en-GB")),
                                        parsedFileRow.Source.ContributorCode,
                                        donorCountValue)
                                }));
                    }
                    
                    var frozenUnitCountValue = columnData[columnHeaderValidation.IndexOf(COLUMN_HEADER_FROZEN_UNIT_COUNT)];
                    int frozenUnitCount = 0;

                    //IRDP-179: AC3 - Reject Row with Invalid Donor Count group
                    if (!int.TryParse(frozenUnitCountValue, out frozenUnitCount) || frozenUnitCount < 0)
                    {
                        parsedFileRow.ParseExceptions.Add(
                            new ParseException(
                                new Task()
                                {
                                    Regarding = regarding,
                                    Owner = accountOwningTeam,
                                    Subject = "Donor/Product Row Error: Invalid Frozen Unit Count",
                                    Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2}. Row with ID {3} was rejected as it contained an invalid value ({4}) for Frozen Unit Count.",
                                        fileName,
                                        createdByName,
                                        createdOn.ToString(new CultureInfo("en-GB")),
                                        parsedFileRow.Source.ContributorCode,
                                        frozenUnitCountValue)
                                }));
                    }

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

                                //IRDP-179: AC5 - Reject Row with Invalid “Rarities” Values
                                if (rarity == null)
                                {
                                    parsedFileRow.ParseExceptions.Add(
                                        new ParseException(
                                            new Task()
                                            {
                                                Regarding = regarding,
                                                Owner = accountOwningTeam,
                                                Subject = "Donor/Product Row Error: Invalid Rarity Result",
                                                Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2}. Row with ID {3} was rejected as it references a rarity ({4}) that does not exist in IRDP",
                                                    fileName,
                                                    createdByName,
                                                    createdOn.ToString(new CultureInfo("en-GB")),
                                                    parsedFileRow.Source.ContributorCode,
                                                    columnValue)
                                            }));
                                }
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


                            //IRDP-179: AC4B - Reject Row with Invalid Antigen Result
                            if (antigenResult == null)
                            {
                                parsedFileRow.ParseExceptions.Add(
                                    new ParseException(
                                        new Task()
                                        {
                                            Regarding = regarding,
                                            Owner = accountOwningTeam,
                                            Subject = "Donor/Product Row Error: Invalid Antigen Result",
                                            Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2}. Row with ID {3} was rejected as it contained an invalid result ({4}) in the antigen column for {5}",
                                                fileName,
                                                createdByName,
                                                createdOn.ToString(new CultureInfo("en-GB")),
                                                parsedFileRow.Source.ContributorCode,
                                                columnValue,
                                                columnHeader)
                                        }));
                            }
                           
                        }
                    }

                    //...add it to the collection
                    parsedFileRows.Add(parsedFileRow);
                }
            }
            catch (Exception ex)
            {
                //var task = new Task()
                //{
                //    Regarding = account,
                //    Owner = accountOwningTeam,
                //    Subject = "File Error: Invalid Content",
                //    Description = String.Format("Bulk upload file named {0} was uploaded by {1} on {2} but an unknown exception occurred. Last known row id: {3}",
                //            fileName,
                //            createdByName,
                //            createdOn.ToString(new CultureInfo("en-GB")),
                //            parsedFileRow == null ? "Unknown" : parsedFileRow.Source.ContributorCode
                //            + "\n ex: " + ex.Message)
                //    //+ "\n file content (base 64): " + Convert.ToBase64String(fileContent)
                //};

                //TODO: Handle upload failure more gracefully?

                throw new InvalidPluginExecutionException(String.Format("Bulk upload file named {0} was uploaded by {1} on {2} but an unknown exception occurred. Last known row id: {3}",
                                fileName,
                                createdByName,
                                createdOn.ToString(new CultureInfo("en-GB")),
                                parsedFileRow == null ? "Unknown" : parsedFileRow.Source.ContributorCode)
                                + "\n ex: " + ex.Message);
                
            }

            return parsedFileRows;          
        }
    }
}
