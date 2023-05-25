// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Core.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace epi.switcher.extron.quantum
{
    /// <summary>
    /// Plugin device template for third party devices that use IBasicCommunication
    /// </summary>
    /// <remarks>
    /// Rename the class to match the device plugin being developed.
    /// </remarks>
    /// <example>
    /// "EssentialsPluginDeviceTemplate" renamed to "SamsungMdcDevice"
    /// </example>
    public class ExtronQuantumDevice : EssentialsBridgeableDevice, IRouting, ICommunicationMonitor, IDeviceInfoProvider, IRoutingNumeric
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        private ExtronQuantumConfig _config;

        /// <summary>
        /// Provides a queue and dedicated worker thread for processing feedback messages from a device.
        /// </summary>
        private GenericQueue ReceiveQueue;

        //private readonly int _staticCanvas;

        #region IBasicCommunication Properties and Constructor.  Remove if not needed.

        // TODO [ ] Add, modify, remove properties and fields as needed for the plugin being developed
        private readonly IBasicCommunication _comms;

        private readonly GenericCommunicationMonitor _commsMonitor;

        // _comms gather for ASCII based API's
        // TODO [ ] If not using an ASCII based API, delete the properties below
        private readonly CommunicationGather _commsGather;

        /// <summary>
        /// Set this value to that of the delimiter used by the API (if applicable)
        /// </summary>
		private const string ReturnDelimiter = "\r\n";

        private const string SendDelimiter = "\r";

        //private int _selectedCanvas;

        public event DeviceInfoChangeHandler DeviceInfoChanged;

        /// <summary>
        /// Connects/disconnects the comms of the plugin device
        /// </summary>
        /// <remarks>
        /// triggers the _comms.Connect/Disconnect as well as thee comms monitor start/stop
        /// </remarks>
        public bool Connect
        {
            get { return _comms.IsConnected; }
        }

        /// <summary>
        /// Reports connect feedback through the bridge
        /// </summary>
        public BoolFeedback ConnectFeedback { get; private set; }

        private readonly string _serialNumber;

        /// <summary>
        /// Reports online feedback through the bridge
        /// </summary>
        public BoolFeedback OnlineFeedback { get; private set; }

        /// <summary>
        /// Reports socket status feedback through the bridge
        /// </summary>

        public Dictionary<int, IntFeedback> InputFeedbacks { get; private set; }
        public Dictionary<string, int> InputRoutes { get; private set; }

        public Dictionary<int, IntFeedback> PresetFeedbacks { get; private set; }
        public Dictionary<int, int> PresetFeedbackData { get; private set; }

        public List<PresetData> PresetList = new List<PresetData>();

        public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }

        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

        public StatusMonitorBase CommunicationMonitor => _commsMonitor;

        private int _pollCounter;

        private readonly string _adminPassword;
        private DeviceInfo _deviceInfo;
        private Timer _deviceInfoTimer;
        public DeviceInfo DeviceInfo => _deviceInfo;

        /// <summary>
        /// Plugin device constructor for devices that need IBasicCommunication
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <param name="comms"></param>
        public ExtronQuantumDevice(string key, string name, ExtronQuantumConfig config, IBasicCommunication comms)
            : base(key, name)
        {
            Debug.Console(0, this, $"Constructing new {name} instance");

            _config = config;

            _adminPassword = _config.Control.TcpSshProperties.Password;

            _serialNumber = _config.DeviceSerialNumber;

            ReceiveQueue = new GenericQueue(key + "-rxqueue");

            ConnectFeedback = new BoolFeedback(() => Connect);
            OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);

            _comms = comms;
            _commsMonitor = new GenericCommunicationMonitor(this,
                _comms, _config.PollTimeMs > 5000 ? _config.PollTimeMs : 5000,
                _config.WarningTimeoutMs > 5000 ? _config.WarningTimeoutMs : 60000,
                _config.ErrorTimeoutMs > 5000 ? _config.ErrorTimeoutMs : 180000,
                Poll);
            Debug.Console(0, this, "Built Comms Monitor");

            if (_comms is ISocketStatus socket)
            {
                socket.ConnectionChange += Socket_ConnectionChange;
            }

            _commsGather = new CommunicationGather(_comms, ReturnDelimiter);
            _commsGather.LineReceived += Handle_LineReceived;

            Debug.Console(0, this, "Built Comms Gather");

            InputPorts = CreateRoutingInputs(_config.Inputs);
            OutputPorts = CreateRoutingOutputs(_config.Windows);

            InputRoutes = new Dictionary<string, int>();
            InputFeedbacks = new Dictionary<int, IntFeedback>();

            PresetFeedbackData = new Dictionary<int, int>();
            PresetFeedbacks = new Dictionary<int, IntFeedback>();

            foreach (var item in OutputPorts)
            {
                var outputPort = item;
                if (outputPort == null)
                {
                    Debug.Console(0, this, "outputPort is null");
                    continue;
                }

                if (!(outputPort.Selector is string selector)) continue;
                var selectorIndex = selector.GetUntil(":");
                var selectorSubstring = selector.GetAfter(":");
                if (!int.TryParse(selectorIndex, out var selectorInt)) continue;
                Debug.Console(0, this, "Selector = {0}", selector);
                InputRoutes.Add(selectorSubstring, 0);
                InputFeedbacks.Add(selectorInt, new IntFeedback(() => InputRoutes[selectorSubstring]));
            }

            PresetList = _config.Presets.Select(kvp => kvp.Value).ToList();

            for (int i = 0; i < 10; i++)
            {
                var iterator = i + 1;
                PresetFeedbackData.Add(iterator, 0);
                PresetFeedbacks.Add(iterator, new IntFeedback(() => PresetFeedbackData[iterator]));
            }

            _deviceInfo = new DeviceInfo();
        }

        private RoutingPortCollection<RoutingInputPort> CreateRoutingInputs(Dictionary<string, NameValue> inputs)
        {
            RoutingPortCollection<RoutingInputPort> newInputs = new RoutingPortCollection<RoutingInputPort>();
            foreach (var item in inputs)
            {
                newInputs.Add(new RoutingInputPort(item.Key, eRoutingSignalType.Video, eRoutingPortConnectionType.Hdmi, item.Value.Value, this));
            }
            return newInputs;
        }

        private RoutingPortCollection<RoutingOutputPort> CreateRoutingOutputs(Dictionary<string, WindowData> outputs)
        {
            RoutingPortCollection<RoutingOutputPort> newOutputs = new RoutingPortCollection<RoutingOutputPort>();
            foreach (var item in outputs)
            {
                newOutputs.Add(new RoutingOutputPort(item.Key, eRoutingSignalType.Video, eRoutingPortConnectionType.Hdmi, $"{item.Value.WindowIndex}:{item.Value.Canvas}:{item.Value.Window}", this));
            }
            return newOutputs;
        }

        public override void Initialize()
        {
            Debug.Console(0, this, "Initialize");
            if (!String.IsNullOrEmpty(_serialNumber))
                _deviceInfo.SerialNumber = _serialNumber;
            _comms.Connect();
        }

        private void Socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
        {
            ConnectFeedback?.FireUpdate();

            switch (args.Client.ClientStatus)
            {
                case SocketStatus.SOCKET_STATUS_CONNECTED:
                    {
                        break;
                    }
            }
        }

        private void Handle_LineReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            ReceiveQueue.Enqueue(new ProcessStringMessage(args.Text, ProcessFeedbackMessage));
        }

        /// <summary>
        /// This method should perform any necessary parsing of feedback messages from the device
        /// </summary>
        /// <param name="message"></param>
        private void ProcessFeedbackMessage(string message)
        {
            Debug.Console(2, this, $"Message received: {message}");

            if (message.IndexOf("login administrator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SendText(_adminPassword);
                return;
            }

            if (message.IndexOf("60-") >= 0)
            {
                SendText("W3CV");
                return;
            }

            if (message.StartsWith("vrb3", StringComparison.OrdinalIgnoreCase))
            {
                _commsMonitor.Start();
                //PollCanvasWindows(_selectedCanvas);
                return;
            }

            if (message.StartsWith("bld", StringComparison.OrdinalIgnoreCase)) //firmware response
            {
                var firmware = message.Replace("Bld", "");

                _deviceInfo.FirmwareVersion = firmware;

                FireDeviceInfoUpdate(_deviceInfo);
                return;
            }

            if (message.StartsWith("ipn", StringComparison.OrdinalIgnoreCase)) // hostname
            {
                var hostname = message.Replace("Ipn ", "");

                _deviceInfo.HostName = hostname;

                FireDeviceInfoUpdate(_deviceInfo);
                return;
            }

            if (message.StartsWith("cisg", StringComparison.OrdinalIgnoreCase)) // IP Info
            {
                var tokens = message.TokenizeParams('*');

                using (var parameters = tokens.GetEnumerator())
                {
                    parameters.Next(); // throw away 'Cisg 1'

                    var ipAddressSubnetMask = parameters.Next().Split('/');

                    _deviceInfo.IpAddress = ipAddressSubnetMask[0];

                    FireDeviceInfoUpdate(_deviceInfo);
                }
                return;
            }

            if (message.StartsWith("iph", StringComparison.OrdinalIgnoreCase)) // MAC Address
            {
                var tokens = message.TokenizeParams('*');

                using (var parameters = tokens.GetEnumerator())
                {
                    parameters.Next(); //throw away 'Iph`';

                    var mac = parameters.Next();

                    _deviceInfo.MacAddress = mac;

                    FireDeviceInfoUpdate(_deviceInfo);
                }
                return;
            }

            if (message.StartsWith("grp", StringComparison.OrdinalIgnoreCase)) // Input Route
            {
                var tokens = message.TokenizeParams(' ');
                using (var parameters = tokens.GetEnumerator())
                {
                    var canvas = parameters.Next().Substring("Grp".Length).TrimStart('0');
                    //if (canvas != _selectedCanvas) return;
                    var window = parameters.Next().Substring("Win".Length).TrimStart('0');
                    var input = parameters.Next().Substring("In".Length).TrimStart('0');
                    InputRoutes[$"{canvas}:{window}"] = String.IsNullOrEmpty(input) ? 0 : int.Parse(input);
                }
                FireInputFeedbacks();
                return;
            }
            if (message.StartsWith("prstl1", StringComparison.OrdinalIgnoreCase))
            {
                var tokens = message.TokenizeParams('*');
                using (var parameters = tokens.GetEnumerator())
                {
                    parameters.Next(); //throw away 'PrstL1*'
                    var canvas = parameters.Next();
                    var preset = parameters.Next();
                    var canvasInt = int.Parse(canvas);

                    PresetFeedbackData[canvasInt] = int.Parse(preset);
                    PresetFeedbacks[canvasInt].FireUpdate();
                }
                FirePresetFeedbacks();
                return;
            }
            if (message.StartsWith("1rpr", StringComparison.OrdinalIgnoreCase))
            {
                var tokens = message.TokenizeParams('*');
                using (var parameters = tokens.GetEnumerator())
                {
                    var canvas = parameters.Next().Substring("1rpr".Length);
                    var preset = parameters.Next();
                    var canvasInt = int.Parse(canvas);

                    PresetFeedbackData[canvasInt] = int.Parse(preset);
                    PresetFeedbacks[canvasInt].FireUpdate();
                }
                FirePresetFeedbacks();
                return;
            }
        }

        private void FireInputFeedbacks()
        {
            foreach (var item in InputFeedbacks)
            {
                var input = item.Value;
                input.FireUpdate();
            }
        }

        private void FirePresetFeedbacks()
        {
            foreach (var item in PresetFeedbacks)
            {
                var fb = item.Value;
                fb.FireUpdate();
            }
        }

        /// <summary>
        /// Sends text to the device plugin comms
        /// </summary>
        /// <remarks>
        /// Can be used to test commands with the device plugin using the DEVPROPS and DEVJSON console commands
        /// </remarks>
        /// <param name="text">Command to be sent</param>
        public void SendText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            _comms.SendText($"{text}{SendDelimiter}");
        }

        /// <summary>
        /// Polls the device
        /// </summary>
        /// <remarks>
        /// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed
        /// </remarks>
        public void Poll()
        {
            switch (_pollCounter)
            {
                case 10:
                    PollLastPreset();
                    break;

                default:
                    SendText("*Q");
                    break;
            }
            _pollCounter++;
            if (_pollCounter > 10) _pollCounter = 0;
        }

        #endregion IBasicCommunication Properties and Constructor.  Remove if not needed.

        #region Overrides of EssentialsBridgeableDevice

        /// <summary>
        /// Links the plugin device to the EISC bridge
        /// </summary>
        /// <param name="trilist"></param>
        /// <param name="joinStart"></param>
        /// <param name="joinMapKey"></param>
        /// <param name="bridge"></param>
        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            var joinMap = customJoins == null ? new ExtronQuantumJoinMap(joinStart, OutputPorts, _config.Presets) : new ExtronQuantumJoinMap(joinStart);

            // This adds the join map to the collection on the bridge

            if (customJoins != null)
            {
                joinMap.SetCustomJoinData(customJoins);
            }

            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));

            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

            OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            if (customJoins == null)
            {

                Debug.Console(0, this, "No Custom Joins Found - Linking Output Ports");
                foreach (var item in OutputPorts)
                {
                    var port = item;
                    //var output = (uint)port.Selector;


                    if (!(port.Selector is string selector)) continue;
                    var output = selector.GetUntil(":");
                    if (!int.TryParse(output, out int outputInt)) continue;
                    if (!joinMap.Joins.TryGetValue($"Output-{output}", out JoinDataComplete switchJoin)) continue;

                    trilist.SetUShortSigAction(switchJoin.JoinNumber, (input) => ExecuteNumericSwitch(input, (ushort)outputInt, eRoutingSignalType.Video));
                    if (!InputFeedbacks.TryGetValue(outputInt, out IntFeedback inputFeedback)) continue;
                    inputFeedback.LinkInputSig(trilist.UShortInput[switchJoin.JoinNumber]);
                    

                }
                Debug.Console(1, this, "No Custom JoinMap Found - Linking Presets");

                foreach (var item in _config.Presets)
                {
                    var preset = item.Value;

                    if (!joinMap.Joins.TryGetValue($"PresetName-{preset.PresetIndex}", out JoinDataComplete presetJoin)) continue;
                    trilist.SetString(joinMap.PresetNames.JoinNumber, preset.Name);
                }
            }
            else
            {
                foreach (var presetConfig in _config.Presets)
                {
                    trilist.SetString((uint)(joinMap.PresetNames.JoinNumber + presetConfig.Value.PresetIndex), presetConfig.Value.Name);
                }

                for (int i = 0; i < joinMap.InputSelect.JoinSpan; i++)
                {
                    var joinOffset = i;
                    var output = (ushort)(i + 1);

                    trilist.SetUShortSigAction(joinMap.InputSelect.JoinNumber + (uint)joinOffset, (input) => ExecuteNumericSwitch(input, output, eRoutingSignalType.Video));
                }
            }
            for (int i = 0; i < joinMap.PresetSelect.JoinSpan; i++)
            {
                var iterator = i + 1;
                PresetFeedbacks[iterator].LinkInputSig(trilist.UShortInput[(uint)(joinMap.PresetSelect.JoinNumber + i)]);
                trilist.SetUShortSigAction((uint)(joinMap.PresetSelect.JoinNumber + i), (a) => RecallPreset(a, iterator));
            }

            trilist.SetUShortSigAction(joinMap.PresetSelect.JoinNumber, (preset) => RecallPreset(preset));

            //trilist.SetUShortSigAction(joinMap.CanvasSelect.JoinNumber, (canvas) => SelectedCanvas = canvas);
            //SelectedCanvasFeedback.LinkInputSig(trilist.UShortInput[joinMap.CanvasSelect.JoinNumber]);

            UpdateFeedbacks();


            trilist.OnlineStatusChange += (o, a) => {
                if (!a.DeviceOnLine) return;

                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

                var offset = 0;
                if (customJoins != null)
                {
                    foreach (var preset in PresetList)
                    {
                        trilist.SetString((uint)(joinMap.PresetNames.JoinNumber + offset), preset.Name);

                        offset += 1;
                    }
                }
                else
                {
                    foreach (var item in _config.Presets)
                    {
                        var preset = item.Value;

                        if (!joinMap.Joins.TryGetValue($"PresetSelect-{preset.PresetIndex}", out JoinDataComplete presetJoin)) continue;
                        trilist.SetString((joinMap.PresetNames.JoinNumber), preset.Name);
                    }
                }
                UpdateFeedbacks();
            };
        }


        #endregion Overrides of EssentialsBridgeableDevice

        private void UpdateFeedbacks()
        {
            ConnectFeedback.FireUpdate();
            OnlineFeedback.FireUpdate();
            PollWindows();
            PollLastPreset();
        }

        public void RecallPreset(int preset, int canvas)
        {
            if (preset <= 0)
            {
                Debug.Console(1, this, "Unable to recall preset. Preset 0 is not valid");
                return;
            }

            SendText($"1*{canvas}*{preset}.");
        }

        public void RecallPreset(int preset)
        {
            var selectedPreset = PresetList.FirstOrDefault(p => p.PresetIndex == preset);

            if (selectedPreset == null) return;

            RecallPreset(selectedPreset.CanvasPresetNumber, selectedPreset.Canvas);
        }

        private void PollWindows()
        {
            foreach (var outputPort in OutputPorts)
            {
                GetOutputRoute(outputPort);
            }
        }

        private void PollLastPreset()
        {
            const string esc = "\u001B";
            const string cr = "\u000D";
            for (int i = 0; i < 10; i++)
            {
                SendText($"{esc}L1*{i + 1}PRST{cr}");
            }
        }

        public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
        {
            //var canvas = _selectedCanvas <= 0 ? 1 : _selectedCanvas;
            if (!(outputSelector is string selectorFull))
            {
                Debug.Console(1, this, "Invalid Output Selector");
                return;
            }
            var selector = selectorFull.GetAfter(":");
            if (string.IsNullOrEmpty(selector)) return;
            if (!uint.TryParse(selector.GetUntil(":"), out uint canvas)) return;
            if (!uint.TryParse(selector.GetAfter(":"), out uint window)) return;

            if ((ushort)window == 0)
            {
                Debug.Console(1, this, "Unable to make switch. Window 0 is not valid");
                return;
            }

            if (canvas > 10)
            {
                Debug.Console(1, this, "Unable to make switch. Canvas values must be between 0 & 10");
                return;
            }

            SendText($"{canvas}*{window}*{inputSelector}!");
        }

        public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
        {
            var outputPortSelected = OutputPorts.FirstOrDefault(o => ushort.Parse(((string)o.Selector).GetUntil(":")) == output);
            if (outputPortSelected == null)
            {
                Debug.Console(1, this, $"Invalid output selection {output}");
                return;
            }
            var selector = outputPortSelected.Selector;
            ExecuteSwitch(input, selector, type);
        }

        private void GetOutputRoute(RoutingOutputPort port)
        {
            var outputSelector = port.Selector;
            if (!(outputSelector is string selectorFull))
            {
                Debug.Console(1, this, "Invalid Output Selector");
                return;
            }
            var selector = selectorFull.GetAfter(":");
            if (string.IsNullOrEmpty(selector)) return;
            if (!uint.TryParse(selector.GetUntil(":"), out uint canvas)) return;
            if (!uint.TryParse(selector.GetAfter(":"), out uint window)) return;

            SendText($"{canvas}*{window}!");
        }

        public void UpdateDeviceInfo()
        {
            SendText("W1CH"); // Get LAN A MAC Address
            SendText("W1CISG"); // Get LAN A IP Information
            SendText("WCN"); // Get Host Name
        }

        private void FireDeviceInfoUpdate(DeviceInfo deviceInfo)
        {
            if (_deviceInfoTimer != null)
            {
                _deviceInfoTimer.Stop();
                _deviceInfoTimer.Dispose();
                _deviceInfoTimer = null;
            }

            _deviceInfoTimer = new Timer(1000);

            _deviceInfoTimer.Elapsed += (s, a) => {
                DeviceInfoChanged?.Invoke(this, new DeviceInfoEventArgs(deviceInfo));
                _deviceInfoTimer.Dispose();
                _deviceInfoTimer = null;
            };

            _deviceInfoTimer.AutoReset = false;
            _deviceInfoTimer.Enabled = true;
        }
    }
}