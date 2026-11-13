using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Gateways;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugins
{
    [Description("Wrapper class for Crestron Infinet-EX Gateways")]
    public class CenRfgwController : CrestronGenericBaseDevice
    {
        private GatewayBase _gateway;

        public GatewayBase GateWay
        {
            get { return _gateway; }
        }

        /// <summary>
        /// Constructor for the on-board gateway
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="gateway"></param>
        public CenRfgwController(string key, string name, GatewayBase gateway) :
            base(key, name, gateway)
        {
            _gateway = gateway;            
        }

        public CenRfgwController(string key, string name, Func<string, EssentialsRfGatewayConfig, GatewayBase> preActivationFunc, DeviceConfig config) :
            base(key, name)
        {
            var props = config.Properties.ToObject<EssentialsRfGatewayConfig>();

            AddPreActivationAction(() =>
            {
                _gateway = preActivationFunc(config.Type, props);
                
                RegisterCrestronGenericBase(_gateway);                
            });
        }
    }

    public enum EExGatewayType
    {
        Ethernet,
        EthernetShared,
        Cresnet
    }


    

    public class CenRfgwControllerFactory : EssentialsPluginDeviceFactory<CenRfgwController>
    {
        public CenRfgwControllerFactory()
        {
            MinimumEssentialsFrameworkVersion = "3.0.0";

            TypeNames = new List<string> { "cenrfgwex", "cenerfgwpoe", "cengwexer", "internal", "cengw1" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.LogDebug("Factory Attempting to create new RF Gateway Device");

            switch (dc.Type.ToLowerInvariant())
            {
                case "internal":
                    if(!Global.ControlSystem.SupportsInternalRFGateway)
                    { 
                        Debug.LogError("Internal RF Gateway is not supported on this processor");
                        return null;
                    }
                    return new CenRfgwController(dc.Key, dc.Name, Global.ControlSystem.ControllerRFGatewayDevice);
                default:
                    return new CenRfgwController(dc.Key, dc.Name, GetRfGatewayDevice, dc);
            }            
        }

        private static GatewayBase GetRfGatewayDevice(string type, EssentialsRfGatewayConfig props)
        {            
            switch (props.GatewayType)
            {
                case (EExGatewayType.Ethernet):
                case (EExGatewayType.EthernetShared):
                    return BuildIpRfGateway(type, props.Control, props.GatewayType == EExGatewayType.EthernetShared);
                case (EExGatewayType.Cresnet):
                    return BuildCresnetRfGateway(type, props.Control);
                default:
                    return null;
            }
        }

        private static GatewayBase BuildCresnetRfGateway(string type, EssentialsControlPropertiesConfig control)
        {               
            var cresnetId = control.CresnetIdInt;

            var branchId = control.ControlPortNumber ?? 1; //assume 1 if ControlPortNumber is ommited;

            var parentKey = string.IsNullOrEmpty(control.ControlPortDevKey) ? "processor" : control.ControlPortDevKey;

            switch (type.ToLowerInvariant())
            {
                case "cenerfgwpoe":
                    {                        
                        if (DeviceManager.GetDeviceForKey(parentKey) is ICresnetBridge cresnetBridge)
                        {
                            return new CenErfgwPoeCresnet(cresnetId, cresnetBridge.Branches[branchId]);
                        }

                        return new CenErfgwPoeCresnet(cresnetId, Global.ControlSystem);                        
                    }
                case "cenrfgwex":
                    {
                        if (DeviceManager.GetDeviceForKey(parentKey) is ICresnetBridge cresnetBridge)
                        {
                            return new CenRfgwExCresnet(cresnetId, cresnetBridge.Branches[branchId]);
                        }
                        return new CenRfgwExCresnet(cresnetId, Global.ControlSystem);
                    }
                default:
                    Debug.LogWarning("Device {device} is not a valid cresnet RF gateway type", type);
                    return null;
            }
        }

        private static GatewayBase BuildIpRfGateway(string type, EssentialsControlPropertiesConfig control, bool shareable)
        {           
            var ipId = control.IpIdInt;
            
            switch (type.ToLowerInvariant())
            {
                case "cengwexer":
                    {
                        if (shareable)
                            return new CenGwExErEthernetSharable(ipId, Global.ControlSystem);

                        return new CenGwExEr(ipId, Global.ControlSystem);
                    }
                case "cenerfgwpoe":
                    {
                        if (shareable)
                            return new CenErfgwPoeEthernetSharable(ipId, Global.ControlSystem);

                        return new CenErfgwPoe(ipId, Global.ControlSystem);
                    }
                case "cenrfgwex":
                    {
                        if (shareable)
                            return new CenRfgwExEthernetSharable(ipId, Global.ControlSystem);
                        return new CenRfgwEx(ipId, Global.ControlSystem);
                    }
                case "cengw1":
                    {
                        return new CenGw1(ipId, Global.ControlSystem);
                    }
                default:
                    Debug.LogWarning("Device {device} is not a valid ethernet RF gateway", type);
                    return null;
            }
        }
    }

}


