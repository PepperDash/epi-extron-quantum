// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Core.Queues;
using System;
using System.Collections.Generic;
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

        private readonly int _staticCanvas;

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

        private int _selectedCanvas;

        public event DeviceInfoChangeHandler DeviceInfoChanged;

        public int SelectedCanvas
        {
            get { return _staticCanvas != 0 ? _staticCanvas : _selectedCanvas; }

            set
            {
                if (value == _selectedCanvas) return;

                _selectedCanvas = _staticCanvas != 0 ? _staticCanvas : value;

                SelectedCanvasFeedback.FireUpdate();
                PollCanvasWindows(_selectedCanvas);
            }
        }

        public IntFeedback SelectedCanvasFeedback { get; private set; }

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
        public IntFeedback StatusFeedback { get; private set; }

        public Dictionary<int, IntFeedback> InputFeedbacks { get; private set; }
        public Dictionary<int, int> InputRoutes { get; private set; }

        public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }

        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

        public StatusMonitorBase CommunicationMonitor => _commsMonitor;

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

            _staticCanvas = _config.StaticCanvas;

            _adminPassword = _config.Control.TcpSshProperties.Password;

            _serialNumber = _config.DeviceSerialNumber;

            ReceiveQueue = new GenericQueue(key + "-rxqueue");

            ConnectFeedback = new BoolFeedback(() => Connect);
            OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);
            StatusFeedback = new IntFeedback(() => (int)_commsMonitor.Status);

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

            SelectedCanvasFeedback = new IntFeedback(() => SelectedCanvas);

            InputPorts = CreateRoutingInputs(_config.Inputs);
            OutputPorts = CreateRoutingOutputs(_config.Windows);

            InputRoutes = new Dictionary<int, int>();
            InputFeedbacks = new Dictionary<int, IntFeedback>();

            foreach (var item in OutputPorts)
            {
                var outputPort = item;
                if (outputPort == null)
                {
                    Debug.Console(0, this, "outputPort is null");
                    continue;
                }

                if (!(outputPort.Selector is uint selector)) continue;
                Debug.Console(0, this, "Selector = {0}", selector);
                InputRoutes.Add((int)selector, 0);
                InputFeedbacks.Add((int)selector, new IntFeedback(() => InputRoutes[(int)selector]));
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

        private RoutingPortCollection<RoutingOutputPort> CreateRoutingOutputs(Dictionary<string, NameValue> outputs)
        {
            RoutingPortCollection<RoutingOutputPort> newOutputs = new RoutingPortCollection<RoutingOutputPort>();
            foreach (var item in outputs)
            {
                newOutputs.Add(new RoutingOutputPort(item.Key, eRoutingSignalType.Video, eRoutingPortConnectionType.Hdmi, item.Value.Value, this));
            }
            return newOutputs;
        }

        public override void Initialize()
        {
            Debug.Console(0, this, "Initialize");
            if(!String.IsNullOrEmpty(_serialNumber))
                _deviceInfo.SerialNumber = _serialNumber;
            _comms.Connect();
        }

        private void Socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
        {
            ConnectFeedback?.FireUpdate();

            StatusFeedback?.FireUpdate();

            switch (args.Client.ClientStatus)
            {
                case SocketStatus.SOCKET_STATUS_CONNECTED:
                    {
                        //Set Ve
                        //SendText("W3CV"); //Set Verbose mode 2
                        //SendText(_adminPassword);
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

            if (message.IndexOf("login administrator", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SendText(_adminPassword);
                return;
            }

            if (message.IndexOf("60-") >= 0)
            {
                SendText("W3CV");
                return;
            }

            if (message.StartsWith("vrb3", System.StringComparison.OrdinalIgnoreCase))
            {
                _commsMonitor.Start();
                PollCanvasWindows(_selectedCanvas);
                return;
            }

            if (message.StartsWith("bld", System.StringComparison.OrdinalIgnoreCase)) //firmware response
            {
                var firmware = message.Replace("Bld", "");

                _deviceInfo.FirmwareVersion = firmware;

                FireDeviceInfoUpdate(_deviceInfo);
                return;
            }

            if (message.StartsWith("ipn", System.StringComparison.OrdinalIgnoreCase)) // hostname
            {
                var hostname = message.Replace("Ipn ", "");

                _deviceInfo.HostName = hostname;

                FireDeviceInfoUpdate(_deviceInfo);
                return;
            }

            if (message.StartsWith("cisg", System.StringComparison.OrdinalIgnoreCase)) // IP Info
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

            if (message.StartsWith("iph", System.StringComparison.OrdinalIgnoreCase)) // MAC Address
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

            if (message.StartsWith("grp", System.StringComparison.OrdinalIgnoreCase)) // Input Route
            {
                var tokens = message.TokenizeParams(' ');
                using (var parameters = tokens.GetEnumerator())
                {
                    var canvas = uint.Parse(parameters.Next().Substring("Grp".Length));
                    if (canvas != _selectedCanvas) return;
                    var window = int.Parse(parameters.Next().Substring("Win".Length));
                    InputRoutes[window] = int.Parse(parameters.Next().Substring("In".Length));
                    InputFeedbacks[window].FireUpdate();
                }
                return;
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
            SendText("*Q");
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
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

            StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
            OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

            if (customJoins == null)
            {
                Debug.Console(0, this, "No Custom Joins Found - Linking Output Ports");
                foreach (var item in OutputPorts)
                {
                    var port = item;
                    var output = (uint)port.Selector;

                    if (!joinMap.Joins.TryGetValue($"Output-{output}", out JoinDataComplete switchJoin)) continue;

                    trilist.SetUShortSigAction(switchJoin.JoinNumber, (input) => ExecuteNumericSwitch(input, (ushort)output, eRoutingSignalType.Video));
                    if (!InputFeedbacks.TryGetValue((int)output, out IntFeedback inputFeedback)) continue;
                    inputFeedback.LinkInputSig(trilist.UShortInput[switchJoin.JoinNumber]);
                }
                Debug.Console(0, this, "No Custom Joins Found - Linking Presets");

                var presetTracker = 1;
                foreach (var item in _config.Presets)
                {
                    var presetConfig = item;

                    if (!joinMap.Joins.TryGetValue($"PresetSelect-{presetTracker}", out JoinDataComplete presetJoin)) continue;
                    trilist.SetString((joinMap.PresetNames.JoinNumber), presetConfig.Value);

                    presetTracker += 1;
                }
            }
            else
            {
                var presetOffset = 0;
                foreach (var presetConfig in _config.Presets)
                {
                    trilist.SetString((uint)(joinMap.PresetNames.JoinNumber + presetOffset), presetConfig.Value);

                    presetOffset += 1;
                }

                for (int i = 0; i < joinMap.InputSelect.JoinSpan; i += 1)
                {
                    var joinOffset = i;
                    var output = (ushort)(i + 1);

                    trilist.SetUShortSigAction(joinMap.InputSelect.JoinNumber + (uint)joinOffset, (input) => ExecuteNumericSwitch(input, output, eRoutingSignalType.Video));
                }
            }

            trilist.SetUShortSigAction(joinMap.PresetSelect.JoinNumber, (preset) => RecallPreset(preset));

            trilist.SetUShortSigAction(joinMap.CanvasSelect.JoinNumber, (canvas) => SelectedCanvas = canvas);
            SelectedCanvasFeedback.LinkInputSig(trilist.UShortInput[joinMap.CanvasSelect.JoinNumber]);

            UpdateFeedbacks();

            trilist.OnlineStatusChange += (o, a) => {
                if (!a.DeviceOnLine) return;

                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

                var offset = 0;
                foreach (var presetConfig in _config.Presets)
                {
                    trilist.SetString((uint)(joinMap.PresetNames.JoinNumber + offset), presetConfig.Value);

                    offset += 1;
                }
                UpdateFeedbacks();
            };
        }

        #endregion Overrides of EssentialsBridgeableDevice

        private void UpdateFeedbacks()
        {
            ConnectFeedback.FireUpdate();
            OnlineFeedback.FireUpdate();
            StatusFeedback.FireUpdate();
            SelectedCanvasFeedback.FireUpdate();
            PollCanvasWindows(_selectedCanvas);
        }

        public void RecallPreset(int preset, int canvas)
        {
            if (preset <= 0)
            {
                Debug.Console(1, this, "Unable to recall preset. Preset 0 is not valid");
                return;
            }

            SendText($"1*{preset}*{canvas}.");
        }

        public void RecallPreset(int preset)
        {
            var canvas = _selectedCanvas < 0 ? 0 : _selectedCanvas;

            RecallPreset(preset, canvas);
        }

        public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
        {
            var canvas = _selectedCanvas <= 0 ? 1 : _selectedCanvas;

            if ((ushort)outputSelector == 0)
            {
                Debug.Console(1, this, "Unable to make switch. Window 0 is not valid");
                return;
            }

            if (canvas > 10)
            {
                Debug.Console(1, this, "Unable to make switch. Canvas values must be between 0 & 10");
                return;
            }

            SendText($"{canvas}*{outputSelector}*{inputSelector}!");
        }

        public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
        {
            ExecuteSwitch(input, output, type);
        }

        private void PollCanvasWindows(int canvas)
        {
            foreach (var outputPort in OutputPorts)
            {
                GetOutputRoute(outputPort, canvas);
            }
        }

        private void GetOutputRoute(RoutingOutputPort port, int canvas)
        {
            SendText($"{canvas}*{(uint)port.Selector}!");
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