using GitVersion.Configuration.Attributes;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal record BranchConfiguration : IBranchConfiguration
{
    [JsonPropertyName("mode")]
    [JsonPropertyDescription("The deployment mode for this branch. Can be 'ManualDeployment', 'ContinuousDelivery', 'ContinuousDeployment'.")]
    public DeploymentMode? DeploymentMode { get; internal init; }

    [JsonPropertyName("label")]
    [JsonPropertyDescription("The label to use for this branch. Use the value {BranchName} or similar as a placeholder to insert a named capture group from RegularExpression (fx. the branch name).")]
    public string? Label { get; internal init; }

    [JsonPropertyName("increment")]
    [JsonPropertyDescription("The increment strategy for this branch. Can be 'Inherit', 'Patch', 'Minor', 'Major', 'None'.")]
    public IncrementStrategy Increment { get; internal init; }

    [JsonIgnore]
    IPreventIncrementConfiguration IBranchConfiguration.PreventIncrement => PreventIncrement;

    [JsonPropertyName("prevent-increment")]
    [JsonPropertyDescription("The prevent increment configuration section.")]
    public PreventIncrementConfiguration PreventIncrement { get; internal init; } = new();

    [JsonPropertyName("track-merge-target")]
    [JsonPropertyDescription("Strategy which will look for tagged merge commits directly off the current branch.")]
    public bool? TrackMergeTarget { get; internal init; }

    [JsonPropertyName("track-merge-message")]
    [JsonPropertyDescription("This property is a branch related property and gives the user the possibility to control the behavior of whether the merge commit message will be interpreted as a next version or not.")]
    public bool? TrackMergeMessage { get; internal init; }

    [JsonPropertyName("commit-message-incrementing")]
    [JsonPropertyDescription("Sets whether it should be possible to increment the version with special syntax in the commit message. Can be 'Disabled', 'Enabled' or 'MergeMessageOnly'.")]
    public CommitMessageIncrementMode? CommitMessageIncrementing { get; internal init; }

    [JsonPropertyName("regex")]
    [JsonPropertyDescription("The regular expression pattern to use to match this branch.")]
    [JsonPropertyFormat(Format.Regex)]
    public string? RegularExpression { get; internal init; }

    [JsonIgnore]
    string? IBranchConfiguration.RegularExpression => RegularExpression;

    [JsonPropertyName("source-branches")]
    [JsonPropertyDescription("The source branches for this branch.")]
    public HashSet<string> SourceBranches { get; internal init; } = [];

    [JsonIgnore]
    IReadOnlyCollection<string> IBranchConfiguration.SourceBranches => SourceBranches;

    [JsonPropertyName("is-source-branch-for")]
    [JsonPropertyDescription("The branches that this branch is a source branch.")]
    public HashSet<string> IsSourceBranchFor { get; internal init; } = [];

    [JsonIgnore]
    IReadOnlyCollection<string> IBranchConfiguration.IsSourceBranchFor => IsSourceBranchFor;

    [JsonPropertyName("tracks-release-branches")]
    [JsonPropertyDescription("Indicates this branch configuration represents develop in GitFlow.")]
    public bool? TracksReleaseBranches { get; internal init; }

    [JsonPropertyName("is-release-branch")]
    [JsonPropertyDescription("Indicates this branch configuration represents a release branch in GitFlow.")]
    public bool? IsReleaseBranch { get; internal init; }

    [JsonPropertyName("is-main-branch")]
    [JsonPropertyDescription("When using Mainline mode, this indicates that this branch is a mainline. By default main and support/* are mainlines.")]
    public bool? IsMainBranch { get; internal init; }

    [JsonPropertyName("pre-release-weight")]
    [JsonPropertyDescription("Provides a way to translate the PreReleaseLabel to a number.")]
    public int? PreReleaseWeight { get; internal init; }

    public virtual IBranchConfiguration Inherit(IBranchConfiguration configuration)
    {
        configuration.NotNull();

        return new BranchConfiguration(this)
        {
            Increment = Increment == IncrementStrategy.Inherit ? configuration.Increment : Increment,
            DeploymentMode = DeploymentMode ?? configuration.DeploymentMode,
            Label = Label ?? configuration.Label,
            PreventIncrement = new PreventIncrementConfiguration
            {
                OfMergedBranch = PreventIncrement.OfMergedBranch ?? configuration.PreventIncrement.OfMergedBranch,
                WhenBranchMerged = PreventIncrement.WhenBranchMerged ?? configuration.PreventIncrement.WhenBranchMerged,
                WhenCurrentCommitTagged = PreventIncrement.WhenCurrentCommitTagged ?? configuration.PreventIncrement.WhenCurrentCommitTagged
            },
            TrackMergeTarget = TrackMergeTarget ?? configuration.TrackMergeTarget,
            TrackMergeMessage = TrackMergeMessage ?? configuration.TrackMergeMessage,
            CommitMessageIncrementing = CommitMessageIncrementing ?? configuration.CommitMessageIncrementing,
            RegularExpression = RegularExpression ?? configuration.RegularExpression,
            TracksReleaseBranches = TracksReleaseBranches ?? configuration.TracksReleaseBranches,
            IsReleaseBranch = IsReleaseBranch ?? configuration.IsReleaseBranch,
            IsMainBranch = IsMainBranch ?? configuration.IsMainBranch,
            PreReleaseWeight = PreReleaseWeight ?? configuration.PreReleaseWeight
        };
    }

    public virtual IBranchConfiguration Inherit(EffectiveConfiguration configuration)
    {
        configuration.NotNull();

        return new BranchConfiguration(this)
        {
            Increment = Increment == IncrementStrategy.Inherit ? configuration.Increment : Increment,
            DeploymentMode = DeploymentMode ?? configuration.DeploymentMode,
            Label = Label ?? configuration.Label,
            PreventIncrement = new PreventIncrementConfiguration
            {
                OfMergedBranch = PreventIncrement.OfMergedBranch ?? configuration.PreventIncrementOfMergedBranch,
                WhenBranchMerged = PreventIncrement.WhenBranchMerged ?? configuration.PreventIncrementWhenBranchMerged,
                WhenCurrentCommitTagged = PreventIncrement.WhenCurrentCommitTagged ?? configuration.PreventIncrementWhenCurrentCommitTagged
            },
            TrackMergeTarget = TrackMergeTarget ?? configuration.TrackMergeTarget,
            TrackMergeMessage = TrackMergeMessage ?? configuration.TrackMergeMessage,
            CommitMessageIncrementing = CommitMessageIncrementing ?? configuration.CommitMessageIncrementing,
            RegularExpression = RegularExpression ?? configuration.RegularExpression,
            TracksReleaseBranches = TracksReleaseBranches ?? configuration.TracksReleaseBranches,
            IsReleaseBranch = IsReleaseBranch ?? configuration.IsReleaseBranch,
            IsMainBranch = IsMainBranch ?? configuration.IsMainBranch,
            PreReleaseWeight = PreReleaseWeight ?? configuration.PreReleaseWeight
        };
    }
}
