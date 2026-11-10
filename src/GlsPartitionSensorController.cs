using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.GeneralIO;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Bridges.JoinMaps;
using PepperDash.Essentials.Core.Config;
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugins
{
    [Description("Wrapper class for GLS Cresnet Partition Sensor")]
    public class GlsPartitionSensorController : CrestronGenericBridgeableBaseDevice, IPartitionStateProvider
    {
        public GlsPartitionSensorPropertiesConfig PropertiesConfig { get; private set; }

        private GlsPartCn _partitionSensor;

        public BoolFeedback EnableFeedback { get; private set; }
        public BoolFeedback PartitionPresentFeedback { get; private set; }

        public bool PartitionPresent
        {
            get
            {
                return InTestMode ? TestPartitionSensedFeedback : _partitionSensor.PartitionSensedFeedback.BoolValue;
            }
        }
        public BoolFeedback PartitionNotSensedFeedback { get; private set; }
        public IntFeedback SensitivityFeedback { get; private set; }

        public bool InTestMode { get; private set; }
        public bool TestEnableFeedback { get; private set; }
        public bool TestPartitionSensedFeedback { get; private set; }
        public int TestSensitivityFeedback { get; private set; }


        public GlsPartitionSensorController(string key, Func<DeviceConfig, GlsPartCn> preActivationFunc, DeviceConfig config)
            : base(key, config.Name)
        {

            var props = config.Properties.ToObject<GlsPartitionSensorPropertiesConfig>();
            if (props != null)
            {
                PropertiesConfig = props;
            }
            else
            {
                this.LogDebug("props are null.  Unable to deserialize into GlsPartSensorPropertiesConfig");
            }

            AddPreActivationAction(() =>
            {
                _partitionSensor = preActivationFunc(config);

                // On this thread, not a background one, so the hardware, its online feedback and the
                // communication monitor are all in place before activation uses them.
                if (_partitionSensor != null) RegisterCrestronGenericBase(_partitionSensor);

                EnableFeedback = new BoolFeedback(() => InTestMode ? TestEnableFeedback : _partitionSensor.EnableFeedback.BoolValue);
                PartitionPresentFeedback = new BoolFeedback("PartitionPresent", () => InTestMode ? TestPartitionSensedFeedback : _partitionSensor.PartitionSensedFeedback.BoolValue);
                PartitionNotSensedFeedback = new BoolFeedback(() => InTestMode ? !TestPartitionSensedFeedback : _partitionSensor.PartitionNotSensedFeedback.BoolValue);
                SensitivityFeedback = new IntFeedback(() => InTestMode ? TestSensitivityFeedback : _partitionSensor.SensitivityFeedback.UShortValue);

                if (_partitionSensor != null)
                {
                    _partitionSensor.BaseEvent += PartitionSensor_BaseEvent;
                    AddToFeedbackList(PartitionPresentFeedback);
                }
            });

            AddPostActivationAction(() =>
            {
                _partitionSensor.OnlineStatusChange += (o, a) =>
                {
                    if (a.DeviceOnLine)
                    {
                        ApplySettingsToSensorFromConfig();
                    }
                };

                if (_partitionSensor.IsOnline)
                {
                    ApplySettingsToSensorFromConfig();
                }

                CrestronEnvironment.ProgramStatusEventHandler += HandleProgramStatusEvent;
            });
        }

        private void ApplySettingsToSensorFromConfig()
        {
            if (_partitionSensor.IsOnline == false) return;

            this.LogDebug("Attempting to apply settings to sensor from config");

            if (PropertiesConfig.Sensitivity != null)
            {
                this.LogDebug("Sensitivity found, attempting to set value '{Sensitivity}' from config",
                    PropertiesConfig.Sensitivity);
                _partitionSensor.Sensitivity.UShortValue = (ushort)PropertiesConfig.Sensitivity;
            }
            else
            {
                this.LogDebug("Sensitivity null, no value specified in config");
            }

            if (PropertiesConfig.EnableSensor != null)
            {
                this.LogDebug("Enable found, attempting to set value '{EnableSensor}' from config",
                    PropertiesConfig.EnableSensor);

                _partitionSensor.Enable.BoolValue = PropertiesConfig.EnableSensor.Value;
            }
            else
            {
                this.LogDebug("Enable Null, no value specific in config. Enable MUST be set using SetEnableState to use sensor");
            }

        }

        private void PartitionSensor_BaseEvent(GenericBase device, BaseEventArgs args)
        {
            this.LogVerbose("EventId: {EventId}, Index: {Index}", args.EventId, args.Index);

            switch (args.EventId)
            {
                case (GlsPartCn.EnableFeedbackEventId):
                    {
                        EnableFeedback.FireUpdate();
                        break;
                    }
                case (GlsPartCn.PartitionSensedFeedbackEventId):
                    {
                        this.LogDebug("Partition Sensed State: {PartitionSensed}", _partitionSensor.PartitionSensedFeedback.BoolValue);
                        PartitionPresentFeedback.FireUpdate();
                        break;
                    }
                case (GlsPartCn.PartitionNotSensedFeedbackEventId):
                    {
                        this.LogDebug("Partition Not Sensed State: {PartitionNotSensed}", _partitionSensor.PartitionNotSensedFeedback.BoolValue);
                        PartitionNotSensedFeedback.FireUpdate();
                        break;
                    }
                case (GlsPartCn.SensitivityFeedbackEventId):
                    {
                        SensitivityFeedback.FireUpdate();
                        break;
                    }
                default:
                    {
                        this.LogVerbose("Unhandled args.EventId: {EventId}", args.EventId);
                        break;
                    }
            }
        }

        public void SetTestMode(bool mode)
        {
            InTestMode = mode;
            this.LogDebug("InTestMode: {InTestMode}", InTestMode.ToString());
        }

        public void SetTestEnableState(bool state)
        {
            if (InTestMode)
            {
                TestEnableFeedback = state;

                EnableFeedback.FireUpdate();

                this.LogDebug("TestEnableFeedback: {TestEnableFeedback}", TestEnableFeedback.ToString());
                return;
            }

            this.LogDebug("InTestMode: {InTestMode}, unable to set enable state: {State}", InTestMode.ToString(), state.ToString());
        }

        public void SetTestPartitionSensedState(bool state)
        {
            if (InTestMode)
            {
                TestPartitionSensedFeedback = state;

                PartitionPresentFeedback.FireUpdate();
                PartitionNotSensedFeedback.FireUpdate();

                this.LogDebug("TestPartitionSensedFeedback: {TestPartitionSensedFeedback}", TestPartitionSensedFeedback.ToString());
                return;
            }

            this.LogDebug("InTestMode: {InTestMode}, unable to set partition state: {State}", InTestMode.ToString(), state.ToString());
        }

        public void SetTestSensitivityValue(int value)
        {
            if (InTestMode)
            {
                TestSensitivityFeedback = value;

                SensitivityFeedback.FireUpdate();
                this.LogDebug("TestSensitivityFeedback: {TestSensitivityFeedback}", TestSensitivityFeedback);
                return;
            }

            this.LogDebug("InTestMode: {InTestMode}, unable to set sensitivity value: {Value}", InTestMode.ToString(), value);
        }

        public void GetSettings()
        {
            var dash = new string('*', 50);
            CrestronConsole.PrintLine(string.Format("{0}\n", dash));

            this.LogInformation("Enabled State: {EnabledState}", _partitionSensor.EnableFeedback.BoolValue);

            this.LogInformation("Partition Sensed State: {PartitionSensed}", _partitionSensor.PartitionSensedFeedback.BoolValue);
            this.LogInformation("Partition Not Sensed State: {PartitionNotSensed}", _partitionSensor.PartitionNotSensedFeedback.BoolValue);

            this.LogInformation("Sensitivity Value: {SensitivityValue}", _partitionSensor.SensitivityFeedback.UShortValue);

            CrestronConsole.PrintLine(string.Format("{0}\n", dash));
        }

        public void SetEnableState(bool state)
        {
            this.LogVerbose("Sensor is {SensorState}, SetEnableState: {State}", _partitionSensor == null ? "null" : "not null", state);
            if (_partitionSensor == null)
                return;

            _partitionSensor.Enable.BoolValue = state;
        }

        public void IncreaseSensitivity()
        {
            this.LogVerbose("Sensor is {SensorState}, IncreaseSensitivity", _partitionSensor == null ? "null" : "not null");
            if (_partitionSensor == null)
                return;

            _partitionSensor.IncreaseSensitivity();
        }

        public void DecreaseSensitivity()
        {
            this.LogVerbose("Sensor is {SensorState}, DecreaseSensitivity", _partitionSensor == null ? "null" : "not null");
            if (_partitionSensor == null)
                return;

            _partitionSensor.DecreaseSensitivity();
        }

        public void SetSensitivity(ushort value)
        {
            this.LogVerbose("Sensor is {SensorState}, SetSensitivity: {Value}", _partitionSensor == null ? "null" : "not null", value);
            if (_partitionSensor == null)
                return;

            _partitionSensor.Sensitivity.UShortValue = value;
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new GlsPartitionSensorJoinMap(joinStart);
            var joinMapSerialized = JoinMapHelper.GetSerializedJoinMapForDevice(joinMapKey);

            if (!string.IsNullOrEmpty(joinMapSerialized))
                joinMap = JsonConvert.DeserializeObject<GlsPartitionSensorJoinMap>(joinMapSerialized);

            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }
            else
            {
                this.LogInformation("Please update config to use 'type': 'EiscApiAdvanced' to get all join map features for this device");
            }

            this.LogDebug("Linking to Trilist '{TrilistId}'", trilist.ID.ToString("X"));
            this.LogInformation("Linking to Bridge Type {BridgeType}", GetType().Name);

            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            trilist.StringInput[joinMap.Name.JoinNumber].StringValue = _partitionSensor.Name;

            trilist.SetBoolSigAction(joinMap.Enable.JoinNumber, SetEnableState);
            EnableFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Enable.JoinNumber]);

            PartitionPresentFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PartitionSensed.JoinNumber]);
            PartitionNotSensedFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PartitionNotSensed.JoinNumber]);

            trilist.SetSigTrueAction(joinMap.IncreaseSensitivity.JoinNumber, IncreaseSensitivity);
            trilist.SetSigTrueAction(joinMap.DecreaseSensitivity.JoinNumber, DecreaseSensitivity);

            SensitivityFeedback.LinkInputSig(trilist.UShortInput[joinMap.Sensitivity.JoinNumber]);
            trilist.SetUShortSigAction(joinMap.Sensitivity.JoinNumber, SetSensitivity);

            FeedbacksFireUpdates();

            // update when device is online
            _partitionSensor.OnlineStatusChange += (o, a) =>
            {
                if (a.DeviceOnLine)
                {
                    FeedbacksFireUpdates();
                }
            };

            // update when trilist is online
            trilist.OnlineStatusChange += (o, a) =>
            {
                if (a.DeviceOnLine)
                {
                    trilist.StringInput[joinMap.Name.JoinNumber].StringValue = _partitionSensor.Name;
                    FeedbacksFireUpdates();
                }
            };
        }

        private void HandleProgramStatusEvent(eProgramStatusEventType programEventType)
        {
            if (programEventType != eProgramStatusEventType.Stopping) return;

            this.LogDebug("Program stopping - unregistering partition sensor");

            if (_partitionSensor == null) return;

            _partitionSensor.BaseEvent -= PartitionSensor_BaseEvent;
            _partitionSensor.UnRegister();
        }

        private void FeedbacksFireUpdates()
        {
            IsOnline.FireUpdate();
            EnableFeedback.FireUpdate();
            PartitionPresentFeedback.FireUpdate();
            PartitionNotSensedFeedback.FireUpdate();
            SensitivityFeedback.FireUpdate();
        }

        #region PreActivation

        private static GlsPartCn GetGlsPartCnDevice(DeviceConfig dc)
        {
            var control = CommFactory.GetControlPropertiesConfig(dc);
            var cresnetId = control.CresnetIdInt;
            var branchId = control.ControlPortNumber;
            var parentKey = string.IsNullOrEmpty(control.ControlPortDevKey) ? "processor" : control.ControlPortDevKey;

            if (parentKey.Equals("processor", StringComparison.CurrentCultureIgnoreCase))
            {
                Debug.LogInformation("Device {ParentKey} is a valid cresnet master - creating new GlsPartCn", parentKey);
                return new GlsPartCn(cresnetId, Global.ControlSystem);
            }

            if (DeviceManager.GetDeviceForKey(parentKey) is IHasCresnetBranches cresnetBridge)
            {
                Debug.LogInformation("Device {ParentKey} is a valid cresnet master - creating new GlsPartCn", parentKey);
                return new GlsPartCn(cresnetId, cresnetBridge.CresnetBranches[(uint)branchId]);
            }
            Debug.LogInformation("Device {ParentKey} is not a valid cresnet master", parentKey);
            return null;
        }
        #endregion


        public class GlsPartitionSensorControllerFactory : EssentialsPluginDeviceFactory<GlsPartitionSensorController>
        {


            public GlsPartitionSensorControllerFactory()
            {
                MinimumEssentialsFrameworkVersion = "3.0.0";

                TypeNames = new List<string> { "glspartcn" };
            }

            public override EssentialsDevice BuildDevice(DeviceConfig dc)
            {
                Debug.LogDebug("Factory Attempting to create new GlsPartitionSensorController Device");

                return new GlsPartitionSensorController(dc.Key, GetGlsPartCnDevice, dc);
            }
        }

    }
}