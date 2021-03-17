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
    [EntityLogicalNameAttribute("import")]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("XrmToolkit", "4.0")]
    [DataContract(Name = "Entity", Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts")]
    public partial class DataImport : BaseProxyClass
    {
        public new const string LogicalName = "import";
        public const int ObjectTypeCode = 4410;
        public const string PrimaryIdAttribute = "importid";
        public const string PrimaryNameAttribute = "name";
        
        static DataImport()
        {
            BaseProxyClass.RegisterProxyType(typeof(DataImport), "import");
            _textOptions = new Dictionary<string, eTextOptions>();
            _numberOptions = new Dictionary<string, eNumberOptions>();
            _errorStrings = new Dictionary<string, string>();
            TextError = "The value for attribute '{0}' cannot be longer than {3} characters. The length of the value is {2} characters.";
            NumberError = "The value for attribute '{0}' must be between {2} and {3}. The value is {1}";
        }
        public DataImport() : base(new Entity("import")) { }
        public DataImport(Entity original) : base(original) { }
        public static string GetLogicalName() { return BaseProxyClass.GetLogicalName<DataImport>(); }
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
///<para>Logical Name: modifiedbyname</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("modifiedbyname")]
public string ModifiedByName
{
	get { return this.GetPropertyValue<string>("modifiedbyname"); }
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
///<para>Logical Name: modifiedonbehalfbyyominame</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("modifiedonbehalfbyyominame")]
public string ModifiedOnBehalfByYomiName
{
	get { return this.GetPropertyValue<string>("modifiedonbehalfbyyominame"); }
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
///<para>Logical Name: modifiedbyyominame</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("modifiedbyyominame")]
public string ModifiedByYomiName
{
	get { return this.GetPropertyValue<string>("modifiedbyyominame"); }
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
///<para>Logical Name: createdbyyominame</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("createdbyyominame")]
public string CreatedByYomiName
{
	get { return this.GetPropertyValue<string>("createdbyyominame"); }
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
///<para>Logical Name: owneridyominame</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("owneridyominame")]
public string OwnerIdYomiName
{
	get { return this.GetPropertyValue<string>("owneridyominame"); }
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
///<para>Logical Name: emailaddress</para>
///<para>Max Length: 100 characters</para>
///</summary>
[AttributeLogicalNameAttribute("emailaddress")]
public string Email
{
	get { return this.GetPropertyValue<string>("emailaddress"); }
	set { this.SetPropertyValue("emailaddress", value, 100, "Email"); }
}
///<summary>
///<para>Key Property (Uniqueidentifier)</para>
///<para>Logical Name: importid</para>
///</summary>
[AttributeLogicalNameAttribute("importid")]
public Guid ImportId
{
	get
	{
		return base.Id;
	}
	set
	{
		base.Id = value;
		base.SetPropertyValue("importid", value, "ImportId");
	}
}
///<summary>
///<para>Logical Name: name</para>
///<para>Max Length: 256 characters</para>
///</summary>
[AttributeLogicalNameAttribute("name")]
public string ImportName
{
	get { return this.GetPropertyValue<string>("name"); }
	set { this.SetPropertyValue("name", value, 256, "ImportName"); }
}
///<summary>
///<para>Logical Name: isimport</para>
///<para>True = Is Import</para>
///<para>False = Is Migration</para>
///</summary>
[AttributeLogicalNameAttribute("isimport")]
public bool? IsImport
{
	get { return this.GetPropertyValue<bool?>("isimport"); }
	set { this.SetPropertyValue<bool?>("isimport", value, "IsImport"); }
}
/// <summary>
///Logical Name: modecode
/// </summary>
[AttributeLogicalNameAttribute("modecode")]
public eMode? Mode
{
	get
	{
		if (Mode_OptionSetValue != null) { return (eMode)Mode_OptionSetValue.Value; }
		return null;
	}
	set
	{
		if (value != null) { this.Mode_OptionSetValue = new OptionSetValue((int)value); }
		else this.Mode_OptionSetValue = null;
	}
}
///<summary>
///<para>Logical Name: modecode</para>
///</summary>
[AttributeLogicalNameAttribute("modecode")]
public OptionSetValue Mode_OptionSetValue
{
	get { return this.GetPropertyValue<OptionSetValue>("modecode"); }
	set { this.SetPropertyValue<OptionSetValue>("modecode", value, "Mode_OptionSetValue"); }
}
/// <summary>
/// Retrieves the current value's text in the user's language.
/// </summary>
/// <param name="Service">CRM Organization Service</param>
/// <returns></returns>
public string Mode_Text(IOrganizationService Service)
{
	return this.Mode_OptionSetValue.GetOptionSetText(Service, this, "modecode");
}
/// <summary>
/// Retrieves the current value's text in the user's language.
/// </summary>
/// <param name="AttributeMetadata">The attribute metadata previously retrieved using the 'GetAttributeMetadata' extension method on the IOrganizationService object.</param>
/// <returns></returns>
public string Mode_Text(EnumAttributeMetadata AttributeMetadata)
{
	return AttributeMetadata.GetOptionSetText(this.Mode_OptionSetValue.Value);
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
///<para>Logical Name: sendnotification</para>
///<para>True = Yes</para>
///<para>False = No</para>
///</summary>
[AttributeLogicalNameAttribute("sendnotification")]
public bool? SendNotification
{
	get { return this.GetPropertyValue<bool?>("sendnotification"); }
	set { this.SetPropertyValue<bool?>("sendnotification", value, "SendNotification"); }
}
///<summary>
///<para>Logical Name: sequence</para>
///<para>Minimum Value: -2147483648</para>
///<para>Maximum Value: 2147483647</para>
///</summary>
[AttributeLogicalNameAttribute("sequence")]
public int? Sequence
{
	get { return this.GetPropertyValue<int?>("sequence"); }
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
/// <para>Schema Name: Import_AsyncOperations</para>
/// </summary>
public List<Entity> GetSystemJobs (IOrganizationService Service, params string[] Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "asyncoperation", "regardingobjectid", Columns); }
/// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: Import_AsyncOperations</para>
/// </summary>
public List<Entity> GetSystemJobs (IOrganizationService Service, ColumnSet Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "asyncoperation", "regardingobjectid", Columns); }
/// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: Import_BulkDeleteFailures</para>
/// </summary>
public List<Entity> GetBulkDeleteFailures (IOrganizationService Service, params string[] Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "bulkdeletefailure", "regardingobjectid", Columns); }
/// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: Import_BulkDeleteFailures</para>
/// </summary>
public List<Entity> GetBulkDeleteFailures (IOrganizationService Service, ColumnSet Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "bulkdeletefailure", "regardingobjectid", Columns); }
/// <summary>
/// <para><b>ImportSourceFile (Import Job ID)</b></para>
/// <para>Schema Name: Import_ImportFile</para>
/// </summary>
public List<ImportSourceFile> GetImports_ImportJobID (IOrganizationService Service, params string[] Columns) { return BaseProxyClass.GetRelatedOneToManyEntities<ImportSourceFile>(Service, this.Id, "importfile", "importid", Columns); }
/// <summary>
/// <para><b>ImportSourceFile (Import Job ID)</b></para>
/// <para>Schema Name: Import_ImportFile</para>
/// </summary>
public List<ImportSourceFile> GetImports_ImportJobID (IOrganizationService Service, ColumnSet Columns) { return BaseProxyClass.GetRelatedOneToManyEntities<ImportSourceFile>(Service, this.Id, "importfile", "importid", Columns); }
/// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: userentityinstancedata_import</para>
/// </summary>
public List<Entity> GetUserEntityInstanceData (IOrganizationService Service, params string[] Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "userentityinstancedata", "objectid", Columns); }
/// <summary>
/// <para><b>Entity ()</b></para>
/// <para>Schema Name: userentityinstancedata_import</para>
/// </summary>
public List<Entity> GetUserEntityInstanceData (IOrganizationService Service, ColumnSet Columns) { return BaseProxyClass.GetRelatedOneToManyEntities(Service, this.Id, "userentityinstancedata", "objectid", Columns); }


        

        

        public enum eMode
{
	///<summary><para>Create</para>
	///<para>Value = 0</para></summary>
	[Description("Create")]
	Create = 0, 
	///<summary><para>Update</para>
	///<para>Value = 1</para></summary>
	[Description("Update")]
	Update = 1
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
	///<summary><para>Submitted</para>
	///<para>Value = 0</para></summary>
	[Description("Submitted")]
	Submitted_Active = 0, 
	///<summary><para>Parsing</para>
	///<para>Value = 1</para></summary>
	[Description("Parsing")]
	Parsing_Active = 1, 
	///<summary><para>Transforming</para>
	///<para>Value = 2</para></summary>
	[Description("Transforming")]
	Transforming_Active = 2, 
	///<summary><para>Importing</para>
	///<para>Value = 3</para></summary>
	[Description("Importing")]
	Importing_Active = 3, 
	///<summary><para>Completed</para>
	///<para>Value = 4</para></summary>
	[Description("Completed")]
	Completed_Active = 4, 
	///<summary><para>Failed</para>
	///<para>Value = 5</para></summary>
	[Description("Failed")]
	Failed_Active = 5
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
	/// <summary><para>modifiedbyname</para>
	/// <para>modifiedbyname</para></summary>
	public const string ModifiedByName = "modifiedbyname";
	/// <summary><para>owneridname</para>
	/// <para>owneridname</para></summary>
	public const string OwnerIdName = "owneridname";
	/// <summary><para>modifiedonbehalfbyyominame</para>
	/// <para>modifiedonbehalfbyyominame</para></summary>
	public const string ModifiedOnBehalfByYomiName = "modifiedonbehalfbyyominame";
	/// <summary><para>createdonbehalfbyname</para>
	/// <para>createdonbehalfbyname</para></summary>
	public const string CreatedOnBehalfByName = "createdonbehalfbyname";
	/// <summary><para>modifiedonbehalfbyname</para>
	/// <para>modifiedonbehalfbyname</para></summary>
	public const string ModifiedOnBehalfByName = "modifiedonbehalfbyname";
	/// <summary><para>createdbyname</para>
	/// <para>createdbyname</para></summary>
	public const string CreatedByName = "createdbyname";
	/// <summary><para>modifiedbyyominame</para>
	/// <para>modifiedbyyominame</para></summary>
	public const string ModifiedByYomiName = "modifiedbyyominame";
	/// <summary><para>createdonbehalfbyyominame</para>
	/// <para>createdonbehalfbyyominame</para></summary>
	public const string CreatedOnBehalfByYomiName = "createdonbehalfbyyominame";
	/// <summary><para>createdbyyominame</para>
	/// <para>createdbyyominame</para></summary>
	public const string CreatedByYomiName = "createdbyyominame";
	/// <summary><para>owneridtype</para>
	/// <para>owneridtype</para></summary>
	public const string OwnerIdType = "owneridtype";
	/// <summary><para>owneridyominame</para>
	/// <para>owneridyominame</para></summary>
	public const string OwnerIdYomiName = "owneridyominame";
	/// <summary><para>Created By</para>
	/// <para>createdby</para></summary>
	public const string CreatedBy = "createdby";
	/// <summary><para>Created By (Delegate)</para>
	/// <para>createdonbehalfby</para></summary>
	public const string CreatedBy_Delegate = "createdonbehalfby";
	/// <summary><para>Created On</para>
	/// <para>createdon</para></summary>
	public const string CreatedOn = "createdon";
	/// <summary><para>Email</para>
	/// <para>emailaddress</para></summary>
	public const string Email = "emailaddress";
	/// <summary><para>Import</para>
	/// <para>importid</para></summary>
	public const string ImportId = "importid";
	/// <summary><para>Import Name</para>
	/// <para>name</para></summary>
	public const string ImportName = "name";
	/// <summary><para>Is Import</para>
	/// <para>isimport</para></summary>
	public const string IsImport = "isimport";
	/// <summary><para>Mode</para>
	/// <para>modecode</para></summary>
	public const string Mode = "modecode";
	/// <summary><para>Modified By</para>
	/// <para>modifiedby</para></summary>
	public const string ModifiedBy = "modifiedby";
	/// <summary><para>Modified By (Delegate)</para>
	/// <para>modifiedonbehalfby</para></summary>
	public const string ModifiedBy_Delegate = "modifiedonbehalfby";
	/// <summary><para>Modified On</para>
	/// <para>modifiedon</para></summary>
	public const string ModifiedOn = "modifiedon";
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
	/// <summary><para>Send Notification</para>
	/// <para>sendnotification</para></summary>
	public const string SendNotification = "sendnotification";
	/// <summary><para>Sequence</para>
	/// <para>sequence</para></summary>
	public const string Sequence = "sequence";
	/// <summary><para>Status</para>
	/// <para>statecode</para></summary>
	public const string Status = "statecode";
	/// <summary><para>Status Reason</para>
	/// <para>statuscode</para></summary>
	public const string StatusReason = "statuscode";
}

    }
}
