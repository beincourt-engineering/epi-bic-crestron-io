using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Lighting;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace PepperDash.Essentials.Plugins
{
    /// <summary>
    /// Wrapper for the CLW-DIMFLVEX-P infiNET EX single-load dimmer. The dimmer is reached through an
    /// infiNET EX gateway device (gatewayDeviceKey), or the processor's own gateway ("processor"), at
    /// the RF ID in control.infinetId (hex). Its public methods (FullOn, Off, SetLevel) can be called
    /// from config device actions.
    /// </summary>
    [Description("Wrapper class for the CLW-DIMFLVEX-P infiNET EX dimmer")]
    public class ClwDimFlvExPController : EssentialsDevice, IHasFeedback, ICommunicationMonitor
    {
        private const uint LoadNumber = 1;

        private ClwDimFlvExP _dimmer;

        public FeedbackCollection<Feedback> Feedbacks { get; } = new FeedbackCollection<Feedback>();

        /// <summary>
        /// Follows the dimmer's online state. Created with the dimmer once every device has
        /// activated, so it is in place before any device initializes; it stays null when the
        /// dimmer cannot be created.
        /// </summary>
        public StatusMonitorBase CommunicationMonitor { get; private set; }

        /// <summary>True while the dimmer is registered and online through its gateway.</summary>
        public BoolFeedback IsOnlineFeedback { get; }

        /// <summary>True while the load is on at any level.</summary>
        public BoolFeedback IsOnFeedback { get; }

        /// <summary>The load level, 0-65535.</summary>
        public IntFeedback LevelFeedback { get; }

        public ClwDimFlvExPController(string key, string name, Func<CrestronRemotePropertiesConfig, ClwDimFlvExP> postActivationFunc,
            DeviceConfig config)
            : base(key, name)
        {
            var props = config.Properties.ToObject<CrestronRemotePropertiesConfig>();

            IsOnlineFeedback = new BoolFeedback("IsOnline", () => _dimmer != null && _dimmer.IsOnline);
            IsOnFeedback = new BoolFeedback("IsOn", () => Load != null && Load.IsOn);
            LevelFeedback = new IntFeedback("Level", () => Load != null ? Load.LevelFeedback.UShortValue : 0);
            Feedbacks.Add(IsOnlineFeedback);
            Feedbacks.Add(IsOnFeedback);
            Feedbacks.Add(LevelFeedback);

            // The gateway device creates its hardware during pre-activation, so the dimmer is created
            // and registered after every device has activated, as the HR remotes are.
            AddPostActivationAction(() =>
            {
                _dimmer = postActivationFunc(props);
                if (_dimmer == null)
                    return;

                _dimmer.OnlineStatusChange += (d, a) =>
                {
                    this.LogInformation("Dimmer is {status}", a.DeviceOnLine ? "online" : "offline");
                    IsOnlineFeedback.FireUpdate();
                };
                _dimmer.LoadStateChange += (d, a) =>
                {
                    IsOnFeedback.FireUpdate();
                    LevelFeedback.FireUpdate();
                };

                if (_dimmer.Registerable)
                    _dimmer.RegisterWithLogging(Key);

                CommunicationMonitor = new CrestronGenericBaseCommunicationMonitor(this, _dimmer, 120000, 300000);
                CommunicationMonitor.Start();
            });
        }

        private ClwDimFlvExPLoad Load =>
            _dimmer != null && _dimmer.DimmingLoads.Contains(LoadNumber) ? _dimmer.DimmingLoads[LoadNumber] : null;

        /// <summary>Fades the load to full on.</summary>
        public void FullOn()
        {
            if (!CheckLoad(nameof(FullOn))) return;
            this.LogInformation("Full on");
            Load.FullOn();
        }

        /// <summary>Fades the load to off.</summary>
        public void Off()
        {
            if (!CheckLoad(nameof(Off))) return;
            this.LogInformation("Off");
            Load.FullOff();
        }

        /// <summary>Sets the load level, 0-65535.</summary>
        public void SetLevel(ushort level)
        {
            if (!CheckLoad(nameof(SetLevel))) return;
            this.LogInformation("Level {level}", level);
            Load.Level.UShortValue = level;
        }

        private bool CheckLoad(string what)
        {
            if (Load != null) return true;
            this.LogWarning("{what} ignored: the dimmer was not created (check gatewayDeviceKey and control.infinetId)", what);
            return false;
        }
    }

    public class ClwDimFlvExPControllerFactory : EssentialsPluginDeviceFactory<ClwDimFlvExPController>
    {
        public ClwDimFlvExPControllerFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";

            TypeNames = new List<string>() { "clwdimflvexp" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.LogDebug("Factory Attempting to create new CLW-DIMFLVEX-P Device");

            return new ClwDimFlvExPController(dc.Key, dc.Name, GetDimmer, dc);
        }

        private static ClwDimFlvExP GetDimmer(CrestronRemotePropertiesConfig config)
        {
            var rfId = config.Control.InfinetIdInt;

            GatewayBase gateway;

            if (config.GatewayDeviceKey == "processor")
            {
                gateway = Global.ControlSystem.ControllerRFGatewayDevice;
            }
            else if (DeviceManager.GetDeviceForKey(config.GatewayDeviceKey) is CenRfgwController gatewayDev)
            {
                gateway = gatewayDev.GateWay;
            }
            else
            {
                Debug.LogWarning("GetDimmer: Device '{gatewayDeviceKey}' is not a valid gateway device", config.GatewayDeviceKey);
                return null;
            }

            if (gateway == null)
            {
                Debug.LogWarning("GetDimmer: Device '{gatewayDeviceKey}' has no gateway hardware", config.GatewayDeviceKey);
                return null;
            }

            return new ClwDimFlvExP(rfId, gateway);
        }
    }
}
