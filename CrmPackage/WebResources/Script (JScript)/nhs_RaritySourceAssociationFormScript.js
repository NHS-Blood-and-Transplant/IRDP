/// <reference path="nhs_global.js" />
/// <reference path="nhs_IrdpCommon.js" />

var formContext;

function formOnLoad(executionContext) {
    formContext = executionContext.getFormContext();
}


function setVisibilityRemoveButton() {

    var success = false;

    var isIrdpAdmin = ClientExtensions.CheckUserRole("irdp admin");
    var isContributor = ClientExtensions.CheckUserRole("irdp contributor");
    var isSysAdmin = ClientExtensions.CheckUserRole("system administrator");

    var owner = Xrm.Page.data.entity.attributes.get("ownerid").getValue();

    var userId = Xrm.Page.context.getUserId().substr(1, 36);

    var team = GetUserTeam(userId);

    if (team != undefined) {

        var ownerId = owner[0].id.substring(1, 37).toLowerCase();

        if (ownerId == team[0].id) {

            if (isContributor || isIrdpAdmin || isSysAdmin) {
                success = true;
            }
        }

    } else if (isIrdpAdmin || isSysAdmin) {
        success = true;
    }

    return success;
}

function manuallyRemoveRarityAssociations(rarityAssocIds) {

    //Ask the user to confirm the removal
    var confirmStrings = { text: "Are you sure you want to remove? \n \u2800 \n All implied antigen associations will also be removed.", title: "Remove Rarity" };
    var confirmOptions = { height: 150, width: 450 };
    Xrm.Navigation.openConfirmDialog(confirmStrings, confirmOptions).then(
    function (success) {
        if (success.confirmed) {
            //Only proceed with the removal(s) if the confirmation dialogue gets an "OK" response
            for (var i = 0; i < rarityAssocIds.length; i++) {
                manuallyRemoveRarityAssociation(rarityAssocIds[i])
            }

            //if the source form is loaded, then poll for cascaded updates.
            if (typeof pollForAntigenUpdates === "function") pollForAntigenUpdates();

        } else {
            //If not OK, do nothing
        }
    });

}

function manuallyRemoveRarityAssociation(rarityAssocId) {
    //Id of an on demand workflow that sets the status reason
    var workflowId = "0a966c4a-919f-4817-885a-550ae2c438de";

    //Run the workflow
    irdpCommon.workflow.executeOnDemand(workflowId, rarityAssocId, refreshRaritySubgrid);
}

function refreshRaritySubgrid() {

    if (formContext != undefined) {
        var gridContext = formContext.getControl("grdRarities"); // get the grid context

        if (gridContext != null) {// if the grid is found (
            gridContext.refresh();// then refresh it
        } else { //if its not found, it's because we're on the Association form, not the source
            //so refresh that instead
            formContext.data.refresh(save = false);
        }
    }
}