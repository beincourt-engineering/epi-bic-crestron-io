using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.GeneralIO;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugins
{
	[Description("Wrapper class for CEN-ODT-C-POE")]
    [ConfigSnippet("\"properties\": {\"control\": {\"method\": \"cresnet\",\"cresnetId\": \"97\"},\"enablePir\": true,\"enableLedFlash\": true,\"enableRawStates\":true,\"remoteTimeout\": 30,\"internalPhotoSensorMinChange\": 0,\"externalPhotoSensorMinChange\": 0,\"enableUsA\": true,\"enableUsB\": true,\"orWhenVacatedState\": true}")]
	public class CenOdtOccupancySensorBaseController : CrestronGenericBridgeableBaseDevice, IOccupancyStatusProvider
	{
		public CenOdtCPoe OccSensor { get; private set; }

        public GlsOccupancySensorPropertiesConfig PropertiesConfig { get; private set; }

		public BoolFeedback RoomIsOccupiedFeedback { get; private set; }

		public BoolFeedback GraceOccupancyDetectedFeedback { get; private set; }

		public BoolFeedback RawOccupancyFeedback { get; private set; }

		public BoolFeedback PirSensorEnabledFeedback { get; private set; }

		public BoolFeedback LedFlashEnabledFeedback { get; private set; }

		public BoolFeedback ShortTimeoutEnabledFeedback { get; private set; }

		public IntFeedback PirSensitivityInVacantStateFeedback { get; private set; }

		public IntFeedback PirSensitivityInOccupiedStateFeedback { get; private set; }

		public IntFeedback CurrentTimeoutFeedback { get; private set; }

		public IntFeedback RemoteTimeoutFeedback { get; private set; }

		public IntFeedback InternalPhotoSensorValue { get; set; }

		public IntFeedback ExternalPhotoSensorValue { get; set; }

		public BoolFeedback OrWhenVacatedFeedback { get; private set; }

		public BoolFeedback AndWhenVacatedFeedback { get; private set; }

		public BoolFeedback UltrasonicAEnabledFeedback { get; private set; }

		public BoolFeedback UltrasonicBEnabledFeedback { get; private set; }

		public IntFeedback UltrasonicSensitivityInVacantStateFeedback { get; private set; }

		public IntFeedback UltrasonicSensitivityInOccupiedStateFeedback { get; private set; }

		public BoolFeedback RawOccupancyPirFeedback { get; private set; }

		public BoolFeedback RawOccupancyUsFeedback { get; private set; }

        public BoolFeedback IdentityModeFeedback { get; private set; }

		// Debug properties
		public bool InTestMode { get; private set; }

		public bool TestRoomIsOccupiedFeedback { get; private set; }

		public Func<bool> RoomIsOccupiedFeedbackFunc
		{
			get
			{
				return () => InTestMode ? TestRoomIsOccupiedFeedback : OccSensor.OccupancyDetectedFeedback.BoolValue;
			}
		}

		public CenOdtOccupancySensorBaseController(string key, string name, CenOdtCPoe sensor, GlsOccupancySensorPropertiesConfig config)
			: base(key, name, sensor)
		{
            PropertiesConfig = config;

			OccSensor = sensor;

			RoomIsOccupiedFeedback = new BoolFeedback("RoomIsOccupied", RoomIsOccupiedFeedbackFunc);
			AddToFeedbackList(RoomIsOccupiedFeedback);

			PirSensorEnabledFeedback = new BoolFeedback(() => OccSensor.PassiveInfraredSensorEnabledFeedback.BoolValue);

			LedFlashEnabledFeedback = new BoolFeedback(() => OccSensor.LedFlashEnabledFeedback.BoolValue);

			ShortTimeoutEnabledFeedback = new BoolFeedback(() => OccSensor.ShortTimeoutEnabledFeedback.BoolValue);

			PirSensitivityInVacantStateFeedback = new IntFeedback(() => (int)OccSensor.PassiveInfraredSensorSensitivityInVacantStateFeedback);

			PirSensitivityInOccupiedStateFeedback = new IntFeedback(() => (int)OccSensor.PassiveInfraredSensorSensitivityInOccupiedStateFeedback);

			CurrentTimeoutFeedback = new IntFeedback(() => OccSensor.CurrentTimeoutFeedback.UShortValue);

			RemoteTimeoutFeedback = new IntFeedback(() => OccSensor.RemoteTimeout.UShortValue);

			GraceOccupancyDetectedFeedback = new BoolFeedback(() => OccSensor.GraceOccupancyDetectedFeedback.BoolValue);

			RawOccupancyFeedback = new BoolFeedback(() => OccSensor.RawOccupancyDetectedFeedback.BoolValue);

			InternalPhotoSensorValue = new IntFeedback(() => OccSensor.InternalPhotoSensorValueFeedback.UShortValue);

			//ExternalPhotoSensorValue = new IntFeedback(() => OccSensor.ex.UShortValue);

			AndWhenVacatedFeedback = new BoolFeedback(() => OccSensor.AndWhenVacatedFeedback.BoolValue);

			OrWhenVacatedFeedback = new BoolFeedback(() => OccSensor.OrWhenVacatedFeedback.BoolValue);

			UltrasonicAEnabledFeedback = new BoolFeedback(() => OccSensor.UltrasonicSensorSideAEnabledFeedback.BoolValue);

			UltrasonicBEnabledFeedback = new BoolFeedback(() => OccSensor.UltrasonicSensorSideBEnabledFeedback.BoolValue);

			RawOccupancyPirFeedback = new BoolFeedback(() => OccSensor.RawOccupancyDetectedByPassiveInfraredSensorFeedback.BoolValue);

			RawOccupancyUsFeedback = new BoolFeedback(() => OccSensor.RawOccupancyDetectedByUltrasonicSensorFeedback.BoolValue);

            IdentityModeFeedback = new BoolFeedback(()=>OccSensor.IdentityModeOnFeedback.BoolValue);

			UltrasonicSensitivityInVacantStateFeedback = new IntFeedback(() => (int)OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback);

			UltrasonicSensitivityInOccupiedStateFeedback = new IntFeedback(() => (int)OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback);

			OccSensor.BaseEvent += new global::Crestron.SimplSharpPro.BaseEventHandler(OccSensor_BaseEvent);

			OccSensor.CenOccupancySensorChange += new GenericEventHandler(OccSensor_CenOccupancySensorChange);

            AddPostActivationAction(() =>
            {
                OccSensor.OnlineStatusChange += (o, a) =>
                {
                    if (a.DeviceOnLine)
                    {
                        ApplySettingsToSensorFromConfig();
                    }
                };

                if (OccSensor.IsOnline)
                {
                    ApplySettingsToSensorFromConfig();

                }
            }); 
		}

        /// <summary>
        /// Applies any sensor settings defined in config 
        /// </summary>
        protected virtual void ApplySettingsToSensorFromConfig()
        {
            this.LogDebug("Checking config for settings to apply");

            if (PropertiesConfig.EnablePir != null)
            {
                SetPirEnable((bool)PropertiesConfig.EnablePir);
            }

            if (PropertiesConfig.EnableLedFlash != null)
            {
                SetLedFlashEnable((bool)PropertiesConfig.EnableLedFlash);
            }

            if (PropertiesConfig.RemoteTimeout != null)
            {
                SetRemoteTimeout((ushort)PropertiesConfig.RemoteTimeout);
            }

            if (PropertiesConfig.ShortTimeoutState != null)
            {
                SetShortTimeoutState((bool)PropertiesConfig.ShortTimeoutState);
            }

            if (PropertiesConfig.EnableRawStates != null)
            {
                EnableRawStates((bool)PropertiesConfig.EnableRawStates);
            }

            if (PropertiesConfig.InternalPhotoSensorMinChange != null)
            {
                SetInternalPhotoSensorMinChange((ushort)PropertiesConfig.InternalPhotoSensorMinChange);
            }

            if (PropertiesConfig.EnableUsA != null)
            {
                SetUsAEnable((bool)PropertiesConfig.EnableUsA);
            }

            if (PropertiesConfig.EnableUsB != null)
            {
                SetUsBEnable((bool)PropertiesConfig.EnableUsB);
            }

            if (PropertiesConfig.OrWhenVacatedState != null)
            {
                SetOrWhenVacatedState((bool)PropertiesConfig.OrWhenVacatedState);
            }

            if (PropertiesConfig.AndWhenVacatedState != null)
            {
                SetAndWhenVacatedState((bool)PropertiesConfig.AndWhenVacatedState);
            }

            // TODO [ ] feature/cenoodtcpoe-sensor-sensitivity-configuration
            if (PropertiesConfig.UsSensitivityOccupied != null)
            {
                SetUsSensitivityOccupied((ushort)PropertiesConfig.UsSensitivityOccupied);
            }

            if (PropertiesConfig.UsSensitivityVacant != null)
            {
                SetUsSensitivityVacant((ushort)PropertiesConfig.UsSensitivityVacant);
            }

            if (PropertiesConfig.PirSensitivityOccupied != null)
            {
                SetPirSensitivityOccupied((ushort)PropertiesConfig.PirSensitivityOccupied);
            }

            if (PropertiesConfig.PirSensitivityVacant != null)
            {
                SetPirSensitivityVacant((ushort)PropertiesConfig.PirSensitivityVacant);
            }
        }

		/// <summary>
		/// Catches events for feedbacks on the base class.  Any extending wrapper class should call this delegate after it checks for it's own event IDs.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="args"></param>
		protected virtual void OccSensor_CenOccupancySensorChange(object device, GenericEventArgs args)
		{
			if (args.EventId == GlsOccupancySensorBase.PirEnabledFeedbackEventId)
				PirSensorEnabledFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.LedFlashEnabledFeedbackEventId)
				LedFlashEnabledFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.ShortTimeoutEnabledFeedbackEventId)
				ShortTimeoutEnabledFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.PirSensitivityInOccupiedStateFeedbackEventId)
				PirSensitivityInOccupiedStateFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.PirSensitivityInVacantStateFeedbackEventId)
				PirSensitivityInVacantStateFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.RawOccupancyPirFeedbackEventId)
				RawOccupancyPirFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.RawOccupancyUsFeedbackEventId)
				RawOccupancyUsFeedback.FireUpdate();
		}

		protected virtual void OccSensor_BaseEvent(global::Crestron.SimplSharpPro.GenericBase device, global::Crestron.SimplSharpPro.BaseEventArgs args)
		{
			this.LogVerbose("PoEOccupancySensorChange EventId: {EventId}", args.EventId);

			if (args.EventId == global::Crestron.SimplSharpPro.GeneralIO.GlsOccupancySensorBase.RoomOccupiedFeedbackEventId
				|| args.EventId == global::Crestron.SimplSharpPro.GeneralIO.GlsOccupancySensorBase.RoomVacantFeedbackEventId)
			{
				this.LogDebug("Occupancy State: {OccupancyState}", OccSensor.OccupancyDetectedFeedback.BoolValue);
				RoomIsOccupiedFeedback.FireUpdate();
			}
			else if (args.EventId == GlsOccupancySensorBase.TimeoutFeedbackEventId)
				CurrentTimeoutFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.TimeoutLocalFeedbackEventId)
				RemoteTimeoutFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.GraceOccupancyDetectedFeedbackEventId)
				GraceOccupancyDetectedFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.RawOccupancyFeedbackEventId)
				RawOccupancyFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.InternalPhotoSensorValueFeedbackEventId)
				InternalPhotoSensorValue.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.ExternalPhotoSensorValueFeedbackEventId)
				ExternalPhotoSensorValue.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.AndWhenVacatedFeedbackEventId)
				AndWhenVacatedFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.OrWhenVacatedFeedbackEventId)
				OrWhenVacatedFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.UsAEnabledFeedbackEventId)
				UltrasonicAEnabledFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.UsBEnabledFeedbackEventId)
				UltrasonicBEnabledFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.UsSensitivityInOccupiedStateFeedbackEventId)
				UltrasonicSensitivityInOccupiedStateFeedback.FireUpdate();
			else if (args.EventId == GlsOccupancySensorBase.UsSensitivityInVacantStateFeedbackEventId)
				UltrasonicSensitivityInVacantStateFeedback.FireUpdate();
		}

		public void SetTestMode(bool mode)
		{
			InTestMode = mode;

			this.LogDebug("In Mock Mode: '{InTestMode}'", InTestMode);
		}

		public void SetTestOccupiedState(bool state)
		{
			if (!InTestMode)
				this.LogDebug("Mock mode not enabled");
			else
			{
				TestRoomIsOccupiedFeedback = state;

				RoomIsOccupiedFeedback.FireUpdate();
			}
		}

        /// <summary>
        /// Sets the identity mode on or off
        /// </summary>
        /// <param name="state"></param>
	    public void SetIdentityMode(bool state)
        {
            if (state)
                OccSensor.IdentityModeOn();
            else
                OccSensor.IdentityModeOff();

            this.LogDebug("Identity Mode: {IdentityMode}", OccSensor.IdentityModeOnFeedback.BoolValue ? "On" : "Off");
        }

	    /// <summary>
		/// Enables or disables the PIR sensor
		/// </summary>
		/// <param name="state"></param>
		public void SetPirEnable(bool state)
		{
			if (state)
			{
				OccSensor.EnablePassiveInfraredSensor();
			}
			else
			{
				OccSensor.DisablePassiveInfraredSensor();
			}
		}

		/// <summary>
		/// Enables or disables the LED Flash
		/// </summary>
		/// <param name="state"></param>
		public void SetLedFlashEnable(bool state)
		{
			if (state)
			{
				OccSensor.EnableLedFlash();
			}
			else
			{
				OccSensor.DisableLedFlash();
			}
		}

		/// <summary>
		/// Enables or disables short timeout based on state
		/// </summary>
		/// <param name="state"></param>
		public void SetShortTimeoutState(bool state)
		{
			if (state)
			{
				OccSensor.EnableShortTimeout();
			}
			else
			{
				OccSensor.DisableShortTimeout();
			}
		}

		public void IncrementPirSensitivityInOccupiedState(bool pressRelease)
		{
			if ((int)OccSensor.PassiveInfraredSensorSensitivityInOccupiedStateFeedback != 3)
			{
				OccSensor.PassiveInfraredSensorSensitivityInOccupiedState = OccSensor.PassiveInfraredSensorSensitivityInOccupiedStateFeedback + 1;
			}
		}

		public void DecrementPirSensitivityInOccupiedState(bool pressRelease)
		{
			if ((int)OccSensor.PassiveInfraredSensorSensitivityInOccupiedStateFeedback != 0)
			{
				OccSensor.PassiveInfraredSensorSensitivityInOccupiedState = OccSensor.PassiveInfraredSensorSensitivityInOccupiedStateFeedback - 1;
			}
		}

		public void IncrementPirSensitivityInVacantState(bool pressRelease)
		{
			if ((int)OccSensor.PassiveInfraredSensorSensitivityInVacantStateFeedback != 3)
			{
				OccSensor.PassiveInfraredSensorSensitivityInVacantState = OccSensor.PassiveInfraredSensorSensitivityInVacantStateFeedback + 1;
			}
		}

		public void DecrementPirSensitivityInVacantState(bool pressRelease)
		{
			if ((int)OccSensor.PassiveInfraredSensorSensitivityInVacantStateFeedback != 0)
			{
				OccSensor.PassiveInfraredSensorSensitivityInVacantState = OccSensor.PassiveInfraredSensorSensitivityInVacantStateFeedback - 1;
			}
		}

		public void IncrementUsSensitivityInOccupiedState(bool pressRelease)
		{
			if ((int)OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback < 3)
			{
				OccSensor.UltrasonicSensorSensitivityInOccupiedState = OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback + 1;
			}
			else if ((int)OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback > 4)
			{
				OccSensor.UltrasonicSensorSensitivityInOccupiedState = OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback - 1;
			}
			else if ((int)OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback == 4)
			{
				OccSensor.UltrasonicSensorSensitivityInOccupiedState = 0;
			}
		}

		public void DecrementUsSensitivityInOccupiedState(bool pressRelease)
		{
			if ((int)OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback > 0
				&& (int)OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback < 4)
			{
				OccSensor.UltrasonicSensorSensitivityInOccupiedState = OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback - 1;
			}
			else if ((int)OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback > 3
				&& (int)OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback < 7)
			{
				OccSensor.UltrasonicSensorSensitivityInOccupiedState = OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback + 1;
			}
		}

		public void IncrementUsSensitivityInVacantState(bool pressRelease)
		{
			if ((int)OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback < 3)
			{
				OccSensor.UltrasonicSensorSensitivityInVacantState = OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback + 1;
			}
			else if ((int)OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback > 4)
			{
				OccSensor.UltrasonicSensorSensitivityInVacantState = OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback - 1;
			}
			else if ((int)OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback == 4)
			{
				OccSensor.UltrasonicSensorSensitivityInVacantState = 0;
			}
		}

		public void DecrementUsSensitivityInVacantState(bool pressRelease)
		{
			if ((int)OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback > 0
				&& (int)OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback < 4)
			{
				OccSensor.UltrasonicSensorSensitivityInVacantState = OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback - 1;
			}
			else if ((int)OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback > 3
				&& (int)OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback < 7)
			{
				OccSensor.UltrasonicSensorSensitivityInVacantState = OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback + 1;
			}
		}

		public void ForceOccupied()
		{
			OccSensor.ForceOccupied();
		}

		public void ForceVacant()
		{
			OccSensor.ForceVacant();
		}

		public void EnableRawStates(bool state)
		{
			if (state)
			{
				OccSensor.EnableRawStates();
			}
			else
				OccSensor.DisableRawStates();
		}

		public void SetRemoteTimeout(ushort time)
		{
			OccSensor.RemoteTimeout.UShortValue = time;
		}

		public void SetInternalPhotoSensorMinChange(ushort value)
		{
			OccSensor.InternalPhotoSensorMinimumChange.UShortValue = value;
		}

		/// <summary>
		/// Sets the OrWhenVacated state
		/// </summary>
		/// <param name="state"></param>
		public void SetOrWhenVacatedState(bool state)
		{
			if (state)
			{
				OccSensor.OrWhenVacated();
			}
		}

		/// <summary>
		/// Sets the AndWhenVacated state
		/// </summary>
		/// <param name="state"></param>
		public void SetAndWhenVacatedState(bool state)
		{
			if (state)
			{
				OccSensor.AndWhenVacated();
			}
		}

		/// <summary>
		/// Enables or disables the Ultrasonic A sensor
		/// </summary>
		/// <param name="state"></param>
		public void SetUsAEnable(bool state)
		{
			if (state)
			{
				OccSensor.EnableUltrasonicSensorSideA();
			}
			else
			{
				OccSensor.DisableUltrasonicSensorSideA();
			}
		}


		/// <summary>
		/// Enables or disables the Ultrasonic B sensor
		/// </summary>
		/// <param name="state"></param>
		public void SetUsBEnable(bool state)
		{
			if (state)
			{
				OccSensor.EnableUltrasonicSensorSideB();
			}
			else
			{
				OccSensor.DisableUltrasonicSensorSideB();
			}
		}

        /// <summary>
        /// Sets the US sensor sensitivity for occupied state
        /// </summary>
        /// <param name="sensitivity"></param>
	    public void SetUsSensitivityOccupied(ushort sensitivity)
        {
            var level = (eSensitivityLevel) sensitivity;
            if (level == 0) return;

            OccSensor.UltrasonicSensorSensitivityInOccupiedState = level;
        }

        /// <summary>
        /// Sets the US sensor sensitivity for vacant state
        /// </summary>
        /// <param name="sensitivity"></param>
        public void SetUsSensitivityVacant(ushort sensitivity)
        {
            var level = (eSensitivityLevel)sensitivity;
            if (level == 0) return;

            OccSensor.UltrasonicSensorSensitivityInVacantState = level;
        }

        /// <summary>
        /// Sets the PIR sensor sensitivity for occupied state
        /// </summary>
        /// <param name="sensitivity"></param>
        public void SetPirSensitivityOccupied(ushort sensitivity)
        {
            var level = (eSensitivityLevel)sensitivity;
            if (level == 0) return;

            OccSensor.PassiveInfraredSensorSensitivityInOccupiedState = level;
        }

        /// <summary>
        /// Sets the PIR sensor sensitivity for vacant state
        /// </summary>
        /// <param name="sensitivity"></param>
        public void SetPirSensitivityVacant(ushort sensitivity)
        {
            var level = (eSensitivityLevel)sensitivity;
            if (level == 0) return;

            OccSensor.PassiveInfraredSensorSensitivityInVacantState = level;
        }

		/// <summary>
		/// Method to print current settings to console
		/// </summary>
		public void GetSettings()
		{
			var dash = new string('*', 50);
			CrestronConsole.PrintLine(string.Format("{0}\n", dash));

            this.LogInformation("Vacancy Detected: {VacancyDetected}",
                OccSensor.VacancyDetectedFeedback.BoolValue);

			this.LogInformation("Timeout Current: {CurrentTimeout} | Remote: {RemoteTimeout}",
				OccSensor.CurrentTimeoutFeedback.UShortValue,
				OccSensor.RemoteTimeout.UShortValue);

			this.LogInformation("Short Timeout Enabled: {ShortTimeoutEnabled}",
				OccSensor.ShortTimeoutEnabledFeedback.BoolValue);

			this.LogInformation("PIR Sensor Enabled: {PirEnabled} | Sensitivity Occupied: {PirSensitivityOccupied} | Sensitivity Vacant: {PirSensitivityVacant}", 
				OccSensor.PassiveInfraredSensorEnabledFeedback.BoolValue,
				OccSensor.PassiveInfraredSensorSensitivityInOccupiedStateFeedback,
				OccSensor.PassiveInfraredSensorSensitivityInVacantStateFeedback);

			this.LogInformation("Ultrasonic Enabled A: {UsAEnabled} | B: {UsBEnabled}", 
				OccSensor.UltrasonicSensorSideAEnabledFeedback.BoolValue,
				OccSensor.UltrasonicSensorSideBEnabledFeedback.BoolValue);

			this.LogInformation("Ultrasonic Sensitivity Occupied: {UsSensitivityOccupied} | Vacant: {UsSensitivityVacant}",
				OccSensor.UltrasonicSensorSensitivityInOccupiedStateFeedback,
				OccSensor.UltrasonicSensorSensitivityInVacantStateFeedback);

			CrestronConsole.PrintLine(string.Format("{0}\n", dash));
		}


		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			LinkOccSensorToApi(this, trilist, joinStart, joinMapKey, bridge);
		}

		protected void LinkOccSensorToApi(CenOdtOccupancySensorBaseController occController,
			BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			CenOdtOccupancySensorBaseJoinMap joinMap = new CenOdtOccupancySensorBaseJoinMap(joinStart);

			var joinMapSerialized = JoinMapHelper.GetSerializedJoinMapForDevice(joinMapKey);

			if (!string.IsNullOrEmpty(joinMapSerialized))
				joinMap = JsonConvert.DeserializeObject<CenOdtOccupancySensorBaseJoinMap>(joinMapSerialized);

			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}
			else
			{
				this.LogInformation("Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
			}

			occController.LogDebug("Linking to Trilist '{TrilistId}'", trilist.ID.ToString("X"));

			occController.IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.Online.JoinNumber]);
			trilist.StringInput[joinMap.Name.JoinNumber].StringValue = occController.Name;

			trilist.OnlineStatusChange += new global::Crestron.SimplSharpPro.OnlineStatusChangeEventHandler((d, args) =>
			{
				if (args.DeviceOnLine)
				{
					trilist.StringInput[joinMap.Name.JoinNumber].StringValue = occController.Name;
				}
			}
			);

			// Occupied status
			trilist.SetSigTrueAction(joinMap.ForceOccupied.JoinNumber, new Action(() => occController.ForceOccupied()));
			trilist.SetSigTrueAction(joinMap.ForceVacant.JoinNumber, new Action(() => occController.ForceVacant()));
			occController.RoomIsOccupiedFeedback.LinkInputSig(trilist.BooleanInput[joinMap.RoomOccupiedFeedback.JoinNumber]);
			occController.RoomIsOccupiedFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.RoomVacantFeedback.JoinNumber]);
			occController.RawOccupancyFeedback.LinkInputSig(trilist.BooleanInput[joinMap.RawOccupancyFeedback.JoinNumber]);
			trilist.SetBoolSigAction(joinMap.EnableRawStates.JoinNumber, new Action<bool>((b) => occController.EnableRawStates(b)));

			// Timouts
			trilist.SetUShortSigAction(joinMap.Timeout.JoinNumber, new Action<ushort>((u) => occController.SetRemoteTimeout(u)));
			occController.CurrentTimeoutFeedback.LinkInputSig(trilist.UShortInput[joinMap.Timeout.JoinNumber]);
			occController.RemoteTimeoutFeedback.LinkInputSig(trilist.UShortInput[joinMap.TimeoutLocalFeedback.JoinNumber]);

			// LED Flash
			trilist.SetSigTrueAction(joinMap.EnableLedFlash.JoinNumber, new Action(() => occController.SetLedFlashEnable(true)));
			trilist.SetSigTrueAction(joinMap.DisableLedFlash.JoinNumber, new Action(() => occController.SetLedFlashEnable(false)));
			occController.LedFlashEnabledFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.EnableLedFlash.JoinNumber]);

			// Short Timeout
			trilist.SetSigTrueAction(joinMap.EnableShortTimeout.JoinNumber, new Action(() => occController.SetShortTimeoutState(true)));
			trilist.SetSigTrueAction(joinMap.DisableShortTimeout.JoinNumber, new Action(() => occController.SetShortTimeoutState(false)));
			occController.ShortTimeoutEnabledFeedback.LinkInputSig(trilist.BooleanInput[joinMap.EnableShortTimeout.JoinNumber]);

			// PIR Sensor
			trilist.SetSigTrueAction(joinMap.EnablePir.JoinNumber, new Action(() => occController.SetPirEnable(true)));
			trilist.SetSigTrueAction(joinMap.DisablePir.JoinNumber, new Action(() => occController.SetPirEnable(false)));
			occController.PirSensorEnabledFeedback.LinkInputSig(trilist.BooleanInput[joinMap.EnablePir.JoinNumber]);

			// PIR Sensitivity in Occupied State
			trilist.SetBoolSigAction(joinMap.IncrementPirInOccupiedState.JoinNumber, new Action<bool>((b) => occController.IncrementPirSensitivityInOccupiedState(b)));
			trilist.SetBoolSigAction(joinMap.DecrementPirInOccupiedState.JoinNumber, new Action<bool>((b) => occController.DecrementPirSensitivityInOccupiedState(b)));
			occController.PirSensitivityInOccupiedStateFeedback.LinkInputSig(trilist.UShortInput[joinMap.PirSensitivityInOccupiedState.JoinNumber]);

			// PIR Sensitivity in Vacant State
			trilist.SetBoolSigAction(joinMap.IncrementPirInVacantState.JoinNumber, new Action<bool>((b) => occController.IncrementPirSensitivityInVacantState(b)));
			trilist.SetBoolSigAction(joinMap.DecrementPirInVacantState.JoinNumber, new Action<bool>((b) => occController.DecrementPirSensitivityInVacantState(b)));
			occController.PirSensitivityInVacantStateFeedback.LinkInputSig(trilist.UShortInput[joinMap.PirSensitivityInVacantState.JoinNumber]);

			// OR When Vacated
			trilist.SetBoolSigAction(joinMap.OrWhenVacated.JoinNumber, new Action<bool>((b) => occController.SetOrWhenVacatedState(b)));
			occController.OrWhenVacatedFeedback.LinkInputSig(trilist.BooleanInput[joinMap.OrWhenVacated.JoinNumber]);

			// AND When Vacated
			trilist.SetBoolSigAction(joinMap.AndWhenVacated.JoinNumber, new Action<bool>((b) => occController.SetAndWhenVacatedState(b)));
			occController.AndWhenVacatedFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AndWhenVacated.JoinNumber]);

			// Ultrasonic A Sensor
			trilist.SetSigTrueAction(joinMap.EnableUsA.JoinNumber, new Action(() => occController.SetUsAEnable(true)));
			trilist.SetSigTrueAction(joinMap.DisableUsA.JoinNumber, new Action(() => occController.SetUsAEnable(false)));
			occController.UltrasonicAEnabledFeedback.LinkInputSig(trilist.BooleanInput[joinMap.EnableUsA.JoinNumber]);

			// Ultrasonic B Sensor
			trilist.SetSigTrueAction(joinMap.EnableUsB.JoinNumber, new Action(() => occController.SetUsBEnable(true)));
			trilist.SetSigTrueAction(joinMap.DisableUsB.JoinNumber, new Action(() => occController.SetUsBEnable(false)));
			occController.UltrasonicAEnabledFeedback.LinkInputSig(trilist.BooleanInput[joinMap.EnableUsB.JoinNumber]);

			// US Sensitivity in Occupied State
			trilist.SetBoolSigAction(joinMap.IncrementUsInOccupiedState.JoinNumber, new Action<bool>((b) => occController.IncrementUsSensitivityInOccupiedState(b)));
			trilist.SetBoolSigAction(joinMap.DecrementUsInOccupiedState.JoinNumber, new Action<bool>((b) => occController.DecrementUsSensitivityInOccupiedState(b)));
			occController.UltrasonicSensitivityInOccupiedStateFeedback.LinkInputSig(trilist.UShortInput[joinMap.UsSensitivityInOccupiedState.JoinNumber]);

			// US Sensitivity in Vacant State
			trilist.SetBoolSigAction(joinMap.IncrementUsInVacantState.JoinNumber, new Action<bool>((b) => occController.IncrementUsSensitivityInVacantState(b)));
			trilist.SetBoolSigAction(joinMap.DecrementUsInVacantState.JoinNumber, new Action<bool>((b) => occController.DecrementUsSensitivityInVacantState(b)));
			occController.UltrasonicSensitivityInVacantStateFeedback.LinkInputSig(trilist.UShortInput[joinMap.UsSensitivityInVacantState.JoinNumber]);

			//Sensor Raw States
			occController.RawOccupancyPirFeedback.LinkInputSig(trilist.BooleanInput[joinMap.RawOccupancyPirFeedback.JoinNumber]);
			occController.RawOccupancyUsFeedback.LinkInputSig(trilist.BooleanInput[joinMap.RawOccupancyUsFeedback.JoinNumber]);  
         
            // Identity mode
		    trilist.SetBoolSigAction(joinMap.IdentityMode.JoinNumber, occController.SetIdentityMode);
            occController.IdentityModeFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IdentityModeFeedback.JoinNumber]);
		}

		public class CenOdtOccupancySensorBaseControllerFactory : EssentialsPluginDeviceFactory<CenOdtOccupancySensorBaseController>
		{
			public CenOdtOccupancySensorBaseControllerFactory()
			{
                MinimumEssentialsFrameworkVersion = "3.0.0";


                TypeNames = new List<string>() { "cenodtcpoe", "cenodtocc" };
			}

			public override EssentialsDevice BuildDevice(DeviceConfig dc)
			{
				Debug.LogDebug("Factory Attempting to create new GlsOccupancySensorBaseController Device");

				var typeName = dc.Type.ToLower();
				var key = dc.Key;
				var name = dc.Name;
				var comm = CommFactory.GetControlPropertiesConfig(dc);

                var props = dc.Properties.ToObject<GlsOccupancySensorPropertiesConfig>();

				var occSensor = new CenOdtCPoe(comm.IpIdInt, Global.ControlSystem);

				if (occSensor == null)
				{
					Debug.LogInformation("ERROR: Unable to create Occupancy Sensor Device. Key: '{Key}'", key);
					return null;
				}

				return new CenOdtOccupancySensorBaseController(key, name, occSensor, props);
			}
		}
	}
}