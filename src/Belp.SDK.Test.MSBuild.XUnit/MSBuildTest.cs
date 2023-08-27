using FluentAssertions;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using System.Collections;
using Xunit.Abstractions;

namespace Belp.SDK.Test.MSBuild.XUnit;

/// <summary>
/// Provides the boilerplate for testing MSBuild projects.
/// </summary>
internal sealed class MSBuildTest
{
    /// <summary>
    /// Inherits from <see cref="BuildParameters"/> and sets additional default values.
    /// </summary>
    private class BuildParametersWithDefaults : BuildParameters
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BuildParameters"/> with additional default values.
        /// </summary>
        public BuildParametersWithDefaults(MSBuildTest test) : this(test._logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BuildParameters"/> with additional default values.
        /// </summary>
        public BuildParametersWithDefaults(XUnitMSBuildLoggerAdapter logger)
        {
            EnableNodeReuse = true;
            Loggers = Loggers == null
                ? new SingleEnumerable<ILogger>(logger)
                : Loggers.Append(logger)
                ;
        }
    }

    /// <summary>
    /// Gets the test's logger.
    /// </summary>
    private readonly XUnitMSBuildLoggerAdapter _logger;

    public IReadOnlyList<Diagnostic>? ExpectedErrors { get; init; }
    public IReadOnlyList<Diagnostic>? ExpectedWarnings { get; init; }
    public IReadOnlyList<Diagnostic>? ExpectedMessages { get; init; }
    public IReadOnlyList<Diagnostic>? ExpectedDiagnostics { get; init; }

    public IReadOnlyDictionary<string, string?>? ExpectedPropertyValues { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="MSBuildTest"/>.
    /// </summary>
    /// <param name="output">The test output helper from xUnit.</param>
    public MSBuildTest(ITestOutputHelper output)
    {
        _logger = new XUnitMSBuildLoggerAdapter(output);
    }

    /// <summary>
    /// Requests the build of the project at the specified path with default parameters and default request data.
    /// </summary>
    /// <param name="projectPath">The path to the project to build.</param>
    /// <returns>The result of the project build.</returns>
    public BuildResult RequestBuild(string projectPath)
    {
        BuildResult result = BuildManager.DefaultBuildManager.Build(
            new BuildParametersWithDefaults(this),
            new BuildRequestData(
                projectPath,
                new Dictionary<string, string>(),
                null,
                new string[] { "Restore", "Build" }, null,
                BuildRequestDataFlags.ProvideProjectStateAfterBuild
            )
        );

        VerifyBuild(result);

        return result;
    }

    /// <summary>
    /// Requests the processing of the specified build request with default parameters.
    /// </summary>
    /// <param name="buildRequest">The request to process.</param>
    /// <returns>The result of the build.</returns>
    public BuildResult RequestBuild(BuildRequestData buildRequest)
    {
        BuildResult result = BuildManager.DefaultBuildManager.Build(
            new BuildParametersWithDefaults(this),
            buildRequest
        );

        VerifyBuild(result);

        return result;
    }

    /// <summary>
    /// Verifies the specified build result is successful.
    /// </summary>
    /// <param name="buildResult">The build result to verify.</param>
    private void VerifyBuild(BuildResult buildResult)
    {
        _ = buildResult.OverallResult.Should().Be(BuildResultCode.Success);
        _ = ExpectedErrors is null or { Count: 0 }
            ? _logger.Errors.Should().BeEmpty()
            : _logger.Errors.Order().Should().BeEquivalentTo(ExpectedErrors.Order())
            ;
        _ = ExpectedWarnings is null or { Count: 0 }
            ? _logger.Warnings.Should().BeEmpty()
            : _logger.Warnings.Order().Should().BeEquivalentTo(ExpectedWarnings.Order())
            ;
        if (ExpectedMessages is not null)
        {
            _ = ExpectedMessages is { Count: 0 }
                ? _logger.Messages.Should().BeEmpty()
                : _logger.Messages.Order().Should().BeEquivalentTo(ExpectedMessages.Order())
                ;
        }
        if (ExpectedDiagnostics is not null)
        {
            _ = ExpectedDiagnostics is { Count: 0 }
                ? _logger.Diagnostics.Should().BeEmpty()
                : _logger.Diagnostics.Order().Should().BeEquivalentTo(ExpectedDiagnostics.Order())
                ;
        }

        if (ExpectedPropertyValues is not null)
        {
            foreach ((string key, string? value) in ExpectedPropertyValues)
            {
                ProjectPropertyInstance? property = buildResult.ProjectStateAfterBuild.GetProperty(key);
                if (value is null)
                {
                    _ = property.Should().BeNull();
                    continue;
                }

                _ = property.Should().NotBeNull();
                _ = property.EvaluatedValue.Should().Be(value);
            }
        }
    }
}

file static class Extensions
{
    public static IOrderedEnumerable<Diagnostic> Order(this IEnumerable<Diagnostic> diagnostics)
    {
        return diagnostics
            .OrderBy(static v => v.Severity)
            .ThenBy(static v => v.Code)
            .ThenBy(static v => v.Span)
            ;
    }
}

file sealed class SingleEnumerable<T> : IEnumerable<T>
{
    private sealed class Enumerator : IEnumerator<T>
    {
        private readonly T _element;
        private int _state;

        public T Current => _state switch
        {
            0 => throw new InvalidOperationException("Enumeration has not started. Call MoveNext."),
            1 => _element,
            2 => throw new InvalidOperationException("Enumeration already finished."),
            _ => throw new InvalidOperationException("Invalid state."),
        };

        object? IEnumerator.Current => Current;

        public Enumerator(T element)
        {
            _element = element;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_state is >= 0 and <= 1)
            {
                _state++;
                return _state == 1;
            }

            return false;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _state = 0;
        }
    }

    private readonly T _element;

    public SingleEnumerable(T element)
    {
        _element = element;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(_element);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
