function rareBoodDonorMap_onLoad() {

    //Get or create a supplementary user record for the current user
    //Used to store last-login date/last acceptance of terms date without requiring access to all the user entities
    rareBloodDonor.termsOfUse.getSupplementaryUserForm();

    //Get the current users team
    rareBloodDonor.common.currentUsersTeamName = irdpCommon.security.getCurrentUsersTeam(rareBloodDonor.common.parseCurrentUsersTeam);

    //request the bing maps licence key
    irdpCommon.query.getConfigEntityValue("Licence Key: Bing Maps", function (results, mapCentre) {
        rareBloodDonor.map.licenceKey = results[0].nhs_value;
        //once the map licence ket is returned
        //work out whether this is IE
        var isBrowserIE = !!window.MSInputMethodContext && !!document.documentMode;

        if (isBrowserIE) {//if it is IE 11, then load at the greeniwich meridian (lat/long 0,0)
            rareBloodDonor.map.loadAtDefaultLocation(null);
        } else {
            //but if it is a more modern browser, 
            //try to get the browser location
            navigator.geolocation.getCurrentPosition(
                //if successful, load at that location
                rareBloodDonor.map.loadAtBrowserLocation,

                //if it returns an error, just load at default location
                rareBloodDonor.map.loadAtDefaultLocation
            );
        }
    });
}

var rareBloodDonor = {
    map: {
        licenceKey: null,
        initialise: function () {
            rareBloodDonor.customButtons.initialise();
            rareBloodDonor.optionsPane.initialise();
            rareBloodDonor.optionsPane.show();
            rareBloodDonor.detailsPane.initialise();
            rareBloodDonor.common.hideIndefiniteProgressOverlay();
            rareBloodDonor.map._bindEventToNoResultsMessage();
            rareBloodDonor.common.repaintHTML();
        },
        parseSearchResults: function (results) {

            //clear the previous results
            rareBloodDonor.map._clearResultsFromMap();

            //reset the subquery counts
            rareBloodDonor.map._subQueries = {
                //this object tracks the count of sub query batches sent for antigen associations 
                //and rarity associations, to allow us to determine if all the responses have been received
                antigenAssocs: {
                    countSent: 0,
                    countRcvd: 0
                },
                rarityAssocs: {
                    countSent: 0,
                    countRcvd: 0
                },
                allComplete: function () {
                    //if the counts all match, then we know we've got all the 
                    //rarity and antigen associations for the returned sources
                    return this.antigenAssocs.countSent == this.antigenAssocs.countRcvd
                    && this.rarityAssocs.countSent == this.rarityAssocs.countRcvd;
                }
            };

            //if there are any results
            if (results.length > 0) {

                //group the results set by institution
                var institutions = rareBloodDonor.map._groupResultsByInstitution(results);
                var layer = new Microsoft.Maps.Layer(id = "irdpInstitutionsLayer");

                //for each institution identified in the previous loop
                for (var institution in institutions) {
                    //create a pushpin at the right location
                    rareBloodDonor.map._addPushPin(institutions[institution], layer);

                    //then fetch the antigen and rarity associations for each source
                    //(not displayed until a pushpin is clicked)
                    rareBloodDonor.map._getAssociationsForResults(institutions[institution].results);
                }

                //put the pushpins layer to the map
                rareBloodDonor.map.object.layers.insert(layer);

            } else {//if there are no results
                // then show the message
                $("div#noResultsOverlay").css("display", "block");
                $("div#noResultsOverlay").off().on("click", rareBloodDonor.optionsPane.show);
            }

            //hide the indefinite progress overlay
            rareBloodDonor.common.hideIndefiniteProgressOverlay();
        },
        _getAssociationsForResults: function (results) {


            //break the sources in each institution into batches
            //(prevents the WebAPI URL for the FetchXml getting too long)
            var batchSize = 100
            for (var i_batchStart = 0; i_batchStart < results.length; i_batchStart += batchSize) {
                var batchOfSources = [];
                for (var i_sources = i_batchStart; i_sources < i_batchStart + batchSize && i_sources < results.length; i_sources++) {
                //push each source to its batch
                    batchOfSources.push(results[i_sources].nhs_rarebloodsourceid);
                }
                //then get the FetchXml for the batch
                var fetchXml = rareBloodDonor.map._getAssociationFetchXmlForBatch(batchOfSources);

                //and execute the FetchXml, with the parser function as the callback
                //first for antigens
                irdpCommon.query.getAllFetchXmlResults(fetchXml.antigenFetchXml, "nhs_antigensourceassociation", rareBloodDonor.map._parseAntigenSourceAssociations);
                rareBloodDonor.map._subQueries.antigenAssocs.countSent += 1;

                //then for rarities
                irdpCommon.query.getAllFetchXmlResults(fetchXml.rarityFetchXml, "nhs_raritysourceassociation", rareBloodDonor.map._parseRaritySourceAssociations);
                rareBloodDonor.map._subQueries.rarityAssocs.countSent += 1;
            }
        },
        _getAssociationFetchXmlForBatch: function(sourceIds) {
            var antigenReq = [];
            var rarityReq = [];

            // open the antigen fetch xml
            antigenReq.push('<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">');
            antigenReq.push(   '<entity name="nhs_antigensourceassociation">');
            antigenReq.push(       '<attribute name="nhs_antigenresult" />');
            antigenReq.push(       '<attribute name="nhs_sourceid" />');
            antigenReq.push(       '<link-entity name="nhs_antigen" from="nhs_antigenid" to="nhs_antigenid" visible="false" link-type="outer" alias="antigen">');
            antigenReq.push(           '<attribute name="nhs_namehtml" />');
            antigenReq.push(           '<attribute name="nhs_nameunicode" />');
            antigenReq.push(       '</link-entity>');
            antigenReq.push(       '<link-entity name="nhs_rarebloodsource" from="nhs_rarebloodsourceid" to="nhs_sourceid" visible="false" link-type="inner" alias="source">');
            antigenReq.push(           '<attribute name="nhs_parentaccount" />');
            antigenReq.push(       '</link-entity>');
            antigenReq.push(       '<filter type="and">');
            antigenReq.push(               '<condition attribute="statecode" operator="eq" value="0" />');
            antigenReq.push(               '<condition attribute="nhs_sourceid" operator="in">');


            // open the rarity fetch xml
            rarityReq.push('<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">');
            rarityReq.push(   '<entity name="nhs_raritysourceassociation">');
            rarityReq.push(       '<attribute name="nhs_sourceid" />');
            rarityReq.push(        '<link-entity name="nhs_rarity" from="nhs_rarityid" to="nhs_rarityid" visible="false" link-type="outer" alias="rarity">');
            rarityReq.push(            '<attribute name="nhs_namehtml" />');
            rarityReq.push(            '<attribute name="nhs_nameunicode" />');
            rarityReq.push(        '</link-entity>');
            rarityReq.push(        '<link-entity name="nhs_rarebloodsource" from="nhs_rarebloodsourceid" to="nhs_sourceid" visible="false" link-type="inner" alias="source">');
            rarityReq.push(            '<attribute name="nhs_parentaccount" />');
            rarityReq.push(        '</link-entity>');
            rarityReq.push(        '<filter type="and">');
            rarityReq.push(            '<condition attribute="statecode" operator="eq" value="0" />');
            rarityReq.push(            '<condition attribute="nhs_sourceid" operator="in">');

            //loop through the source ids
            for (var i = 0; i < sourceIds.length; i++) {
                var criteriaLine = '<value>{' + sourceIds[i] + '}</value>';

                //insert each source ID in the to antigen fetchxml
                antigenReq.push(criteriaLine);

                //insert each source ID in the to rarity fetchxml
                rarityReq.push(criteriaLine);

            }

            //close the antigen fetch xml
            antigenReq.push(           '</condition>');
            antigenReq.push(       '</filter>');
            antigenReq.push(   '</entity>');
            antigenReq.push('</fetch>');

            //close the rarity fetch xml
            rarityReq.push(           '</condition>');
            rarityReq.push(       '</filter>');
            rarityReq.push(   '</entity>');
            rarityReq.push('</fetch>');

            //return the fetchxml for the two entity types
            return {
                rarityFetchXml: rarityReq.join("\n"),
                antigenFetchXml: antigenReq.join("\n")
            };
            
        },
        _parseAntigenSourceAssociations: function (results, isFinalPage) {


            if (results.length > 0) {

                //get the institution this association relates to
                var institution = rareBloodDonor.map._getInstitutionPushpin(results[0]);

                for (var i = 0; i < results.length; i++) {
                    var assoc = results[i];

                    //find the source within that institution's pushpin metadata
                    source = $.grep(
                        institution.metadata.results,
                        function (n, i) {
                            return n.nhs_rarebloodsourceid == assoc._nhs_sourceid_value;
                        }
                    );

                    var antigenHtmlName = assoc["antigen.nhs_namehtml"].trim();
                    var antigenUnicodeName = assoc["antigen.nhs_nameunicode"].trim();
                    var arrayToPushTo;
                    switch (assoc.nhs_antigenresult) {
                        case 127130001: //absent antigens
                            source[0].antigensAbsent.push(antigenHtmlName);
                            source[0].antigensAbsentUnicode.push(antigenUnicodeName);
                            break;
                        case 127130000: //present antigens
                            source[0].antigensPresent.push(antigenHtmlName);
                            source[0].antigensPresentUnicode.push(antigenUnicodeName);
                            break;
                        case 127130002: //weak antigens
                            source[0].antigensPresent.push(antigenHtmlName + " (weak)");
                            source[0].antigensPresentUnicode.push(antigenUnicodeName);
                            break;
                    }

                };
            }

            //if this is the final page for this queri
            if (isFinalPage) {
                //then increment the count of subqueries recieved
                rareBloodDonor.map._subQueries.antigenAssocs.countRcvd += 1;

                if (rareBloodDonor.map._subQueries.allComplete()) {
                    //enable the download button now that the data is complete
                    rareBloodDonor.customButtons.downloadButton.enable();

                    //refresh the source details on the details pane
                    rareBloodDonor.detailsPane.refreshBloodSourceDetails(institution.metadata);
                }
            }
            
            



        },
        _parseRaritySourceAssociations: function (results, isFinalPage) {
           
            if (results.length > 0) {
                for (var i = 0; i < results.length; i++) {
                    var assoc = results[i];
                    //get the institution this association relates to
                    var institution = rareBloodDonor.map._getInstitutionPushpin(assoc);

                    //find the source within that institution
                    source = $.grep(
                        institution.metadata.results,
                        function (n, i) {
                            return n.nhs_rarebloodsourceid == assoc._nhs_sourceid_value;
                        }
                    );

                    //push the rarity details to the rarities array for that source
                    source[0].rarities.push(assoc["rarity.nhs_namehtml"]);
                    source[0].raritiesUnicode.push(assoc["rarity.nhs_nameunicode"]);
                }
            }

            //if this is the final page for this query
            if (isFinalPage) {

                //then increment the count of subqueries recieved
                rareBloodDonor.map._subQueries.rarityAssocs.countRcvd += 1;

                if (rareBloodDonor.map._subQueries.allComplete()) {
                    //enable the download button now that the data is complete
                    rareBloodDonor.customButtons.downloadButton.enable();

                    //refresh the source details on the details pane
                    rareBloodDonor.detailsPane.refreshBloodSourceDetails(institution.metadata);
                }

            }
        },
        _getInstitutionPushpin: function(associationObject) {
            var institutionId = associationObject["source.nhs_parentaccount"];
            var pushPinCollection = rareBloodDonor.map.object.layers[0].getPrimitives();
            var matchingPushpins = $.grep(
                pushPinCollection,
                function (n, i) {
                    return n.metadata.id == institutionId;
                }
            );

            return matchingPushpins[0];


        },
        _clearResultsFromMap: function () {

            rareBloodDonor.map.object.layers.clear();

        },
        loadAtBrowserLocation: function (browserLocation) {

            //parse the browser location as a bing maps location
            var mapCentre = new Microsoft.Maps.Location(
                    browserLocation.coords.latitude,
                    browserLocation.coords.longitude
                );

            //load the map at that location
            rareBloodDonor.map._loadMap(mapCentre);

            
        },
        loadAtDefaultLocation: function (error) {

        // set the map centre to a default location - greenwich longitiude at the equator
            var mapCentre = new Microsoft.Maps.Location(
                    0,
                    0
                );

            //load the map at that location
            rareBloodDonor.map._loadMap(mapCentre);
        },
        _bindEventToNoResultsMessage: function() {
            //reshow the option pane on click
            $("div#noResultsFoundMessage").off().on("click", rareBloodDonor.optionsPane.show);
            $("div#noResultsOverlay").off().on("click", rareBloodDonor.optionsPane.show);
        },
        _loadMap: function (mapCentre) {
            //Get the map from bing
            rareBloodDonor.map.object = new Microsoft.Maps.Map('#myMap', {
                credentials: rareBloodDonor.map.licenceKey,
                center: mapCentre,
                zoom: 1
        });
        },
        _addPushPin: function(resultRecord, layer) {
            //Create a new marker for the record
            var pushPinLocation = new Microsoft.Maps.Location(resultRecord.latitude, resultRecord.longitude);

            var markerOptions = {
                anchor: new Microsoft.Maps.Point(24, 48),
                visible: true,
                icon: rareBloodDonor.map._getPushPinMarker(resultRecord)
            }

            var pushPin = new Microsoft.Maps.Pushpin(pushPinLocation, markerOptions);
            pushPin.metadata = resultRecord;

            //add the pushpin to the layer in the map
            layer.add(pushPin);

            Microsoft.Maps.Events.addHandler(pushPin, 'click', function (event) {
                rareBloodDonor.detailsPane.show(event.target.metadata);
            });
            Microsoft.Maps.Events.addHandler(pushPin, 'click', function (event) {
                rareBloodDonor.detailsPane.show(event.target.metadata);
            });
        },
        _groupResultsByInstitution: function(results) {
            var institutions = {}; //using an object rather than an array as it gives us native deduplication

            //Loop through each result
            for (var i = 0; i < results.length; i++) {
                result = results[i];
                var institutionId = result["parentAccount.accountid"];



                //if there isn't already a key for that object
                if (institutions[institutionId] == undefined) {
                    //create an object for the results instution
                    var institution = {
                        id: institutionId,
                        name: result["parentAccount.name"],
                        addressBlock: result["parentAccount.address1_composite"],
                        latitude: result["parentAccount.address1_latitude"],
                        longitude: result["parentAccount.address1_longitude"],
                        countryName: result["parentAccount.nhs_countryid@OData.Community.Display.V1.FormattedValue"],
                        totalMatchingDonors: 0,
                        totalMatchingFrozenUnits: 0,
                        results: [] //empty array to contain the results for this insitution
                    };

                    var institutionObject = {};
                    institutionObject[institutionId] = institution;

                    //Add the institution to the collection
                    //(deduplication: object concatenation has no effect if the institution GUID already exists)
                    institutions = $.extend(institutions, institutionObject);
                }
                //if the institution doesn't have a results array already
                if (institutions[institutionId].results == undefined) {
                    //then create one
                    institutions[institutionId].results = [];
                }


                //increment the result count.
                institutions[institutionId].totalMatchingDonors += result.nhs_donorcount;
                institutions[institutionId].totalMatchingFrozenUnits += result.nhs_frozenunitcount;

                //add empty arrays to contain the associations for this result
                result.antigensPresent = [];
                result.antigensAbsent = [];
                result.rarities = [];

                result.antigensPresentUnicode = [];
                result.antigensAbsentUnicode = [];
                result.raritiesUnicode = [];

                // put the full result object in the institution object's results array
                institutions[institutionId].results.push(result);

            }

            return institutions;
        },
        _getPushPinMarker: function(resultRecord) {
            var svgMarker = [];
            
            svgMarker.push('<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:ev="http://www.w3.org/2001/xml-events" width="48px" height="48px" viewBox="0 0 97.7283 81.2874" xml:space="preserve" color-interpolation-filters="sRGB" >');

            if (resultRecord.totalMatchingDonors > 0) {
                svgMarker.push('    <g class="institutionPushPin_Donors" transform="translate(0.25,-0.25)">');
                svgMarker.push('        <path stroke="#fff" stroke-width="2" d="M48.19 24.78 A24.0945 24.0945 -180 1 0 7.2 41.77 A24.0945 24.0945 -180 0 0 7.36 41.93 L48.19 81.29 L48.19 24.78 Z" class="institutionPushPin_Donors" fill="#D00000"/>');
                svgMarker.push('    </g>');
                if (resultRecord.totalMatchingDonors > 99) {
                    svgMarker.push('    <text text-anchor="middle" x="26" y="31" font-size="1.3em" fill="#FFF" font-weight="bold" font-family="Segoe UI,Tahoma,Arial,sans-serif">99+</text>');
                } else {
                    svgMarker.push('    <text text-anchor="middle" x="23" y="35" font-size="1.8em" fill="#FFF" font-weight="bold" font-family="Segoe UI,Tahoma,Arial,sans-serif">' + resultRecord.totalMatchingDonors + '</text>');
                }
            }
            if (resultRecord.totalMatchingFrozenUnits > 0) {
                svgMarker.push('    <g class="institutionPushPin_FrozenUnits" transform="translate(97.4783,-0.25) scale(-1,1)">');
                svgMarker.push('        <path stroke="#fff" stroke-width="2" d="M48.19 24.78 A24.0945 24.0945 -180 1 0 7.2 41.77 A24.0945 24.0945 -180 0 0 7.36 41.93 L48.19 81.29 L48.19 24.78 Z" class="institutionPushPin_FrozenUnits" fill="#0070c0"/>');
                svgMarker.push('    </g>');
                if (resultRecord.totalMatchingFrozenUnits > 99) {
                    svgMarker.push('    <text text-anchor="middle" x="73" y="31" font-size="1.3em" fill="#FFF" font-weight="bold" font-family="Segoe UI,Tahoma,Arial,sans-serif">99+</text>');
                } else {
                    svgMarker.push('    <text text-anchor="middle" x="73" y="35" font-size="1.8em" fill="#FFF" font-weight="bold" font-family="Segoe UI,Tahoma,Arial,sans-serif">' + resultRecord.totalMatchingFrozenUnits + '</text>');
                }
            }
            svgMarker.push('</svg>');

            return svgMarker.join("\n");

        },
        pushPins: [],
        _subQueries: {},
        exportMapDataAsCsv: function () {

            var pushPinCollection = rareBloodDonor.map.object.layers[0].getPrimitives();
            var csvOutput = [
                [
                    '"Institution"',
                    '"Source ID"', 
                    '"IRDP ID"', 
                    '"Source Type"', 
                    '"Donor Count"',
                    '"Frozen Unit Count"',
                    '"ABO Group"',
                    '"Antigens Present"',
                    '"Antigens Absent"',
                    '"Rarities"'
                ]
            ];
            

            //loop throun each institution/pushpin on the mape
            for (var i_institution = 0; i_institution  < pushPinCollection.length; i_institution ++) {
                var institution = pushPinCollection[i_institution].metadata;
                //loop through each rare blood source within that institution
                for (var i_bloodSource = 0; i_bloodSource < institution.results.length; i_bloodSource ++) {
                    var bloodSource = institution.results[i_bloodSource];
                    var bloodSourceCsv = [
                        '"' + institution.name + '"',
                        '"' + bloodSource.nhs_contributorcode + '"',
                        '"' + bloodSource.nhs_panelcode + '"',
                        '"' + bloodSource["nhs_sourcetype@OData.Community.Display.V1.FormattedValue"] + '"',
                        bloodSource.nhs_donorcount,
                        bloodSource.nhs_frozenunitcount,
                        '"' + bloodSource["nhs_abotype@OData.Community.Display.V1.FormattedValue"] + '"',
                        '"' + bloodSource.antigensPresentUnicode.join(", ") + '"',
                        '"' + bloodSource.antigensAbsentUnicode.join(", ") + '"',
                        '"' + bloodSource.raritiesUnicode.join(", ") + '"'
                    ];

                    csvOutput.push(bloodSourceCsv.join(","));
                }
            }

            var csvBlob = new Blob(["\uFEFF" + csvOutput.join("\r\n")], { type: 'text/csv;charset=utf-8' });
            var fileName = new Date().toISOString().split("T")[0] + " - IRDP Search Results.csv";

            saveAs(csvBlob, fileName);

        }
    },
    common: {
        showIndefiniteProgressOverlay: function () {
            var indefiniteProgressOverlayControl = $("#indefiniteProgressOverlay");
            indefiniteProgressOverlayControl.css("z-index", "1200").css("display", "block");
            //indefiniteProgressOverlayControl.parent().hide().show();
        },
        hideIndefiniteProgressOverlay: function () {
            $("#indefiniteProgressOverlay").css("z-index", "-1").css("display", "none");
        },
        repaintHTML: function () {
            //read an arbitrary css property to fore the page to repaint.
            var parentDiv = $("#filterOptionsOverlay");
            return parentDiv.offsetHeight;
        },
        aboSubGroups: [
            { id: 1010, label: "All", includedAboTypes: [] },
            { id: 1020, label: "O", includedAboTypes: [127130007] }, //O only
            { id: 1030, label: "O or A", includedAboTypes: [127130007, 127130000, 127130001, 127130003] }, //O, A, A1, A2
            { id: 1040, label: "O or B", includedAboTypes: [127130007, 127130006] }, //O, B
            { id: 1050, label: "O<sub>h</sub>", includedAboTypes: [127130008] } //Oh only
        ],
        supplementaryUserFormGuid: null,
        postToCurrentUserWall: function (textToPostToWall) {

            var entity = {};
            entity.nhs_texttopostonwall = textToPostToWall;

            var req = new XMLHttpRequest();
            req.open("PATCH", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/nhs_usersupplementaryfieldses(" + rareBloodDonor.common.supplementaryUserFormGuid + ")", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 204) {
                        //Success - No Return Data - Do Something
                    } else {
                        Xrm.Utility.alertDialog(this.statusText);
                    }
                }
            };
            req.send(JSON.stringify(entity));

        },
        parseCurrentUsersTeam: function(result) {
            if (result.length > 1) {
                Xrm.Utility.alertDialog("You have been assigned to multiple teams in error - Please contact IRDP admin team to rectify this.");
            }
            rareBloodDonor.common.currentUsersTeam = result[0];
        },
        currentUsersTeam: null
    },
    customButtons: {
        _config: {
            //name value pairs of SVG ID
            //and the function that will be called on click of the SVG
            "filterOptions": function () {
                rareBloodDonor.optionsPane.show();
            },
            "filterOptionsHeader": function () {
                rareBloodDonor.optionsPane.hide();
            },
            "downloadResults": function () {
                rareBloodDonor.map.exportMapDataAsCsv();
            }

        },
        initialise: function () {

            //bind events to each of the custom buttons.
            rareBloodDonor.customButtons._bindEventsToCustomButtonSVGs();

            //show the custom buttons which should be visible on load
            $("svg.ShowAfterLoad").removeClass("Hidden ShowAfterLoad");
        },
        _bindEventsToCustomButtonSVGs: function () {

            //binds events to the custom buttons in the top left corner of the map pane
            //then loop through and apply these definitions
            for (var buttonDefinition in rareBloodDonor.customButtons._config) {
                //bind the events to the buttons
                $("#" + buttonDefinition).off().on(
                    "click",
                    rareBloodDonor.customButtons._config[buttonDefinition]
                );
            }
        },
        downloadButton: {
            enable: function () {
                $(".svgDisabled").addClass("svgEnabled").removeClass("svgDisabled");

            },
            disable: function () {
                $(".svgEnabled").addClass("svgDisabled").removeClass("svgEnabled");

            }
        },
    },
    optionsPane: {
        rarityDefinitions: {},
        dirtyFilterOptions: false,
        show: function () {

            if (!rareBloodDonor.optionsPane._isFilterOptionsPaneVisible()) {

                //disbaled the download button as it won't have any data to download
                rareBloodDonor.customButtons.downloadButton.disable();

                //hide the "No results" message if it is shown
                $("div#noResultsOverlay").css("display", "none");
                $("div#noResultsOverlay").off();

                //slide the filter options pane into view
                $("#filterOptionsOverlay").animate({ left: "+=360px" }, 350);

                //make the map blocking div visible
                $("#blockMapClicksOverlay").css("display", "block");

                //bind an event to run the search on click off of the criteria
                $("#blockMapClicksOverlay").off().on("click", rareBloodDonor.optionsPane._executeSearch);
            }
        },
        hide: function () {

            //if the filter options pane is visible
            if (rareBloodDonor.optionsPane._isFilterOptionsPaneVisible()) {

                //shift the pane back out of view
                $("#filterOptionsOverlay").animate({left: "-=360px"}, 350);

                //hide the map blocking overlay
                $("#blockMapClicksOverlay").css("display", "none");

            }

        },
        initialise: function () {

            //Populate the list of ABO sub groups in the dropdown list
            rareBloodDonor.optionsPane._populateDropDownAboGroups();

            //Populate the Rarity dropdown
            rareBloodDonor.optionsPane._populateDropDownRarity();
 
            //Populate the Antigen dropdown withs the list of blood groups
            rareBloodDonor.optionsPane._populateDropDownAntigen();

            //bind an event to the find button
            $("button#buttonFind").on("click", rareBloodDonor.optionsPane._executeSearch);

        },
        _isAboGroupOh: function () {
            selectedOption = $("select#aboGroup").children("option:selected");
            selectedOptionId = $(selectedOption[0]).attr("value");
            return selectedOptionId == 1050;
        },
        _executeSearch: function () {

            //Get GUIDs of explicit & implicit Antigen Associations
            var antigenAssocCriteria =
                rareBloodDonor.optionsPane._getExplicitAntigenCriteria()
                    //Get Guids of implied Antigen Associations
                    .concat(rareBloodDonor.optionsPane._getImpliedAntigenCriteria());

            if (
                antigenAssocCriteria.length == 0 //if there are no antigens implied or explicitly define in the search
                && !rareBloodDonor.optionsPane._isAboGroupOh() //and the ABO group selected isn't Oh
            ) {
                //flash the  warning re minimum search criteria
                $("#findButton_minCriteriaDesc").fadeTo(180, 0).fadeTo(180, 1).fadeTo(180, 0).fadeTo(180, 1).fadeTo(180, 0).fadeTo(180, 0.4);
            } else {

                //show the indefinite progress overlay (spinning disc)
                rareBloodDonor.common.showIndefiniteProgressOverlay();

                //hide the options pane
                rareBloodDonor.optionsPane.hide();

                //record the search criteria
                rareBloodDonor.optionsPane._recordSearchCriteria();

                //get the search results for the current criteria
                rareBloodDonor.optionsPane._getSearchResults(antigenAssocCriteria);
            }
        },
        _interimResults: [],
        _getSearchResults: function(antigenAssocCriteria) {

            //clear the previous search results
            rareBloodDonor.optionsPane._interimResults = [];

            

            //need to break the antigen assocs down into blocks of 9
            for (var i = 0; i <= antigenAssocCriteria.length; i = i + 9) {
                    
                //get the batch of up to 9 antigens
                var batchOfAntigenCriteria = [];
                for (var j = i; (j < i + 9 && j < antigenAssocCriteria.length); j++) {
                    batchOfAntigenCriteria.push(antigenAssocCriteria[j]);
                }

                //construct the Fetch xml for that batch
                var batchFetchXml = rareBloodDonor.optionsPane._getSourceSelectFetchXml(batchOfAntigenCriteria);

                //Execute the FetchXML
                irdpCommon.query.getAllFetchXmlResults(batchFetchXml, "nhs_rarebloodsource", rareBloodDonor.optionsPane._parsePartialInterimResultSet);

            }
        },

        _parsePartialInterimResultSet: function(results, isFinalResultsSet) {

            //capture the interim results in an array optionspane namespacein the
            rareBloodDonor.optionsPane._interimResults.push(results);

            //if this is the final results set
            if (isFinalResultsSet) {
                //then send the full results set to the parser
                var fullResultSet = rareBloodDonor.optionsPane._parseFullInterimResultSet();
                rareBloodDonor.map.parseSearchResults(fullResultSet);
            }
        },

        _parseFullInterimResultSet: function() {

            //get the cumulative results from the variable in the optionspane namespace
            interimResults = rareBloodDonor.optionsPane._interimResults;

            //Filter the interim results to give a results set of sources that are included in all the batch queries
            //i.e. where all the antigen assoc criteria match - not just the batches of 9

            //if only one batch result is returned, then the end results will be the same as that batches result
            if (interimResults.length == 1) {
                return interimResults[0];
            };

            //if any of the batch results are length zero,then the end result will be zero
            var areAnyBatchResultSetsEmpty = $.grep(interimResults, function (interimResultSet, i) {
                return interimResultSet.length > 0;
            }).length == 0;
            if (areAnyBatchResultSetsEmpty) { return []; }

            //Otherwise, we need to work out which ones appear in all results sets
            //capture the first results set as the initial candidates
            var finalResultsSet = interimResults[0];

            //loop through the results sets after the first  refining the list of candidates each time to only include those that appear in all
            for (var iterator_resultsArrays = 1; iterator_resultsArrays < interimResults.length; iterator_resultsArrays++) {
                var currentCandidates = finalResultsSet;
                finalResultsSet = [];
                for (var iterator_Candidates = 0; iterator_Candidates < currentCandidates.length; iterator_Candidates++) {
                    var currentCandidate = currentCandidates[iterator_Candidates];
                    
                    //search for the current candidate to see if it is found in the results
                    var isFoundInBatch = $.grep(interimResults[iterator_resultsArrays], function (interimResult, i) {
                        return interimResult == currentCandidate;
                    }) > 0;
                    
                    //if its in the results set, then push it to the final results set
                    if (isFoundInBatch) {
                        finalResultsSet.push(currentCandidate);
                    }
                    
                }
            }

            //return the final results set
            return finalResultsSet;
        },
        _getSourceSelectFetchXml: function (antigenCriteria) {
            sourceSelectFetchXml = [];

            sourceSelectFetchXml.push( '<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">' );
            sourceSelectFetchXml.push( '<entity name="nhs_rarebloodsource">');
            sourceSelectFetchXml.push(     '<attribute name="nhs_rarebloodsourceid" />');
            sourceSelectFetchXml.push(     '<attribute name="nhs_panelcode" />');
            sourceSelectFetchXml.push(     '<attribute name="nhs_contributorcode" />');
            sourceSelectFetchXml.push(     '<attribute name="createdon" />');
            sourceSelectFetchXml.push(     '<attribute name="nhs_sourcetype" />');
            sourceSelectFetchXml.push(     '<attribute name="nhs_frozenunitcount" />');
            sourceSelectFetchXml.push(     '<attribute name="nhs_donorcount" />');
            sourceSelectFetchXml.push(     '<attribute name="nhs_abotype" />');
            sourceSelectFetchXml.push(     '<attribute name="nhs_parentaccount" />');
            sourceSelectFetchXml.push(     '<filter type="and">');
            sourceSelectFetchXml.push(             '<condition attribute="statecode" operator="eq" value="0" />');
            sourceSelectFetchXml.push(             '<filter type="or">');
            sourceSelectFetchXml.push(                   '<condition attribute="nhs_donorcount" operator="gt" value="0" />');
            sourceSelectFetchXml.push(                   '<condition attribute="nhs_frozenunitcount" operator="gt" value="0" />');
            sourceSelectFetchXml.push(             '</filter>');
            sourceSelectFetchXml.push(rareBloodDonor.optionsPane._getAboSelectFetchXml());
            sourceSelectFetchXml.push(    '</filter>');
            sourceSelectFetchXml.push(rareBloodDonor.optionsPane._getAntigenSelectFetchXml(antigenCriteria));
            sourceSelectFetchXml.push(     '<link-entity name="account" from="accountid" to="nhs_parentaccount" visible="false" link-type="inner" alias="parentAccount">');
            sourceSelectFetchXml.push(         '<attribute name="accountid" />');
            sourceSelectFetchXml.push(         '<attribute name="nhs_countryid" />');
            sourceSelectFetchXml.push(         '<attribute name="address1_composite" />');
            sourceSelectFetchXml.push(         '<attribute name="name" />');
            sourceSelectFetchXml.push(         '<attribute name="address1_longitude" />');
            sourceSelectFetchXml.push(         '<attribute name="address1_latitude" />');
            sourceSelectFetchXml.push(         '<order attribute="address1_latitude" descending="false"/>');
            sourceSelectFetchXml.push(     '</link-entity>');
            sourceSelectFetchXml.push( '</entity>');
            sourceSelectFetchXml.push( '</fetch>');

            return sourceSelectFetchXml.join('\n');
        },
        _getAboSelectFetchXml: function () {
            
            var aboSelectfetchXml = [];

            selectedOption = $("select#aboGroup").children("option:selected");
            selectedOptionId = $(selectedOption[0]).attr("value");

            aboTypes = $.grep(rareBloodDonor.common.aboSubGroups,
                function (arrayItem, arrayItemIndex) { return arrayItem.id == selectedOptionId; }
            )[0].includedAboTypes;

            if (aboTypes.length > 0) { //if an Abo filter is required (i.e. it's not any)
                
                //set-up the wrapper for each type to be included
                aboSelectfetchXml.push('<condition attribute="nhs_abotype" operator="in">');

                for (var i = 0; i < aboTypes.length; i++) {
                    //insert each abo type
                    aboSelectfetchXml.push('<value>'+ aboTypes[i] + '</value>');
                }

                //close the ABO wrapper
                aboSelectfetchXml.push('</condition>');
            }

            return aboSelectfetchXml.join("\n");
        },
        _getAntigenSelectFetchXml: function(antigenCriteriaLines) {
            
            var selectStatement = [];

            //loop through the antigen criteria
            for (var i = 0; i < antigenCriteriaLines.length; i++) {
                
                antigenCriteria = antigenCriteriaLines[i];
                antigenGuid = Object.keys(antigenCriteria)[0];
                antigenResult = antigenCriteria[antigenGuid];


                selectStatement.push('<link-entity name="nhs_antigensourceassociation" from="nhs_sourceid" to="nhs_rarebloodsourceid" link-type="inner" alias="ac' + i + '">');
                selectStatement.push(    '<filter type="and">');
                selectStatement.push(        '<condition attribute="statuscode" operator="eq" value="1" />'); //i.e. only query active associations
                selectStatement.push(        '<condition attribute="nhs_antigenid" operator="eq" value="{' + antigenGuid + '}" />');
                selectStatement.push(        '<condition attribute="nhs_antigenresult" operator="in">');

                if (antigenResult == 127130000) {//i.e. if we're searching for the presence of an antigen
                    selectStatement.push(            '<value>127130000</value>'); //i.e. positive
                    selectStatement.push(            '<value>127130002</value>'); //inlcude weak
                } else { //i.e. we're searching for the absence
                    selectStatement.push(            '<value>127130001</value>'); //i.e. negative
                }

                selectStatement.push(        '</condition>');
                selectStatement.push(    '</filter>');
                selectStatement.push('</link-entity>');
            }

            return selectStatement.join('\n');

        },
        _getImpliedAntigenCriteria: function () {

            var antigenCriteriaLines = [];
            var rarityDefinitions = rareBloodDonor.optionsPane.rarityDefinitions.value;

            //get the selected rarities
            selectedOptions = $("select#rarity").children("option:selected");

            //then loop through the selected rarities
            for (var i = 0; i < selectedOptions.length; i++) {

                var rarityId = $(selectedOptions[i]).attr("value");

                //get the definition of that rarity from the rarity definitions
                var rarityDefinition = $.grep(rarityDefinitions, 
                    function (arrayItem, arrayItemIndex) { return arrayItem.nhs_rarityid == rarityId; }
                );

                var rarityAntigenResults = rarityDefinition[0].nhs_nhs_rarity_nhs_antigenrarityassociation_RarityId;

                //loop through the antigen results for that rarity
                for (var j = 0; j < rarityAntigenResults.length; j++) {

                    var antigenResult = rarityAntigenResults[j];
                    var antigenCriteria = {};

                    //add a name value pair with the GUID and result
                    antigenCriteria[antigenResult._nhs_antigenid_value] = antigenResult.nhs_antigenresultraritycontext;

                    //append each antigen result to the array
                    antigenCriteriaLines.push(antigenCriteria);
                }
            };

            //TODO actually get the implied antigen criteria
            return antigenCriteriaLines;
        },
        _getExplicitAntigenCriteria: function() {

            var antigenCriteria = [];

            //get the selected options from the form (Absent and Present)
            antigenCriteria = antigenCriteria.concat(rareBloodDonor.optionsPane._getExplicitAntigenGuidsFromControl(control = "select#antigenPresent", presenceValue = 127130000));//i.e. optionset present ID
            antigenCriteria = antigenCriteria.concat(rareBloodDonor.optionsPane._getExplicitAntigenGuidsFromControl(control = "select#antigenAbsent", presenceValue = 127130001));//i.e. optionset present ID

            //return the antigen 
            return antigenCriteria;

        },
        _getExplicitAntigenGuidsFromControl: function(control, presenceValue) {

            var antigenCriteriaLines = [];
            
            //get the selected values
            selectedOptions = $(control).children("option:selected");

            //then loop through athe values
            for (var i = 0; i < selectedOptions.length; i++) {

                var antigenCriteria = {};
                antigenCriteria[$(selectedOptions[i]).attr("value")] = presenceValue;

                //and push each value to the array as an object
                antigenCriteriaLines.push(
                    antigenCriteria
                );
            }

            return antigenCriteriaLines;

        },
        _recordSearchCriteria: function () {

            var criteria = {
                aboGroup: rareBloodDonor.optionsPane._getSelectedValuesString("aboGroup"),
                antigensAbsent: rareBloodDonor.optionsPane._getSelectedValuesString("antigenAbsent"),
                antigensPresent: rareBloodDonor.optionsPane._getSelectedValuesString("antigenPresent"),
                rarities: rareBloodDonor.optionsPane._getSelectedValuesString("rarity")
            };

            //post to the users wall, to present admins with a timeline of user activity
            rareBloodDonor.optionsPane._postSearchSummaryToUsersWall(criteria);
            
            //Get the values of the previous search (to determine if this is a duplicate)
            rareBloodDonor.optionsPane._getPreviousSearchRecord(
                currentCriteria = criteria,

                //then create a search execution report for reporting purposes., and for non-admin  users to view
                callbackFunction = rareBloodDonor.optionsPane._createSearchExecutionRecord
            );

        },
        _getPreviousSearchRecord: function (currentCriteria, callbackFunction) {
            var req = new XMLHttpRequest();


            //get the current user id
            var currentUserGuid = irdpCommon.security.getCurrentUserGuid();

            //request the previous search for that user
            req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/nhs_searchexecutions?$select=nhs_criteria_abo,nhs_criteria_antigensabsent,nhs_criteria_antigenspresent,nhs_criteria_rarities,createdon&$filter=_createdby_value eq " + currentUserGuid + "&$orderby=createdon desc", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations=\"*\",odata.maxpagesize=1");
            req.onreadystatechange = function () {
                rareBloodDonor.optionsPane._parsePreviousSearchRecord(this, currentCriteria, callbackFunction);
            };
            req.send();
        },
        _parsePreviousSearchRecord: function (event, currentCriteria, createSearchExecutionRecord) {
                if (event.readyState === 4) {
                    event.onreadystatechange = null;
                    if (event.status === 200) {
                        var results = JSON.parse(event.response);
                        var isDuplicate = false; // default, assume its not a duplicate

                        if (results.value.length != 0) {
                            //if there is a result, need to test the values
                            var previousCriteria = {
                                aboGroup: results.value[0]["nhs_criteria_abo"],
                                antigensAbsent: results.value[0]["nhs_criteria_antigensabsent"],
                                antigensPresent: results.value[0]["nhs_criteria_antigenspresent"],
                                rarities: results.value[0]["nhs_criteria_rarities"]
                            };

                            //if the previous search is the same as the current search
                            
                            var yesterday = new Date() - 86400000; //milliseconds in a day
                            var isCriteriaSameAsPrevious = JSON.stringify(previousCriteria) == JSON.stringify(currentCriteria);
                            var isPreviousInLast24Hours = new Date(results.value[0]["createdon"]) > yesterday;
                            
                            isDuplicate = isCriteriaSameAsPrevious && isPreviousInLast24Hours;
                        }

                        //create the search execution record
                        createSearchExecutionRecord(currentCriteria, isDuplicate);

                    } else {
                        Xrm.Utility.alertDialog(this.statusText);
                    }
                }
            },
        _postSearchSummaryToUsersWall: function(criteria) {
            var postText = "User executed search with the following criteria:  " +
                "    \nABO Groups: " + criteria.aboGroup +
                "    \nRarities: " + criteria.rarities +
                "    \nAntigens Present: " + criteria.antigensPresent +
                "    \nAntigens Absent: " + criteria.antigensAbsent;

            //post to the users wall to populate the timeline
            rareBloodDonor.common.postToCurrentUserWall(postText);
        },
        _createSearchExecutionRecord: function (criteria, isDuplicate) {

            var entity = {
                nhs_criteria_abo: criteria.aboGroup,
                nhs_criteria_antigensabsent: criteria.antigensAbsent,
                nhs_criteria_antigenspresent: criteria.antigensPresent,
                nhs_criteria_rarities: criteria.rarities,
                nhs_isduplicate: isDuplicate
            };

            if (rareBloodDonor.common.currentUsersTeam != undefined) {
                entity["nhs_OnBehalfofTeam@odata.bind"] = "/teams(" + rareBloodDonor.common.currentUsersTeam.teamid + ")";
            }

            var req = new XMLHttpRequest();
            req.open("POST", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/nhs_searchexecutions", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status != 204) {
                        Xrm.Utility.alertDialog(this.statusText);
                    }
                }
            };
            req.send(JSON.stringify(entity));

        },
        _getSelectedValuesString: function (selectId) {

            var selectedOptions = $("select#" + selectId).children("option:selected");

            //Get the first value
            var outputString = $(selectedOptions[0]).attr("title");

            //then loop through any subsequent values
            for (var i = 1; i < selectedOptions.length; i++) {
                //and append with a comma
                outputString = outputString + ", " + $(selectedOptions[i]).attr("title");
            };

            if (outputString != undefined) {
                return outputString;
            } else {
                return "[none specified] ";
            }
        },
        _isFilterOptionsPaneVisible: function () {
            var cssLeftAttribute = $("#filterOptionsOverlay").css("left");
            return cssLeftAttribute == "0px";
        },
        _populateDropDownAboGroups: function () {
            var values = rareBloodDonor.common.aboSubGroups;
            var control = $("#aboGroup");
            var canSelectMany = false;

            rareBloodDonor.optionsPane._populateDropDownGeneric_HardcodedVals(values, control, canSelectMany);

        },
        _populateDropDownGeneric_HardcodedVals: function (commonObjectValues, control, canSelectMany) {
            var i;

            //Add in the values in turn.
            for (i = 0; i < commonObjectValues.length; i++) {
                commonObjectValue = commonObjectValues[i];
                opt = document.createElement('option');
                opt.value = commonObjectValue.id;
                opt.innerHTML = commonObjectValue.label;
                control.append(opt);
            }

            rareBloodDonor.optionsPane._applySelect2Styling(control, canSelectMany);


        },
        _populateDropDownAntigen: function() {
            rareBloodDonor.optionsPane.__dynamicallyRetrieveValues(
                "/api/data/v9.1/nhs_antigens?$select=nhs_antigenid,nhs_namehtml,nhs_nameunicode&$filter=statecode eq 0&$orderby=nhs_nameunicode asc",
                rareBloodDonor.optionsPane.__pushResultsToAntigenSelect
            );
        },
        _populateDropDownRarity: function () {
            rareBloodDonor.optionsPane.__dynamicallyRetrieveValues(
                "/api/data/v9.1/nhs_rarities?$select=nhs_namehtml,nhs_nameunicode,nhs_rarityid&$filter=statecode eq 0&$orderby=nhs_nameunicode asc&$expand=nhs_nhs_rarity_nhs_antigenrarityassociation_RarityId($select=_nhs_antigenid_value,nhs_antigenresultraritycontext)",
                rareBloodDonor.optionsPane.__pushResultsToRaritySelect
            );
        },
        __dynamicallyRetrieveValues: function (odataString, callbackFunction) {
            var req = new XMLHttpRequest();
            req.open("GET", Xrm.Page.context.getClientUrl() + odataString, true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
            req.onreadystatechange = function () { callbackFunction(this.readyState, this.status, this.statusText, this.response); };
            req.send();
        },
        __pushResultsToAntigenSelect: function(readyState, status, statusText, response) {

            if (readyState === 4) {

                if (status === 200) {
                    rareBloodDonor.optionsPane.__populateDropDownGeneric_DynamicsVals($("select#antigenPresent, select#antigenAbsent"), response, "nhs_antigenid", "nhs_namehtml", canSelectMany = true);

                } else {
                    Xrm.Utility.alertDialog("Error retreiving Antigens via oData: "+ statusText);
                }
            }
        },
        __pushResultsToRaritySelect: function (readyState, status, statusText, response) {

            if (readyState === 4) {

                if (status === 200) {

                    //Store the response in a variable
                    rareBloodDonor.optionsPane.rarityDefinitions = JSON.parse(response);

                    //Populate the rarity dropdown
                    rareBloodDonor.optionsPane.__populateDropDownGeneric_DynamicsVals($("#rarity"), response, "nhs_rarityid", "nhs_namehtml", canSelectMany = true);

                } else {
                    Xrm.Utility.alertDialog("Error retreiving Rarities via oData: " + statusText);
                }
            }
        },
        __populateDropDownGeneric_DynamicsVals: function(control, response, idSource, textSource, canSelectMany) {

            var results = JSON.parse(response);
            var opt;


            //if it's not a multiselect field
            if (canSelectMany == false) {
                //then add a blank option as the default
                opt = document.createElement('option');
                opt.value = "";
                opt.innerHTML = "";
                control.append(opt);
            }
 

            //Loop through the results and append each to the select control
            for (var i = 0; i < results.value.length; i++) {
                var result = results.value[i];
                opt = document.createElement('option');

                opt.value = result[idSource];
                opt.innerHTML = result[textSource];
                control.append(opt);
            };
            
            //loop through the collection of controls
            for (var i = 0; i < control.length; i++) {
                //convert the native select control to a select2 style control
                rareBloodDonor.optionsPane._applySelect2Styling(control[i], canSelectMany);
            }
            
            
        },

        _applySelect2Styling: function(selectControl, canSelectMany) {
            
            selectControl = $(selectControl);
            var selectOptions = selectControl.children("option");

            var data = [];

            //build a new collection i the select2 format based on the native select statements options
            for (var i = 0; i < selectOptions.length; i++) {
                var selectOption = selectOptions[i];
                data.push(
                    {
                        id: selectOption.value,
                        html: selectOption.innerHTML,
                        text: selectOption.innerHTML,
                        title: selectOption.text 
                    }
                );
            };

            //Remove the options from the native control
            selectControl.empty();
            selectControl.removeData();

            //apply the select2 styling and append the data collection
            selectControl.select2({
                data: data,
                dropdownParent: selectControl.parent(),
                minimumResultsForSearch: 6,
                multiple: canSelectMany,
                escapeMarkup: function (m) { return m; },
                templateResult: function (data) { return data.html; },
                templateSelection: function (data) { return data.html; },
                matcher: rareBloodDonor.optionsPane._select2CustomMatcher // a custom matcher is required because the select2 default was including HTML tags in the searched text

            });

        },

        _select2CustomMatcher: function(params, data) {
            // If there are no search terms, return all of the data
            if ($.trim(params.term) === '') {
              return data;
            }

            // Do not display the item if there is no 'text' property
            if (typeof data.text === 'undefined') {
              return null;
            }

            // `params.term` should be the term that is used for searching
            // `data.text` is the text that is displayed for the data object
            if (data.title.toLowerCase().indexOf(params.term.toLowerCase()) > -1) {
                return data;
            }

            // Return `null` if the term should not be displayed
            return null;
        },         

        _select2Template: function (data) {
            return data.html;
        }
    },
    detailsPane: {
        currentEntityData: null,
        initialise: function () {
            $(".detailsFormClose").off().on("click", rareBloodDonor.detailsPane.hide);
        },
        show: function (result) {
  
            //show the map block overlay
            $("#blockMapClicksOverlay").css("display", "block");

            $("#blockMapClicksOverlay").off().on("click", function () {
                $("#blockMapClicksOverlay").css("display", "none");
                rareBloodDonor.detailsPane.hide();
            });

            //==========
            //Populate the name of the institution
            var donorCountText;
            var frozenUnitCountText;
            var totalCountText;
            donorCountText = rareBloodDonor.detailsPane._createNounPhraseForRegularPlural(result.totalMatchingDonors, "donor");
            frozenUnitCountText = rareBloodDonor.detailsPane._createNounPhraseForRegularPlural(result.totalMatchingFrozenUnits, "frozen unit");

            if (donorCountText != "" && frozenUnitCountText != "") {
                //if there's a value in both strings
                //then concatenate them together with " and "
                totalCountText = donorCountText + " and " + frozenUnitCountText;
            } else {
                //otherwise, just use whichever value is populated
                totalCountText = donorCountText + frozenUnitCountText;
            }

            $("#instTitle").text(result.name + "\n" + totalCountText);
            $("#instAddressBlock").text(result.addressBlock + "\n" + result.countryName);

            //Populate the contact details for the instituion
            rareBloodDonor.detailsPane._getContactDetails(result.id);

            var bloodSourceHtml = [];
            var sourceIds = [];

            for (var i = 0; i < result.results.length; i++) {
                var bloodSource = result.results[i];
                sourceIds.push(bloodSource.nhs_rarebloodsourceid);

                var frozenUnit = rareBloodDonor.detailsPane._createNounPhraseForRegularPlural(bloodSource.nhs_frozenunitcount, "Frozen Unit");

                //Open the div for the source
                bloodSourceHtml.push('<div class="rareBloodSource">');
                bloodSourceHtml.push('    <div class="rareBloodSource_header">');
                if (bloodSource.nhs_donorcount == 1) {
                    // if its a donor
                    bloodSourceHtml.push('      <div class="rareBloodSource_donor">');
                    bloodSourceHtml.push('              Donor ' + bloodSource.nhs_contributorcode);
                    bloodSourceHtml.push('      </div>');
                    if (bloodSource.nhs_frozenunitcount > 0) {
                        
                        bloodSourceHtml.push('      <div class="rareBloodSource_product">(' + frozenUnit + ')</div>');
                        
                    } else {

                    }
                } else {
                    //If its a product line (i.e. frozen units only)
                    bloodSourceHtml.push('      <div class="rareBloodSource_product"> ' + bloodSource.nhs_contributorcode + "\n(" + frozenUnit + ')</div>');
                }
                bloodSourceHtml.push('      </div>');
                bloodSourceHtml = bloodSourceHtml.concat(rareBloodDonor.detailsPane._getSourceDetailHtml(bloodSource));
                
            }

            //insert the cumulative html into the details pane
            $("div#bloodSourceCollection").html(bloodSourceHtml.join("\n"));

            //scroll to the top of the details pane 
            $("div#DetailsPaneContent").scrollTop();

            //slide the details pane into view
            $("#entityFormOverlay").animate({ right: "0px" }, 350);
        },
        refreshBloodSourceDetails:function(pushPinMetaData) {

            //if that institution is currently displayed in the details
            //(i.e. test if the first results is included)
            if ($("div.rareBloodSource_detail#" + pushPinMetaData.results[0].nhs_rarebloodsourceid).length > 0) {

                //used when the antigen and rarity results are returned (asynchrononously) after the initial search
                for (var i = 0; i < pushPinMetaData.results.length; i++) {
                    //get the blood source in the collection
                    var bloodSource = pushPinMetaData.results[i];

                    //generate the HTML for that blood 
                    var bloodSourceDetailHtml = rareBloodDonor.detailsPane._getSourceDetailHtml(bloodSource);

                    //insert that HTML into the right div in the DOM
                    $("div.rareBloodSource_detail#" + bloodSource.nhs_rarebloodsourceid).html(bloodSourceDetailHtml);
                }
            }
        },
        _getSourceDetailHtml: function (bloodSource) {
            var bloodSourceHtml = [];

            //get the string values of the associations
            var raritiesString = bloodSource.rarities.join(", ");
            var antigensPresentString = bloodSource.antigensPresent.join(", ");
            var antigensAbsentString = bloodSource.antigensAbsent.join(", ");

            //if they are all blank
            if (raritiesString == "" && antigensPresentString == "" && antigensAbsentString == "") {
                //and if the results are 
                if (rareBloodDonor.map._subQueries.allComplete()) {
                    raritiesString = "None";
                    antigensPresentString = "None";
                    antigensAbsentString = "None";
                } else {
                    //but if the resulst are still loading, set them to "Loading"
                    raritiesString = "Loading...";
                    antigensPresentString = "Loading...";
                    antigensAbsentString = "Loading...";
                }
                    
            };
            
            bloodSourceHtml.push('      <div id="' + bloodSource.nhs_rarebloodsourceid + '" class="rareBloodSource_detail">');
            bloodSourceHtml.push('              <div class="detailsLabel">IRDP ID:</div>');
            bloodSourceHtml.push('              <div class="detailsValue">' + bloodSource.nhs_panelcode + '</div>');
            if (bloodSource.nhs_donorcount != 1) {
                bloodSourceHtml.push('              <div class="donorCount_label detailsLabel">Donors:</div>');
                bloodSourceHtml.push('              <div class="donorCount_value detailsValue">' + bloodSource.nhs_donorcount + '</div>');
            } else {
                bloodSourceHtml.push('              <div class="donorCount_label detailsLabel"></div>');
                bloodSourceHtml.push('              <div class="donorCount_value detailsValue"></div>');
            }
            bloodSourceHtml.push('              <div class="aboGroup_label detailsLabel">ABO Group:</div>');
            bloodSourceHtml.push('              <div class="aboGroup_value detailsValue">' + bloodSource["nhs_abotype@OData.Community.Display.V1.FormattedValue"] + '</div>');
            bloodSourceHtml.push('              <div class="rarities_label detailsLabel">Rarities:</div>');
            bloodSourceHtml.push('              <div id="rarities_values' + bloodSource.nhs_rarebloodsourceid  + '" class="detailsValue">' + raritiesString + '</div>');
            bloodSourceHtml.push('              <div class="antigensPresent_label detailsLabel">Antigens present:</div>');
            bloodSourceHtml.push('              <div id="antigensPresent_values' + bloodSource.nhs_rarebloodsourceid + '" class="detailsValue">' + antigensPresentString + '</div>');
            bloodSourceHtml.push('              <div class="antigensAbsent_label detailsLabel">Antigens absent:</div>');
            bloodSourceHtml.push('              <div id="antigensAbsent_values' + bloodSource.nhs_rarebloodsourceid + '" class="detailsValue">' + antigensAbsentString + '</div>');
            bloodSourceHtml.push('      </div>');
            bloodSourceHtml.push('</div>');

            return bloodSourceHtml.join("\n");
        },
        _getContactDetails: function (accountId) {
            var req = new XMLHttpRequest();
            req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/contacts?$select=emailaddress1,fullname,telephone1,telephone2&$filter=statecode eq 0 and _parentcustomerid_value eq " + accountId, true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 200) {
                        var results = JSON.parse(this.response);
                        rareBloodDonor.detailsPane._parseContactDetails(results.value);
                    } else {
                        Xrm.Utility.alertDialog(this.statusText);
                    }
                }
            };
            req.send();
        },
        _parseContactDetails: function(contacts) {
            var emailAllAddresses = [];
            var contactsHtml = [];
            var contactsHeaderHtml = [];
            

            contactsHtml.push("<div id='contactDetailsCollection'>");
            //first, set up a div for each contact
            for (var i = 0; i < contacts.length; i++) {
                contact = contacts[i];

                contactsHtml.push('    <div class="detailsPaneContact">' + contact.fullname);
                //if phone number 1 is populated
                if (contact.telephone1 != null) {
                    //then add it to the contact div
                    contactsHtml.push(contact.telephone1);
                }
                //if phone number 2 is populated
                if (contact.telephone2 != null) {
                    //then add it to the contact div
                    contactsHtml.push(contact.telephone2);
                }
                //if the email address is populated
                if (contact.emailaddress1 != null) {
                    //then add it to the contact div
                    contactsHtml.push('<a href="mailto:' + contact.emailaddress1 + '?Subject=IRDP%20Query%20regarding%20Rare%20Blood%20Product%20Availability" target="_top">' + contact.emailaddress1 + '</a>');
                    //AND add it to the email all array
                    emailAllAddresses.push(contact.emailaddress1);
                }
                contactsHtml.push('</div>');
            }
            contactsHtml.push('</div>');

            //then set up the header for the contacts
            contactsHeaderHtml.push('<div id="detailsPaneContactHeader">');
            contactsHeaderHtml.push('<span id="detailsPaneContactHeader_title">Contact: </span>');
            if (contacts.length > 1) { //if there's more than one contact
                //then include an "Email all" button
                contactsHeaderHtml.push('<span id="detailsPaneContactHeader_emailAll"><a href="mailto:' + emailAllAddresses.join(";") + '?Subject=IRDP%20Query%20regarding%20Rare%20Blood%20Product%20Availability" target="_top">Email all...</a></span>');
            }
            contactsHeaderHtml.push('</div>');

            $("div#contactDetails").html(contactsHeaderHtml.join("\n") + "\n" + contactsHtml.join("\n"));

        },
        hide: function () {

            $("div#entityFormOverlay").animate({ right: "-360px" }, 350);
        },
        _createNounPhraseForRegularPlural: function(count, singularNounForm) {
            
            var pluralNounForm = singularNounForm + "s";
            
            var nounPhrase;

            switch (count) {
                case 0:
                    nounPhrase = "";
                    break;  
                case 1:
                    nounPhrase = "1 " + singularNounForm;
                    break;
                default:
                    nounPhrase = count + " " + pluralNounForm;
            };

            return nounPhrase;
        }
    },
    termsOfUse: {
        preDialogChecks: function (isTermsPresentationDue) {

            var xrmUserSettings = Xrm.Utility.getGlobalContext().userSettings;

            var isExternalUser = rareBloodDonor.termsOfUse.getIfExternalUser();

            
            if (isTermsPresentationDue && isExternalUser) {
                // if they're an external user who hasn't accepted the terms of use in the last 24 hours
                rareBloodDonor.termsOfUse.showDialog(xrmUserSettings);

             } else {

                //Initialise the additional controls we've laid on top of the map.
                rareBloodDonor.map.initialise();
            }
        },
        getIfExternalUser: function () {
            //lookup if user in either contributor or consumer groups
            return irdpCommon.security.checkIfIrdpContributor() || irdpCommon.security.checkIfIrdpConsumer();
        },
        getLastAcceptedTermsDate: function (callbackFunction) {

            //get the current user's supplementary record guid
            var objectGuid = rareBloodDonor.common.supplementaryUserFormGuid;


            var req = new XMLHttpRequest();
            req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/nhs_usersupplementaryfieldses(" + objectGuid + ")?$select=nhs_lastacceptedtermson", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 200) {
                        var result = JSON.parse(this.response);
                        if (result["nhs_lastacceptedtermson"] == null) {
                            //if there isn't a date in that field,
                            //Then show the terms
                            showTerms = true;
                        } else {
                            //otherwsie test the date
                            //to see if it is more than 24 hours ago
                            var lastShownDate = Date.parse(result["nhs_lastacceptedtermson"]);
                            var yesterday = new Date() - 86400000; //milliseconds in a day
                            showTerms = yesterday > lastShownDate;
                        }

                        callbackFunction(showTerms);
                    } else {
                        Xrm.Utility.alertDialog(this.statusText);
                    }
                }
            };
            req.send();
        },
        showDialog: function (userSettings) {
            Xrm.Navigation.openConfirmDialog(
                 confirmStrings = {
                     title: "NHS Blood and Transplant User Notice",
                     text: "I understand that when logging onto the International Rare Donor Panel (IRDP) that I should only access data and information that I have a right to access, and all access is monitored and controlled to meet legal privacy and confidentiality requirements protecting registry contributors, donors and patients.\n"
                     + "\u2800 \n"
                     + "I understand that intentional or unintentional misuse of data which NHSBT holds on behalf of ISBT can have significant consequences for myself, my organisation and NHSBT, and I agree to comply with all applicable data protection legislation within my country's jurisdiction (which if based in the EU is the General Data Protection Regulation 2016/679)."
                 },
                 confirmOptions = {
                     height: 400,
                     width: 450
                 }
            ).then(
                successCallback = function (success) {
                    if (success.confirmed) {
                        //Post to the users wall
                        rareBloodDonor.common.postToCurrentUserWall("User clicked 'OK' on NHSBT's user notice");

                        //set the accepted on date
                        rareBloodDonor.termsOfUse.setUserLastAcceptedTermsDate();

                        //Initialise the additional controls we've laid on top of the map.
                        rareBloodDonor.map.initialise();
                    } else {
                        rareBloodDonor.common.postToCurrentUserWall("User closed NHSBT's user notice without clicking OK (e.g. cancelled or closed X) and was denied access.");
                        window.top.location.href = "about:blank";
                    }
                },
                 errorCallback = function () {
                     rareBloodDonor.common.postToCurrentUserWall("Terms of use dialogue was presented to this user, but it reported an error. The user was denied access.");
                     window.top.location.href = "about:blank";
                }
            );

        },
        setUserLastLoginDate: function () {
            irdpCommon.workflow.executeOnDemand(
                "8813D2C9-0330-424F-AA38-8A251D9ED63A",
                rareBloodDonor.common.supplementaryUserFormGuid,
                function () { }
            );
        }, 
        setUserLastAcceptedTermsDate: function () {

            irdpCommon.workflow.executeOnDemand(
                "E8D1BC87-43DE-4072-9052-8D1B6F18D870",
                rareBloodDonor.common.supplementaryUserFormGuid,
                function () { }
            );
        },
        getSupplementaryUserForm: function() {

            var userGuid = irdpCommon.security.getCurrentUserGuid();

            var req = new XMLHttpRequest();
            
            req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/nhs_usersupplementaryfieldses?$select=nhs_usersupplementaryfieldsid&$filter=_createdby_value eq " + userGuid, true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
            req.onreadystatechange = function() {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 200) {
                        var results = JSON.parse(this.response);

                        if (results.value.length == 1) { rareBloodDonor.termsOfUse.upsertSupplementaryUserForm(results.value[0]["nhs_usersupplementaryfieldsid"]);}
                        else if (results.value.length > 1) { rareBloodDonor.termsOfUse.multipleSupplementsFound();}
                        else { rareBloodDonor.termsOfUse.upsertSupplementaryUserForm(null);}
                        
                    } else {
                        Xrm.Utility.alertDialog(this.statusText);
                    }
                }
            };
            req.send();

            

            
        },
        upsertSupplementaryUserForm: function (supplementGuid) {

            
            switch (supplementGuid) {
                case null:
                    //if no supplement found - create one and store the guid
                    rareBloodDonor.termsOfUse.createSupplementaryUserForm(function () {
                        rareBloodDonor.termsOfUse.confirmLogin();
                    });
                    
                    break;
                default:
                    //If the GUID is provided, then the user already has a supplementary entity
                    //so capture the guid
                    rareBloodDonor.common.supplementaryUserFormGuid = supplementGuid;
                    rareBloodDonor.termsOfUse.confirmLogin();
                    break;
            }

        },
        createSupplementaryUserForm: function(callbackFunction) {
            var entity = {};
            entity.nhs_name = "New User Supplementary Form";

            var req = new XMLHttpRequest();
            req.open("POST", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/nhs_usersupplementaryfieldses", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.onreadystatechange = function() {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 204) {
                        var uri = this.getResponseHeader("OData-EntityId");
                        var regExp = /\(([^)]+)\)/;
                        var matches = regExp.exec(uri);
                        rareBloodDonor.common.supplementaryUserFormGuid = matches[1];
                        callbackFunction();
                    } else {
                        Xrm.Utility.alertDialog(this.statusText);
                    }
                }
            };
            req.send(JSON.stringify(entity));
        },
        confirmLogin: function() {

            //Post to the users wall
            rareBloodDonor.common.postToCurrentUserWall("User logged in");

            //set the last log-in date
            rareBloodDonor.termsOfUse.setUserLastLoginDate();

            //test whether the terms of use should be shown
            rareBloodDonor.termsOfUse.getLastAcceptedTermsDate(
                callbackFunction = rareBloodDonor.termsOfUse.preDialogChecks
            );

        }
    }
};