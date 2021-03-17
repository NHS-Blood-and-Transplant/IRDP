/// <reference path="nhs_global.js" />

var formContext;

var csvCache;


function Form_OnLoad(executionContext) {
    formContext = executionContext.getFormContext();
    cacheCsvExportData();
    setIrdpAdminTabVisibility();
}

function AccountName_OnChange() {
    var name = Xrm.Page.getAttribute("name").getValue();
    if (name != null) {
        Xrm.Page.getAttribute("name").setValue(name.toUpperCase());
    }
}

function Website_OnChange() {
    var url = Xrm.Page.getAttribute("websiteurl").getValue();
    if (url != null) {
        Xrm.Page.getAttribute("websiteurl").setValue(url.toLowerCase());
    }
}

function IsUserInContributorRole() {
    //N.B. This is used by the ribbon to show hide buttons
    return ClientExtensions.CheckUserRole("irdp contributor");
}


function Validate_OnChange(executionContext) {

    var attribute = executionContext.getEventSource();
    var fieldName = attribute.getName();
    Xrm.Page.getControl(fieldName).clearNotification();
    switch (fieldName) {
        case "websiteurl":
            Xrm.Page.getControl(fieldName).clearNotification();
            if (!RegexLibrary.Test(Xrm.Page.getAttribute(fieldName).getValue(), RegexType.REGEX_WEBSITE_URL)) {
                Xrm.Page.getControl(fieldName).setNotification("Must be prefixed with http:// or https:// and suffixed with a domain such as .com", "WARNING");
            }
            break;
        case "telephone1":
        case "telephone2":
            Xrm.Page.getControl(fieldName).clearNotification();
            if (!RegexLibrary.Test(Xrm.Page.getAttribute(fieldName).getValue(), RegexType.REGEX_TELEPHONE_NO)) {
                Xrm.Page.getControl(fieldName).setNotification("Must start with international code (+) followed numbers or spaces and be between 9 and 20 characters in length", "WARNING");
            }
            break;

        case "nhs_telephone1ext":
        case "nhs_telephone2ext":

            Xrm.Page.getControl(fieldName).clearNotification();
            if (!RegexLibrary.Test(Xrm.Page.getAttribute(fieldName).getValue(), RegexType.REGEX_TELEPHONE_EXT)) {
                Xrm.Page.getControl(fieldName).setNotification("Must be numerical characters only", "WARNING");
            }
            
            break;

        default:
            break;
    }
}

function setIrdpAdminTabVisibility() {

    var isAdmin = !(irdpCommon.security.checkIfIrdpConsumer() || irdpCommon.security.checkIfIrdpContributor());

    if (isAdmin) {
        //show the IRDP admin tab
        formContext.ui.tabs.get("tab_irdpAdmin").setVisible(true);
    } else {
        //hide the IRDP admin tab
        //but already hidden by default
    }
}



function exportRareBloodSourceCSV() {

    if (csvCache.antigenAssociations.isComplete == true && csvCache.rarityAssociations.isComplete == true && csvCache.rareBloodSources.isComplete == true) {
        //if all the data has been successfully cached
        //then parse the data 
        var csvData = parseRareBloodSourceAsCSV();

        //and export the file
        var accountName = formContext.getControl("name").getValue();
        var csvBlob = new Blob(["\uFEFF" + csvData], { type: 'text/csv;charset=utf-8' });
        var fileName = new Date().toISOString().split("T")[0] + " - IRDP Export for " + accountName + ".csv";

        saveAs(csvBlob, fileName);
    } else {
        //if the data isn't complete yet
        //then show a warning, ask them to wait and try again
        Xrm.Utility.alertDialog("The donor data for this institution is still loading. Please wait a few seconds and try again.");
    }

}
function parseRareBloodSourceAsCSV() {

    var parsedValues = {
        maxRaritiesPerSource: 0,
        uniqueAntigens: {},
        sources: []
    };

    var csvRows = [];
   
    //get the list of unique antigens in the cache
    for (var i_antigen = 0; i_antigen < csvCache.antigenAssociations.data.length; i_antigen++) {
        var raw_antigen = csvCache.antigenAssociations.data[i_antigen];
        parsedValues.uniqueAntigens[raw_antigen["antigen.nhs_nameunicode"]] = null;
    }


    //parse the cached blood sources as new JSON objects
    for (var i_source = 0; i_source < csvCache.rareBloodSources.data.length; i_source++) {
        var raw_source = csvCache.rareBloodSources.data[i_source];
        var parsed_Source = {
            guid: raw_source.nhs_rarebloodsourceid,
            contributorCode: raw_source.nhs_contributorcode,
            aboGroup: raw_source["nhs_abotype@OData.Community.Display.V1.FormattedValue"],
            frozenUnitCount: raw_source.nhs_frozenunitcount,
            donorCount: raw_source.nhs_donorcount,
            rarities: [],
            antigens: {}
        };
        Object.assign(parsed_Source.antigens, parsedValues.uniqueAntigens);

        parsedValues.sources.push(parsed_Source);
    }

    //parse the cached rarities onto the parsed sources
    for (var i_rarity = 0; i_rarity < csvCache.rarityAssociations.data.length; i_rarity++) {
        var raw_rarity = csvCache.rarityAssociations.data[i_rarity];
        //find the source this association relates to
        var source = $.grep(parsedValues.sources, function (source) { return source.guid == raw_rarity["_nhs_sourceid_value"]; })[0];
        //put this rarity in the sources rarities array
        source.rarities.push(raw_rarity["rarity.nhs_nameunicode"]);
        //and if this increases the max number of rarities per donor, capture the value
        if (source.rarities.length > parsedValues.maxRaritiesPerSource) parsedValues.maxRaritiesPerSource = source.rarities.length;
    }
    
    //parse the cached antigens onto the parsed sources
    for (var i_antigen = 0; i_antigen < csvCache.antigenAssociations.data.length; i_antigen++) {
        var raw_antigen = csvCache.antigenAssociations.data[i_antigen];
        //find the source this association relates to
        var source = $.grep(parsedValues.sources, function (source) { return source.guid == raw_antigen["_nhs_sourceid_value"]; })[0];
        //set the antigen result for that source
        source.antigens[raw_antigen["antigen.nhs_nameunicode"]] = raw_antigen["nhs_antigenresult@OData.Community.Display.V1.FormattedValue"]
    }
    
    //construct the CSV header row
    //starting with the fixed headers
    var csvHeader = ["ID", "ABO Group", "Donor Count", "Frozen Unit Count"];

    //then as many rarity columns as required for the data
    for (var i = 1; i <= parsedValues.maxRaritiesPerSource; i++) {
        csvHeader.push("Rarity " + i);
    }
    //then a column for each unique antigen
    for (var antigen in parsedValues.uniqueAntigens) {
        csvHeader.push(antigen.trim());
    }

    //then add it to the csvData array
    csvRows.push(csvHeader.join(','));

    //construct the CSV details rows
    for (var i_sources = 0; i_sources < parsedValues.sources.length; i_sources++) {
        var rowDataObject = parsedValues.sources[i_sources];

        //set the values of the fixed columns
        var rowData = [
            rowDataObject.contributorCode.trim(),
            rowDataObject.aboGroup,
            rowDataObject.donorCount,
            rowDataObject.frozenUnitCount
        ];

        //then input the rarity values
        for (i_rarities = 0; i_rarities < parsedValues.maxRaritiesPerSource; i_rarities++) {
            //if its still in the range of the array
            if (i_rarities < rowDataObject.rarities.length  ) {
                //then add the rarity name
                rowData.push(rowDataObject.rarities[i_rarities].trim());
            } else {
                //otherwise, add an empty string
                rowData.push(null);
            }
        }

        //then input the antigen results
        for (var antigen in rowDataObject.antigens) {
            rowData.push(rowDataObject.antigens[antigen]);
        }

        csvRows.push(rowData.join(','));
    }

    return csvRows.join("\n");
}

function cacheCsvExportData() {

    clearCache();

    currentInstitutionGuid = formContext.data.entity.getId();

    fetchRareBloodSources(currentInstitutionGuid);
    fetchRarityAssocs(currentInstitutionGuid);
    fetchAntigenAssocs(currentInstitutionGuid);

}

function clearCache() {
    csvCache = {
        rareBloodSources: { data: [], isComplete: false },
        rarityAssociations: { data: [], isComplete: false },
        antigenAssociations: { data: [], isComplete: false }
    };
}

function fetchRareBloodSources(accountGuid) {
    var fetchXml = [];
    var entityName = "nhs_rarebloodsource"
    fetchXml.push('<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">');
    fetchXml.push('<entity name="nhs_rarebloodsource">');
    fetchXml.push('<attribute name="nhs_rarebloodsourceid" />');
    fetchXml.push('<attribute name="nhs_contributorcode" />');
    fetchXml.push('<attribute name="nhs_frozenunitcount" />');
    fetchXml.push('<attribute name="nhs_donorcount" />');
    fetchXml.push('<attribute name="nhs_abotype" />');
    fetchXml.push('<order attribute="nhs_contributorcode" descending="false" />');
    fetchXml.push('<filter type="and">');
    fetchXml.push('<condition attribute="nhs_parentaccount" operator="eq" uitype="account" value="' + accountGuid + '" />');
    fetchXml.push('<condition attribute="statecode" operator="eq" value="0" />');
    fetchXml.push('</filter>');
    fetchXml.push('</entity>');
    fetchXml.push('</fetch>');

    irdpCommon.query.getAllFetchXmlResults(fetchXml, entityName, saveSourcesToCsvCache);

}
function fetchRarityAssocs(accountGuid) {

    var fetchXml = [];
    var entityName = "nhs_raritysourceassociation"
    fetchXml.push('<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">');
    fetchXml.push('<entity name="nhs_raritysourceassociation">');
    fetchXml.push('<attribute name="nhs_raritysourceassociationid" />');
    fetchXml.push('<attribute name="nhs_sourceid" />');
    fetchXml.push('<order attribute="nhs_sourceid" descending="false" />');
    fetchXml.push('<filter type="and">');
    fetchXml.push('<condition attribute="statecode" operator="eq" value="0" />');
    fetchXml.push('</filter>');
    fetchXml.push('<link-entity name="nhs_rarebloodsource" from="nhs_rarebloodsourceid" to="nhs_sourceid" link-type="inner" alias="source">');
    fetchXml.push('<filter type="and">');
    fetchXml.push('<condition attribute="nhs_parentaccount" operator="eq" uitype="account" value="' + accountGuid + '" />');
    fetchXml.push('<condition attribute="statecode" operator="eq" value="0" />');
    fetchXml.push('</filter>');
    fetchXml.push('</link-entity>');
    fetchXml.push('<link-entity name="nhs_rarity" from="nhs_rarityid" to="nhs_rarityid" visible="false" link-type="outer" alias="rarity">');
    fetchXml.push('<attribute name="nhs_nameunicode" />');
    fetchXml.push('</link-entity>');
    fetchXml.push('</entity>');
    fetchXml.push('</fetch>');

    irdpCommon.query.getAllFetchXmlResults(fetchXml, entityName, saveRarityAssocsToCsvCache);

}
function fetchAntigenAssocs(accountGuid) {
    var fetchXml = [];
    var entityName = "nhs_antigensourceassociation"

    fetchXml.push('<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">');
    fetchXml.push('<entity name="nhs_antigensourceassociation">');
    fetchXml.push('<attribute name="nhs_antigensourceassociationid" />');
    fetchXml.push('<attribute name="nhs_antigenresult" />');
    fetchXml.push('<attribute name="nhs_sourceid" />');
    fetchXml.push('<filter type="and">');
    fetchXml.push('<condition attribute="statecode" operator="eq" value="0" />');
    fetchXml.push('</filter>');
    fetchXml.push('<link-entity name="nhs_rarebloodsource" from="nhs_rarebloodsourceid" to="nhs_sourceid" link-type="inner" alias="source">');
    fetchXml.push('<filter type="and">');
    fetchXml.push('<condition attribute="statecode" operator="eq" value="0" />');
    fetchXml.push('<condition attribute="nhs_parentaccount" operator="eq" uitype="account" value="' + accountGuid + '" />');
    fetchXml.push('</filter>');
    fetchXml.push('</link-entity>');
    fetchXml.push('<link-entity name="nhs_antigen" from="nhs_antigenid" to="nhs_antigenid" visible="false" link-type="outer" alias="antigen">');
    fetchXml.push('<attribute name="nhs_nameunicode" />');
    fetchXml.push('<order attribute="nhs_nameunicode" descending="false" />');
    fetchXml.push('</link-entity>');
    fetchXml.push('</entity>');
    fetchXml.push('</fetch>');

    irdpCommon.query.getAllFetchXmlResults(fetchXml, entityName, saveAntigenAssocsToCsvCache);
    
}

function saveSourcesToCsvCache(result, isComplete) {
    
    csvCache.rareBloodSources.data = csvCache.rareBloodSources.data.concat(result);
    csvCache.rareBloodSources.isComplete = isComplete;
    
}
function saveAntigenAssocsToCsvCache(result, isComplete) {

    csvCache.antigenAssociations.data = csvCache.antigenAssociations.data.concat(result);
    csvCache.antigenAssociations.isComplete = isComplete;

}
function saveRarityAssocsToCsvCache(result, isComplete) {

    csvCache.rarityAssociations.data = csvCache.rarityAssociations.data.concat(result);
    csvCache.rarityAssociations.isComplete = isComplete;
    
}


