// <auto-generated />

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Client;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace NHSBT.IRDP.Plugins.ProxyClasses
{
    [EntityLogicalNameAttribute("importlog")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XrmToolkit", "4.0")]
    [DataContract(Name = "Entity", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public partial class ImportLog : BaseProxyClass
    {
        public new const string LogicalName = "importlog";
        public const int ObjectTypeCode = 4423;
        public const string PrimaryIdAttribute = "importlogid";
        public const string PrimaryNameAttribute = "";
        
        static ImportLog()
        {
            BaseProxyClass.RegisterProxyType(typeof(ImportLog), "importlog");
            _textOptions = new Dictionary<string, eTextOptions>();
            _numberOptions = new Dictionary<string, eNumberOptions>();
            _errorStrings = new Dictionary<string, string>();
            TextError = "The value for attribute '{0}' cannot be longer than {3} characters. The length of the value is {2} characters.";
            NumberError = "The value for attribute '{0}' must be between {2} and {3}. The value is {1}";
        }
        public ImportLog() : base(new Entity("importlog")) { }
        public ImportLog(Entity original) : base(original) { }
        public static string GetLogicalName() { return BaseProxyClass.GetLogicalName<ImportLog>(); }
        /// <summary>
        /// Action to perform when the string value is greater than the allowed length.
        /// <para>This is the default for any string attribute in this Entity</para>
        /// </summary>
        public static eTextOptions TextOptions { get; set; }
        private static Dictionary<string, eTextOptions> _textOptions;
        /// <summary>
        /// Use this to set an action and error string when a value is greater than the allowed length
        /// </summary>
        /// <param name="logicalName">Name of Attribute</param>
        /// <param name="options">Action to perform when the value is greater than the allowed length</param>
        /// <param name="errorString">Optional: Error to throw if the eTextOptions == ThrowError
        /// <para>If nothing is specified then the 'TextError' string is used.</para>
        /// <para>You may use the following parameters:</para>
        /// <para>{0} = Attribute Logical Name</para>
        /// <para>{1} = Value</para>
        /// <para>{2} = Length</para>
        /// <para>{3} = Max Length</para>
        /// </param>
        public static void SetTextOptions(string logicalName, eTextOptions options, string errorString = null)
        {
            if (_textOptions.ContainsKey(logicalName)) { _textOptions[logicalName] = options; }
            else { _textOptions.Add(logicalName, options); }
            if (!string.IsNullOrEmpty(errorString))
            {
                if (_errorStrings.ContainsKey(logicalName)) { _errorStrings[logicalName] = errorString; }
                else { _errorStrings.Add(logicalName, errorString); }
            }
            else if (_errorStrings.ContainsKey(logicalName)) { _errorStrings.Remove(logicalName); }
        }
        protected override eTextOptions GetTextOptions(string logicalName)
        {
            if (_textOptions.ContainsKey(logicalName)) { return _textOptions[logicalName]; }
            return TextOptions;
        }
        /// <summary>
        /// Action to perform when the number value is greater or less than the allowed value.
        /// <para>This is the default for any int, decimal, double, or money attribute in this Entity</para>
        /// </summary>
        public static eNumberOptions NumberOptions { get; set; }
        private static Dictionary<string, eNumberOptions> _numberOptions;
        /// <summary>
        /// Use this to set an action and error string when a value is greater or less than the allowed value
        /// </summary>
        /// <param name="logicalName">Name of Attribute</param>
        /// <param name="options">Action to perform when the value is greater or less than the allowed value</param>
        /// <param name="errorString">Optional: Error to throw if the eNumberOptions == ThrowError
        /// <para>If nothing is specified then the 'NumberError' string is used.</para>
        /// <para>You may use the following parameters:</para>
        /// <para>{0} = Attribute Logical Name</para>
        /// <para>{1} = Value</para>
        /// <para>{2} = Min Value</para>
        /// <para>{3} = Max Value</para>
        /// </param>
        public static void SetNumberOptions(string logicalName, eNumberOptions options, string errorString = null)
        {
            if (_numberOptions.ContainsKey(logicalName)) { _numberOptions[logicalName] = options; }
            else { _numberOptions.Add(logicalName, options); }
            if (!string.IsNullOrEmpty(errorString))
            {
                if (_errorStrings.ContainsKey(logicalName)) { _errorStrings[logicalName] = errorString; }
                else { _errorStrings.Add(logicalName, errorString); }
            }
            else if (_errorStrings.ContainsKey(logicalName)) { _errorStrings.Remove(logicalName); }
        }
        protected override eNumberOptions GetNumberOptions(string logicalName)
        {
            if (_numberOptions.ContainsKey(logicalName)) { return _numberOptions[logicalName]; }
            return NumberOptions;
        }
        private static Dictionary<string, string> _errorStrings;
        protected override string GetErrorString(string attributeName, BaseProxyClass.eErrorType defaultErrorType)
        {
            if (_errorStrings.ContainsKey(attributeName))
            {
                return _errorStrings[attributeName];
            }
            return defaultErrorType == BaseProxyClass.eErrorType.Text ? TextError : NumberError;
        }
        /// <summary>
        /// <para>Default error string is: The value for attribute '{0}' cannot be longer than {3} characters. The length of the value is {2} characters.</para>
        /// <para>You may use the following parameters</para>
        /// <para>{0} = Attribute Name</para>
        /// <para>{1} = Value</para>
        /// <para>{2} = Length</para>
        /// <para>{3} = Max Length</para>
        /// </summary>
        public static string TextError { get; set; }
        /// <summary>
        /// <para>Default error string is: The value for attribute '{0}' must be between {2} and {3}. The value is {1}.</para>
        /// <para>You may use the following parameters</para>
        /// <para>{0} = Attribute Name</para>
        /// <para>{1} = Value</para>
        /// <para>{2} = Min Value</para>
        /// <para>{3} = Max Value</para>
        /// </summary>
        public static string NumberError { get; set; }

        ///<summary>
///<para>Logical Name: importfileidname</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("importfileidname")]
public string ImportFileIdName
{
	get { return this.GetPropertyValue<string>("importfileidname"); }
}
///<summary>
///<para>Logical Name: createdonbehalfbyyominame</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("createdonbehalfbyyominame")]
public string CreatedOnBehalfByYomiName
{
	get { return this.GetPropertyValue<string>("createdonbehalfbyyominame"); }
}
///<summary>
///<para>Logical Name: modifiedbyname</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("modifiedbyname")]
public string ModifiedByName
{
	get { return this.GetPropertyValue<string>("modifiedbyname"); }
}
///<summary>
///<para>Logical Name: modifiedonbehalfbyname</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("modifiedonbehalfbyname")]
public string ModifiedOnBehalfByName
{
	get { return this.GetPropertyValue<string>("modifiedonbehalfbyname"); }
}
///<summary>
///<para>Logical Name: createdbyname</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("createdbyname")]
public string CreatedByName
{
	get { return this.GetPropertyValue<string>("createdbyname"); }
}
///<summary>
///<para>Logical Name: owneridtype</para>
///</summary>
[AttributeLogicalNameAttribute("owneridtype")]
public string OwnerIdType
{
	get { return this.GetPropertyValue<string>("owneridtype"); }
	set { this.SetPropertyValue<string>("owneridtype", value, "OwnerIdType"); }
}
///<summary>
///<para>Logical Name: modifiedonbehalfbyyominame</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("modifiedonbehalfbyyominame")]
public string ModifiedOnBehalfByYomiName
{
	get { return this.GetPropertyValue<string>("modifiedonbehalfbyyominame"); }
}
///<summary>
///<para>Logical Name: modifiedbyyominame</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("modifiedbyyominame")]
public string ModifiedByYomiName
{
	get { return this.GetPropertyValue<string>("modifiedbyyominame"); }
}
///<summary>
///<para>Logical Name: createdonbehalfbyname</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("createdonbehalfbyname")]
public string CreatedOnBehalfByName
{
	get { return this.GetPropertyValue<string>("createdonbehalfbyname"); }
}
///<summary>
///<para>Logical Name: owneridname</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("owneridname")]
public string OwnerIdName
{
	get { return this.GetPropertyValue<string>("owneridname"); }
}
///<summary>
///<para>Logical Name: importdataidname</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("importdataidname")]
public string ImportDataIdName
{
	get { return this.GetPropertyValue<string>("importdataidname"); }
}
///<summary>
///<para>Key Property (Uniqueidentifier)</para>
///<para>Logical Name: importlogid</para>
///</summary>
[AttributeLogicalNameAttribute("importlogid")]
public Guid ImportLogId
{
	get
	{
		return base.Id;
	}
	set
	{
		base.Id = value;
		base.SetPropertyValue("importlogid", value, "ImportLogId");
	}
}
///<summary>
///<para>Logical Name: createdbyyominame</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("createdbyyominame")]
public string CreatedByYomiName
{
	get { return this.GetPropertyValue<string>("createdbyyominame"); }
}
///<summary>
///<para>Logical Name: owneridyominame</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("owneridyominame")]
public string OwnerIdYomiName
{
	get { return this.GetPropertyValue<string>("owneridyominame"); }
}
///<summary>
///<para>Logical Name: headercolumn</para>
///<para>Max Length: 1073741823 characters</para>
///</summary>
[AttributeLogicalNameAttribute("headercolumn")]
public string ColumnHeading
{
	get { return this.GetPropertyValue<string>("headercolumn"); }
	set { this.SetPropertyValue("headercolumn", value, 1073741823, "ColumnHeading"); }
}
///<summary>
///<para>Logical Name: columnvalue</para>
///<para>Max Length: 160 characters</para>
///</summary>
[AttributeLogicalNameAttribute("columnvalue")]
public string ColumnValue
{
	get { return this.GetPropertyValue<string>("columnvalue"); }
	set { this.SetPropertyValue("columnvalue", value, 160, "ColumnValue"); }
}
///<summary>
///<para>Logical Name: createdby</para>
///</summary>
[AttributeLogicalNameAttribute("createdby")]
public EntityReference CreatedBy
{
	get { return this.GetPropertyValue<EntityReference>("createdby"); }
}
///<summary>
///<para>Logical Name: createdonbehalfby</para>
///</summary>
[AttributeLogicalNameAttribute("createdonbehalfby")]
public EntityReference CreatedBy_Delegate
{
	get { return this.GetPropertyValue<EntityReference>("createdonbehalfby"); }
}
///<summary>
///<para>Logical Name: createdon</para>
///</summary>
[AttributeLogicalNameAttribute("createdon")]
public DateTime? CreatedOn
{
	get { return this.GetPropertyValue<DateTime?>("createdon"); }
}
///<summary>
///<para>Logical Name: errordescription</para>
///<para>Max Length: 512 characters</para>
///</summary>
[AttributeLogicalNameAttribute("errordescription")]
public string Description
{
	get { return this.GetPropertyValue<string>("errordescription"); }
	set { this.SetPropertyValue("errordescription", value, 512, "Description"); }
}
///<summary>
///<para>Logical Name: errornumber</para>
///<para>Minimum Value: 0</para>
///<para>Maximum Value: 10000000</para>
///</summary>
[AttributeLogicalNameAttribute("errornumber")]
public int? ErrorCode
{
	get { return this.GetPropertyValue<int?>("errornumber"); }
	set { this.SetPropertyValue("errornumber", (int?)value, (int)0, (int)10000000, "ErrorCode"); }
}
///<summary>
///<para>Logical Name: importfileid</para>
///</summary>
[AttributeLogicalNameAttribute("importfileid")]
public EntityReference ImportFileId
{
	get { return this.GetPropertyValue<EntityReference>("importfileid"); }
	set { this.SetPropertyValue<EntityReference>("importfileid", value, "ImportFileId"); }
}
/// <summary>
///Logical Name: logphasecode
/// </summary>
[AttributeLogicalNameAttribute("logphasecode")]
public eLogPhase? LogPhase
{
	get
	{
		if (LogPhase_OptionSetValue != null) { return (eLogPhase)LogPhase_OptionSetValue.Value; }
		return null;
	}
	set
	{
		if (value != null) { this.LogPhase_OptionSetValue = new OptionSetValue((int)value); }
		else this.LogPhase_OptionSetValue = null;
	}
}
///<summary>
///<para>Logical Name: logphasecode</para>
///</summary>
[AttributeLogicalNameAttribute("logphasecode")]
public OptionSetValue LogPhase_OptionSetValue
{
	get { return this.GetPropertyValue<OptionSetValue>("logphasecode"); }
	set { this.SetPropertyValue<OptionSetValue>("logphasecode", value, "LogPhase_OptionSetValue"); }
}
/// <summary>
/// Retrieves the current value's text in the user's language.
/// </summary>
/// <param name="Service">CRM Organization Service</param>
/// <returns></returns>
public string LogPhase_Text(IOrganizationService Service)
{
	return this.LogPhase_OptionSetValue.GetOptionSetText(Service, this, "logphasecode");
}
/// <summary>
/// Retrieves the current value's text in the user's language.
/// </summary>
/// <param name="AttributeMetadata">The attribute metadata previously retrieved using the 'GetAttributeMetadata' extension method on the IOrganizationService object.</param>
/// <returns></returns>
public string LogPhase_Text(EnumAttributeMetadata AttributeMetadata)
{
	return AttributeMetadata.GetOptionSetText(this.LogPhase_OptionSetValue.Value);
}
///<summary>
///<para>Logical Name: modifiedby</para>
///</summary>
[AttributeLogicalNameAttribute("modifiedby")]
public EntityReference ModifiedBy
{
	get { return this.GetPropertyValue<EntityReference>("modifiedby"); }
}
///<summary>
///<para>Logical Name: modifiedonbehalfby</para>
///</summary>
[AttributeLogicalNameAttribute("modifiedonbehalfby")]
public EntityReference ModifiedBy_Delegate
{
	get { return this.GetPropertyValue<EntityReference>("modifiedonbehalfby"); }
}
///<summary>
///<para>Logical Name: modifiedon</para>
///</summary>
[AttributeLogicalNameAttribute("modifiedon")]
public DateTime? ModifiedOn
{
	get { return this.GetPropertyValue<DateTime?>("modifiedon"); }
}
///<summary>
///<para>Logical Name: additionalinfo</para>
///<para>Max Length: 5000 characters</para>
///</summary>
[AttributeLogicalNameAttribute("additionalinfo")]
public string MoreInformation
{
	get { return this.GetPropertyValue<string>("additionalinfo"); }
	set { this.SetPropertyValue("additionalinfo", value, 5000, "MoreInformation"); }
}
///<summary>
///<para>Logical Name: linenumber</para>
///<para>Minimum Value: 0</para>
///<para>Maximum Value: 10000000</para>
///</summary>
[AttributeLogicalNameAttribute("linenumber")]
public int? OriginalRowNumber
{
	get { return this.GetPropertyValue<int?>("linenumber"); }
	set { this.SetPropertyValue("linenumber", (int?)value, (int)0, (int)10000000, "OriginalRowNumber"); }
}
///<summary>
///<para>Logical Name: ownerid</para>
///</summary>
[AttributeLogicalNameAttribute("ownerid")]
public EntityReference Owner
{
	get { return this.GetPropertyValue<EntityReference>("ownerid"); }
	set { this.SetPropertyValue<EntityReference>("ownerid", value, "Owner"); }
}
///<summary>
///<para>Logical Name: owningbusinessunit</para>
///</summary>
[AttributeLogicalNameAttribute("owningbusinessunit")]
public EntityReference OwningBusinessUnit
{
	get { return this.GetPropertyValue<EntityReference>("owningbusinessunit"); }
}
///<summary>
///<para>Logical Name: owningteam</para>
///</summary>
[AttributeLogicalNameAttribute("owningteam")]
public EntityReference OwningTeam
{
	get { return this.GetPropertyValue<EntityReference>("owningteam"); }
}
///<summary>
///<para>Logical Name: owninguser</para>
///</summary>
[AttributeLogicalNameAttribute("owninguser")]
public EntityReference OwningUser
{
	get { return this.GetPropertyValue<EntityReference>("owninguser"); }
}
///<summary>
///<para>Logical Name: sequencenumber</para>
///<para>Minimum Value: 0</para>
///<para>Maximum Value: 10000000</para>
///</summary>
[AttributeLogicalNameAttribute("sequencenumber")]
public int? SequenceNumber
{
	get { return this.GetPropertyValue<int?>("sequencenumber"); }
}
///<summary>
///<para>Logical Name: importdataid</para>
///</summary>
[AttributeLogicalNameAttribute("importdataid")]
public EntityReference SourceRow
{
	get { return this.GetPropertyValue<EntityReference>("importdataid"); }
	set { this.SetPropertyValue<EntityReference>("importdataid", value, "SourceRow"); }
}
/// <summary>
///Logical Name: statecode
/// </summary>
[AttributeLogicalNameAttribute("statecode")]
public eStatus? Status
{
	get
	{
		if (Status_OptionSetValue != null) { return (eStatus)Status_OptionSetValue.Value; }
		return null;
	}
}
///<summary>
///<para>Logical Name: statecode</para>
///</summary>
[AttributeLogicalNameAttribute("statecode")]
public OptionSetValue Status_OptionSetValue
{
	get { return this.GetPropertyValue<OptionSetValue>("statecode"); }
}
/// <summary>
/// Retrieves the current value's text in the user's language.
/// </summary>
/// <param name="Service">CRM Organization Service</param>
/// <returns></returns>
public string Status_Text(IOrganizationService Service)
{
	return this.Status_OptionSetValue.GetOptionSetText(Service, this, "statecode");
}
/// <summary>
/// Retrieves the current value's text in the user's language.
/// </summary>
/// <param name="AttributeMetadata">The attribute metadata previously retrieved using the 'GetAttributeMetadata' extension method on the IOrganizationService object.</param>
/// <returns></returns>
public string Status_Text(EnumAttributeMetadata AttributeMetadata)
{
	return AttributeMetadata.GetOptionSetText(this.Status_OptionSetValue.Value);
}
/// <summary>
///Logical Name: statuscode
/// </summary>
[AttributeLogicalNameAttribute("statuscode")]
public eStatusReason? StatusReason
{
	get
	{
		if (StatusReason_OptionSetValue != null) { return (eStatusReason)StatusReason_OptionSetValue.Value; }
		return null;
	}
	set
	{
		if (value != null) { this.StatusReason_OptionSetValue = new OptionSetValue((int)value); }
		else this.StatusReason_OptionSetValue = null;
	}
}
///<summary>
///<para>Logical Name: statuscode</para>
///</summary>
[AttributeLogicalNameAttribute("statuscode")]
public OptionSetValue StatusReason_OptionSetValue
{
	get { return this.GetPropertyValue<OptionSetValue>("statuscode"); }
	set { this.SetPropertyValue<OptionSetValue>("statuscode", value, "StatusReason_OptionSetValue"); }
}
/// <summary>
/// Retrieves the current value's text in the user's language.
/// </summary>
/// <param name="Service">CRM Organization Service</param>
/// <returns></returns>
public string StatusReason_Text(IOrganizationService Service)
{
	return this.StatusReason_OptionSetValue.GetOptionSetText(Service, this, "statuscode");
}
/// <summary>
/// Retrieves the current value's text in the user's language.
/// </summary>
/// <param name="AttributeMetadata">The attribute metadata previously retrieved using the 'GetAttributeMetadata' extension method on the IOrganizationService object.</param>
/// <returns></returns>
public string StatusReason_Text(EnumAttributeMetadata AttributeMetadata)
{
	return AttributeMetadata.GetOptionSetText(this.StatusReason_OptionSetValue.Value);
}


        /// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: ImportLog_AsyncOperations</para>
/// </summary>
public List<Entity> GetSystemJobs (IOrganizationService Service, params string[] Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "asyncoperation", "regardingobjectid", Columns); }
/// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: ImportLog_AsyncOperations</para>
/// </summary>
public List<Entity> GetSystemJobs (IOrganizationService Service, ColumnSet Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "asyncoperation", "regardingobjectid", Columns); }
/// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: ImportLog_BulkDeleteFailures</para>
/// </summary>
public List<Entity> GetBulkDeleteFailures (IOrganizationService Service, params string[] Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "bulkdeletefailure", "regardingobjectid", Columns); }
/// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: ImportLog_BulkDeleteFailures</para>
/// </summary>
public List<Entity> GetBulkDeleteFailures (IOrganizationService Service, ColumnSet Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "bulkdeletefailure", "regardingobjectid", Columns); }
/// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: userentityinstancedata_importlog</para>
/// </summary>
public List<Entity> GetUserEntityInstanceData (IOrganizationService Service, params string[] Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "userentityinstancedata", "objectid", Columns); }
/// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: userentityinstancedata_importlog</para>
/// </summary>
public List<Entity> GetUserEntityInstanceData (IOrganizationService Service, ColumnSet Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "userentityinstancedata", "objectid", Columns); }


        

        

        public enum eLogPhase
{
	///<summary><para>Parse</para>
	///<para>Value = 0</para></summary>
	[Description("Parse")]
	Parse = 0, 
	///<summary><para>Transform</para>
	///<para>Value = 1</para></summary>
	[Description("Transform")]
	Transform = 1, 
	///<summary><para>Import Create</para>
	///<para>Value = 2</para></summary>
	[Description("Import Create")]
	ImportCreate = 2, 
	///<summary><para>Import Update</para>
	///<para>Value = 3</para></summary>
	[Description("Import Update")]
	ImportUpdate = 3
}
public enum eStatus
{
	///<summary><para>Active</para>
	///<para>Value = 0</para></summary>
	[Description("Active")]
	Active = 0
}
public enum eStatusReason
{
	///<summary><para>Active</para>
	///<para>Value = 0</para></summary>
	[Description("Active")]
	Active_Active = 0
}
public void SetState(IOrganizationService Service, eStatus State, eStatusReason Status)
{
	Service.SetState(this, (int)State, (int)Status);
}
public async System.Threading.Tasks.Task SetStateAsync(IOrganizationService Service, eStatus State, eStatusReason Status)
{
	 await Service.SetStateAsync(this, (int)State, (int)Status);
}


        public static class Properties
{
	/// <summary><para>importfileidname</para>
	/// <para>importfileidname</para></summary>
	public const string ImportFileIdName = "importfileidname";
	/// <summary><para>createdonbehalfbyyominame</para>
	/// <para>createdonbehalfbyyominame</para></summary>
	public const string CreatedOnBehalfByYomiName = "createdonbehalfbyyominame";
	/// <summary><para>modifiedbyname</para>
	/// <para>modifiedbyname</para></summary>
	public const string ModifiedByName = "modifiedbyname";
	/// <summary><para>modifiedonbehalfbyname</para>
	/// <para>modifiedonbehalfbyname</para></summary>
	public const string ModifiedOnBehalfByName = "modifiedonbehalfbyname";
	/// <summary><para>createdbyname</para>
	/// <para>createdbyname</para></summary>
	public const string CreatedByName = "createdbyname";
	/// <summary><para>owneridtype</para>
	/// <para>owneridtype</para></summary>
	public const string OwnerIdType = "owneridtype";
	/// <summary><para>modifiedonbehalfbyyominame</para>
	/// <para>modifiedonbehalfbyyominame</para></summary>
	public const string ModifiedOnBehalfByYomiName = "modifiedonbehalfbyyominame";
	/// <summary><para>modifiedbyyominame</para>
	/// <para>modifiedbyyominame</para></summary>
	public const string ModifiedByYomiName = "modifiedbyyominame";
	/// <summary><para>createdonbehalfbyname</para>
	/// <para>createdonbehalfbyname</para></summary>
	public const string CreatedOnBehalfByName = "createdonbehalfbyname";
	/// <summary><para>owneridname</para>
	/// <para>owneridname</para></summary>
	public const string OwnerIdName = "owneridname";
	/// <summary><para>importdataidname</para>
	/// <para>importdataidname</para></summary>
	public const string ImportDataIdName = "importdataidname";
	/// <summary><para>importlogid</para>
	/// <para>importlogid</para></summary>
	public const string ImportLogId = "importlogid";
	/// <summary><para>createdbyyominame</para>
	/// <para>createdbyyominame</para></summary>
	public const string CreatedByYomiName = "createdbyyominame";
	/// <summary><para>owneridyominame</para>
	/// <para>owneridyominame</para></summary>
	public const string OwnerIdYomiName = "owneridyominame";
	/// <summary><para>Column Heading</para>
	/// <para>headercolumn</para></summary>
	public const string ColumnHeading = "headercolumn";
	/// <summary><para>Column Value</para>
	/// <para>columnvalue</para></summary>
	public const string ColumnValue = "columnvalue";
	/// <summary><para>Created By</para>
	/// <para>createdby</para></summary>
	public const string CreatedBy = "createdby";
	/// <summary><para>Created By (Delegate)</para>
	/// <para>createdonbehalfby</para></summary>
	public const string CreatedBy_Delegate = "createdonbehalfby";
	/// <summary><para>Created On</para>
	/// <para>createdon</para></summary>
	public const string CreatedOn = "createdon";
	/// <summary><para>Description</para>
	/// <para>errordescription</para></summary>
	public const string Description = "errordescription";
	/// <summary><para>Error Code</para>
	/// <para>errornumber</para></summary>
	public const string ErrorCode = "errornumber";
	/// <summary><para>Import File Id</para>
	/// <para>importfileid</para></summary>
	public const string ImportFileId = "importfileid";
	/// <summary><para>Log Phase</para>
	/// <para>logphasecode</para></summary>
	public const string LogPhase = "logphasecode";
	/// <summary><para>Modified By</para>
	/// <para>modifiedby</para></summary>
	public const string ModifiedBy = "modifiedby";
	/// <summary><para>Modified By (Delegate)</para>
	/// <para>modifiedonbehalfby</para></summary>
	public const string ModifiedBy_Delegate = "modifiedonbehalfby";
	/// <summary><para>Modified On</para>
	/// <para>modifiedon</para></summary>
	public const string ModifiedOn = "modifiedon";
	/// <summary><para>More Information</para>
	/// <para>additionalinfo</para></summary>
	public const string MoreInformation = "additionalinfo";
	/// <summary><para>Original Row Number</para>
	/// <para>linenumber</para></summary>
	public const string OriginalRowNumber = "linenumber";
	/// <summary><para>Owner</para>
	/// <para>ownerid</para></summary>
	public const string Owner = "ownerid";
	/// <summary><para>Owning Business Unit</para>
	/// <para>owningbusinessunit</para></summary>
	public const string OwningBusinessUnit = "owningbusinessunit";
	/// <summary><para>Owning Team</para>
	/// <para>owningteam</para></summary>
	public const string OwningTeam = "owningteam";
	/// <summary><para>Owning User</para>
	/// <para>owninguser</para></summary>
	public const string OwningUser = "owninguser";
	/// <summary><para>Sequence Number</para>
	/// <para>sequencenumber</para></summary>
	public const string SequenceNumber = "sequencenumber";
	/// <summary><para>Source Row</para>
	/// <para>importdataid</para></summary>
	public const string SourceRow = "importdataid";
	/// <summary><para>Status</para>
	/// <para>statecode</para></summary>
	public const string Status = "statecode";
	/// <summary><para>Status Reason</para>
	/// <para>statuscode</para></summary>
	public const string StatusReason = "statuscode";
}

    }
}