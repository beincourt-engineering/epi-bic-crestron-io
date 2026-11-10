using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Remotes;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace PepperDash.Essentials.Plugins
{
    [Description("Wrapper class for all HR-Series remotes")]
    public class Hrxx0WirelessRemoteController : EssentialsBridgeableDevice, IHasFeedback, IHR52Button, ICommunicationMonitor
    {        
        private Hr1x0WirelessRemoteBase _remote;        

        public FeedbackCollection<Feedback> Feedbacks { get; } = new FeedbackCollection<Feedback>();

        /// <summary>
        /// Follows the remote's online state. Created with the remote once every device has
        /// activated, so it is in place before any device initializes; it stays null when the
        /// remote cannot be created.
        /// </summary>
        public StatusMonitorBase CommunicationMonitor { get; private set; }

        public CrestronCollection<Button> Buttons => _remote.Button;

        private readonly CrestronRemotePropertiesConfig props;

        public Hrxx0WirelessRemoteController(string key, string name, Func<string, CrestronRemotePropertiesConfig, Hr1x0WirelessRemoteBase> postActivationFunc, 
            DeviceConfig config)
            : base(key, name)
        {           
            props = config.Properties.ToObject<CrestronRemotePropertiesConfig>();
            
            AddPostActivationAction(() =>
            {
                _remote = postActivationFunc(config.Type, props);                

                RegisterEvents();

                if (_remote != null && _remote.Registerable)
                {
                    _remote.RegisterWithLogging(Key);
                }

                if (_remote != null)
                {
                    CommunicationMonitor = new CrestronGenericBaseCommunicationMonitor(this, _remote, 120000, 300000);
                    CommunicationMonitor.Start();
                }
            });            
        }        

        void Remote_BaseEvent(GenericBase device, BaseEventArgs args)
        {
            this.LogVerbose("Base Event: {id}", args.EventId);

            if(args.EventId == Hr1x0EventIds.BatteryCriticalFeedbackEventId)
                Feedbacks["BatteryCritical"].FireUpdate();
            if(args.EventId == Hr1x0EventIds.BatteryLowFeedbackEventId)
                Feedbacks["BatteryLow"].FireUpdate();
            if(args.EventId == Hr1x0EventIds.BatteryVoltageFeedbackEventId)
                Feedbacks["BatteryVoltage"].FireUpdate();
        }

        private void RegisterEvents()
        {
            _remote.ButtonStateChange += Remote_ButtonStateChange;
            _remote.BaseEvent += Remote_BaseEvent;
            _remote.OnlineStatusChange += (d, a) => this.LogInformation("Remote Online Status: {status}", a.DeviceOnLine ? "Online" : "Offline");

            Feedbacks.Add(new BoolFeedback("BatteryCritical", () => _remote.BatteryCriticalFeedback.BoolValue));
            Feedbacks.Add(new BoolFeedback("BatteryLow", () => _remote.BatteryLowFeedback.BoolValue));
            Feedbacks.Add(new IntFeedback("BatteryVoltage", () => _remote.BatteryVoltageFeedback.UShortValue));
        }

        void Remote_ButtonStateChange(GenericBase device, ButtonEventArgs args)
        {
            this.LogVerbose("button pressed: {buttonName}", args.Button.Name);
            
            try
            {
                // firing the Crestron defined event again here to allow for consuming devices to handle something directly
                ButtonStateChange?.Invoke(device, args);

                // firing the Essentials defined event to allow for consuming devices to handle something directly
                EssentialsButtonStateChange?.Invoke(this, args);                

                // checking if there's a delegate of some sort stored in the UserObject property on the Button Sig
                var handler = args.Button.UserObject;

                if (handler == null) {
                    this.LogDebug("No handler for button {buttonId}", args.Button.Name);
                    return;
                }

                this.LogDebug("Executing Action for button {buttonId}", args.Button.Name);

                if (handler is Action<bool> action)
                {
                    action(args.Button.State == eButtonState.Pressed);
                }
            }
            catch (Exception e)
            {
                this.LogError(e, "Error in ButtonStateChange handler");
            }
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new Hrxxx0WirelessRemoteControllerJoinMap(joinStart);

            var joinMapSerialized = JoinMapHelper.GetSerializedJoinMapForDevice(joinMapKey);

            if (!string.IsNullOrEmpty(joinMapSerialized))
                joinMap = JsonConvert.DeserializeObject<Hrxxx0WirelessRemoteControllerJoinMap>(joinMapSerialized);

            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }
            else
            {
                this.LogInformation("Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
            }

            //List<string> ExcludedKeys = new List<string>();
            foreach (var feedback in Feedbacks)
            {
                var myFeedback = feedback;

                var joinData =
                    joinMap.Joins.FirstOrDefault(
                        x => 
                            x.Key.Equals(myFeedback.Key, StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty((joinData.Key))) continue;

                var name = joinData.Key;
                var join = joinData.Value;

                if (join.Metadata.JoinType == eJoinType.Digital)
                {
                    this.LogInformation("Linking Bool Feedback '{Name}' to join {JoinNumber}", name, join.JoinNumber);
                    var someFeedback = myFeedback as BoolFeedback;
                    if(someFeedback == null) continue;
                    someFeedback.LinkInputSig(trilist.BooleanInput[join.JoinNumber]);
                }
                if (join.Metadata.JoinType == eJoinType.Analog)
                {
                    this.LogInformation("Linking Analog Feedback '{Name}' to join {JoinNumber}", name, join.JoinNumber);
                    var someFeedback = myFeedback as IntFeedback;
                    if (someFeedback == null) continue;
                    someFeedback.LinkInputSig(trilist.UShortInput[join.JoinNumber]);
                }
                if (join.Metadata.JoinType == eJoinType.Serial)
                {
                    this.LogInformation("Linking Serial Feedback '{Name}' to join {JoinNumber}", name, join.JoinNumber);
                    var someFeedback = myFeedback as StringFeedback;
                    if (someFeedback == null) continue;
                    someFeedback.LinkInputSig(trilist.StringInput[join.JoinNumber]);
                }
            }

            //var newJoinKeys = joinMap.Joins.Keys.Except(ExcludedKeys).ToList();

            //var newJoinMap = newJoinKeys.Where(k => joinMap.Joins.ContainsKey(k)).Select(k => joinMap.Joins[k]);


            this.LogVerbose("There are {ButtonCount} remote buttons", _remote.Button.Count);
            for (uint i = 1; i <= _remote.Button.Count; i++)
            {
                this.LogVerbose("Attempting to link join index {Index}", i);
                var index = i;
                var joinData =
                    joinMap.Joins.FirstOrDefault(
                        o =>
                            o.Key.Equals(_remote.Button[index].Name.ToString(),
                                StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty((joinData.Key))) continue;

                var join = joinData.Value;
                var name = joinData.Key;

                this.LogVerbose("Setting User Object for '{Name}'", name);
                if (join.Metadata.JoinType == eJoinType.Digital)
                {
                    _remote.Button[i].SetButtonAction((b) => trilist.BooleanInput[join.JoinNumber].BoolValue = b);
                }
            }

            trilist.OnlineStatusChange += (d, args) =>
            {
                if (!args.DeviceOnLine) return;

                foreach (var feedback in Feedbacks)
                {
                    feedback.FireUpdate();
                }
                
            };
        }
        public void SetTrilistBool(BasicTriList trilist, uint join, bool b)
        {
            trilist.BooleanInput[join].BoolValue = b;
        }

        #region IHR52Button Members

        public Button Custom9
        {
            get
            {
                var localRemote = (IHR52Button) _remote;
                return localRemote?.Custom9;
            }
        }

        public Button Favorite
        {
            get
            {
                var localRemote = (IHR52Button)_remote;
                return localRemote?.Favorite;
            }
        }
        

        public Button Home
        {
            get
            {
                var localRemote = (IHR52Button)_remote;
                return localRemote?.Home;
            }
        }

        #endregion

        #region IHR49Button Members

        public Button Clear
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Clear;
            }
        }

        public Button Custom5
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Custom5;
            }
        }

        public Button Custom6
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Custom6;
            }
        }

        public Button Custom7
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Custom7;
            }
        }

        public Button Custom8
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Custom8;
            }
        }

        public Button Enter
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Enter;
            }
        }

        public Button Keypad0
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Keypad0;
            }
        }

        public Button Keypad1
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Keypad1;
            }
        }

        public Button Keypad2Abc
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Keypad2Abc;
            }
        }

        public Button Keypad3Def
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Keypad3Def;
            }
        }

        public Button Keypad4Ghi
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Keypad4Ghi;
            }
        }

        public Button Keypad5Jkl
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Keypad5Jkl;
            }
        }

        public Button Keypad6Mno
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Keypad6Mno;
            }
        }

        public Button Keypad7Pqrs
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Keypad7Pqrs;
            }
        }

        public Button Keypad8Tuv
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Keypad8Tuv;
            }
        }

        public Button Keypad9Wxyz
        {
            get
            {
                var localRemote = (IHR49Button)_remote;
                return localRemote?.Keypad9Wxyz;
            }
        }

        #endregion

        #region IHR33Button Members

        public Button Blue
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Blue;
            }
        }

        public Button ChannelDown
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.ChannelDown;
            }
        }

        public Button ChannelUp
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.ChannelUp;
            }
        }

        public Button Custom1
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Custom1;
            }
        }

        public Button Custom2
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Custom2;
            }
        }

        public Button Custom3
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Custom3;
            }
        }

        public Button Custom4
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Custom4;
            }
        }

        public Button DialPadDown
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.DialPadDown;
            }
        }

        public Button DialPadEnter
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.DialPadEnter;
            }
        }

        public Button DialPadLeft
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.DialPadLeft;
            }
        }

        public Button DialPadRight
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.DialPadRight;
            }
        }

        public Button DialPadUp
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.DialPadUp;
            }
        }

        public Button Dvr
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Dvr;
            }
        }

        public Button Exit
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Exit;
            }
        }

        public Button FastForward
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.FastForward;
            }
        }

        public Button Green
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Green;
            }
        }

        public Button Guide
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Guide;
            }
        }

        public Button Information
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Information;
            }
        }

        public Button Last
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Last;
            }
        }

        public Button Menu
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Menu;
            }
        }

        public Button Mute
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Mute;
            }
        }

        public Button NextTrack
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.NextTrack;
            }
        }

        public Button Pause
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Pause;
            }
        }

        public Button Play
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Play;
            }
        }

        public Button Power
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Power;
            }
        }

        public Button PreviousTrack
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.PreviousTrack;
            }
        }

        public Button Record
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Record;
            }
        }

        public Button Red
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Red;
            }
        }

        public Button Rewind
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Rewind;
            }
        }

        public Button Stop
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Stop;
            }
        }

        public Button VolumeDown
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.VolumeDown;
            }
        }

        public Button VolumeUp
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.VolumeUp;
            }
        }

        public Button Yellow
        {
            get
            {
                var localRemote = (IHR33Button)_remote;
                return localRemote?.Yellow;
            }
        }

        #endregion

        #region IButton Members

        public CrestronCollection<Button> Button
        {
            get { return Buttons; }
        }

        public event ButtonEventHandler ButtonStateChange;

        public delegate void EssentialsButtonEventHandler(EssentialsDevice device, ButtonEventArgs args);

        public event EssentialsButtonEventHandler EssentialsButtonStateChange;

        #endregion
    }

    public class Hrxx0WirelessRemoteControllerFactory : EssentialsPluginDeviceFactory<Hrxx0WirelessRemoteController>
    {
        public Hrxx0WirelessRemoteControllerFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";

            TypeNames = new List<string>() { "hr100", "hr150", "hr310" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.LogDebug("Factory Attempting to create new HR-x00 Remote Device");

            return new Hrxx0WirelessRemoteController(dc.Key, dc.Name, GetHr1x0WirelessRemote, dc);
        }

        private Hr1x0WirelessRemoteBase GetHr1x0WirelessRemote(string type, CrestronRemotePropertiesConfig config)
        {         
            var rfId = config.Control.InfinetIdInt;

            GatewayBase gateway;

            if (config.GatewayDeviceKey == "processor")
            {
                gateway = Global.ControlSystem.ControllerRFGatewayDevice;
            }
            else
            {
                if (!(DeviceManager.GetDeviceForKey(config.GatewayDeviceKey) is CenRfgwController gatewayDev))
                {
                    Debug.LogWarning("GetHr1x0WirelessRemote: Device '{gatewayDeviceKey}' is not a valid device", config.GatewayDeviceKey);
                    return null;
                }

                Debug.LogWarning("GetHr1x0WirelessRemote: Device '{gatewayDeviceKey}' is a valid device", config.GatewayDeviceKey);

                gateway = gatewayDev.GateWay;
            }

            if (gateway == null)
            {
                Debug.LogWarning("GetHr1x0WirelessRemote: Device '{gatewayDeviceKey}' is not a valid gateway", config.GatewayDeviceKey);
                return null;
            }
           
            switch (type)
            {
                case ("hr100"):                    
                    return new Hr100(rfId, gateway);
                case ("hr150"):
                    return new Hr150(rfId, gateway);                    
                case ("hr310"):
                    return new Hr310(rfId, gateway);                    
                default:
                    return null;
            }
        }
    }
}
