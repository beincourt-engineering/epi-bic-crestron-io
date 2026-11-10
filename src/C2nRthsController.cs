using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.GeneralIO;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace PepperDash.Essentials.Plugins
{
    [Description("Wrapper class for the C2N-RTHS sensor")]
    public class C2nRthsController : CrestronGenericBridgeableBaseDevice, ITemperatureSensor, IHumiditySensor
    {
        private C2nRths _device;
        private readonly CTimer _pollTimer;
        
        public IntFeedback TemperatureFeedback { get; private set; }
        public BoolFeedback TemperatureInCFeedback { get; private set; }
        public IntFeedback HumidityFeedback { get; private set; }

        public C2nRthsController(string key, Func<DeviceConfig, C2nRths> preActivationFunc, DeviceConfig config)
            : base(key, config.Name)
        {
            // Refreshes the feedbacks 30 s after start, then every 5 minutes. TotalMilliseconds, not
            // Milliseconds: the latter is only the milliseconds part of the span, which is 0 here.
            _pollTimer = new CTimer(
                _ => RefreshFeedbacks(),
                this,
                (long)TimeSpan.FromSeconds(30).TotalMilliseconds,
                (long)TimeSpan.FromMinutes(5).TotalMilliseconds);
            
            AddPreActivationAction(() =>
            {
                _device = preActivationFunc(config);
                if (_device == null)
                {
                    this.LogError("Unable to create C2nRths Device");
                    return;
                }

                RegisterCrestronGenericBase(_device);
                    
                TemperatureFeedback = new IntFeedback("Temperature", () => _device.TemperatureFeedback.UShortValue);
                TemperatureInCFeedback = new BoolFeedback("TemperatureInC", () => _device.TemperatureFormat.BoolValue);
                HumidityFeedback = new IntFeedback("Humidity", () => _device.HumidityFeedback.UShortValue);
                
                Feedbacks.AddRange(new List<Feedback> { TemperatureFeedback, TemperatureInCFeedback, HumidityFeedback });
                _device.BaseEvent += DeviceOnBaseEvent;
                
                _device.OnlineStatusChange += (d, args) => 
                    this.LogDebug("Device status change... Online:{IsOnline} Temp:{Temperature} Humidity:{Humidity}", _device.IsOnline, _device.TemperatureFeedback.UShortValue, _device.HumidityFeedback.UShortValue);
            });
        }

        /// <summary>
        /// Updates every feedback on the list, keeping one that cannot be read from stopping the
        /// rest. Runs on the timer's thread, where an exception would otherwise go unhandled.
        /// </summary>
        private void RefreshFeedbacks()
        {
            foreach (var feedback in Feedbacks.ToList())
            {
                try
                {
                    feedback.FireUpdate();
                }
                catch (Exception ex)
                {
                    this.LogDebug("Feedback {FeedbackKey} could not be refreshed: {Message}", feedback.Key, ex.Message);
                }
            }
        }

        private void DeviceOnBaseEvent(GenericBase device, BaseEventArgs args)
        {
            switch (args.EventId)
            {
                case C2nRths.TemperatureFeedbackEventId:
                    TemperatureFeedback.FireUpdate();
                    TemperatureInCFeedback.FireUpdate();
                    break;
                case C2nRths.HumidityFeedbackEventId:
                    HumidityFeedback.FireUpdate();
                    break;
            }
        }

        public void SetTemperatureFormat(bool setToC)
        {
            _device.TemperatureFormat.BoolValue = setToC;

            TemperatureInCFeedback.FireUpdate();
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new C2nRthsControllerJoinMap(joinStart);

            var joinMapSerialized = JoinMapHelper.GetSerializedJoinMapForDevice(joinMapKey);

            if (!string.IsNullOrEmpty(joinMapSerialized))
                joinMap = JsonConvert.DeserializeObject<C2nRthsControllerJoinMap>(joinMapSerialized);

            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }
            else
            {
                this.LogInformation("Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
            }

            this.LogDebug("Linking to Trilist '{TrilistId}'", trilist.ID.ToString("X"));


            trilist.SetBoolSigAction(joinMap.TemperatureFormat.JoinNumber, SetTemperatureFormat);



            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            TemperatureFeedback.LinkInputSig(trilist.UShortInput[joinMap.Temperature.JoinNumber]);
            HumidityFeedback.LinkInputSig(trilist.UShortInput[joinMap.Humidity.JoinNumber]);

            trilist.StringInput[joinMap.Name.JoinNumber].StringValue = Name;

            trilist.OnlineStatusChange += (d, args) =>
            {
                if (!args.DeviceOnLine) return;

                UpdateFeedbacksWhenOnline();

                trilist.StringInput[joinMap.Name.JoinNumber].StringValue = Name;
            };
        }

        private void UpdateFeedbacksWhenOnline()
        {
            IsOnline.FireUpdate();
            TemperatureFeedback.FireUpdate();
            TemperatureInCFeedback.FireUpdate();
            HumidityFeedback.FireUpdate();
        }

        #region PreActivation

        private static C2nRths GetC2nRthsDevice(DeviceConfig dc)
        {
            var control = CommFactory.GetControlPropertiesConfig(dc);
            var cresnetId = control.CresnetIdInt;
            var branchId = control.ControlPortNumber;
            var parentKey = string.IsNullOrEmpty(control.ControlPortDevKey) ? "processor" : control.ControlPortDevKey;

            if (parentKey.Equals("processor", StringComparison.CurrentCultureIgnoreCase))
            {
                Debug.LogInformation("Device {ParentKey} is a valid cresnet master - creating new C2nRths", parentKey);
                return new C2nRths(cresnetId, Global.ControlSystem);
            }
            var cresnetBridge = DeviceManager.GetDeviceForKey(parentKey) as IHasCresnetBranches;

            if (cresnetBridge != null)
            {
                Debug.LogInformation("Device {ParentKey} is a valid cresnet master - creating new C2nRths", parentKey);
                return new C2nRths(cresnetId, cresnetBridge.CresnetBranches[(uint)branchId]);
            }
            Debug.LogInformation("Device {ParentKey} is not a valid cresnet master", parentKey);
            return null;
        }
        #endregion

        public class C2nRthsControllerFactory : EssentialsPluginDeviceFactory<C2nRthsController>
        {
            public C2nRthsControllerFactory()
            {
                MinimumEssentialsFrameworkVersion = "3.0.0";


                TypeNames = new List<string>() { "c2nrths" };
            }

            public override EssentialsDevice BuildDevice(DeviceConfig dc)
            {
                Debug.LogDebug("Factory Attempting to create new C2N-RTHS Device");

                return new C2nRthsController(dc.Key, GetC2nRthsDevice, dc);
            }
        }
    }
}