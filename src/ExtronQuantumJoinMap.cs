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

        [JoinName("WindowMute")]
        public JoinDataComplete WindowMute = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 10,
                JoinSpan = 1,
            },
            new JoinMetadata
            {
                Description = "Window Mute for a window. High = Mute (invisible), Low = Unmute (visible)",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            }
        );

        #endregion Digital

        #region Analog

        // TODO [ ] Add analog joins below plugin being developed

        [JoinName("PresetSelect")]
        public JoinDataComplete PresetSelect = new JoinDataComplete(
               new JoinData
               {
                   JoinNumber = 1,
                   JoinSpan = 10,
               },
               new JoinMetadata
               {
                   Description = "Preset selection per canvas",
                   JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                   JoinType = eJoinType.Analog
               }
            );

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

        #endregion Serial

        /// <summary>
        /// Plugin device BridgeJoinMap constructor
        /// </summary>
        /// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
        public ExtronQuantumJoinMap(uint joinStart, RoutingPortCollection<RoutingOutputPort> outputPorts, Dictionary<string, PresetData> presets)
            : base(joinStart, typeof(ExtronQuantumJoinMap))
        {
            foreach (var item in outputPorts)
            {
                var port = item;

                if (!(port.Selector is string windowIndexString)) continue;
                if (!uint.TryParse(windowIndexString.GetUntil(":"), out uint windowIndex)) continue;

                var join = new JoinDataComplete(
                    new JoinData
                    {
                        JoinNumber = windowIndex + 10 + joinStart - 1,
                        JoinSpan = 1
                    },
                    new JoinMetadata
                    {
                        Description = $"Input Select for window{windowIndex}.",
                        JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                        JoinType = eJoinType.Analog
                    });
                Joins.Add($"Output-{windowIndex}", join);

                var muteJoin = new JoinDataComplete(
                    new JoinData
                    {
                        JoinNumber = windowIndex + 10 + joinStart - 1,
                        JoinSpan = 1
                    },
                    new JoinMetadata
                    {
                        Description = $"Window Mute for window{windowIndex}. High = Mute (invisible), Low = Unmute (visible)",
                        JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                        JoinType = eJoinType.Digital
                    });
                Joins.Add($"WindowMute-{windowIndex}", muteJoin);
            }

            foreach (var item in presets)
            {
                var preset = item.Value;
                var nameJoin = new JoinDataComplete(
                    new JoinData
                    {
                        JoinNumber = (uint)(preset.PresetIndex + 10 + joinStart - 1),
                        JoinSpan = 1
                    },
                    new JoinMetadata
                    {
                        Description = $"Preset-{preset.PresetIndex} Name",
                        JoinCapabilities = eJoinCapabilities.ToSIMPL,
                        JoinType = eJoinType.Serial
                    });

                Joins.Add($"PresetName-{preset.PresetIndex}", nameJoin);
            }
            Joins.Remove("InputSelect");
            Joins.Remove("WindowMute");
            Joins.Remove("PresetNames");
        }
        public ExtronQuantumJoinMap(uint joinStart)
    : base(joinStart, typeof(ExtronQuantumJoinMap))
        {
        }
    }
}