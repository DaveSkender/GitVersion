using GitVersion.Extensions;

namespace GitVersion.Core.Tests.IntegrationTests;

internal static class RepositoryFixtureExtensions
{
    public static void MakeACommit(this RepositoryFixtureBase fixture, string commitMsg)
    {
        fixture.Repository.MakeACommit(commitMsg);
        var diagramBuilder = (StringBuilder?)typeof(SequenceDiagram)
            .GetField("diagramBuilder", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(fixture.SequenceDiagram);

        var participant = GetParticipant(fixture.Repository.Head.FriendlyName);
        if (participant != null)
            diagramBuilder?.AppendLineFormat("{0} -> {0}: Commit '{1}'", participant, commitMsg);
        return;

        string? GetParticipant(string participantName) =>
            (string?)typeof(SequenceDiagram).GetMethod("GetParticipant", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(fixture.SequenceDiagram,
                [
                    participantName
                ]);
    }
}
