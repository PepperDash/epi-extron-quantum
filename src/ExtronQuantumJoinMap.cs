using PepperDash.Essentials.Core;
using System.Collections.Generic;

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
            new JoinData {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata {
                Description = "Is Online",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        #endregion Digital

        #region Analog

        // TODO [ ] Add analog joins below plugin being developed

        [JoinName("CanvasSelect")]
        public JoinDataComplete CanvasSelect = new JoinDataComplete(
                new JoinData {
                    JoinNumber = 3,
                    JoinSpan = 1,
                },
                new JoinMetadata {
                    Description = "Select a canvas for routing & preset recall. 0 recalls preset on all canvases. 0 is not valid for routing purposes",
                    JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                    JoinType = eJoinType.Analog
                }
            );

        [JoinName("PresetSelect")]
        public JoinDataComplete PresetSelect = new JoinDataComplete(
               new JoinData {
                   JoinNumber = 2,
                   JoinSpan = 1,
               },
               new JoinMetadata {
                   Description = "Preset selection for selected canvas. If no Canvas is selected, canvas 1 is assumed",
                   JoinCapabilities = eJoinCapabilities.FromSIMPL,
                   JoinType = eJoinType.Analog
               }
            );

        [JoinName("Status")]
        public JoinDataComplete Status = new JoinDataComplete(
            new JoinData {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata {
                Description = "Socket Status",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });
        
        [JoinName("InputSelect")]
        public JoinDataComplete InputSelect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 10,
                JoinSpan = 1,
            },
            new JoinMetadata
            {
                Description = "Input Select for a window. Canvas value needs to be set first, or canvas 1 is assumed",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            }
        );
        

        #endregion Analog

        #region Serial

        // TODO [ ] Add serial joins below plugin being developed
        [JoinName("DeviceName")]
        public JoinDataComplete DeviceName = new JoinDataComplete(
            new JoinData {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata {
                Description = "Device Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("PresetNames")]
        public JoinDataComplete PresetNames = new JoinDataComplete(
                new JoinData {
                    JoinNumber = 11,
                    JoinSpan = 20,
                },
                new JoinMetadata {
                    Description = "Preset name",
                    JoinCapabilities = eJoinCapabilities.FromSIMPL,
                    JoinType = eJoinType.Serial
                }
            );

        #endregion Serial

        /// <summary>
        /// Plugin device BridgeJoinMap constructor
        /// </summary>
        /// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
        public ExtronQuantumJoinMap(uint joinStart, RoutingPortCollection<RoutingOutputPort> outputPorts, Dictionary<string, string> presets)
            : base(joinStart, typeof(ExtronQuantumJoinMap))
        {
            foreach (var item in outputPorts)
            {
                var port = item;
                var join = new JoinDataComplete(
                    new JoinData {
                        JoinNumber = (uint)port.Selector + 10 + joinStart - 1,
                        JoinSpan = 1
                    },
                    new JoinMetadata {
                        Description = $"Input Select for window{(uint)port.Selector}. Canvas value needs to be set first, or canvas 1 is assumed",
                        JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                        JoinType = eJoinType.Analog
                    });
                Joins.Add($"Output-{(uint)port.Selector}", join);
            }

            var presetTracker = 1;
            foreach (var item in presets)
            {
                var preset = item;
                var nameJoin = new JoinDataComplete(
                    new JoinData {
                        JoinNumber = (uint)presetTracker + 10 + joinStart - 1,
                        JoinSpan = 1
                    },
                    new JoinMetadata {
                        Description = $"Preset-{presetTracker} Name",
                        JoinCapabilities = eJoinCapabilities.ToSIMPL,
                        JoinType = eJoinType.Serial
                    });
                Joins.Add($"PresetName-{(uint)presetTracker}", nameJoin);
                presetTracker += 1;
            }
            Joins.Remove("InputSelect");
            Joins.Remove("PresetNames");
            }

        public ExtronQuantumJoinMap(uint joinStart)
            : base(joinStart, typeof(ExtronQuantumJoinMap))
        {
        }
    }
}