/// <reference path="nhs_global.js" />
/// <reference path="nhs_IrdpCommon.js" />

var formContext;

function formOnLoad(executionContext) {
    
    formContext = executionContext.getFormContext();

    switch (ClientExtensions.GetFormType()) {

        case FormType.FORM_TYPE_CREATE:

            formContext.getAttribute("nhs_isexplicit").setValue(true);

            break;

        case FormType.FORM_TYPE_UPDATE:
            if (formContext.getControl("nhs_antigenid") != null && formContext.getControl("nhs_antigenid") != undefined) {
                formContext.getControl("nhs_antigenid").setDisabled(true);
            }

            //Put the antigen IDs in an array
            var antigenIds = new Array(formContext.data.entity.getId().replace("{", "").replace("}", ""));

            //count the implying rarities for the antigen
            var countOfActiveImplyingRarities = getCountOfActiveImplyingRarities(antigenIds);
            var resultControl = formContext.getControl("nhs_antigenresult");

            if (resultControl == null) {
                break;
            }
            else if (countOfActiveImplyingRarities == 0 && resultControl != null && resultControl != undefined) {
                //enabled the result control
                resultControl.setDisabled(false);
            } else {
                //disable the result control
                resultControl.setDisabled(true);
            }

            break;

        default:
            break;
    }


}

function validateAntigenRemoval(antigenSourceAssociationIds) {

    //If there are any SELECTED Antigen Source Association records...
    if (antigenSourceAssociationIds.length > 0) {

        //Then get the count of the active implying rarities for the antigen set
        countOfActiveImplyingRarities = getCountOfActiveImplyingRarities(antigenSourceAssociationIds);
        
        if (countOfActiveImplyingRarities == 0) {

            manuallyRemoveAntigenAssocs(antigenSourceAssociationIds);

        } else {
            //If there are any implying rarities
            //Set the validation message
            var validationMessage = getValidationMessage(antigenSourceAssociationIds);
        
            //present the validation message to the user
            Xrm.Utility.alertDialog(validationMessage, function () { });

            // then stop!
        }

    }
}

function validateAntigenSelection(executionContext) {

    formContext = executionContext.getFormContext();
    if (formContext.ui.getFormType() == FormType.FORM_TYPE_CREATE) {

        var antigenControl = formContext.getControl("nhs_antigenid");
        var rawAntigenId = formContext.getAttribute("nhs_antigenid").getValue();

        //if the antigen field is populated
        if (rawAntigenId != null) {
            var antigenId = rawAntigenId[0].id.replace("{", "").replace("}", "")
            var sourceId = formContext.getAttribute("nhs_sourceid").getValue()[0].id.replace("{", "").replace("}", "");

            //then check if there is an active association already in place for this source/antigen combination
            var queryStringForDuplicates = encodeURI("/nhs_antigensourceassociations?$filter=_nhs_antigenid_value eq " + antigenId + " and  _nhs_sourceid_value eq " + sourceId + " and  statecode eq 0&$count=true");

            var queryResult = ClientExtensions.RetrieveMultipleRecordsAdv(queryStringForDuplicates, 1, true);

            if (queryResult.length > 0) { // if there pre-existing duplicates with the same antigen selection

                //the show a validation warning on the antigen selection field.
                var antigenControl = formContext.getControl("nhs_antigenid");
                antigenControl.setNotification("This donor/product line already has a result for this antigen.");
            }


        } else {
            antigenControl.clearNotification();
        }

    }

}


function getCountOfActiveImplyingRarities(antigenIds) {

    var selectStatementAll = encodeURI("/nhs_anitensrcassoc_nhs_raritysrcassocset?$select=nhs_raritysourceassociationid&$filter=nhs_antigensourceassociationid eq " + antigenIds.join(" or nhs_antigensourceassociationid eq "));
    var allResults = ClientExtensions.RetrieveMultipleRecordsAdv(selectStatementAll, 50000, true);
    
    if (allResults.length == 0) {
        //if there are no results, then the active acount will be zero.
        return 0;
    } else {
        // if any results are found, we need to find out how many of them are active.
        var resultRarityAssocIds = [];
        for (var i = 0; i < allResults.length; i++) {
            resultRarityAssocIds.push(allResults[i].nhs_raritysourceassociationid)
        }

        var selectStatementActive = encodeURI("/nhs_raritysourceassociations?$filter=(nhs_raritysourceassociationid eq " + resultRarityAssocIds.join(" or nhs_raritysourceassociationid eq ") + ") and  statecode eq 0");
        var activeResults = ClientExtensions.RetrieveMultipleRecordsAdv(selectStatementActive, 5000, true);

        //return the number of active results
        return activeResults.length;
    }
}

function manuallyRemoveAntigenAssocs(antigenIds) {

    var i;

    //loop through the selected antigens
    for (i = 0; i < antigenIds.length; i++) {
        //deactivating each in turn
        manuallyRemoveAntigenAssoc(antigenIds[i]);
    }

}

function manuallyRemoveAntigenAssoc(antigenId) {

    //Id of an on demand workflow that sets the status reason
    var workflowId = "ED0C3B22-BD4E-4633-A3B1-BC65E4D1C02C";

    //Run the workflow
    irdpCommon.workflow.executeOnDemand(workflowId, antigenId, refreshAntigenSubgrid);
};

function refreshAntigenSubgrid() {

    if (formContext != undefined) {
        var gridContext = formContext.getControl("grdAntigens"); // get the grid context

        if (gridContext != null) {// if the grid is found (
            gridContext.refresh();// then refresh it
        } else { //if its not found, it's because we're on the Association form, not the source
            //so refresh that instead
            formContext.data.refresh(save = false);
        }
    }
};

function getValidationMessage(antigenIds) {
    
    if (antigenIds.length == 1) {
        validationMessage = "This antigen is implied by a rarity (or rarities) on the donor/unit. To delete this antigen, you must first delete the rarity.";
    } else {
        validationMessage = "At least one of the selected antigens is implied by a rarity (or rarities) on the donor/unit. To delete this antigen, you must first delete the rarity.";
    }

    return validationMessage;
};