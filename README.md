![PepperDash Essentials Pluign Logo](/images/essentials-plugin-blue.png)

# Essentials Plugin Template (c) 2023

## License

Provided under MIT license

## Overview

Fork this repo when creating a new plugin for Essentials. For more information about plugins, refer to the Essentials Wiki [Plugins](https://github.com/PepperDash/Essentials/wiki/Plugins) article.

This repo contains example classes for the three main categories of devices:
* `EssentialsPluginTemplateDevice`: Used for most third party devices which require communication over a streaming mechanism such as a Com port, TCP/SSh/UDP socket, CEC, etc
* `EssentialsPluginTemplateLogicDevice`:  Used for devices that contain logic, but don't require any communication with third parties outside the program
* `EssentialsPluginTemplateCrestronDevice`:  Used for devices that represent a piece of Crestron hardware

There are matching factory classes for each of the three categories of devices.  The `EssentialsPluginTemplateConfigObject` should be used as a template and modified for any of the categories of device.  Same goes for the `EssentialsPluginTemplateBridgeJoinMap`.

This also illustrates how a plugin can contain multiple devices.

## Cloning Instructions

After forking this repository into your own GitHub space, you can create a new repository using this one as the template.  Then you must install the necessary dependencies as indicated below.

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

To install dependencies once nuget.exe is installed, run the following command from the root directory of your repository:
`nuget install .\packages.config -OutputDirectory .\packages -excludeVersion`.
Alternatively, you can simply run the `GetPackages.bat` file.
To verify that the packages installed correctly, open the plugin solution in your repo and make sure that all references are found, then try and build it.

### Installing Different versions of PepperDash Core

If you need a different version of PepperDash Core, use the command `nuget install .\packages.config -OutputDirectory .\packages -excludeVersion -Version {versionToGet}`. Omitting the `-Version` option will pull the version indicated in the packages.config file.

### Instructions for Renaming Solution and Files

See the Task List in Visual Studio for a guide on how to start using the template.  There is extensive inline documentation and examples as well.

For renaming instructions in particular, see the XML `remarks` tags on class definitions

## Build Instructions (PepperDash Internal) 

## Generating Nuget Package 

In the solution folder is a file named "PDT.EssentialsPluginTemplate.nuspec" 

1. Rename the file to match your plugin solution name 
2. Edit the file to include your project specifics including
    1. <id>PepperDash.Essentials.Plugin.MakeModel</id> Convention is to use the prefix "PepperDash.Essentials.Plugin" and include the MakeModel of the device. 
    2. <projectUrl>https://github.com/PepperDash/EssentialsPluginTemplate</projectUrl> Change to your url to the project repo

There is no longer a requirement to adjust workflow files for nuget generation for private and public repositories.  This is now handled automatically in the workflow.

__If you do not make these changes to the nuspec file, the project will not generate a nuget package__
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "ceniory104",
    "group": "Group",
    "properties": {
        "enablePir": true,
        "enableLedFlash": true,
        "shortTimeoutState": true,
        "enableRawStates": true,
        "remoteTimeout": "SampleValue",
        "internalPhotoSensorMinChange": "SampleValue",
        "externalPhotoSensorMinChange": "SampleValue",
        "enableUsA": true,
        "enableUsB": true,
        "orWhenVacatedState": true,
        "andWhenVacatedState": true,
        "usSensitivityOccupied": "SampleValue",
        "usSensitivityVacant": "SampleValue",
        "pirSensitivityOccupied": "SampleValue",
        "pirSensitivityVacant": "SampleValue"
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->
### Supported Types

- ceniory104
- din-cencn2
- dincencn2
- din-cencn2-poe
- dincencn2poe
- cenioir104
- c2nrths
- c2nio
- ceniodigin104
- din8sw8
- clwdimflvexp
- DinIo8
- cenodtcpoe
- cenodtocc
- hr310
- hr150
- hr100
- statussign
<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Device Online |

#### Serials

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Com 1 TX/RX |
| 3 | R | Com 2 TX/RX |
| 5 | R | Device Name |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IComPorts
- IIROutputPorts
- IRelayPorts
- IIOPorts
- IPartitionStateProvider
- ISwitchedOutput
- IHasCresnetBranches
- ITemperatureSensor
- IHumiditySensor
- IDigitalInputPorts
- ISwitchedOutputCollection
- ICommunicationMonitor
- IHasFeedback
- IOccupancyStatusProvider
- IHR52Button
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- CrestronGenericBaseDevice
- JoinMapBaseAdvanced
- C3CardControllerBase
- GlsOccupancySensorBaseController
- CrestronGenericBridgeableBaseDevice
- EssentialsDevice
- EssentialsBridgeableDevice
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void SetOrWhenVacatedState(bool state)
- public void SetAndWhenVacatedState(bool state)
- public void SetUsAEnable(bool state)
- public void SetUsBEnable(bool state)
- public void IncrementUsSensitivityInOccupiedState(bool pressRelease)
- public void DecrementUsSensitivityInOccupiedState(bool pressRelease)
- public void IncrementUsSensitivityInVacantState(bool pressRelease)
- public void DecrementUsSensitivityInVacantState(bool pressRelease)
- public void SetTestMode(bool mode)
- public void SetTestEnableState(bool state)
- public void SetTestPartitionSensedState(bool state)
- public void SetTestSensitivityValue(int value)
- public void GetSettings()
- public void SetEnableState(bool state)
- public void IncreaseSensitivity()
- public void DecreaseSensitivity()
- public void SetSensitivity(ushort value)
- public void On()
- public void Off()
- public void SetTemperatureFormat(bool setToC)
- public void FullOn()
- public void Off()
- public void SetLevel(ushort level)
- public void SetTestMode(bool mode)
- public void SetTestOccupiedState(bool state)
- public void SetPirEnable(bool state)
- public void SetLedFlashEnable(bool state)
- public void SetShortTimeoutState(bool state)
- public void IncrementPirSensitivityInOccupiedState(bool pressRelease)
- public void DecrementPirSensitivityInOccupiedState(bool pressRelease)
- public void IncrementPirSensitivityInVacantState(bool pressRelease)
- public void DecrementPirSensitivityInVacantState(bool pressRelease)
- public void ForceOccupied()
- public void ForceOccupied(bool value)
- public void ForceVacant()
- public void ForceVacant(bool value)
- public void EnableRawStates(bool state)
- public void SetRemoteTimeout(ushort time)
- public void SetInternalPhotoSensorMinChange(ushort value)
- public void SetExternalPhotoSensorMinChange(ushort value)
- public void SetTestMode(bool mode)
- public void SetTestOccupiedState(bool state)
- public void SetIdentityMode(bool state)
- public void SetPirEnable(bool state)
- public void SetLedFlashEnable(bool state)
- public void SetShortTimeoutState(bool state)
- public void IncrementPirSensitivityInOccupiedState(bool pressRelease)
- public void DecrementPirSensitivityInOccupiedState(bool pressRelease)
- public void IncrementPirSensitivityInVacantState(bool pressRelease)
- public void DecrementPirSensitivityInVacantState(bool pressRelease)
- public void IncrementUsSensitivityInOccupiedState(bool pressRelease)
- public void DecrementUsSensitivityInOccupiedState(bool pressRelease)
- public void IncrementUsSensitivityInVacantState(bool pressRelease)
- public void DecrementUsSensitivityInVacantState(bool pressRelease)
- public void ForceOccupied()
- public void ForceVacant()
- public void EnableRawStates(bool state)
- public void SetRemoteTimeout(ushort time)
- public void SetInternalPhotoSensorMinChange(ushort value)
- public void SetOrWhenVacatedState(bool state)
- public void SetAndWhenVacatedState(bool state)
- public void SetUsAEnable(bool state)
- public void SetUsBEnable(bool state)
- public void SetUsSensitivityOccupied(ushort sensitivity)
- public void SetUsSensitivityVacant(ushort sensitivity)
- public void SetPirSensitivityOccupied(ushort sensitivity)
- public void SetPirSensitivityVacant(ushort sensitivity)
- public void GetSettings()
- public void SetTrilistBool(BasicTriList trilist, uint join, bool b)
- public void EnableLedControl(bool red, bool green, bool blue)
- public void SetColor(uint red, uint green, uint blue)
- public void Device_Implements_ICommunicationMonitor(string typeName)
- public void DigIn104_Hands_Its_Hardware_To_The_Base()
- public void Wireless_Device_Monitor_Is_Created_And_Started_With_Its_Hardware(string file, string hardware)
- public void Din8sw8_Monitor_Is_Created_In_The_Constructor_And_Started_On_Activation()
- public void Partition_Sensor_Registers_Its_Hardware_On_The_Activation_Thread()
- public void Sensor_Feedback_Is_Keyed(string file, string property, string type, string key)
- public void Sensor_Feedback_Is_On_The_List(string file, string add)
- public void Sensor_Keys_Do_Not_Clash_With_The_Base_List(string file)
- public void Rths_Refresh_Timer_Uses_Whole_Durations_And_Contains_Errors()
- public void Config_Class_Exists(string className)
- public void Config_Has_Parameterless_Constructor(string className)
- public void Config_Property_Has_JsonPropertyAttribute(string className, string propertyName, string jsonName)
- public void Assembly_Loads_Successfully()
- public void Assembly_Name_Matches_Expected()
- public void Factory_Count_Matches_Expected()
- public void Factory_Exists_ByName(string factoryName)
- public void All_Factories_Have_Parameterless_Constructor()
- public void All_Factory_Sources_Set_MinimumEssentialsFrameworkVersion_To_Expected(string factoryClass)
- public void All_Factory_Sources_Set_TypeNames(string factoryClass)
- public void Factory_Source_Contains_TypeName(string factoryClass, string typeName)
- public void No_Duplicate_TypeNames_Across_Factory_Sources()
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- OrWhenVacatedFeedback
- AndWhenVacatedFeedback
- UltrasonicAEnabledFeedback
- UltrasonicBEnabledFeedback
- RawOccupancyPirFeedback
- RawOccupancyUsFeedback
- EnableFeedback
- PartitionPresentFeedback
- PartitionNotSensedFeedback
- OutputIsOnFeedback
- TemperatureInCFeedback
- IsOnlineFeedback
- IsOnFeedback
- RoomIsOccupiedFeedback
- GraceOccupancyDetectedFeedback
- RawOccupancyFeedback
- PirSensorEnabledFeedback
- LedFlashEnabledFeedback
- ShortTimeoutEnabledFeedback
- RoomIsOccupiedFeedback
- GraceOccupancyDetectedFeedback
- RawOccupancyFeedback
- PirSensorEnabledFeedback
- LedFlashEnabledFeedback
- ShortTimeoutEnabledFeedback
- OrWhenVacatedFeedback
- AndWhenVacatedFeedback
- UltrasonicAEnabledFeedback
- UltrasonicBEnabledFeedback
- RawOccupancyPirFeedback
- RawOccupancyUsFeedback
- IdentityModeFeedback
- RedLedEnabledFeedback
- GreenLedEnabledFeedback
- BlueLedEnabledFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- UltrasonicSensitivityInVacantStateFeedback
- UltrasonicSensitivityInOccupiedStateFeedback
- SensitivityFeedback
- TemperatureFeedback
- HumidityFeedback
- LevelFeedback
- PirSensitivityInVacantStateFeedback
- PirSensitivityInOccupiedStateFeedback
- CurrentTimeoutFeedback
- LocalTimoutFeedback
- InternalPhotoSensorValue
- ExternalPhotoSensorValue
- PirSensitivityInVacantStateFeedback
- PirSensitivityInOccupiedStateFeedback
- CurrentTimeoutFeedback
- RemoteTimeoutFeedback
- InternalPhotoSensorValue
- ExternalPhotoSensorValue
- UltrasonicSensitivityInVacantStateFeedback
- UltrasonicSensitivityInOccupiedStateFeedback
- RedLedBrightnessFeedback
- GreenLedBrightnessFeedback
- BlueLedBrightnessFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->

<!-- END String Feedbacks -->
