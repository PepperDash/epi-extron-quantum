using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace EssentialsPluginTemplate
{
	/// <summary>
	/// Plugin device factory for devices that use IBasicCommunication
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed
	/// </remarks>
	/// <example>
	/// "EssentialsPluginFactoryTemplate" renamed to "MyDeviceFactory"
	/// </example>
    public class ExtronQuantumFactory : EssentialsPluginDeviceFactory<ExtronQuantumDevice>
    {
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		/// <remarks>
		/// Update the MinimumEssentialsFrameworkVersion & TypeNames as needed when creating a plugin
		/// </remarks>
		/// <example>
 		/// Set the minimum Essentials Framework Version
		/// <code>
		/// MinimumEssentialsFrameworkVersion = "1.6.4;
        /// </code>
		/// In the constructor we initialize the list with the typenames that will build an instance of this device
        /// <code>
		/// TypeNames = new List<string>() { "SamsungMdc", "SamsungMdcDisplay" };
        /// </code>
		/// </example>
        public ExtronQuantumFactory()
        {
            // Set the minimum Essentials Framework Version
			// TODO [ ] Update the Essentials minimum framework version which this plugin has been tested against
			MinimumEssentialsFrameworkVersion = "1.13.3";

            // In the constructor we initialize the list with the typenames that will build an instance of this device
			// TODO [ ] Update the TypeNames for the plugin being developed
            TypeNames = new List<string>() { "extronQuantum" };
        }
        
		/// <summary>
		/// Builds and returns an instance of EssentialsPluginDeviceTemplate
		/// </summary>
		/// <param name="dc">device configuration</param>
		/// <returns>plugin device or null</returns>
		/// <remarks>		
		/// The example provided below takes the device key, name, properties config and the comms device created.
		/// Modify the EssetnialsPlugingDeviceTemplate constructor as needed to meet the requirements of the plugin device.
		/// </remarks>
		/// <seealso cref="PepperDash.Core.eControlMethod"/>
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            Debug.Console(1, $"[{dc.Key}] Factory Attempting to create new device from type: ${dc.Type}");
            
            var propertiesConfig = dc.Properties.ToObject<ExtronQuantumConfig>();
            if (propertiesConfig == null)
            {
                Debug.Console(0, $"[{dc.Key}] Factory: failed to read properties config for ${dc.Name}");
                return null;
            }            
                                    
            var comms = CommFactory.CreateCommForDevice(dc);
            if (comms == null)
            {
                Debug.Console(1, $"[{dc.Key}] Factory Notice: No control object present for device {dc.Name}");
                return null;
            }
            else
            {
                return new ExtronQuantumDevice(dc.Key, dc.Name, propertiesConfig, comms);
            }
        }

    }
}

          