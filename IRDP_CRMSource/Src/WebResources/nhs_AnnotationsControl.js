const Constants = {

    SYSTEM_JOB_STATUS_REASON_ENUM: {
        SUCCEEDED: 30,
        IN_PROGRESS: 20,
        FAILED: 31,
    },

    SYSTEM_JOB_OPERATION_TYPE_ENUM: {
        SYSTEM_EVENT: 1,
        INFORM_FILE_PARSE: 3,
        TRANSFORM_PARSE_DATA: 4,
        IMPORT: 5,
    },
};

var pageParams;

var annotationsControl = {

    checkAccess: function () {

        pageParams = Xrm.Utility.getGlobalContext().getQueryStringParameters();

        var isUpdateForm = Xrm.Page.ui.getFormType() == 2;

        var isUserInElevatedRole = (irdpCommon.security.checkIfSysAdmin() || irdpCommon.security.checkIfIrdpAdmin());

        //if the user is a contributor, IRDP admin, or sys admin, and this is an update form
        if (isUserInElevatedRole && isUpdateForm) {
            //then initialise the control
            annotationsControl.initialise();
        } else if (isUpdateForm) { //otherwise, if they're not admin user, but this is an update form
            //we need to check their team membership
            irdpCommon.security.checkIfOwningTeamMember(function (isOwningTeamMember) {
                if (isOwningTeamMember) {
                    //if they are in the owning team
                    //then initialise the control
                    annotationsControl.initialise()
                } else {
                    //otherwise, hide the control
                    $("body").empty();
                }
            })


        }
    },
    initialise: function () {

        //display the control
        $("body").show();

        $("#fileimport").show();


        //fetch the existing annotations and append them to the list
        annotationsControl.fetchExisting(pageParams.id);

        //enable the controls
        $("input").prop("disabled", false);



        //Only show the "Submit" button after the input is complete
        annotationsControl.submitButton.hide();
        $("input:file").val("")
            .off()
            .on("input", annotationsControl.submitButton.show)
            .on("change", annotationsControl.submitButton.show);

        $("input:submit").off().on("click", annotationsControl.uploadNew.clickSave);

    },

    resetProgressBars: function () {
        annotationsControl.refresh();
        $('#FileProgress').css({ "background-color": "#4CAF50", "width": "0%" });
        $('#FileRowParseProgress').css({ "background-color": "#4CAF50", "width": "0%" });

        $('#MessageDescription img').css('display', 'none');
        $('#MessageDescription span').text('');
        $('#FileStepsTitle span').text('Uploading File (Step 0 out of 4 Completed)');
        $('#FileParsingRowTitle span').text('Parsing Rows ( 0 of 0 )');

    },

    updateFileProgressStepBar: function (step, width) {

        $('#FileStepsTitle span').text('Uploading File (Step ' + step + ' out of 4 Completed)');
        $('#FileProgress').css({ "background-color": "#4CAF50", "width": width + "%" });

    },

    updateFileParseRowBar: function (parsedRowTotal, overallRowTotal) {

        var width = ((parsedRowTotal / overallRowTotal) * 100);

        $('#FileParsingRowTitle span').text('Parsing Rows (' + parsedRowTotal + ' of ' + overallRowTotal + ' )');
        $('#FileRowParseProgress').css({ "background-color": "#4CAF50", "width": width + "%" });

    },

    showParsingRowGraph: function (filename, fileCreationDate) {
        var file = annotationsControl.getFile(filename, fileCreationDate);

        var parseTotal = file.successcount + file.failurecount + file.partialfailurecount;
        //if the file is not found, display failed message 
        if (file == null) {

            annotationsControl.showFailedMessageFileImport();

        } else {

            switch (file.statuscode) {
                case 4:
                    setTimeout(function () { annotationsControl.updateFileParseRowBar(parseTotal, file.totalcount); }, 200);
                    break;
                default:
                    setTimeout(function () { annotationsControl.updateFileParseRowBar(parseTotal, file.totalcount); }, 200);
                    setTimeout(function () { annotationsControl.showParsingRowGraph(filename, fileCreationDate) }, 200);
                    break;
            }
        }
    },

    showProgressBar: function (filename, fileCreationDate) {

        var userTimeZonefileCreationDate =  annotationsControl.getUserDateTimeTimezone(fileCreationDate);
        var systemJob = annotationsControl.getPostCreateNotePluginSystemJob(filename, userTimeZonefileCreationDate);
        var hasFileFinishedImporting = new Boolean(false);

        if (systemJob == null) {
            setTimeout(function () { annotationsControl.showProgressBar(filename, fileCreationDate) }, 100);
        } else {

            switch (systemJob.statuscode) {

                case Constants.SYSTEM_JOB_STATUS_REASON_ENUM.FAILED:
                    annotationsControl.showFailedMessageFileImport();
                    break;
                case Constants.SYSTEM_JOB_STATUS_REASON_ENUM.SUCCEEDED:
                    setTimeout(function () { annotationsControl.nextSystemJob(systemJob, hasFileFinishedImporting, filename, userTimeZonefileCreationDate) }, 100);
                    break;
                default:
                    setTimeout(function () { annotationsControl.showProgressBar(filename, fileCreationDate) }, 100);
                    break;
            }
        }
    },

    nextSystemJob: function (systemJob, hasFileFinishedImporting, filename, fileCreationDate) {

        var formGuid = Xrm.Page.data.entity.getId();

        var importFilename = filename;
        var correlationid = systemJob.correlationid;

        if (hasFileFinishedImporting == false) {

            var systemJobSuccess = systemJob.statuscode;
            var operationtype = systemJob.operationtype;

            //If the file upload (OTB) succeeded
            if (systemJobSuccess != Constants.SYSTEM_JOB_STATUS_REASON_ENUM.FAILED) {

                //then test the operation type
                switch (operationtype) {

                    //if the operation type is System Event
                    case Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.SYSTEM_EVENT:
                        //then check if there is a file level failure
                        var hasFailedActivity = annotationsControl.getFailActivityFromFileImport(formGuid, importFilename, fileCreationDate);
                        if (!hasFailedActivity) {
                            //and if a file-level failure isn't detected
                            //then update the progress bar
                            annotationsControl.updateFileProgressStepBar(1, 25);
                            //and detect the next system job (file parsing)
                            if (annotationsControl.getCorrelationSystemJob(correlationid, Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.INFORM_FILE_PARSE) != false) {
                                systemJob = annotationsControl.getCorrelationSystemJob(correlationid, Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.INFORM_FILE_PARSE);
                            }

                        } else {
                            // if the OTB import failed, then we're done here
                            annotationsControl.showFailedMessageFileImport();
                            hasFileFinishedImporting = true;
                        }

                        break;

                    //if the operation type is parsing
                    case Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.INFORM_FILE_PARSE:
                        //then update the progress bar
                        systemJob = annotationsControl.getCorrelationSystemJob(correlationid, Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.INFORM_FILE_PARSE);

                        if (systemJob.statuscode == Constants.SYSTEM_JOB_STATUS_REASON_ENUM.SUCCEEDED) {
                            //then update the progress bar
                            annotationsControl.updateFileProgressStepBar(2, 50);
                            //and detect the next system job (transforming)
                            systemJob = annotationsControl.getCorrelationSystemJob(correlationid, Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.TRANSFORM_PARSE_DATA);

                        } else if (systemJob.statuscode == Constants.SYSTEM_JOB_STATUS_REASON_ENUM.FAILED) {
                            // if the OTB import failed, then we're done here
                            annotationsControl.showFailedMessageFileImport();
                            hasFileFinishedImporting = true;
                        }

                        break;

                    //if the operation type is transforming
                    case Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.TRANSFORM_PARSE_DATA:
                        systemJob = annotationsControl.getCorrelationSystemJob(correlationid, Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.TRANSFORM_PARSE_DATA);


                        if (systemJob.statuscode == Constants.SYSTEM_JOB_STATUS_REASON_ENUM.SUCCEEDED) {
                            //then update the progress bar
                            annotationsControl.updateFileProgressStepBar(3, 75);
                            //and start updating the row parsing graph
                            annotationsControl.showParsingRowGraph(filename, fileCreationDate);

                            //and detect the next system job (import)
                            systemJob = annotationsControl.getCorrelationSystemJob(correlationid, Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.IMPORT);

                        } else if (systemJob.statuscode == Constants.SYSTEM_JOB_STATUS_REASON_ENUM.FAILED) {
                            // if the OTB import failed, then we're done here
                            annotationsControl.showFailedMessageFileImport();
                            hasFileFinishedImporting = true;
                        }
                        break;

                    //If the operation type is importing
                    case Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.IMPORT:

                        systemJob = annotationsControl.getCorrelationSystemJob(correlationid, Constants.SYSTEM_JOB_OPERATION_TYPE_ENUM.IMPORT);


                        if (systemJob.statuscode == Constants.SYSTEM_JOB_STATUS_REASON_ENUM.SUCCEEDED) {
                            // then show the file import as 100% complete
                            annotationsControl.updateFileProgressStepBar(4, 100);
                            //show file import
                            $("#newAnnotation").show();
                            hasFileFinishedImporting = true;


                        } else if (systemJob.statuscode == Constants.SYSTEM_JOB_STATUS_REASON_ENUM.FAILED) {
                            // if the OTB import failed, then we're done here
                            annotationsControl.showFailedMessageFileImport();
                            hasFileFinishedImporting = true;
                        }

                        break;
                    default:
                        annotationsControl.showProgressBar(filename, fileCreationDate);
                        break;
                }
            } else {
                // if the OTB import failed, then we're done here
                annotationsControl.showFailedMessageFileImport();
                hasFileFinishedImporting = true;
            }

            setTimeout(
                function () {
                    annotationsControl.nextSystemJob(systemJob, hasFileFinishedImporting, importFilename, fileCreationDate)
                }
                , 200);
        }
    },

    getUserDateTimeTimezone: function (fileCreationDate) {
        var timezone;
        var dateValue = null;
        var initialDate = new Date(fileCreationDate);

        var userGuid = Xrm.Page.context.getUserId().substr(1, 36);;
        var entityName = "usersettingscollection";

        var fetchXml = [];

        fetchXml.push('<fetch>');
        fetchXml.push('<entity name="usersettings" >');
        fetchXml.push('<attribute name="timezonebias" />');
        fetchXml.push('<filter>');
        fetchXml.push('<condition attribute="systemuserid" operator="eq" value="'+userGuid+'" />');
        fetchXml.push('</filter>');
        fetchXml.push('</entity>');
        fetchXml.push('</fetch>');

        timezone = irdpCommon.query.getFetchXmlResult(fetchXml, entityName);

        if (timezone.value.length != 0) {

            dateValue = new Date(initialDate.getUTCFullYear(), initialDate.getUTCMonth(), initialDate.getUTCDate(), initialDate.getUTCHours(), initialDate.getUTCMinutes(), initialDate.getUTCSeconds());
            var actMinutes = dateValue.getMinutes();

            if (timezone['value'][0]['usersettings1.timezonebias@OData.Community.Display.V1.FormattedValue'] != null) {
                dateValue.setMinutes(actMinutes + (timezone['value'][0]['usersettings1.timezonebias'] * -1));
            }
            return dateValue;
        } else {
            return fileCreationDate;
        }
    }, 

    getFailActivityFromFileImport: function (formGuid, filename, fileCreationDate) {
        var activity;
        var initialDate = new Date(fileCreationDate);

        var fetchXml = [];
        var entityName = "activitypointers";
        fetchXml.push('<fetch>');
        fetchXml.push('<entity name="activitypointer" >');
        fetchXml.push('<filter>');
        fetchXml.push('<condition attribute="subject" operator="begins-with" value="File Error: " />');
        fetchXml.push('<condition attribute="regardingobjectid" operator="eq" value="' + formGuid + '" />');
        fetchXml.push('<condition attribute="statuscode" operator="eq" value="2" />');
        fetchXml.push('<condition attribute="description" operator="like" value="%' + filename + '%" />');
        fetchXml.push('<condition attribute="createdon" operator="gt" value="' + initialDate.toISOString().split('.')[0] + ' " />');
        fetchXml.push('</filter>');
        fetchXml.push('<order attribute="createdon" descending="true" />');
        fetchXml.push('</entity>');
        fetchXml.push('</fetch>');

        activity = irdpCommon.query.getFetchXmlResult(fetchXml, entityName);

        if (activity.value.length == 0) {
            return false;

        } else {
            return true;
        }

    },

    getPostCreateNotePluginSystemJob: function (fileName, fileCreationDate) {
        var systemJob = [];
        var initialDate = new Date(fileCreationDate);
        initialDate.setSeconds(initialDate.getSeconds() - 1);

        var fetchXml = [];
        var entityName = "asyncoperations";

        fetchXml.push('<fetch>');
        fetchXml.push('<entity name="asyncoperation" >');
        fetchXml.push('<attribute name="statuscode" />');
        fetchXml.push('<attribute name="correlationid" />');
        fetchXml.push('<attribute name="operationtype" />');
        fetchXml.push('<filter type="and" >');
        fetchXml.push('<condition attribute="createdon" operator="gt" value="' + initialDate.toISOString().split('.')[0] + ' " />');
        fetchXml.push('<condition attribute="name" operator="eq" value="NHSBT.IRDP.Plugins.NotePlugin: Post Create (Async)" />');
        fetchXml.push('</filter>');
        fetchXml.push('<order attribute="createdon" descending="true" />');
        fetchXml.push('<link-entity name="annotation" from="annotationid" to="regardingobjectid" >');
        fetchXml.push('<attribute name="filename" />');
        fetchXml.push('<filter>');
        fetchXml.push('<condition attribute="filename" operator="eq" value="' + fileName + '" />');
        fetchXml.push('</filter>');
        fetchXml.push('</link-entity>');
        fetchXml.push('</entity>');
        fetchXml.push('</fetch>');

        systemJob = irdpCommon.query.getFetchXmlResult(fetchXml, entityName);

        if (systemJob.value.length == 0) {

            //If the system job hasn't yet been created
            //then wait a bit and try again
            return false;

        } else {

            systemJob = systemJob['value'][0];

            systemJob = {
                "statuscode": systemJob["statuscode"],
                "correlationid": systemJob["correlationid"],
                "operationtype": systemJob["operationtype"],
                "filename": systemJob["annotation1.filename"],
            }
        }
        return systemJob;

    },

    getCorrelationSystemJob: function (correlationid, operationtype, filename) {
        var systemJobResults = [];
        var systemJob;

        var fetchXml = [];
        var entityName = "asyncoperations";

        fetchXml.push('<fetch>');
        fetchXml.push('<entity name="asyncoperation" >');
        fetchXml.push('<attribute name="statuscode" />');
        fetchXml.push('<attribute name="correlationid" />');
        fetchXml.push('<attribute name="operationtype" />');
        fetchXml.push('<filter>');
        fetchXml.push('<condition attribute="correlationid" operator="eq" value="' + correlationid + '" />');
        fetchXml.push('<condition attribute="operationtype" operator="eq" value="' + operationtype + '" />');
        fetchXml.push('<condition attribute="name" operator="begins-with" value="Rare Blood Source import - " />');
        fetchXml.push('</filter>');
        fetchXml.push('</entity>');
        fetchXml.push('</fetch>');

        systemJobResults = irdpCommon.query.getFetchXmlResult(fetchXml, entityName);

        if (systemJobResults.value.length == 0) {

            //If the system job hasn't yet been created
            //then wait a bit and try again
            return false;

        } else {


            //if the system job has been created
            systemJob = systemJobResults['value'][0];


            systemJob = {
                "statuscode": systemJob["statuscode"],
                "correlationid": systemJob["correlationid"],
                "operationtype": systemJob["operationtype"],
            }

            //the return it
            return systemJob;
        }

    },

    getFile: function (fileName, fileCreationDate) {
        var file;
        var initialDate = new Date(fileCreationDate);

        var fetchXml = [];

        var entityName = "importfiles"
        fetchXml.push('<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">');
        fetchXml.push('<entity name="importfile">');
        fetchXml.push('<attribute name="totalcount"/>');
        fetchXml.push('<attribute name="successcount"/>');
        fetchXml.push('<attribute name="failurecount" />');
        fetchXml.push('<attribute name="statuscode" />');
        fetchXml.push('<attribute name="partialfailurecount" />');
        fetchXml.push('<filter>');
        fetchXml.push('<condition attribute="createdon" operator="gt" value="' + initialDate.toISOString().split('.')[0] + ' " />');
        fetchXml.push('<condition attribute="source" operator="eq" value="' + fileName + '" />');
        fetchXml.push('</filter>');
        fetchXml.push('<order attribute="createdon" descending="true" />');
        fetchXml.push('</entity>');
        fetchXml.push('</fetch>');

        file = irdpCommon.query.getFetchXmlResult(fetchXml, entityName);

        if (file.value.length == 0) {

            file = null;
        } else {

            file = {
                "totalcount": file['value'][0]["totalcount"],
                "successcount": file['value'][0]["successcount"],
                "failurecount": file['value'][0]["failurecount"],
                "statuscode": file['value'][0]["statuscode"],
                "partialfailurecount": file['value'][0]["partialfailurecount"],
            }
        }

        return file;

    },

    showFailedMessageFileImport: function () {
        //show file import
        $("#newAnnotation").show();
        $('#FileProgress').css({ "background-color": "#FF0000", "width": "100%" });
        $('#FileRowParseProgress').css({ "background-color": "#FF0000", "width": "100%" });
        $('#MessageDescription span').text('File Rejected - Check the dashboard for details').css({ "color": "#FF0000" });
        $('#FileParsingRowTitle span').text('');
        $('#FileStepsTitle span').text('');
        $('#messageImage').attr('src', Xrm.Utility.getGlobalContext().getClientUrl() +'/WebResources/nhs_RedX');
        $('#MessageDescription img').show();

    },

    submitButton: {
        show: function () {
            if ($("input:file").val() != "") {
                $("input:submit").show();
            }
        },
        hide: function () {
            $("input:submit").hide();
        }
    },
    refresh: function () {
        //clear the existing annotations list
        $("div#existingAnnotations").empty()

        //then reinitialise the annotations pane
        annotationsControl.initialise();
    },
    fetchExisting: function (objectId) {

        //construct a web api query to get the annotations for the parent records
        var objectType = "annotation";
        var queryString = "$select=annotationid,createdon,filename,filesize&$orderby=createdon desc&$filter=_objectid_value eq " + objectId + " and  isdocument eq true";

        //execute it passing the parser as a callback function
        irdpCommon.query.getAllWebApiResults(queryString, objectType, annotationsControl.parseExistingResults);

    },
    parseExistingResults: function (results) {
        //callback function to put the existing annotations in the div
        //clear the existing annotations list
        $("div#existingAnnotations").empty()

        //loop through the returned annotations
        for (var i = 0; i < results.length; i++) {
            var result = results[i];
            //Insert a div for each previous attachments
            annotationsControl.insertExisting(result);

        }

    },
    downloadExisting: function (clickTarget) {
        //get the annotation id
        var annotationId = clickTarget.currentTarget.id

        //Initiate the download of the annotation id
        irdpCommon.query.getRecordByGuid(annotationId, "annotation", ["documentbody", "filename"], annotationsControl.parseDownloadResponse)

    },

    parseDownloadResponse: function (result) {

        //download the base64 file to the users device
        var content = atob(result.documentbody);
        var blob = new Blob([content], { type: "text/csv;charset=utf-8" });
        saveAs(blob, result.filename);

    },

    insertExisting: function (annotation) {
        annotationHtml = [];

        //parse the date in the users local time
        var createdOn = new Date(annotation.createdon.valueOf());

        //build the HTML
        annotationHtml.push('<div id="' + annotation.annotationid + '" class="existingAnnotation">');
        annotationHtml.push('   <div class="existingAnnotation_fileName">' + annotation.filename + '</div>');
        annotationHtml.push('   <div class="existingAnnotation_metadata">(' + annotation.filesize + ' bytes uploaded on ' + createdOn.toLocaleString() + ')</div>');
        annotationHtml.push('</div');

        annotationHtml = annotationHtml.join(" ");

        //then insert it into the DOM
        $("div#existingAnnotations").append(annotationHtml);

        //bind an event to the control to initiate the download of that file
        //i.e. annotationsControl.downloadExisting(annotationId)
        $("div#" + annotation.annotationid).on("click", annotationsControl.downloadExisting);
    },
    uploadNew: {
        clickSave: function () {

            var file = $(":file")[0].files[0];
            if (file) {
                //disable the controls
                $("input").prop("disabled", true);

                var subject = "Bulk Upload File";
                var desc = "Attached via the custom control on the institution (Account) form";
                var reader = new FileReader();
                reader.onload = function (evt) {
                    var str = annotationsControl.uploadNew._arrayBufferToBase64(reader.result);
                    annotationsControl.uploadNew._createNote(subject, desc, str, file.name, file.type);
                }
                reader.readAsArrayBuffer(file);
            }

            annotationsControl.resetProgressBars();

            //Show Progress Bar
            $('#ProgressBar').show();

            //hide file import
            $("#newAnnotation").hide();

            annotationsControl.showProgressBar(file.name, new Date());


        },

        _arrayBufferToBase64: function (buffer) { // Convert Array Buffer to Base 64 string
            var binary = '';
            var bytes = new Uint8Array(buffer);
            var len = bytes.byteLength;
            for (var i = 0; i < len; i++) {
                binary += String.fromCharCode(bytes[i]);
            }
            return window.btoa(binary);
        },

        _createNote: function (title, description, docBody, fName, mType) {
            var entity = {};
            entity["objectid_account@odata.bind"] = "/accounts(" + pageParams.id.replace("{", "").replace("}", "") + ")";
            entity.isdocument = true;
            entity.documentbody = docBody;
            entity.notetext = description;
            entity.filename = fName;
            entity.subject = title;

            var req = new XMLHttpRequest();
            req.open("POST", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/annotations", true);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 204) {
                        //Refresh the annotation pane to include this new file
                        annotationsControl.refresh();

                    } else {
                        Xrm.Utility.alertDialog(this.statusText);
                    }
                }
            };
            req.send(JSON.stringify(entity));
        }

    }

};

annotationsControl.checkAccess();