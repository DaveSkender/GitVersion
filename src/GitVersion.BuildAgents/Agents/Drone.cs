using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class Drone(IEnvironment environment, ILog log, IFileSystem fileSystem) : BuildAgentBase(environment, log, fileSystem)
{
    public const string EnvironmentVariableName = "DRONE";
    protected override string EnvironmentVariable => EnvironmentVariableName;
    public override bool CanApplyToCurrentContext() => "true".Equals(Environment.GetEnvironmentVariable(EnvironmentVariable), StringComparison.OrdinalIgnoreCase);

    public override string SetBuildNumber(GitVersionVariables variables) => variables.FullSemVer;

    public override string[] SetOutputVariables(string name, string? value) =>
    [
        $"GitVersion_{name}={value}"
    ];

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        // In Drone DRONE_BRANCH variable is equal to destination branch in case of pull request
        // https://discourse.drone.io/t/getting-the-branch-a-pull-request-is-created-from/670
        // Unfortunately, DRONE_REFSPEC isn't populated, however CI_COMMIT_REFSPEC can be used instead of.
        var pullRequestNumber = Environment.GetEnvironmentVariable("DRONE_PULL_REQUEST");
        if (!pullRequestNumber.IsNullOrWhiteSpace())
        {
            // DRONE_SOURCE_BRANCH is available in Drone 1.x.x version
            var sourceBranch = Environment.GetEnvironmentVariable("DRONE_SOURCE_BRANCH");
            if (!sourceBranch.IsNullOrWhiteSpace())
                return sourceBranch;

            // In drone lower than 1.x.x source branch can be parsed from CI_COMMIT_REFSPEC
            // CI_COMMIT_REFSPEC - {sourceBranch}:{destinationBranch}
            // https://github.com/drone/drone/issues/2222
            var ciCommitRefSpec = Environment.GetEnvironmentVariable("CI_COMMIT_REFSPEC");
            if (!ciCommitRefSpec.IsNullOrWhiteSpace())
            {
                var colonIndex = ciCommitRefSpec.IndexOf(':');
                if (colonIndex > 0)
                {
                    return ciCommitRefSpec.Substring(0, colonIndex);
                }
            }
        }

        return Environment.GetEnvironmentVariable("DRONE_BRANCH");
    }

    public override bool PreventFetch() => false;
}
