using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion;

internal class GitVersionExecutor(
    ILog log,
    IFileSystem fileSystem,
    IConsole console,
    IVersionWriter versionWriter,
    IHelpWriter helpWriter,
    IConfigurationFileLocator configurationFileLocator,
    IConfigurationProvider configurationProvider,
    IConfigurationSerializer configurationSerializer,
    IGitVersionCalculateTool gitVersionCalculateTool,
    IGitVersionOutputTool gitVersionOutputTool,
    IGitRepository gitRepository,
    IGitRepositoryInfo repositoryInfo)
    : IGitVersionExecutor
{
    private readonly ILog log = log.NotNull();
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IConsole console = console.NotNull();
    private readonly IVersionWriter versionWriter = versionWriter.NotNull();
    private readonly IHelpWriter helpWriter = helpWriter.NotNull();

    private readonly IConfigurationFileLocator configurationFileLocator = configurationFileLocator.NotNull();
    private readonly IConfigurationProvider configurationProvider = configurationProvider.NotNull();
    private readonly IConfigurationSerializer configurationSerializer = configurationSerializer.NotNull();

    private readonly IGitVersionCalculateTool gitVersionCalculateTool = gitVersionCalculateTool.NotNull();
    private readonly IGitVersionOutputTool gitVersionOutputTool = gitVersionOutputTool.NotNull();
    private readonly IGitRepository gitRepository = gitRepository.NotNull();
    private readonly IGitRepositoryInfo repositoryInfo = repositoryInfo.NotNull();

    public int Execute(GitVersionOptions gitVersionOptions)
    {
        if (!HandleNonMainCommand(gitVersionOptions, out var exitCode))
        {
            exitCode = RunGitVersionTool(gitVersionOptions);
        }

        if (exitCode != 0)
        {
            // Dump log to console if we fail to complete successfully
            this.console.Write(this.log.ToString());
        }

        return exitCode;
    }

    private int RunGitVersionTool(GitVersionOptions gitVersionOptions)
    {
        this.gitRepository.DiscoverRepository(gitVersionOptions.WorkingDirectory);
        var mutexName = this.repositoryInfo.DotGitDirectory?.Replace(FileSystemHelper.Path.DirectorySeparatorChar.ToString(), "") ?? string.Empty;
        using var mutex = new Mutex(true, $@"Global\gitversion{mutexName}", out var acquired);

        try
        {
            if (!acquired)
            {
                mutex.WaitOne();
            }

            var variables = this.gitVersionCalculateTool.CalculateVersionVariables();

            var configuration = this.configurationProvider.Provide(gitVersionOptions.ConfigurationInfo.OverrideConfiguration);

            this.gitVersionOutputTool.OutputVariables(variables, configuration.UpdateBuildNumber);
            this.gitVersionOutputTool.UpdateAssemblyInfo(variables);
            this.gitVersionOutputTool.UpdateWixVersionFile(variables);
        }
        catch (WarningException exception)
        {
            var error = $"An error occurred:{FileSystemHelper.Path.NewLine}{exception.Message}";
            this.log.Warning(error);
            return 1;
        }
        catch (Exception exception)
        {
            var error = $"An unexpected error occurred:{FileSystemHelper.Path.NewLine}{exception}";
            this.log.Error(error);

            try
            {
                GitExtensions.DumpGraphLog(logMessage => this.log.Info(logMessage));
            }
            catch (Exception dumpGraphException)
            {
                this.log.Error($"Couldn't dump the git graph due to the following error: {dumpGraphException}");
            }
            return 1;
        }
        finally
        {
            mutex.ReleaseMutex();
        }

        return 0;
    }

    private bool HandleNonMainCommand(GitVersionOptions gitVersionOptions, out int exitCode)
    {
        if (gitVersionOptions.IsVersion)
        {
            var assembly = Assembly.GetExecutingAssembly();
            this.versionWriter.Write(assembly);
            exitCode = 0;
            return true;
        }

        if (gitVersionOptions.IsHelp)
        {
            this.helpWriter.Write();
            exitCode = 0;
            return true;
        }

        if (gitVersionOptions.Diag)
        {
            gitVersionOptions.Settings.NoCache = true;
        }

        ConfigureLogging(gitVersionOptions, this.log, this.fileSystem);

        var workingDirectory = gitVersionOptions.WorkingDirectory;
        if (gitVersionOptions.Diag)
        {
            GitExtensions.DumpGraphLog(logMessage => this.log.Info(logMessage));
        }

        if (!this.fileSystem.Directory.Exists(workingDirectory))
        {
            this.log.Warning($"The working directory '{workingDirectory}' does not exist.");
        }
        else
        {
            this.log.Info("Working directory: " + workingDirectory);
        }

        if (gitVersionOptions.ConfigurationInfo.ShowConfiguration)
        {
            if (gitVersionOptions.RepositoryInfo.TargetUrl.IsNullOrWhiteSpace())
            {
                this.configurationFileLocator.Verify(workingDirectory, this.repositoryInfo.ProjectRootDirectory);
            }
            var configuration = this.configurationProvider.Provide();
            var configurationString = configurationSerializer.Serialize(configuration);
            this.console.WriteLine(configurationString);
            exitCode = 0;
            return true;
        }

        exitCode = 0;
        return false;
    }

    private static void ConfigureLogging(GitVersionOptions gitVersionOptions, ILog log, IFileSystem fileSystem)
    {
        if (gitVersionOptions.Output.Contains(OutputType.BuildServer) || gitVersionOptions.LogFilePath == "console")
        {
            log.AddLogAppender(new ConsoleAppender());
        }

        if (gitVersionOptions.LogFilePath != null && gitVersionOptions.LogFilePath != "console")
        {
            log.AddLogAppender(new FileAppender(fileSystem, gitVersionOptions.LogFilePath));
        }
    }
}
