/// <reference path="nhs_global.js" />
var formContext;

function Form_OnLoad(executionContext) {
    
    formContext = executionContext.getFormContext();

    

    switch (ClientExtensions.GetFormType()) {

        case FormType.FORM_TYPE_CREATE:
            
            setContributorAccount();

            break
    }

    setParentAccountStatus();

}

function Validate_OnChange(executionContext) {
    var attribute = executionContext.getEventSource();
    var fieldName = attribute.getName();
    formContext.getControl(fieldName).clearNotification();
    switch (fieldName) {
        case "telephone1":
        case "telephone2":
            formContext.getControl(fieldName).clearNotification();
            if (!RegexLibrary.Test(formContext.getAttribute(fieldName).getValue(), RegexType.REGEX_TELEPHONE_NO)) {
                formContext.getControl(fieldName).setNotification("Must start with international code (+) followed by numbers or spaces and 9 to 20 characters in length", "WARNING");
            }
            break;

        case "nhs_telephone1ext":
        case "nhs_telephone2ext":

            formContext.getControl(fieldName).clearNotification();
            if (!RegexLibrary.Test(formContext.getAttribute(fieldName).getValue(), RegexType.REGEX_TELEPHONE_EXT)) {
                formContext.getControl(fieldName).setNotification("Must be numerical characters only", "WARNING");
            }

            break;

        default:
            break;
    }
}

function setContributorAccount() {

    if (ClientExtensions.CheckUserRole("irdp contributor")) {

        var userId = irdpCommon.security.getCurrentUserGuid();

        var team = getUserTeam(userId);

        if (team != null) {//If there is one and only one team, get the account

            var account = getTeamAccount(team[0].id);

            if (account != null) {

                formContext.getAttribute("parentcustomerid").setValue(account);
                formContext.getControl("parentcustomerid").setDisabled(true);
                formContext.getAttribute("ownerid").setValue(team);
            }
        }//if there are multiple accounts, or none, leave the value blank
    }
}

function setParentAccountStatus() {

    var parentAccountValue = formContext.getAttribute("parentcustomerid").getValue();
    var parentAccountControl = formContext.getControl("parentcustomerid");

    //if the parent account field is populated
    if (parentAccountValue != null) {
        //lock and hide the field
        parentAccountControl.setDisabled(true);
    } else {
        //otherwise, unlock and show the field
        parentAccountControl.setDisabled(false);
    }
}

