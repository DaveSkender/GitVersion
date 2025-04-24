using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class LastCommitOnTrunkWithPreReleaseTag : CommitOnTrunkWithPreReleaseTagBase
{
    // B 58 minutes ago (HEAD -> main) (tag 0.2.0-1) <<--
    // A 59 minutes ago

    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is null;

    public override IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        foreach (var item in base.GetIncrements(iteration, commit, context))
        {
            yield return item;
        }

        if (!iteration.GetEffectiveConfiguration(context.Configuration).IsMainBranch) yield break;
        context.Increment = commit.GetIncrementForcedByBranch(context.Configuration);

        var effectiveConfiguration = commit.GetEffectiveConfiguration(context.Configuration);
        context.Label = effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null);
        context.ForceIncrement = false;

        yield return new BaseVersionOperator
        {
            Source = GetType().Name,
            BaseVersionSource = context.BaseVersionSource,
            Increment = context.Increment,
            ForceIncrement = context.ForceIncrement,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max()
        };
    }
}
