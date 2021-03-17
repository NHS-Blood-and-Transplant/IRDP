function validateIsbtNo(executionContext) {

    var fieldName = 'nhs_isbtno';
    var invalidWarningText = 'Invalid value - ISBT Numbers must be 3 numerical digits (padding with leading zeroes if necessary).';
    var regularExpression = '^[0-9]{3}$'; //RegEx expression to test for 3 numeric characters

    //Get the form context.
    var formContext = executionContext.getFormContext();

    //Get the field value
    var htmlNameValue = formContext.getAttribute(fieldName).getValue();

    //check if the field value is   3 numeric digits by testing the Regular Expression
    var is3NumericCharacters = irdpCommon.validation.conformsToRegex(htmlNameValue, regularExpression);

    if (is3NumericCharacters) {
        //If it is, remove any pre-existing warning
        formContext.getControl(fieldName).clearNotification();
    } else {
        //if not, show the warning text		
        formContext.getControl(fieldName).setNotification(invalidWarningText);
    }
};