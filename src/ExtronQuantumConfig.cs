using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using System.Collections.Generic;

namespace epi.switcher.extron.quantum
{
    /// <summary>
    /// Plugin device configuration object
    /// </summary>
    /// <remarks>
    /// Rename the class to match the device plugin being created
    /// </remarks>
    /// <example>
    /// "EssentialsPluginConfigObjectTemplate" renamed to "SamsungMdcConfig"
    /// </example>
    [ConfigSnippet("\"properties\":{\"control\":{}")]
    public class ExtronQuantumConfig
    {

        [JsonProperty("control")]
        public EssentialsControlPropertiesConfig Control { get; set; }

        /// <summary>
        /// Serializes the poll time value
        /// </summary>
        /// <remarks>
        /// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
        /// </remarks>
        /// <value>
        /// PollTimeMs property gets/sets the value as a long
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"polltimeMs": 30000
        /// }
        /// </code>
        /// </example>
        [JsonProperty("pollTimeMs")]
        public long PollTimeMs { get; set; }

        /// <summary>
        /// Serializes the warning timeout value
        /// </summary>
        /// <remarks>
        /// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
        /// </remarks>
        /// <value>
        /// WarningTimeoutMs property gets/sets the value as a long
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"warningTimeoutMs": 180000
        /// }
        /// </code>
        /// </example>
        [JsonProperty("warningTimeoutMs")]
        public long WarningTimeoutMs { get; set; }

        /// <summary>
        /// Serializes the error timeout value
        /// </summary>
        /// /// <remarks>
        /// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
        /// </remarks>
        /// <value>
        /// ErrorTimeoutMs property gets/sets the value as a long
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"errorTimeoutMs": 300000
        /// }
        /// </code>
        /// </example>
        [JsonProperty("errorTimeoutMs")]
        public long ErrorTimeoutMs { get; set; }

        /// <summary>
        /// Example dictionary of objects
        /// </summary>
        /// <remarks>
        /// This is an example collection configuration object.  This should be modified or deleted as needed for the plugin being built.
        /// </remarks>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"presets": {
        ///			"preset1": {
        ///				"enabled": true,
        ///				"name": "Preset 1"
        ///			}
        ///		}
        /// }
        /// </code>
        /// </example>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"inputNames": {
        ///			"input1": "Input 1",
        ///			"input2": "Input 2"		
        ///		}
        /// }
        /// </code>
        /// </example>
        [JsonProperty("inputs")]
        public Dictionary<string, NameValue> Inputs { get; set; }

        [JsonProperty("windows")]
        public Dictionary<string, NameValue> Windows { get; set; }

        [JsonProperty("presets")]
        public Dictionary<string, string> Presets { get; set; }


        /// <summary>
        /// Constuctor
        /// </summary>
        /// <remarks>
        /// If using a collection you must instantiate the collection in the constructor
        /// to avoid exceptions when reading the configuration file 
        /// </remarks>
        public ExtronQuantumConfig()
        {
            Inputs = new Dictionary<string, NameValue>();
            Windows = new Dictionary<string, NameValue>();
            Presets = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Example plugin configuration dictionary object
    /// </summary>
    /// <remarks>
    /// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
    /// </remarks>
    /// <example>
    /// <code>
    /// "properties": {
    ///		"dictionary": {
    ///			"item1": {
    ///				"name": "Item 1 Name",
    ///				"value": "Item 1 Value"
    ///			}
    ///		}
    /// }
    /// </code>
    /// </example>
    public class NameValue
    {
        /// <summary>
        /// Serializes collection name property
        /// </summary>
        /// <remarks>
        /// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
        /// </remarks>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Serializes collection value property
        /// </summary>
        /// <remarks>
        /// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
        /// </remarks>
        [JsonProperty("value")]
        public uint Value { get; set; }
    }
}