using PepperDash.Essentials.Core;

namespace epi.switcher.extron.quantum
{
    /// <summary>
    /// Plugin device Bridge Join Map
    /// </summary>
    /// <remarks>
    /// Rename the class to match the device plugin being developed.  Reference Essentials JoinMaps, if one exists for the device plugin being developed
    /// </remarks>
    /// <see cref="PepperDash.Essentials.Core.Bridges"/>
    /// <example>
    /// "EssentialsPluginBridgeJoinMapTemplate" renamed to "SamsungMdcBridgeJoinMap"
    /// </example>
    public class ExtronQuantumJoinMap : JoinMapBaseAdvanced
    {
        #region Digital


        [JoinName("IsOnline")]
        public JoinDataComplete IsOnline = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Is Online",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        #endregion


        #region Analog

        // TODO [ ] Add analog joins below plugin being developed

        [JoinName("Status")]
        public JoinDataComplete Status = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Socket Status",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("PresetSelect")]
        public JoinDataComplete PresetSelect = new JoinDataComplete(
               new JoinData
               {
                   JoinNumber = 2,
                   JoinSpan = 1,
               },
               new JoinMetadata
               {
                   Description = "Preset selection for selected canvas. If no Canvas is selected, canvas 1 is assumed",
                   JoinCapabilities = eJoinCapabilities.FromSIMPL,
                   JoinType = eJoinType.Analog
               }
            );

        [JoinName("CanvasSelect")]
        public JoinDataComplete CanvasSelect = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 3,
                    JoinSpan = 1,
                },
                new JoinMetadata
                {
                    Description = "Select a canvas for routing & preset recall. 0 recalls preset on all canvases. 0 is not valid for routing purposes",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Analog
                }
            );

        [JoinName("InputSelect")]
        public JoinDataComplete InputSelect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 40,
            },
            new JoinMetadata
            {
                Description = "Input Select for a window. Canvas value needs to be set first, or canvas 1 is assumed",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            }
        );

        #endregion


        #region Serial

        // TODO [ ] Add serial joins below plugin being developed
        [JoinName("DeviceName")]
        public JoinDataComplete DeviceName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("PresetNames")]
        public JoinDataComplete PresetNames = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 11,
                    JoinSpan = 20,
                },
                new JoinMetadata
                {
                    Description = "Preset name",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Serial
                }
            );

        #endregion

        /// <summary>
        /// Plugin device BridgeJoinMap constructor
        /// </summary>
        /// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
        public ExtronQuantumJoinMap(uint joinStart)
            : base(joinStart, typeof(ExtronQuantumJoinMap))
        {
        }
    }
}