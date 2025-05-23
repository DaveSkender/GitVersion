using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Agents.Tests;

[TestFixture]
public class AzurePipelinesTests : TestBase
{
    private const string key = "BUILD_BUILDNUMBER";
    private const string logPrefix = "##vso[build.updatebuildnumber]";

    private IEnvironment environment;
    private AzurePipelines buildServer;

    [SetUp]
    public void SetEnvironmentVariableForTest()
    {
        var sp = ConfigureServices(services => services.AddSingleton<AzurePipelines>());
        this.environment = sp.GetRequiredService<IEnvironment>();
        this.buildServer = sp.GetRequiredService<AzurePipelines>();

        this.environment.SetEnvironmentVariable(key, "Some Build_Value $(GitVersion_FullSemVer) 20151310.3 $(UnknownVar) Release");
    }

    [TearDown]
    public void ClearEnvironmentVariableForTest() => this.environment.SetEnvironmentVariable(key, null);

    [Test]
    public void ShouldSetBuildNumber()
    {
        var vars = new TestableGitVersionVariables { FullSemVer = "0.0.0-Unstable4" };
        var vsVersion = this.buildServer.SetBuildNumber(vars);

        vsVersion.ShouldBe("##vso[build.updatebuildnumber]Some Build_Value 0.0.0-Unstable4 20151310.3 $(UnknownVar) Release");
    }

    [Test]
    public void ShouldSetOutputVariables()
    {
        var vsVersion = this.buildServer.SetOutputVariables("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");

        vsVersion.ShouldContain("##vso[task.setvariable variable=GitVersion.Foo]0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
        vsVersion.ShouldContain("##vso[task.setvariable variable=GitVersion.Foo;isOutput=true]0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
    }

    [Test]
    public void MissingEnvShouldNotBlowUp()
    {
        this.environment.SetEnvironmentVariable(key, null);

        const string semver = "0.0.0-Unstable4";
        var vars = new TestableGitVersionVariables { FullSemVer = semver };
        var vsVersion = this.buildServer.SetBuildNumber(vars);
        vsVersion.ShouldBe(semver);
    }

    [TestCase("$(GitVersion.FullSemVer)", "1.0.0", "1.0.0")]
    [TestCase("$(GITVERSION_FULLSEMVER)", "1.0.0", "1.0.0")]
    [TestCase("$(GitVersion.FullSemVer)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
    [TestCase("$(GITVERSION_FULLSEMVER)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
    public void AzurePipelinesBuildNumberWithFullSemVer(string buildNumberFormat, string myFullSemVer, string expectedBuildNumber)
    {
        this.environment.SetEnvironmentVariable(key, buildNumberFormat);
        var vars = new TestableGitVersionVariables { FullSemVer = myFullSemVer };
        var logMessage = this.buildServer.SetBuildNumber(vars);
        logMessage.ShouldBe(logPrefix + expectedBuildNumber);
    }

    [TestCase("$(GitVersion.SemVer)", "1.0.0", "1.0.0")]
    [TestCase("$(GITVERSION_SEMVER)", "1.0.0", "1.0.0")]
    [TestCase("$(GitVersion.SemVer)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
    [TestCase("$(GITVERSION_SEMVER)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
    public void AzurePipelinesBuildNumberWithSemVer(string buildNumberFormat, string mySemVer, string expectedBuildNumber)
    {
        this.environment.SetEnvironmentVariable(key, buildNumberFormat);
        var vars = new TestableGitVersionVariables { SemVer = mySemVer };
        var logMessage = this.buildServer.SetBuildNumber(vars);
        logMessage.ShouldBe(logPrefix + expectedBuildNumber);
    }

    [Test]
    public void GetCurrentBranchShouldHandleBranches()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BUILD_SOURCEBRANCH", $"refs/heads/{MainBranch}");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe($"refs/heads/{MainBranch}");
    }

    [Test]
    public void GetCurrentBranchShouldHandleTags()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BUILD_SOURCEBRANCH", "refs/tags/1.0.0");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void GetCurrentBranchShouldHandlePullRequests()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("BUILD_SOURCEBRANCH", "refs/pull/1/merge");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe("refs/pull/1/merge");
    }
}
