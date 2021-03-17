
//get the key of the Help Page URL
//(Stored in the HTML Head Title element)
var helpKey = document.title;

//Get the help page URL and navigate to it
irdpCommon.query.getConfigEntityValue(helpKey, navigateToUrl);

function navigateToUrl(results) {

    var knowledgeArticleGuid = results[0].nhs_value

    var req = new XMLHttpRequest();
    req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v9.1/knowledgearticles(" + knowledgeArticleGuid + ")?$select=content", true);
    req.setRequestHeader("OData-MaxVersion", "4.0");
    req.setRequestHeader("OData-Version", "4.0");
    req.setRequestHeader("Accept", "application/json");
    req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
    req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
    req.onreadystatechange = function () {
        if (this.readyState === 4) {
            req.onreadystatechange = null;
            if (this.status === 200) {
                var result = JSON.parse(this.response);
                var content = result["content"];
                document.body.innerHTML = content;
            } else {
                Xrm.Utility.alertDialog(this.statusText);
            }
        }
    };
    req.send();

}