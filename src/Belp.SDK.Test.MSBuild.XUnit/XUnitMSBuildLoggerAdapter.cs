using Microsoft.Build.Framework;
using Xunit.Abstractions;

namespace Belp.SDK.Test.MSBuild.XUnit;

/// <summary>
/// Provides an adapter for <see cref="ITestOutputHelper"/> to <see cref="ILogger"/>.
/// </summary>
public class XUnitMSBuildLoggerAdapter : ITestOutputHelper, ILogger
{
    private readonly ITestOutputHelper _output;

    string? ILogger.Parameters
    {
        get => null;

        set
        {
        }
    }

    /// <inheritdoc />
    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;

    /// <summary>
    /// Initializes a new instance of <see cref="XUnitMSBuildLoggerAdapter"/> for the specified <paramref name="output"/>.
    /// </summary>
    /// <param name="output">The <see cref="ITestOutputHelper"/> to adapt.</param>
    public XUnitMSBuildLoggerAdapter(ITestOutputHelper output)
    {
        _output = output;
    }

    #region ILogger

    void ILogger.Initialize(IEventSource eventSource)
    {
        Initialize(eventSource);
    }

    void ILogger.Shutdown()
    {
        Shutdown();
    }

    /// <inheritdoc />
    public virtual void Initialize(IEventSource eventSource)
    {
        eventSource.BuildFinished += (sender, e) => WriteLine(e.Message);
        eventSource.BuildStarted += (sender, e) => WriteLine(e.Message);
        eventSource.ErrorRaised += (sender, e) => WriteLine($"ERR {e.Code}: {e.Message}");
        eventSource.MessageRaised += (sender, e) =>
        {
            if (e.Importance == MessageImportance.High)
            {
                WriteLine(e.Message);
            }
        };
        eventSource.ProjectFinished += (sender, e) => WriteLine(e.Message);
        eventSource.ProjectStarted += (sender, e) => WriteLine(e.Message);
        eventSource.WarningRaised += (sender, e) => WriteLine($"WRN {e.Code}: {e.Message}");
    }

    /// <inheritdoc />
    public virtual void Shutdown()
    {
    }

    #endregion

    /// <inheritdoc />
    public void WriteLine(string message)
    {
        _output.WriteLine(message);
    }

    /// <inheritdoc />
    public void WriteLine(string format, params object[] args)
    {
        _output.WriteLine(format, args);
    }
}
