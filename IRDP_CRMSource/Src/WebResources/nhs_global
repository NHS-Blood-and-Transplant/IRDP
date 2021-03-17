var FormType;
(function (FormType) {
    FormType[FormType["FORM_TYPE_CREATE"] = 1] = "FORM_TYPE_CREATE";
    FormType[FormType["FORM_TYPE_UPDATE"] = 2] = "FORM_TYPE_UPDATE";
    FormType[FormType["FORM_TYPE_READ_ONLY"] = 3] = "FORM_TYPE_READ_ONLY";
    FormType[FormType["FORM_TYPE_DISABLED"] = 4] = "FORM_TYPE_DISABLED";
    FormType[FormType["FORM_TYPE_QUICK_CREATE"] = 5] = "FORM_TYPE_QUICK_CREATE";
    FormType[FormType["FORM_TYPE_BULK_EDIT"] = 6] = "FORM_TYPE_BULK_EDIT";
})(FormType || (FormType = {}));
var ClientExtensions = (function () {
    function ClientExtensions() {
    }
    ClientExtensions.DisplayNotification = function (message, type, id, clear, TimeToClear) {
        if (id === void 0) { id = "notificationid"; }
        if (clear === void 0) { clear = false; }
        if (TimeToClear === void 0) { TimeToClear = 10; }
        var _timeToClearmills = TimeToClear * 1000;
        Xrm.Page.ui.setFormNotification(message, NotificationType[type], id);
        if (clear) {
            setTimeout(function () {
                Xrm.Page.ui.clearFormNotification(id);
            }, _timeToClearmills);
        }
    };
    ClientExtensions.CheckUserRole = function (roleName) {
        var currentUserRoles = Xrm.Page.context.getUserRoles();
        for (var i = 0; i < currentUserRoles.length; i++) {
            var userRoleId = currentUserRoles[i];
            var userRoleName = this.GetRoleName(userRoleId);
            if (userRoleName.toLowerCase() == roleName.toLowerCase())
                return true;
        }
        return false;
    };
    ClientExtensions.GetRoleName = function (roleId) {
        var role = this.RetrieveRecord(roleId, "role", "name");
        if (!role) {
            alert('Error getting role, check permissions');
            return '';
        }
        return role["name"];
    };
    ClientExtensions.GetFormType = function () {
        var formTypeInt = Xrm.Page.ui.getFormType();
        if (formTypeInt == 1)
            return FormType.FORM_TYPE_CREATE;
        if (formTypeInt == 2)
            return FormType.FORM_TYPE_UPDATE;
        if (formTypeInt == 3)
            return FormType.FORM_TYPE_READ_ONLY;
        if (formTypeInt == 4)
            return FormType.FORM_TYPE_DISABLED;
        if (formTypeInt == 5)
            return FormType.FORM_TYPE_QUICK_CREATE;
        if (formTypeInt == 6)
            return FormType.FORM_TYPE_BULK_EDIT;
        return null;
    };
    ClientExtensions.CreateRecord = function (object, entity, synchronous, successCallBack, errorHandler) {
        if (synchronous === void 0) { synchronous = true; }
        if (successCallBack === void 0) { successCallBack = null; }
        if (errorHandler === void 0) { errorHandler = null; }
        try {
            var ret_1 = "";
            if (entity.slice(-1) != "s")
                entity = entity + "s";
            var req = new XMLHttpRequest();
            var url = Xrm.Page.context.getClientUrl() + "/api/data/v9.1/" + entity;
            req.open("POST", encodeURI(url), !synchronous);
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.onreadystatechange = function () {
                if (this.readyState == 4) {
                    req.onreadystatechange = null;
                    if (this.status == 204) {
                        var entityUri = this.getResponseHeader("OData-EntityId");
                        entityUri = entityUri.split(/[()]/);
                        entityUri = entityUri[1];
                        if (synchronous)
                            ret_1 = entityUri;
                        else
                            successCallBack(entityUri);
                    }
                    else {
                        if (errorHandler)
                            errorHandler(req);
                        else
                            ClientExtensions.errorHandler(req);
                    }
                }
            };
            req.send(JSON.stringify(object));
            return ret_1;
        }
        catch (e) {
            alert('error global.CreateRecord ' + e.message);
            console.log(e.message);
        }
    };
    ClientExtensions.RetrieveRecord = function (id, entity, select, expand, synchronous, successCallBack) {
        if (select === void 0) { select = ""; }
        if (expand === void 0) { expand = ""; }
        if (synchronous === void 0) { synchronous = true; }
        if (successCallBack === void 0) { successCallBack = null; }
        var options = "";
        if (entity.slice(-1) != "s")
            entity = entity + "s";
        if (select != null || expand != null) {
            options = "?";
            if (select != null) {
                var selectString = "$select=" + select;
                if (expand != null) {
                    selectString = selectString + "," + expand;
                }
                options = options + selectString;
            }
            if (expand != null) {
                options = options + "&$expand=" + expand;
            }
        }
        var ret = new Object();
        id = id.replace("{", "").replace("}", "");
        var url = Xrm.Page.context.getClientUrl() + "/api/data/v9.1/" + entity + "(" + id + ")" + options;
        var req = new XMLHttpRequest();
        req.open("GET", url, !synchronous);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.onreadystatechange = function () {
            if (this.readyState == 4) {
                req.onreadystatechange = null;
                if (this.status == 200) {
                    if (synchronous)
                        ret = JSON.parse(this.response);
                    else
                        successCallBack(this.response);
                }
                else {
                    ClientExtensions.errorHandler(req);
                }
            }
        };
        req.send();
        return ret;
    };
    ClientExtensions.RetrieveMultipleRecordsAdv = function (options, maxResults, synchronous, successCallBack) {
        if (maxResults === void 0) { maxResults = 250; }
        if (synchronous === void 0) { synchronous = true; }
        if (successCallBack === void 0) { successCallBack = null; }
        try {
            var ret_2 = new Object();
            var url = Xrm.Page.context.getClientUrl() + "/api/data/v9.1/" + options;
            var req = new XMLHttpRequest();
            req.open("GET", url, !synchronous);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations=\"OData.Community.Display.V1.FormattedValue\", odata.maxpagesize=" + maxResults.toString());
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 200) {
                        if (synchronous)
                            ret_2 = JSON.parse(this.response);
                        else
                            successCallBack(JSON.parse(this.response));
                    }
                    else
                        ClientExtensions.errorHandler(req);
                }
            };
            req.send();
            return ret_2["value"];
        }
        catch (e) {
            alert('error global.RetrieveMultipleRecordsAdv ' + e.message);
            console.log(e.message);
        }
    };
    ClientExtensions.RetrieveMultipleRecords = function (entity, select, filter, otherOptions, maxResults, synchronous, successCallBack) {
        if (otherOptions === void 0) { otherOptions = ""; }
        if (maxResults === void 0) { maxResults = 250; }
        if (synchronous === void 0) { synchronous = true; }
        if (successCallBack === void 0) { successCallBack = null; }
        try {
            if (entity.slice(-1) != "s")
                entity = entity + "s";
            var ret_3 = new Array;
            var options = (select == "" ? "" : "$select=" + select);
            options = options + (select == "" || filter == "" ? "" : "&");
            options = options + (filter == "" ? "" : "$filter=" + filter);
            if (otherOptions != "") {
                options = (options == "" ? "" : "&");
                options = options + otherOptions;
            }
            var url = Xrm.Page.context.getClientUrl() + "/api/data/v9.1/" + entity + "?" + options;
            var req = new XMLHttpRequest();
            req.open("GET", url, !synchronous);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations='OData.Community.Display.V1.FormattedValue', odata.maxpagesize=" + maxResults.toString());
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 200) {
                        if (synchronous)
                            ret_3 = JSON.parse(this.response);
                        else
                            successCallBack(this.response);
                    }
                    else
                        ClientExtensions.errorHandler(req);
                }
            };
            req.send();
            return ret_3["value"];
        }
        catch (e) {
            alert('error global.RetrieveMultipleRecords ' + e.message);
            console.log(e.message);
        }
    };
    ClientExtensions.UpdateRecord = function (id, object, entity, synchronous, successCallBack) {
        if (synchronous === void 0) { synchronous = true; }
        if (successCallBack === void 0) { successCallBack = null; }
        if (entity.slice(-1) != "s")
            entity = entity + "s";
        var ret = false;
        var req = new XMLHttpRequest();
        var url = Xrm.Page.context.getClientUrl() + "/api/data/v9.1/" + entity + "(" + id + ")";
        req.open("PATCH", encodeURI(url), !synchronous);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.onreadystatechange = function () {
            if (this.readyState == 4) {
                req.onreadystatechange = null;
                if (this.status == 204 || this.status == 1223) {
                    if (synchronous)
                        ret = true;
                    else
                        successCallBack();
                }
                else {
                    ClientExtensions.errorHandler(req);
                }
            }
        };
        req.send(JSON.stringify(object));
        return ret;
    };
    ClientExtensions.DeleteRecord = function (id, entity, synchronous, successCallBack) {
        if (synchronous === void 0) { synchronous = true; }
        if (successCallBack === void 0) { successCallBack = null; }
        if (entity.slice(-1) != "s")
            entity = entity + "s";
        var ret = false;
        var req = new XMLHttpRequest();
        var url = Xrm.Page.context.getClientUrl() + "/api/data/v9.1/" + entity + "(" + id + ")";
        req.open("DELETE", encodeURI(url), !synchronous);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.onreadystatechange = function () {
            if (this.readyState == 4) {
                req.onreadystatechange = null;
                if (this.status == 204) {
                    if (synchronous)
                        ret = true;
                    else
                        successCallBack();
                }
                else {
                    ClientExtensions.errorHandler(req);
                }
            }
        };
        req.send();
        return ret;
    };
    ClientExtensions.errorHandler = function (req) {
        if (req.status == 12029) {
            alert("The attempt to connect to the server failed.");
            return;
        }
        if (req.status == 12007) {
            alert("The server name could not be resolved.");
            return;
        }
        var errorText;
        try {
            errorText = JSON.parse(req.responseText).error.message.value;
        }
        catch (e) {
            errorText = req.responseText;
        }
        var errormsg = "Error : " +
            req.status + ": " +
            req.statusText + ": " + errorText;
        alert(errormsg);
    };
    ClientExtensions.GridLoadHandler = function (gridName, methodToCall) {
        try {
            setTimeout(function () {
                if (Xrm.Page != null && Xrm.Page != undefined && Xrm.Page.getControl(gridName) != null && Xrm.Page.getControl(gridName) != undefined)
                    methodToCall();
            }, 3000);
        }
        catch (e) {
            alert('error global.GridLoadHandler ' + gridName + ' ' + e.message);
            console.log(e.message);
        }
    };
    ClientExtensions.prototype.varToEnumStringVal = function (value, enumerator) {
        var en = enumerator;
        if (!isNaN(parseInt(value)))
            return en[value];
        var num = en[value];
        return en[num];
    };
    ClientExtensions.ValidateDateNotInTheFuture = function (dateToCheck) {
        var success = false;
        var today = new Date();
        var today_date = today.getDate();
        var today_month = today.getMonth();
        today_month++;
        var today_year = today.getFullYear();
        var dateinput_date = dateToCheck.getDate();
        var dateinput_month = dateToCheck.getMonth();
        dateinput_month++;
        var dateinput_year = dateToCheck.getFullYear();
        if (dateToCheck != null && dateinput_year < today_year) {
            success = true;
        }
        else if (dateToCheck != null && dateinput_year == today_year && dateinput_month < today_month) {
            success = true;
        }
        else if (dateToCheck != null && dateinput_year == today_year && dateinput_month == today_month && dateinput_date < today_date) {
            success = true;
        }
        else if (dateToCheck != null && dateinput_year == today_year && dateinput_month == today_month && dateinput_date == today_date) {
            success = true;
        }
        return success;
    };
    ClientExtensions.CalculateAge = function (birthday, ondate) {
        if (birthday == null) {
            birthday = new Date();
        }
        if (ondate == null) {
            ondate = new Date();
        }
        if (ondate < birthday) {
            return 0;
        }
        var age = ondate.getFullYear() - birthday.getFullYear();
        if (birthday.getMonth() > ondate.getMonth() || (birthday.getMonth() == ondate.getMonth() && birthday.getDate() > ondate.getDate())) {
            age--;
        }
        return age;
    };
    return ClientExtensions;
}());
var NotificationType;
(function (NotificationType) {
    NotificationType[NotificationType["NONE"] = 0] = "NONE";
    NotificationType[NotificationType["INFO"] = 1] = "INFO";
    NotificationType[NotificationType["WARNING"] = 2] = "WARNING";
    NotificationType[NotificationType["ERROR"] = 3] = "ERROR";
})(NotificationType || (NotificationType = {}));

var RegexType;
(function (RegexType) {
    RegexType[RegexType["REGEX_NATIONAL_INSURANCE_NO"] = 1] = "REGEX_NATIONAL_INSURANCE_NO";
    RegexType[RegexType["REGEX_TELEPHONE_NO"] = 2] = "REGEX_TELEPHONE_NO";
    RegexType[RegexType["REGEX_MOBILE_NO"] = 3] = "REGEX_MOBILE_NO";
    RegexType[RegexType["REGEX_NAMES"] = 4] = "REGEX_NAMES";
    RegexType[RegexType["REGEX_ORGANISATION"] = 5] = "REGEX_ORGANISATION";
    RegexType[RegexType["REGEX_EMAIL"] = 6] = "REGEX_EMAIL";
    RegexType[RegexType["REGEX_ADDRESS_LINE_1_3"] = 7] = "REGEX_ADDRESS_LINE_1_3";
    RegexType[RegexType["REGEX_ADDRESS_CITY_COUNTY_COUNTRY"] = 8] = "REGEX_ADDRESS_CITY_COUNTY_COUNTRY";
    RegexType[RegexType["REGEX_UK_POSTCODE"] = 9] = "REGEX_UK_POSTCODE";
    RegexType[RegexType["REGEX_TELEPHONE_EXT"] = 10] = "REGEX_TELEPHONE_NO";
})(RegexType || (RegexType = {}));

var RegexLibrary = (function () {
    function RegexLibrary() {
    }
    RegexLibrary.Test = function (valueToCheck, regex) {
        var success = false;
        var regexp;
        switch (regex) {
            case RegexType.REGEX_ADDRESS_LINE_1_3:
                regexp = /^[a-zA-Z0-9&àáâäãåąčćęèéêëėēįìíîïłńòóôöõøùúûüųūÿýżźñçčśšžÀÁÂÄÃÅĄĆČĖĘÈÉÊËÌÍÎÏĮŁŃÒÓÔÖÕØÙÚÛÜŲŪŸÝŻŹÑßÇŒÆČŠŽ∂ð ,.'-]+$/;
                success = regexp.test(valueToCheck);
                break;
            case RegexType.REGEX_ADDRESS_CITY_COUNTY_COUNTRY:
                regexp = /^[a-zA-ZàáâäãåąčćęèéêëėēįìíîïłńòóôöõøùúûüųūÿýżźñçčśšžÀÁÂÄÃÅĄĆČĖĘÈÉÊËÌÍÎÏĮŁŃÒÓÔÖÕØÙÚÛÜŲŪŸÝŻŹÑßÇŒÆČŠŽ∂ð ,.'-]+$/;
                success = regexp.test(valueToCheck);
                break;
            case RegexType.REGEX_UK_POSTCODE:
                regexp = /^([A-Z][A-Z0-9]?[A-Z0-9]?[A-Z0-9]? {0,1}[0-9][A-Z0-9]{2})$/i;
                success = regexp.test(valueToCheck);
                break;
            case RegexType.REGEX_NATIONAL_INSURANCE_NO:
                regexp = /^(?!BG)(?!GB)(?!NK)(?!KN)(?!TN)(?!NT)(?!ZZ)(?:[A-CEGHJ-PR-TW-Z][A-CEGHJ-NPR-TW-Z])(?:\s*\d\s*){6}([A-D]|\s)$/;
                success = regexp.test(valueToCheck);
                break;
            case RegexType.REGEX_MOBILE_NO:
                regexp = /^(\+44\s?0*7\d{3}|\(\+44\)\s?0*7\d{3}|\(?07\d{3}\)?)\s?\d{3}\s?\d{3}$/gi;
                success = regexp.test(valueToCheck);
                break;
            case RegexType.REGEX_NAMES:
                regexp = /^[a-zA-ZàáâäãåąčćęèéêëėēįìíîïłńòóôöõøùúûüųūÿýżźñçčśšžÀÁÂÄÃÅĄĆČĖĘÈÉÊËÌÍÎÏĮŁŃÒÓÔÖÕØÙÚÛÜŲŪŸÝŻŹÑßÇŒÆČŠŽ∂ð ,.'-]+$/;
                success = regexp.test(valueToCheck);
                break;
            case RegexType.REGEX_ORGANISATION:
                regexp = /^[a-zA-Z0-9&àáâäãåąčćęèéêëėįìíîïłńòóôöõøùúûüųūÿýżźñçčšžÀÁÂÄÃÅĄĆČĖĘÈÉÊËÌÍÎÏĮŁŃÒÓÔÖÕØÙÚÛÜŲŪŸÝŻŹÑßÇŒÆČŠŽ∂ð ,.'-]+$/;
                success = regexp.test(valueToCheck);
                break;
            case RegexType.REGEX_EMAIL:
                regexp = /^[a-zA-Z0-9._% +-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
                success = regexp.test(valueToCheck);
                break;
            case RegexType.REGEX_WEBSITE_URL:
                regexp = /^(http(s)?:\/\/.)?(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)$/;
                success = regexp.test(valueToCheck);
                break;
            case RegexType.REGEX_TELEPHONE_NO:
                regexp = /\+[0-9]{1,3}[0-9 ]{6,16}[0-9]$/g;  
                success = regexp.test(valueToCheck);
                break;
            case RegexType.REGEX_TELEPHONE_EXT:
                regexp = /^[0-9]*$/;
                success = regexp.test(valueToCheck);
                break;
            default:
                break;
        }
        return success;
    };
    RegexLibrary.prototype.BindTextbox = function (textbox) {
    };
    RegexLibrary.prototype.Get = function (regex) {
    };
    return RegexLibrary;
}());
function ChromeQuickCreateFix() {
    var isChrome = !!window.chrome && !!window.chrome.webstore;
    if (isChrome) {
        if (window.top.document.getElementsByClassName("mscrm-globalqc-iframe")[0].style.height == "0px") {
            window.top.document.getElementsByClassName("mscrm-globalqc-iframe")[0].style.height = "160px";
        }
    }
}
//# sourceMappingURL=cn_global.js.map