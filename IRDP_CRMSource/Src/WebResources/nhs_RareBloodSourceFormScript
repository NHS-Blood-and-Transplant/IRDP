/// <reference path="nhs_global.js" />
/// <reference path="nhs_IrdpCommon.js" />

var formContext;

function formOnLoad(executionContext) {
    
    formContext = executionContext.getFormContext();

    switch (ClientExtensions.GetFormType()) {

        case FormType.FORM_TYPE_CREATE:

            setContributorAccount();

            formContext.getControl("grdAntigens").addOnLoad(antigensGridLoadHandler);
            formContext.getControl("grdRarities").addOnLoad(raritiesGridLoadHandler);
            
            //Show the Antigen Assoc on Creation section
            Xrm.Page.ui.tabs.get("tab_General").sections.get("section_AntigenAssocsOnCreate").setVisible(true);

            break;

        case FormType.FORM_TYPE_UPDATE:

            formContext.getControl("grdAntigens").addOnLoad(antigensGridLoadHandler);
            formContext.getControl("grdRarities").addOnLoad(raritiesGridLoadHandler);

            checkForAntigenContradictions();

            //Hide the antigen assoc on Creation section
            Xrm.Page.ui.tabs.get("tab_General").sections.get("section_AntigenAssocsOnCreate").setVisible(false);

            break;

        default:

            checkForAntigenContradictions();

            break;
    }

    setParentAccountVisibility();

    sourceTypeOnChange();
}

function formOnChange(executionContext) {
    recalculateLastReviewDate();
}

function antigensGridLoadHandler() {

      setTimeout(function () {
        if (formContext != null && formContext != undefined && formContext.getControl("grdAntigens") != null && formContext.getControl("grdAntigens") != undefined) {
            checkForAntigenContradictions();
            pollForAntigenUpdates();
        }
        else {
            antigensGridLoadHandler();
        }
    }, 1000);
}

function pollForAntigenUpdates(currentCheckCount = 0, maxCheckCount = 20, previousAntigenCount = null, timeBetweenChecks = 1000) {

    
    var sourceId = Xrm.Page.data.entity.getId();

    if (sourceId != "") { //i.e. don't run on a create form
        if (previousAntigenCount == null) {
            previousAntigenCount = ClientExtensions.RetrieveMultipleRecordsAdv("nhs_antigensourceassociations?$select=nhs_antigensourceassociationid&$filter=_nhs_sourceid_value eq " + sourceId + " and  statecode eq 1", 100, true).length;
        }


        setTimeout(
            function (
                prevAntigenCount = previousAntigenCount,
                currCheckCount = currentCheckCount,
                maxChecks = maxCheckCount
            ) {



                var sourceId = Xrm.Page.data.entity.getId();
                var newAntigenCount = ClientExtensions.RetrieveMultipleRecordsAdv("nhs_antigensourceassociations?$select=nhs_antigensourceassociationid&$filter=_nhs_sourceid_value eq " + sourceId + " and  statecode eq 1", 100, true).length;
                //if the counts aren't the same
                if (newAntigenCount != prevAntigenCount) {
                    // then refresh the antigen grid
                    formContext.getControl("grdAntigens").refresh()
                    //and display the warning banner if there are contradictions
                    checkForAntigenContradictions();
                }

                //If we've not exhausted the check count
                if (currCheckCount < maxChecks) {
                    //then increment the check count
                    currCheckCount++;

                    //and keep trying for more updates.
                    pollForAntigenUpdates(
                        currCheckCount,
                        maxChecks,
                        newAntigenCount
                    );
                }



            },
            timeBetweenChecks
        )

    }
    
}

function raritiesGridLoadHandler() {

    setTimeout(function () {
        if (formContext != null && formContext != undefined && formContext.getControl("grdRarities") != null && formContext.getControl("grdRarities") != undefined) {
            pollForAntigenUpdates();
        }
        else {
            raritiesGridLoadHandler();
        }
    }, 1000);
}

function accountOnChange() {

    var accountId = Xrm.Page.data.entity.attributes.get("nhs_parentaccount").getValue();

    if (accountId != null) {

        var teamId = getAccountTeam(accountId[0].id);

        Xrm.Page.getAttribute("ownerid").setValue(teamId);
    }
    else {

        Xrm.Page.getAttribute("ownerid").setValue(null);
    }

}

function sourceTypeOnChange() {

    var SOURCE_TYPE_RARE_FROZEN = 1;
    var sourceType = Xrm.Page.getAttribute("nhs_sourcetype").getValue();
    
    if (sourceType != null && sourceType == SOURCE_TYPE_RARE_FROZEN) {

        Xrm.Page.getControl("nhs_donorcount").setVisible(true);
        if (ClientExtensions.GetFormType() == FormType.FORM_TYPE_CREATE)
            Xrm.Page.getAttribute("nhs_donorcount").setValue(null);

    } else {

        Xrm.Page.getControl("nhs_donorcount").setVisible(false);
        Xrm.Page.getAttribute("nhs_donorcount").setValue(1);
    }
}

function getAccountTeam(accountId) {


    var teamId;

    var account = ClientExtensions.RetrieveRecord(accountId, "account", "_owningteam_value");

    if (account["_owningteam_value"] != null) {

        var team = ClientExtensions.RetrieveRecord(account["_owningteam_value"], "team", "teamid,name");

        teamId = new Array();

        teamId[0] = new Object();
        teamId[0].id = team["teamid"];
        teamId[0].name = team["name"];
        teamId[0].entityType = "team";
    }

    return teamId;
}



function setContributorAccount() {

    if (ClientExtensions.CheckUserRole("irdp contributor")) {

        var userId = Xrm.Page.context.getUserId().substr(1, 36);

        var team = getUserTeam(userId);

        if (team != null) {//If there is one and only one team, get the account

            var account = getTeamAccount(team[0].id);

            if (account != null) {

                Xrm.Page.getAttribute("nhs_parentaccount").setValue(account);
                Xrm.Page.getControl("nhs_parentaccount").setDisabled(true);
                Xrm.Page.getAttribute("ownerid").setValue(team);
            }
        }//if there are multiple accounts, or none, leave the value blank
    }
}

function setParentAccountVisibility() {

    var parentAccountValue = Xrm.Page.getAttribute("nhs_parentaccount").getValue();
    var parentAccountControl = Xrm.Page.getControl("nhs_parentaccount");

    //if the parent account field is populated
    if (parentAccountValue != null) {
        //lock and hide the field
        parentAccountControl.setDisabled(true);
    } else {
        //otherwise, unlock and show the field
        parentAccountControl.setDisabled(false);
    }
}

function checkForAntigenContradictions() {

    Xrm.Page.ui.clearFormNotification("msgContradictaryAntigens");

    var sourceId = Xrm.Page.data.entity.getId();

    if (sourceId != "") {

        var antigenSourceAssociations = ClientExtensions.RetrieveMultipleRecordsAdv("nhs_antigensourceassociations?$select=nhs_antigensourceassociationid,nhs_name&$filter=_nhs_sourceid_value eq " + sourceId + " and  statuscode eq 127130004", 1, true);

        if (antigenSourceAssociations != undefined && antigenSourceAssociations.length == 1) {
            ClientExtensions.DisplayNotification("Resolve the antigen result contradictions to reactivate this record.", NotificationType.WARNING, "msgContradictaryAntigens");
        }
    }
}

function recalculateLastReviewDate() {

    var lastModified = formContext.getAttribute("modifiedon").getValue();
    var lastReviewed = formContext.getAttribute("nhs_lastreviewedon").getValue();

    if (lastModified > lastReviewed) {
        formContext.getAttribute("nhs_lastreviewedon").setValue(lastModified);
    }

}

function resetLastReviewDate() {
    
    var lastReviewed = formContext.getAttribute("nhs_lastreviewedon");
    var today = new Date();

    Xrm.Utility.confirmDialog(
        message = "This will mark the donor/product line data as correct as of today’s date.",
        yesCloseCallback = function () {
            lastReviewed.setValue(today);
            formContext.data.save();
        }
    );
    
}



