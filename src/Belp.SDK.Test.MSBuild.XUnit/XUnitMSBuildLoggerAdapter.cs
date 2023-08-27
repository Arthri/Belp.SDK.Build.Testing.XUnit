using Microsoft.Build.Framework;
using System.Collections;
using Xunit.Abstractions;

namespace Belp.SDK.Test.MSBuild.XUnit;

/// <summary>
/// Provides an adapter for <see cref="ITestOutputHelper"/> to <see cref="ILogger"/>.
/// </summary>
internal partial class XUnitMSBuildLoggerAdapter : ITestOutputHelper, ILogger
{
    private sealed class DiagnosticAggregateList : IReadOnlyList<Diagnostic>
    {
        private readonly IReadOnlyList<Diagnostic> _errors;
        private readonly IReadOnlyList<Diagnostic> _warnings;
        private readonly IReadOnlyList<Diagnostic> _messages;

        public Diagnostic this[int index]
        {
            get
            {
                return index < _errors.Count
                    ? _errors[index]
                    : index - _errors.Count < _warnings.Count
                        ? _warnings[index]
                        : _messages[index]
                    ;
            }
        }

        public int Count => _errors.Count + _warnings.Count + _messages.Count;

        public DiagnosticAggregateList(XUnitMSBuildLoggerAdapter logger)
        {
            _errors = logger._errors;
            _warnings = logger._warnings;
            _messages = logger._messages;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<Diagnostic> GetEnumerator()
        {
            for (int i = 0; i < _errors.Count; i++)
            {
                yield return _errors[i];
            }

            for (int i = 0; i < _warnings.Count; i++)
            {
                yield return _warnings[i];
            }

            for (int i = 0; i < _messages.Count; i++)
            {
                yield return _messages[i];
            }
        }
    }

    private readonly ITestOutputHelper _output;
    private IEventSource _eventSource;

    string? ILogger.Parameters
    {
        get => null;

        set
        {
        }
    }

    /// <inheritdoc />
    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;



    private readonly List<Diagnostic> _errors = new();
    private readonly List<Diagnostic> _warnings = new();
    private readonly List<Diagnostic> _messages = new();

    public IReadOnlyList<Diagnostic> Errors => _errors.AsReadOnly();
    public IReadOnlyList<Diagnostic> Warnings => _warnings.AsReadOnly();
    public IReadOnlyList<Diagnostic> Messages => _messages.AsReadOnly();
    public IReadOnlyList<Diagnostic> Diagnostics => new DiagnosticAggregateList(this);



    /// <summary>
    /// Initializes a new instance of <see cref="XUnitMSBuildLoggerAdapter"/> for the specified <paramref name="output"/>.
    /// </summary>
    /// <param name="output">The <see cref="ITestOutputHelper"/> to adapt.</param>
    public XUnitMSBuildLoggerAdapter(ITestOutputHelper output)
    {
        _output = output;
        _eventSource = null!;
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
        _eventSource = eventSource;

        eventSource.BuildFinished += OnBuildStatus;
        eventSource.BuildStarted += OnBuildStatus;
        eventSource.ProjectFinished += OnBuildStatus;
        eventSource.ProjectStarted += OnBuildStatus;

        eventSource.ErrorRaised += OnErrorRaised;
        eventSource.WarningRaised += OnWarningRaised;
        eventSource.MessageRaised += OnMessageRaised;
    }

    protected virtual void OnBuildStatus(object sender, BuildStatusEventArgs e)
    {
        if (e.Message is not null)
        {
            WriteLine(e.Message);
        }
    }

    protected virtual void OnErrorRaised(object sender, BuildErrorEventArgs e)
    {
        var diagnostic = new Diagnostic(e);
        _errors.Add(diagnostic);

        if (OnDiagnosticRaised(sender, diagnostic))
        {
            return;
        }

        if (Verbosity <= LoggerVerbosity.Quiet)
        {
            WriteLine(diagnostic.ToString());
        }
    }

    protected virtual void OnWarningRaised(object sender, BuildWarningEventArgs e)
    {
        var diagnostic = new Diagnostic(e);
        _warnings.Add(diagnostic);

        if (OnDiagnosticRaised(sender, diagnostic))
        {
            return;
        }

        if (Verbosity <= LoggerVerbosity.Minimal)
        {
            WriteLine(diagnostic.ToString());
        }
    }

    protected virtual void OnMessageRaised(object sender, BuildMessageEventArgs e)
    {
        var diagnostic = new Diagnostic(e);
        if (e.Importance <= MessageImportance.Normal)
        {
            _messages.Add(diagnostic);
        }

        if (OnDiagnosticRaised(sender, diagnostic))
        {
            return;
        }

        if (
            Verbosity switch
            {
                LoggerVerbosity.Detailed => e.Importance <= MessageImportance.Normal,
                LoggerVerbosity.Diagnostic => e.Importance <= MessageImportance.Low,
                LoggerVerbosity.Quiet or LoggerVerbosity.Minimal or LoggerVerbosity.Normal => e.Importance <= MessageImportance.High,
                _ => throw new NotSupportedException(),
            }
        )
        {
            WriteLine(diagnostic.ToString());
        }
    }

    protected virtual bool OnDiagnosticRaised(object sender, Diagnostic diagnostic)
    {
        return false;
    }

    /// <inheritdoc />
    public virtual void Shutdown()
    {
        _eventSource.BuildFinished -= OnBuildStatus;
        _eventSource.BuildStarted -= OnBuildStatus;
        _eventSource.ProjectFinished -= OnBuildStatus;
        _eventSource.ProjectStarted -= OnBuildStatus;

        _eventSource.ErrorRaised -= OnErrorRaised;
        _eventSource.WarningRaised -= OnWarningRaised;
        _eventSource.MessageRaised -= OnMessageRaised;
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
