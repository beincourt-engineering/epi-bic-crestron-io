using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugins.Tests;

/// <summary>
/// Guards for what monitoring consumers rely on. The devices need a processor to run, so these
/// check interfaces by name and pin source facts a later edit could undo without anything else
/// failing: a device is followed only through a non-null communication monitor, and only keyed
/// feedbacks on its Feedbacks list are read.
/// </summary>
public class MonitoringSourceTests
{
    private static string Source(string file)
    {
        var path = Path.Combine(AssemblyFixture.SourceDirectory, file);
        File.Exists(path).Should().BeTrue($"the source should be at {path}");
        return File.ReadAllText(path);
    }

    /// <summary>The text from the opening brace after <paramref name="signature"/> to its match.</summary>
    private static string Body(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1, $"'{signature}' should be in the source");

        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}' && --depth == 0) return source.Substring(open, i - open + 1);
        }

        throw new InvalidOperationException($"unbalanced braces after '{signature}'");
    }

    [Theory]
    [InlineData("PepperDash.Essentials.Plugins.ClwDimFlvExPController")]
    [InlineData("PepperDash.Essentials.Plugins.Hrxx0WirelessRemoteController")]
    [InlineData("PepperDash.Essentials.Plugins.Din8sw8Controller")]
    public void Device_Implements_ICommunicationMonitor(string typeName)
    {
        var type = AssemblyFixture.PluginAssembly.GetType(typeName);
        type.Should().NotBeNull();

        type!.GetInterfaces().Select(i => i.FullName)
            .Should().Contain("PepperDash.Essentials.Core.ICommunicationMonitor");
    }

    [Fact]
    public void DigIn104_Hands_Its_Hardware_To_The_Base()
    {
        // Without the hardware the base creates no monitor, and activation reads a null device.
        Source("CenIoDigIn104Controller.cs").Should().Contain(": base(key, name, di104)");
    }

    [Theory]
    [InlineData("ClwDimFlvExPController.cs", "_dimmer")]
    [InlineData("Hrxx0WirelessRemoteController.cs", "_remote")]
    public void Wireless_Device_Monitor_Is_Created_And_Started_With_Its_Hardware(string file, string hardware)
    {
        // The hardware exists only once every device has activated; the monitor is created with
        // it, in the same post-activation action, and started there.
        var action = Body(Source(file), "AddPostActivationAction(");

        var created = action.IndexOf(
            $"CommunicationMonitor = new CrestronGenericBaseCommunicationMonitor(this, {hardware}, ",
            StringComparison.Ordinal);
        var started = action.IndexOf("CommunicationMonitor.Start();", StringComparison.Ordinal);

        created.Should().BeGreaterThan(-1);
        started.Should().BeGreaterThan(created);
    }

    [Fact]
    public void Din8sw8_Monitor_Is_Created_In_The_Constructor_And_Started_On_Activation()
    {
        var source = Source("Din8sw8.cs");

        Body(source, "public Din8sw8Controller(string key, uint cresnetId)")
            .Should().Contain("CommunicationMonitor = new CrestronGenericBaseCommunicationMonitor(this, SwitchModule, ");
        Body(source, "protected override bool CustomActivate()")
            .Should().Contain("CommunicationMonitor.Start();");
    }

    [Fact]
    public void Partition_Sensor_Registers_Its_Hardware_On_The_Activation_Thread()
    {
        // On a background thread it races activation, which reads the hardware and starts the monitor.
        var source = Source("GlsPartitionSensorController.cs");

        source.Should().NotMatchRegex(@"BeginInvoke\([^;]*RegisterCrestronGenericBase");
        Body(source, "AddPreActivationAction(")
            .Should().Contain("RegisterCrestronGenericBase(_partitionSensor);");
    }

    [Theory]
    [InlineData("C2nRthsController.cs", "TemperatureFeedback", "IntFeedback", "Temperature")]
    [InlineData("C2nRthsController.cs", "TemperatureInCFeedback", "BoolFeedback", "TemperatureInC")]
    [InlineData("C2nRthsController.cs", "HumidityFeedback", "IntFeedback", "Humidity")]
    [InlineData("GlsOccupancySensorBaseController.cs", "RoomIsOccupiedFeedback", "BoolFeedback", "RoomIsOccupied")]
    [InlineData("CenOdtOccupancySensorBaseController.cs", "RoomIsOccupiedFeedback", "BoolFeedback", "RoomIsOccupied")]
    [InlineData("GlsPartitionSensorController.cs", "PartitionPresentFeedback", "BoolFeedback", "PartitionPresent")]
    public void Sensor_Feedback_Is_Keyed(string file, string property, string type, string key)
    {
        Source(file).Should().MatchRegex($@"{property}\s*=\s*new {type}\(""{key}""");
    }

    [Theory]
    [InlineData("C2nRthsController.cs", "Feedbacks.AddRange(new List<Feedback> { TemperatureFeedback, TemperatureInCFeedback, HumidityFeedback });")]
    [InlineData("GlsOccupancySensorBaseController.cs", "AddToFeedbackList(RoomIsOccupiedFeedback);")]
    [InlineData("CenOdtOccupancySensorBaseController.cs", "AddToFeedbackList(RoomIsOccupiedFeedback);")]
    [InlineData("GlsPartitionSensorController.cs", "AddToFeedbackList(PartitionPresentFeedback);")]
    public void Sensor_Feedback_Is_On_The_List(string file, string add)
    {
        Source(file).Should().Contain(add);
    }

    [Theory]
    [InlineData("C2nRthsController.cs")]
    [InlineData("GlsOccupancySensorBaseController.cs")]
    [InlineData("CenOdtOccupancySensorBaseController.cs")]
    [InlineData("GlsPartitionSensorController.cs")]
    public void Sensor_Keys_Do_Not_Clash_With_The_Base_List(string file)
    {
        // The base list already holds these, and the list rejects a second feedback with the same
        // key, case-insensitively.
        var baseKeys = new[] { "IsOnlineFeedback", "IpConnectionsText" };

        Regex.Matches(Source(file), @"new (?:Bool|Int|String)Feedback\(""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .Should().NotContain(k => baseKeys.Contains(k, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Rths_Refresh_Timer_Uses_Whole_Durations_And_Contains_Errors()
    {
        var source = Source("C2nRthsController.cs");

        // TimeSpan.Milliseconds is only the milliseconds part of the span: 0 for 30 s and 5 min.
        source.Should().NotMatchRegex(@"TimeSpan\.From\w+\([^)]*\)\.Milliseconds\b");
        source.Should().Contain("(long)TimeSpan.FromSeconds(30).TotalMilliseconds");
        source.Should().Contain("(long)TimeSpan.FromMinutes(5).TotalMilliseconds");

        var refresh = Body(source, "private void RefreshFeedbacks()");
        refresh.Should().Contain("try");
        refresh.Should().Contain("catch (Exception");
    }
}
