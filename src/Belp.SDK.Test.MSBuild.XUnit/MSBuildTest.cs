using FluentAssertions;
using Microsoft.Build.Execution;
using Xunit.Abstractions;

namespace Belp.SDK.Test.MSBuild.XUnit;

/// <summary>
/// Provides the boilerplate for testing MSBuild projects.
/// </summary>
public class MSBuildTest
{
    /// <summary>
    /// Inherits from <see cref="BuildParameters"/> and sets additional default values.
    /// </summary>
    protected class BuildParametersWithDefaults : BuildParameters
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BuildParameters"/> with additional default values.
        /// </summary>
        public BuildParametersWithDefaults(MSBuildTest test) : this(test.Logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BuildParameters"/> with additional default values.
        /// </summary>
        public BuildParametersWithDefaults(XUnitMSBuildLoggerAdapter logger)
        {
            EnableNodeReuse = true;
            Loggers = Loggers == null
                ? logger.AsSingleEnumerable()
                : Loggers.Append(logger)
                ;
        }
    }

    /// <summary>
    /// Gets the test's logger.
    /// </summary>
    protected XUnitMSBuildLoggerAdapter Logger { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="MSBuildTest"/>.
    /// </summary>
    /// <param name="output">The test output helper from xUnit.</param>
    public MSBuildTest(ITestOutputHelper output)
    {
        Logger = new XUnitMSBuildLoggerAdapter(output);
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
    public static void VerifyBuild(BuildResult buildResult)
    {
        _ = buildResult.OverallResult.Should().Be(BuildResultCode.Success);
    }
}
