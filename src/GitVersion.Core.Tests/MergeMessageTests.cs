using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;

namespace GitVersion.Core.Tests;

[TestFixture]
public class MergeMessageTests : TestBase
{
    private readonly GitFlowConfigurationBuilder configurationBuilder = GitFlowConfigurationBuilder.New;

    [Test]
    public void NullMessageStringThrows() =>
        // Act / Assert
        Should.Throw<ArgumentNullException>(() => new MergeMessage(null!, this.configurationBuilder.Build()));

    [TestCase("")]
    [TestCase("\t\t  ")]
    public void EmptyMessageString(string message)
    {
        // Act
        var sut = new MergeMessage(message, this.configurationBuilder.Build());

        // Assert
        sut.TargetBranch.ShouldBeNull();
        sut.MergedBranch.ShouldBeNull();
        sut.IsMergedPullRequest.ShouldBeFalse();
        sut.PullRequestNumber.ShouldBe(null);
        sut.Version.ShouldBeNull();
    }

    [TestCase("")]
    [TestCase("\t\t  ")]
    [TestCase(null)]
    public void EmptyTagPrefix(string? prefix)
    {
        // Arrange
        const string message = "Updated some code.";
        var configuration = GitHubFlowConfigurationBuilder.New.WithTagPrefixPattern(prefix).Build();

        // Act
        var sut = new MergeMessage(message, configuration);

        // Assert
        sut.TargetBranch.ShouldBeNull();
        sut.MergedBranch.ShouldBeNull();
        sut.IsMergedPullRequest.ShouldBeFalse();
        sut.PullRequestNumber.ShouldBe(null);
        sut.Version.ShouldBeNull();
    }

    private static readonly object?[] MergeMessages =
    [
        new object?[] { "Merge branch 'feature/one'", "feature/one", null, null },
        new object?[] { "Merge branch 'origin/feature/one'", "origin/feature/one", null, null },
        new object?[] { $"Merge tag 'v4.0.0' into {MainBranch}", "v4.0.0", MainBranch, new SemanticVersion(4) },
        new object?[] { $"Merge tag 'V4.0.0' into {MainBranch}", "V4.0.0", MainBranch, new SemanticVersion(4) },
        new object?[] { "Merge branch 'feature/4.1.0/one'", "feature/4.1.0/one", null, new SemanticVersion(4, 1) },
        new object?[] { "Merge branch 'origin/4.1.0/feature/one'", "origin/4.1.0/feature/one", null, new SemanticVersion(4, 1) },
        new object?[] { $"Merge tag 'v://10.10.10.10' into {MainBranch}", "v://10.10.10.10", MainBranch, null }
    ];

    [TestCaseSource(nameof(MergeMessages))]
    public void ParsesMergeMessage(
        string message,
        string expectedMergedBranch,
        string expectedTargetBranch,
        SemanticVersion expectedVersion)
    {
        // Act
        var sut = new MergeMessage(message, this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe("Default");
        sut.TargetBranch.ShouldBe(expectedTargetBranch);
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBe(expectedMergedBranch);
        sut.IsMergedPullRequest.ShouldBeFalse();
        sut.PullRequestNumber.ShouldBe(null);
        sut.Version.ShouldBe(expectedVersion);
    }

    private static readonly object?[] GitHubPullPullMergeMessages =
    [
        new object?[] { "Merge pull request #1234 from organization/feature/one", "feature/one", null, null, 1234 },
        new object?[] { "Merge pull request #1234 in organization/feature/one", "feature/one", null, null, 1234 },
        new object?[] { "Merge pull request #1234 in v4.0.0", "v4.0.0", null, new SemanticVersion(4), 1234 },
        new object?[] { "Merge pull request #1234 in organization/feature/4.1.0/one", "feature/4.1.0/one", null, new SemanticVersion(4, 1), 1234 },
        new object?[] { "Merge pull request #1234 from organization/feature/one into dev", "feature/one", "dev", null, 1234 }
    ];

    [TestCaseSource(nameof(GitHubPullPullMergeMessages))]
    public void ParsesGitHubPullMergeMessage(
        string message,
        string expectedMergedBranch,
        string expectedTargetBranch,
        SemanticVersion expectedVersion,
        int? expectedPullRequestNumber)
    {
        // Act
        var sut = new MergeMessage(message, this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe("GitHubPull");
        sut.TargetBranch.ShouldBe(expectedTargetBranch);
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBe(expectedMergedBranch);
        sut.IsMergedPullRequest.ShouldBeTrue();
        sut.PullRequestNumber.ShouldBe(expectedPullRequestNumber);
        sut.Version.ShouldBe(expectedVersion);
    }

    private static readonly object?[] BitBucketPullMergeMessages =
    [
        new object?[] { "Merge pull request #1234 from feature/one from feature/two to dev", "feature/two", "dev", null, 1234 },
        new object?[] { "Merge pull request #1234 in feature/one from feature/two to dev", "feature/two", "dev", null, 1234 },
        new object?[] { "Merge pull request #1234 in v4.0.0 from v4.1.0 to dev", "v4.1.0", "dev", new SemanticVersion(4, 1), 1234 },
        new object?[] { "Merge pull request #1234 from origin/feature/one from origin/feature/4.2.0/two to dev", "origin/feature/4.2.0/two", "dev", new SemanticVersion(4, 2), 1234 },
        new object?[] { "Merge pull request #1234 in feature/4.1.0/one from feature/4.2.0/two to dev", "feature/4.2.0/two", "dev", new SemanticVersion(4, 2), 1234 },
        new object?[] { $"Merge pull request #1234 from feature/one from feature/two to {MainBranch}", "feature/two", MainBranch, null, 1234 },
        new object?[] { "Merge pull request #1234 in V4.1.0 from V://10.10.10.10 to dev", "V://10.10.10.10", "dev", null, 1234 },
        //TODO: Investigate successful bitbucket merge messages that may be invalid
        // Regex has double 'from/in from' section.  Is that correct?
        new object?[] { $"Merge pull request #1234 from feature/one from v4.0.0 to {MainBranch}", "v4.0.0", MainBranch, new SemanticVersion(4), 1234 }
    ];

    [TestCaseSource(nameof(BitBucketPullMergeMessages))]
    public void ParsesBitBucketPullMergeMessage(
        string message,
        string expectedMergedBranch,
        string expectedTargetBranch,
        SemanticVersion expectedVersion,
        int? expectedPullRequestNumber)
    {
        // Act
        var sut = new MergeMessage(message, this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe("BitBucketPull");
        sut.TargetBranch.ShouldBe(expectedTargetBranch);
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBe(expectedMergedBranch);
        sut.IsMergedPullRequest.ShouldBeTrue();
        sut.PullRequestNumber.ShouldBe(expectedPullRequestNumber);
        sut.Version.ShouldBe(expectedVersion);
    }

    private static readonly object[] BitBucketPullMergeMessages_v7 =
    [
        new object[] { $"""
                        Pull request #68: Release/2.2.0

                        Merge in aaa/777 from release/2.2.0 to {MainBranch}

                        * commit '750aa37753dec1a85b22cc16db851187649d9e97':
                        """, "release/2.2.0", MainBranch, new SemanticVersion(2, 2), 68 }
    ];

    [TestCaseSource(nameof(BitBucketPullMergeMessages_v7))]
    public void ParsesBitBucketPullMergeMessage_v7(
        string message,
        string expectedMergedBranch,
        string expectedTargetBranch,
        SemanticVersion expectedVersion,
        int? expectedPullRequestNumber)
    {
        // Act
        var sut = new MergeMessage(message, this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe("BitBucketPullv7");
        sut.TargetBranch.ShouldBe(expectedTargetBranch);
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBe(expectedMergedBranch);
        sut.IsMergedPullRequest.ShouldBeTrue();
        sut.PullRequestNumber.ShouldBe(expectedPullRequestNumber);
        sut.Version.ShouldBe(expectedVersion);
    }

    private static readonly object?[] BitBucketCloudPullMergeMessages =
    [
        new object?[] { """
                        Merged in release/2.301.0 (pull request #1789)

                        Release/2.301.0

                        Approved-by: John Doe
                        """, "release/2.301.0", null, new SemanticVersion(2, 301), 1789 }
    ];

    [TestCaseSource(nameof(BitBucketCloudPullMergeMessages))]
    public void ParsesBitBucketCloudPullMergeMessage(
        string message,
        string expectedMergedBranch,
        string expectedTargetBranch,
        SemanticVersion expectedVersion,
        int? expectedPullRequestNumber)
    {
        // Act
        var sut = new MergeMessage(message, this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe("BitBucketCloudPull");
        sut.TargetBranch.ShouldBe(expectedTargetBranch);
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBe(expectedMergedBranch);
        sut.IsMergedPullRequest.ShouldBeTrue();
        sut.PullRequestNumber.ShouldBe(expectedPullRequestNumber);
        sut.Version.ShouldBe(expectedVersion);
    }

    private static readonly object?[] SmartGitMergeMessages =
    [
        new object?[] { "Finish feature/one", "feature/one", null, null },
        new object?[] { "Finish origin/feature/one", "origin/feature/one", null, null },
        new object?[] { "Finish v4.0.0", "v4.0.0", null, new SemanticVersion(4) },
        new object?[] { "Finish feature/4.1.0/one", "feature/4.1.0/one", null, new SemanticVersion(4, 1) },
        new object?[] { "Finish origin/4.1.0/feature/one", "origin/4.1.0/feature/one", null, new SemanticVersion(4, 1) },
        new object?[] { "Finish V://10.10.10.10", "V://10.10.10.10", null, null },
        new object?[] { $"Finish V4.0.0 into {MainBranch}", "V4.0.0", MainBranch, new SemanticVersion(4) }
    ];

    [TestCaseSource(nameof(SmartGitMergeMessages))]
    public void ParsesSmartGitMergeMessage(
        string message,
        string expectedMergedBranch,
        string expectedTargetBranch,
        SemanticVersion expectedVersion)
    {
        // Act
        var sut = new MergeMessage(message, this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe("SmartGit");
        sut.TargetBranch.ShouldBe(expectedTargetBranch);
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBe(expectedMergedBranch);
        sut.IsMergedPullRequest.ShouldBeFalse();
        sut.PullRequestNumber.ShouldBe(null);
        sut.Version.ShouldBe(expectedVersion);
    }

    private static readonly object?[] RemoteTrackingMergeMessages =
    [
        new object?[] { $"Merge remote-tracking branch 'feature/one' into {MainBranch}", "feature/one", MainBranch, null },
        new object?[] { "Merge remote-tracking branch 'origin/feature/one' into dev", "origin/feature/one", "dev", null },
        new object?[] { $"Merge remote-tracking branch 'v4.0.0' into {MainBranch}", "v4.0.0", MainBranch, new SemanticVersion(4) },
        new object?[] { $"Merge remote-tracking branch 'V4.0.0' into {MainBranch}", "V4.0.0", MainBranch, new SemanticVersion(4) },
        new object?[] { "Merge remote-tracking branch 'feature/4.1.0/one' into dev", "feature/4.1.0/one", "dev", new SemanticVersion(4, 1) },
        new object?[] { $"Merge remote-tracking branch 'origin/4.1.0/feature/one' into {MainBranch}", "origin/4.1.0/feature/one", MainBranch, new SemanticVersion(4, 1) },
        new object?[] { $"Merge remote-tracking branch 'v://10.10.10.10' into {MainBranch}", "v://10.10.10.10", MainBranch, null }
    ];

    [TestCaseSource(nameof(RemoteTrackingMergeMessages))]
    public void ParsesRemoteTrackingMergeMessage(
        string message,
        string expectedMergedBranch,
        string expectedTargetBranch,
        SemanticVersion expectedVersion)
    {
        // Act
        var sut = new MergeMessage(message, this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe("RemoteTracking");
        sut.TargetBranch.ShouldBe(expectedTargetBranch);
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBe(expectedMergedBranch);
        sut.IsMergedPullRequest.ShouldBeFalse();
        sut.PullRequestNumber.ShouldBe(null);
        sut.Version.ShouldBe(expectedVersion);
    }

    private static readonly object?[] InvalidMergeMessages =
    [
        new object?[] { "Merge pull request # from feature/one" },
        new object?[] { $"Merge pull request # in feature/one from feature/two to {MainBranch}" },
        new object?[] { $"Zusammengeführter PR : feature/one mit {MainBranch} mergen" }
    ];

    [TestCaseSource(nameof(InvalidMergeMessages))]
    public void ParsesInvalidMergeMessage(string message)
    {
        // Act
        var sut = new MergeMessage(message, this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBeNull();
        sut.TargetBranch.ShouldBeNull();
        sut.MergedBranch.ShouldBeNull();
        sut.IsMergedPullRequest.ShouldBeFalse();
        sut.PullRequestNumber.ShouldBeNull();
        sut.Version.ShouldBeNull();
    }

    [Test]
    public void MatchesSingleCustomMessage()
    {
        // Arrange
        const string message = "My custom message";
        const string definition = "MyCustom";
        this.configurationBuilder.WithMergeMessageFormats(new Dictionary<string, string>
        {
            [definition] = message
        });

        // Act
        var sut = new MergeMessage(message, this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe(definition);
        sut.TargetBranch.ShouldBeNull();
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBeEmpty();
        sut.IsMergedPullRequest.ShouldBeFalse();
        sut.PullRequestNumber.ShouldBe(null);
        sut.Version.ShouldBeNull();
    }

    [Test]
    public void MatchesMultipleCustomMessages()
    {
        // Arrange
        const string format = "My custom message";
        const string definition = "MyCustom";
        this.configurationBuilder.WithMergeMessageFormats(new Dictionary<string, string>
        {
            ["Default2"] = "some example",
            ["Default3"] = "another example",
            [definition] = format
        });

        // Act
        var sut = new MergeMessage(format, this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe(definition);
        sut.TargetBranch.ShouldBeNull();
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBeEmpty();
        sut.IsMergedPullRequest.ShouldBeFalse();
        sut.PullRequestNumber.ShouldBe(null);
        sut.Version.ShouldBeNull();
    }

    [Test]
    public void MatchesCaptureGroupsFromCustomMessages()
    {
        // Arrange
        const string format = @"^Merged PR #(?<PullRequestNumber>\d+) into (?<TargetBranch>[^\s]*) from (?:(?<SourceBranch>[^\s]*))";
        const string definition = "MyCustom";
        this.configurationBuilder.WithMergeMessageFormats(new Dictionary<string, string>
        {
            [definition] = format
        });
        const int pr = 1234;
        const string source = "feature/2.0.0/example";

        // Act
        var sut = new MergeMessage($"Merged PR #{pr} into {MainBranch} from {source}", this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe(definition);
        sut.TargetBranch.ShouldBe(MainBranch);
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBe(source);
        sut.IsMergedPullRequest.ShouldBeTrue();
        sut.PullRequestNumber.ShouldBe(pr);
        sut.Version.ShouldBe(new(2));
    }

    [Test]
    public void ReturnsAfterFirstMatchingPattern()
    {
        // Arrange
        const string format = @"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*";
        const string definition = "MyCustom";
        this.configurationBuilder.WithMergeMessageFormats(new Dictionary<string, string>
        {
            [definition] = format,
            ["Default2"] = format,
            ["Default3"] = format
        });

        // Act
        var sut = new MergeMessage("Merge branch 'this'", this.configurationBuilder.Build());

        // Assert
        sut.FormatName.ShouldBe(definition);
        sut.TargetBranch.ShouldBeNull();
        sut.MergedBranch.ShouldNotBeNull();
        sut.MergedBranch.Friendly.ShouldBe("this");
        sut.IsMergedPullRequest.ShouldBeFalse();
        sut.PullRequestNumber.ShouldBe(null);
        sut.Version.ShouldBeNull();
    }
}
