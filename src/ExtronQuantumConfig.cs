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

        [JsonProperty("pollTimeMs")]
        public long PollTimeMs { get; set; }

        [JsonProperty("warningTimeoutMs")]
        public long WarningTimeoutMs { get; set; }

        [JsonProperty("errorTimeoutMs")]
        public long ErrorTimeoutMs { get; set; }

        [JsonProperty("staticCanvas")]
        public int  StaticCanvas { get; set; }
        
        [JsonProperty("inputs")]
        public Dictionary<string, NameValue> Inputs { get; set; }

        [JsonProperty("windows")]
        public Dictionary<string, WindowData> Windows { get; set; }

        [JsonProperty("presets")]
        public Dictionary<string, PresetData> Presets { get; set; }

        [JsonProperty("deviceSerialNumber")]
        public string DeviceSerialNumber { get; set; }


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
            Windows = new Dictionary<string, WindowData>();
            Presets = new Dictionary<string, PresetData>();
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

    public class WindowData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("canvas")]
        public uint Canvas { get; set; }

        [JsonProperty("window")]
        public uint Window { get; set; }

        [JsonProperty("windowIndex")]
        public uint WindowIndex { get; set; }
    }

    public class PresetData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("canvas")]
        public int Canvas { get; set; }
        [JsonProperty("canvasPresetNumber")]
        public int CanvasPresetNumber { get; set; }

        [JsonProperty("presetIndex")]
        public int PresetIndex { get; set; }

    }
}