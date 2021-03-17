// =====================================================================
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
// =====================================================================

//<snippetBulkImportHelper>
using System;
using System.IO;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;


namespace NHSBT.IRDP.Plugins.ProxyClasses
{
    public enum asyncoperation_statuscode
    {

        [System.Runtime.Serialization.EnumMemberAttribute()]
        WaitingForResources = 0,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Waiting = 10,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        InProgress = 20,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Pausing = 21,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Canceling = 22,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Succeeded = 30,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Failed = 31,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Canceled = 32,
    }

    public static class BulkImportHelper
    {
        /// <summary>
        /// Reads data from the specified .csv file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string ReadCsvFile(string filePath)
        {
            string data = string.Empty;
            using (StreamReader reader = new StreamReader(filePath))
            {
                string value = reader.ReadLine();
                while (value != null)
                {
                    data += value;
                    data += "\n";
                    value = reader.ReadLine();
                }
            }
            return data;
        }

        /// <summary>
        /// Reads data from the specified .xml file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string ReadXmlFile(string filePath)
        {
            string data = string.Empty;
            using (StreamReader reader = new StreamReader(filePath))
            {
                data = reader.ReadToEnd();
            }
            return data;
        }

        /// <summary>
        /// Check for importlog records
        /// </summary>
        /// <param name="service"></param>
        /// <param name="importFileId"></param>
        public static void ReportErrors(IOrganizationService service, Guid importFileId)
        {
            QueryByAttribute importLogQuery = new QueryByAttribute();
            importLogQuery.EntityName = ImportLog.LogicalName;
            importLogQuery.ColumnSet = new ColumnSet(true);
            importLogQuery.Attributes.Add("importfileid");
            importLogQuery.Values.Add(new object[1]);
            importLogQuery.Values[0] = importFileId;

            EntityCollection importLogs = service.RetrieveMultiple(importLogQuery);

            if (importLogs.Entities.Count > 0)
            {
                Console.WriteLine("Number of Failures: " + importLogs.Entities.Count.ToString());
                Console.WriteLine("Sequence Number    Error Number    Description    Column Header    Column Value   Line Number");

                // Display errors.
                foreach (ImportLog log in importLogs.Entities)
                {
                    Console.WriteLine(
                        string.Format("Sequence Number: {0}\nError Number: {1}\nDescription: {2}\nColumn Header: {3}\nColumn Value: {4}\nLine Number: {5}",
                            log.SequenceNumber.Value,
                            log.ErrorCode.Value,
                            log.Description,
                            log.ColumnHeading,
                            log.ColumnValue,
                            log.OriginalRowNumber.Value));
                }
            }
        }

        /// <summary>
        /// Waits for the async job to complete.
        /// </summary>
        /// <param name="asyncJobId"></param>
        public static void WaitForAsyncJobCompletion(IOrganizationService service, Guid asyncJobId)
        {
            ColumnSet cs = new ColumnSet("statecode", "statuscode");
            var asyncjob = (SystemJob)service.Retrieve("asyncoperation", asyncJobId, cs);

            int retryCount = 100;

            while (asyncjob.Status.Value != SystemJob.eStatus.Completed && retryCount > 0)
            {
                asyncjob = (SystemJob)service.Retrieve("asyncoperation", asyncJobId, cs);
                System.Threading.Thread.Sleep(2000);
                retryCount--;
                Console.WriteLine("Async operation state is " + asyncjob.Status.Value.ToString());
            }

            Console.WriteLine("Async job is " + asyncjob.Status.Value.ToString() + " with status " + ((asyncoperation_statuscode)asyncjob.Status.Value).ToString());
        }
    }
}
//</snippetBulkImportHelper>