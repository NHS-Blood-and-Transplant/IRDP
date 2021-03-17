if (Xrm == undefined) var Xrm = parent.Xrm;

var irdpCommon = {
    validation: {
        isValidHtml: function (inputHtml) {

            //Create a new DIV as doc
            var doc = document.createElement('div');

            //Attempt to put the inputHtml into that div
            doc.innerHTML = inputHtml;

            //if the HTML was invalid, then the previous attempt would not have succeeded
            //so the following test determines if it is valid

            var isValid = (doc.innerHTML == inputHtml);

            return isValid;

        },
        conformsToRegex: function (inputText, regexExpression) {
            //Create a Javascript RegExp object with the provided Expression
            var regex = new RegExp(regexExpression);

            //test the input string against this Expression
            var matchFound = regex.test(inputText);

            return matchFound;
        }
    },
    workflow: {
        executeOnDemand: function (workflowId, entityId, callbackFunction) {

            var data = {
                "EntityId": entityId
            };

            var req = new XMLHttpRequest();
            req.open("POST", Xrm.Page.context.getClientUrl() + "/api/data/v9.0/workflows(" + workflowId + ")/Microsoft.Dynamics.CRM.ExecuteWorkflow", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status >= 200 && this.status < 300) {
                        callbackFunction();
                    } else {
                        Xrm.Utility.alertDialog(this.status + ":" + this.statusText, function () { });
                    }
                }
            };

            req.send(JSON.stringify(data));

        }
    },
    security: {
        checkIfSysAdmin: function () {
            return irdpCommon.security.checkForRoleMembership("306A50FF-271E-EA11-A812-000D3A4688AF"); //Sys Admin GUID
        },
        checkIfIrdpAdmin: function () {
            return irdpCommon.security.checkForRoleMembership("B227D587-BC12-EA11-A811-000D3A86B423"); //IRDP Admin GUID
        },
        checkIfIrdpContributor: function () {
            return irdpCommon.security.checkForRoleMembership("B6D9A5DE-B412-EA11-A811-000D3A86B423"); //IRDP Contributor GUID
        },
        checkIfIrdpConsumer: function () {
            return irdpCommon.security.checkForRoleMembership("D986FDC5-AE12-EA11-A811-000D3A86B423"); //IRDP consumer GUID
        },
        checkForRoleMembership: function (inputRoleId) {
            inputRoleId = inputRoleId.toLowerCase();
            var xrmUserSettings = Xrm.Utility.getGlobalContext().userSettings;
            var currentUserRoles = xrmUserSettings.securityRoles;
            for (var i = 0; i < currentUserRoles.length; i++) {
                var roleToTestId = currentUserRoles[i].toLowerCase();
                if (inputRoleId == roleToTestId) { return true; }
            };
            return false;
        },
        getCurrentUserGuid: function () {
            if (irdpCommon.security._currentUserGuid == null) {
                var xrmUserSettings = Xrm.Utility.getGlobalContext().userSettings;
                irdpCommon.security._currentUserGuid = xrmUserSettings.userId.replace("{", "").replace("}", "");
            }

            return irdpCommon.security._currentUserGuid;

        },
        checkIfOwningTeamMember: function(callbackFunction) {
            if (irdpCommon.security._currentUserGuid == null) {
                var xrmUserSettings = Xrm.Utility.getGlobalContext().userSettings;
                irdpCommon.security._currentUserGuid = xrmUserSettings.userId.replace("{", "").replace("}", "");
            }

            

            //get the users team
            irdpCommon.security.getCurrentUsersTeam(function (result) {
                
                var ownerId = Xrm.Page.getAttribute("ownerid").getValue()[0].id.replace("{", "").replace("}", "");

                if (result != undefined) {
                    if (result.length > 1) {
                        //throw an error and
                        Xrm.Utility.alertDialog("You have been assigned to multiple teams in error - please contact IRDP Admin to rectify this.");
                        callbackFuntion(false);
                    } else if (result[0].teamid.toUpperCase() == ownerId) {
                        //they are a member - return true to the callback
                        callbackFunction(true);
                    } else {
                        //they're not a member, but no error to report
                        callbackFunction(false);
                    }

                    //if the users team is the owner, then execute callback passing true
                    callbackFunction(true);
                } else {
                    //otherwise, execute the callback passing false
                    callbackFunction(false);
                }
            })


        },
        getCurrentUsersTeam: function(callbackFunction) {
            if (irdpCommon.security._currentUserGuid == null) {
                var xrmUserSettings = Xrm.Utility.getGlobalContext().userSettings;
                irdpCommon.security._currentUserGuid = xrmUserSettings.userId.replace("{", "").replace("}", "");
            }
            var fetchXml =
            "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>" +
            "   <entity name='teammembership'>" +
            "       <attribute name='teamid' alias='teamid'/>" +
            "       <filter type = 'and'>" +
            "            <condition attribute='systemuserid' operator='eq' value='" + irdpCommon.security._currentUserGuid + "' />" +
            "       </filter>" +
            "       <link-entity name='team' from='teamid' to='teamid' visible='false' intersect='true'>" +
            "           <attribute name='name'  alias='teamname' />" +
            "           <filter type='and'>" +
            "               <condition attribute='isdefault' operator='eq' value='0' />" +
            "           </filter>" +
            "       </link-entity>" +
            "   </entity>" +
            "</fetch>";

            irdpCommon.query.getAllFetchXmlResults(fetchXml, "teammembership", callbackFunction);
        },
        _currentUserGuid: null
        
    },
    query: {
        getConfigEntityValue: function(key, callbackForResults) {
            //construct the URL
            var queryUrl = encodeURI("/api/data/v9.1/nhs_configentities?$select=nhs_value&$filter=nhs_key eq '" + key + "'");

            //then execute it
            irdpCommon.query._getPaginatedWebApiResults(queryUrl, callbackForResults);

        },
        getFetchXmlResult: function (fetchXml, entityName) {

            //if the input fetch xml is in an array, then join it
            if (Array.isArray(fetchXml)) fetchXml = fetchXml.join("\n");

            var queryUrl = "/api/data/v9.1/" + entityName + "?fetchXml=" + encodeURI(fetchXml);
            //then execute it
           return irdpCommon.query._getWebApiResult(queryUrl);


        }
        ,
        getAllFetchXmlResults: function (fetchXml, entityName, callbackForResults) {
            //overcomes the 5,000 result limit on fetchXml results


            //if the input fetch xml is in an array, then join it
            if (Array.isArray(fetchXml)) fetchXml = fetchXml.join("\n");

            //add page 1 to the fetchxml header
            fetchXml = fetchXml.replace('<fetch ', '<fetch page="1" ');

            // make up the query URL
            var queryUrl = "/api/data/v9.1/" + entityName + "s?fetchXml=" + encodeURI(fetchXml);

            //then execute it
            irdpCommon.query._getPaginatedWebApiResults(queryUrl, callbackForResults);
        },
        getAllWebApiResults: function (queryString, entityName, callbackForResults) {
            //construct the query url
            var queryUrl = "/api/data/v9.1/" + entityName + "s?" + encodeURI(queryString);
            
            //then execute it
            irdpCommon.query._getPaginatedWebApiResults(queryUrl, callbackForResults);
        },
        getRecordByGuid: function (guid, entityName, fieldList, callbackForResult) {
            var req = new XMLHttpRequest();
            req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/" + entityName + "s(" + guid + ")?$select=" + fieldList.join(","), true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 200) {
                        var result = JSON.parse(this.response);
                        callbackForResult(result);
                    } else {
                        Xrm.Utility.alertDialog(this.statusText);
                    }
                }
            };
            req.send();
        },
        
        _getPaginatedWebApiResults: function (queryUrl, callbackForResults, pageNumber) {

            if (pageNumber == undefined) pageNumber = 1;

            // we build the request
            var httpRequest = new XMLHttpRequest();
            httpRequest.open("GET", queryUrl, true); // false = synchronous request
            httpRequest.setRequestHeader("Accept", "application/json");
            httpRequest.setRequestHeader("OData-MaxVersion", "4.0");
            httpRequest.setRequestHeader("OData-Version", "4.0");
            httpRequest.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            httpRequest.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
            httpRequest.onreadystatechange = function () {
            if (this.readyState === 4) {
                httpRequest.onreadystatechange = null;
                if (this.status >= 200 && this.status < 300) {

                    var parsedResults = JSON.parse(httpRequest.responseText);

                    if (parsedResults != null && parsedResults.value != null) {

                        //work out if this is the final page of results
                        var isFinalResultsSet = (parsedResults["@Microsoft.Dynamics.CRM.fetchxmlpagingcookie"] == null ||
                                            parsedResults["@Microsoft.Dynamics.CRM.fetchxmlpagingcookie"] == 'undefined');

                        //return the results to the callback function
                        callbackForResults(parsedResults.value, isFinalResultsSet);

                        // check if there are more records and set the new url, otherwise we set to null the url
                        if (!isFinalResultsSet) {
                            pageNumber++;
                            // Updating Query with page number to fetch Next set of records.
                            //then set the page no attribute

                            //change the page number for web api calls
                            queryUrl = queryUrl.replace("page='" + (pageNumber - 1) + "'", "page='" + pageNumber.toString() + "'");

                            //change the page number for fetchXML calls
                            queryUrl = queryUrl.replace(encodeURI('page="' + (pageNumber - 1) + '"'), encodeURI('page="' + pageNumber.toString() + '"'));

                            irdpCommon.query._getPaginatedWebApiResults(queryUrl, callbackForResults, pageNumber);
                        }
                            

                        }
                    } else {
                        //if there are no parsed resl
                        Xrm.Utility.alertDialog(this.status + ":" + this.statusText, function () { });
                    }
                }
            };


            httpRequest.send();
        },

        _getWebApiResult: function (queryUrl)
        {
            var data = null;
            var httpRequest = new XMLHttpRequest();
            httpRequest.open("GET", queryUrl, false); // false = synchronous request
            httpRequest.setRequestHeader("Accept", "application/json");
            httpRequest.setRequestHeader("OData-MaxVersion", "4.0");
            httpRequest.setRequestHeader("OData-Version", "4.0");
            httpRequest.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            httpRequest.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
            httpRequest.onreadystatechange = function ()
            {
                if (this.readyState === 4)
                {
                    httpRequest.onreadystatechange = null;
                    if (this.status === 200)
                    {
                        var result = JSON.parse(this.response);
                        data = result;
                    } else
                    {
                        Xrm.Utility.alertDialog(this.statusText);
                    }
                }
            };
            httpRequest.send();
            return data;
        },
    }
};


function getUserTeam(userId) {

    var team;

    var fetchXml =
        "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>" +
        "   <entity name='teammembership'>" +
        "       <attribute name='teamid' alias='teamid'/>" +
        "       <filter type = 'and'>" +
        "            <condition attribute='systemuserid' operator='eq' value='" + userId + "' />" +
        "       </filter>" +
        "       <link-entity name='team' from='teamid' to='teamid' visible='false' intersect='true'>" +
        "           <attribute name='name'  alias='teamname' />" +
        "           <filter type='and'>" +
        "               <condition attribute='isdefault' operator='eq' value='0' />" +
        "           </filter>" +
        "       </link-entity>" +
        "   </entity>" +
        "</fetch>";

    var teams = ClientExtensions.RetrieveMultipleRecordsAdv("/teammemberships?fetchXml=" + encodeURI(fetchXml), 2, true);

    //If a single team can be found for the user then...
    if (teams != undefined && teams.length == 1) {

        team = new Array();

        team[0] = new Object();
        team[0].id = teams[0]["teamid"];
        team[0].name = teams[0]["teamname"];
        team[0].entityType = "team";
    }

    return team;
};

function getTeamAccount(teamId) {

    var account;

    var accounts = ClientExtensions.RetrieveMultipleRecordsAdv("accounts?$select=accountid,name&$filter=_ownerid_value eq " + teamId, 2, true);

    if (accounts != undefined && accounts.length == 1) {

        account = new Array();

        account[0] = new Object();
        account[0].id = accounts[0]["accountid"];
        account[0].name = accounts[0]["name"];
        account[0].entityType = "account";
    }

    return account;
};

function DisplayExplicitIcon(rowData, userLCID) {

    var str = JSON.parse(rowData);

    var imgName = "";
    var tooltip = "";

    if (str.nhs_isexplicit_Value != null) {

        switch (str.nhs_isexplicit_Value._val) {
            case 1:
                imgName = "nhs_tick16x16.png";
                break;

            default:
                imgName = "";
                tooltip = "";
                break;
        }
    }

    var resultarray = [imgName, tooltip];

    return resultarray;
};

function DisplayContradictionIcon(rowData, userLCID) {

    var str = JSON.parse(rowData);

    var imgName = "";
    var tooltip = "";

    if (str.statuscode != null) {

        switch (str.statuscode_Value) {
            case 127130004:
                imgName = "nhs_warning16x16.png";
                break;

            default:
                imgName = "";
                tooltip = "";
                break;
        }
    }

    var resultarray = [imgName, tooltip];

    return resultarray;
};
