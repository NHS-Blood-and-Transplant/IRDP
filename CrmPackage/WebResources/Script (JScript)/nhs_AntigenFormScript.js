
function validateHtmlName(executionContext) {

    var fieldName = 'nhs_namehtml';
    var invalidWarningText = 'Invalid HTML - ensure both open and close tags are included for any HTML formatting.';

    //Get the form context.
    var formContext = executionContext.getFormContext();

    //Get the field value
    var htmlNameValue = formContext.getAttribute(fieldName).getValue();

    //check if the field value is correctly formed HTML
    if (irdpCommon.validation.isValidHtml(htmlNameValue)) {
        //If it is, remove any pre-existing warning
        formContext.getControl(fieldName).clearNotification();
    } else {
        //if not, show the warning text		
        formContext.getControl(fieldName).setNotification(invalidWarningText);
    }
};