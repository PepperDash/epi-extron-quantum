// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Core.Queues;
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
            get { return _selectedCanvas; }

            set
            {
                if (value == _selectedCanvas) return;

                _selectedCanvas = value;

                SelectedCanvasFeedback.FireUpdate();
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

        /// <summary>
        /// Reports online feedback through the bridge
        /// </summary>
        public BoolFeedback OnlineFeedback { get; private set; }

        /// <summary>
        /// Reports socket status feedback through the bridge
        /// </summary>
        public IntFeedback StatusFeedback { get; private set; }

        public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }

        public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

        public StatusMonitorBase CommunicationMonitor => _commsMonitor;

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

            ReceiveQueue = new GenericQueue(key + "-rxqueue");

            ConnectFeedback = new BoolFeedback(() => Connect);
            OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);
            StatusFeedback = new IntFeedback(() => (int)_commsMonitor.Status);

            _comms = comms;
            _commsMonitor = new GenericCommunicationMonitor(this, _comms, _config.PollTimeMs, _config.WarningTimeoutMs, _config.ErrorTimeoutMs, Poll);

            if (_comms is ISocketStatus socket)
            {
                // device comms is IP **ELSE** device comms is RS232
                socket.ConnectionChange += Socket_ConnectionChange;
            }

            _commsGather = new CommunicationGather(_comms, ReturnDelimiter);
            _commsGather.LineReceived += Handle_LineReceived;

            SelectedCanvasFeedback = new IntFeedback(() => SelectedCanvas);

            InputPorts = CreateRoutingInputs(_config.Inputs);
            OutputPorts = CreateRoutingOutputs(_config.Windows);

            _deviceInfo = new DeviceInfo();
        }

        private RoutingPortCollection<RoutingInputPort> CreateRoutingInputs(Dictionary<string, NameValue> inputs)
        {
            return inputs.Select((kv) => new RoutingInputPort(kv.Key, eRoutingSignalType.Video, eRoutingPortConnectionType.Hdmi, kv.Value.Value, this)).ToList() as RoutingPortCollection<RoutingInputPort>;
        }

        private RoutingPortCollection<RoutingOutputPort> CreateRoutingOutputs(Dictionary<string, NameValue> outputs)
        {
            return outputs.Select((kv) => new RoutingOutputPort(kv.Key, eRoutingSignalType.Video, eRoutingPortConnectionType.Hdmi, kv.Value.Value, this)).ToList() as RoutingPortCollection<RoutingOutputPort>;
        }

        public override void Initialize()
        {
            _comms.Connect();
            _commsMonitor.Start();
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
                        SendText("W3CV"); //Set Verbose mode 2
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
        void ProcessFeedbackMessage(string message)
        {
            Debug.Console(2, this, $"Message received: {message}");

            if (message.StartsWith("Bld")) //firmware response
            {
                var firmware = message.Replace("Bld", "");

                _deviceInfo.FirmwareVersion = firmware;

                FireDeviceInfoUpdate(_deviceInfo);
                return;
            }

            if (message.StartsWith("Ipn")) // hostname
            {
                var hostname = message.Replace("Ipn ", "");

                _deviceInfo.HostName = hostname;

                FireDeviceInfoUpdate(_deviceInfo);
                return;
            }

            if (message.StartsWith("Cisg")) // IP Info
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

            if (message.StartsWith("Iph")) // MAC Address
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

        #endregion


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
            var joinMap = new ExtronQuantumJoinMap(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins != null)
            {
                joinMap.SetCustomJoinData(customJoins);
            }

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

            // TODO [ ] Implement bridge links as needed

            // links to bridge
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

            var presetOffset = 0;
            foreach (var presetConfig in _config.Presets)
            {
                trilist.SetString((uint)(joinMap.PresetNames.JoinNumber + presetOffset), presetConfig.Value);

                presetOffset += 1;
            }


            StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
            OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

            for (int i = 0; i < joinMap.InputSelect.JoinSpan; i += 1)
            {
                var joinOffset = i;
                var output = (ushort)(i + 1);

                trilist.SetUShortSigAction(joinMap.InputSelect.JoinNumber + (uint)joinOffset, (input) => ExecuteNumericSwitch(input, output, eRoutingSignalType.Video));
            }

            trilist.SetUShortSigAction(joinMap.PresetSelect.JoinNumber, (preset) => RecallPreset(preset));

            trilist.SetUShortSigAction(joinMap.CanvasSelect.JoinNumber, (canvas) => SelectedCanvas = canvas);
            SelectedCanvasFeedback.LinkInputSig(trilist.UShortInput[joinMap.CanvasSelect.JoinNumber]);


            UpdateFeedbacks();

            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;

                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

                var offset = 0;
                foreach (var presetConfig in _config.Presets)
                {
                    trilist.SetString((uint)(joinMap.PresetNames.JoinNumber + presetOffset), presetConfig.Value);

                    offset += 1;
                }
                UpdateFeedbacks();
            };
        }

        #endregion

        private void UpdateFeedbacks()
        {
            ConnectFeedback.FireUpdate();
            OnlineFeedback.FireUpdate();
            StatusFeedback.FireUpdate();
            SelectedCanvasFeedback.FireUpdate();
        }

        public void RecallPreset(int preset, int canvas)
        {
            if (preset <= 0)
            {
                Debug.Console(1, this, "Unable to recall preset. Preset 0 is not valid");
                return;
            }

            SendText($"1*{preset}*{canvas}");
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

            _deviceInfoTimer.Elapsed += (s, a) =>
            {
                DeviceInfoChanged?.Invoke(this, new DeviceInfoEventArgs(deviceInfo));
                _deviceInfoTimer.Dispose();
                _deviceInfoTimer = null;
            };

            _deviceInfoTimer.AutoReset = false;
            _deviceInfoTimer.Enabled = true;
        }
    }
}

