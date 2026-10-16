using System.Diagnostics;

namespace CsTranslate.Audio.Tests;

/// <summary>
/// Real WASAPI capture (<see cref="ProcessLoopbackCapture"/>, <see cref="WasapiSystemLoopbackCapture"/>)
/// needs an actual audio session and isn't practical to assert on in an automated
/// CI run - that's covered manually via tools/wav-dump, tools/stt-test, and
/// tools/pipeline-test (see docs/SETUP.md). This covers the one piece of Audio
/// that's pure logic.
/// </summary>
public class ProcessLookupTests
{
    [Fact]
    public void FindProcessId_WithNumericInput_ReturnsThatPidDirectly()
    {
        int? result = ProcessLookup.FindProcessId("12345");

        Assert.Equal(12345, result);
    }

    [Fact]
    public void FindProcessId_WithCurrentProcessName_ResolvesToARunningProcess()
    {
        string currentProcessName = Process.GetCurrentProcess().ProcessName;

        int? result = ProcessLookup.FindProcessId(currentProcessName);

        Assert.NotNull(result);
    }

    [Fact]
    public void FindProcessId_WithUnknownProcessName_ReturnsNull()
    {
        int? result = ProcessLookup.FindProcessId("definitely-not-a-real-process-name-xyz");

        Assert.Null(result);
    }
}
