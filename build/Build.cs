using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.NuGet.NuGetTasks;
using static Nuke.Common.IO.HttpTasks;

[GitHubActions(
    "ubuntu-latest",
    GitHubActionsImage.Ubuntu2204,
    FetchDepth = 0,
    //OnPushBranchesIgnore = new[] {"main"},
    OnPushBranches = new[] {"main"},
    InvokedTargets = new[] {nameof(Push)},
    EnableGitHubToken = true,
    PublishArtifacts = false,
    ImportSecrets = new[] {"NUGET_PUBLISH_KEY"}
)]
[DotNetVerbosityMapping]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitRepository] readonly GitRepository GitRepository;
    [NerdbankGitVersioning] readonly NerdbankGitVersioning NerdbankGitVersioning;

    [Solution] readonly Solution Solution;

    [Parameter(Name = "NUGET_PUBLISH_KEY")] [Secret] string NuGetApiKey;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath ContentFilesDirectory => RootDirectory / "contentFiles";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            ContentFilesDirectory.DeleteDirectory();
            ArtifactsDirectory.DeleteDirectory();
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(path => path.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(path => path.DeleteDirectory());
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution.GetProject("Esc.Sdk.Cli"))
                .SetConfiguration(Configuration)
                .EnableNoRestore());

            var frameworks = from project in Solution.Projects
                from framework in project.GetTargetFrameworks()
                where project.Name == "Esc.Sdk.Cli"
                select (project, framework);

            DotNetPublish(_ => _
                .SetConfiguration(Configuration)
                .SetRepositoryUrl(GitRepository.HttpsUrl)
                .SetNoRestore(SucceededTargets.Contains(Restore))
                .SetAssemblyVersion(NerdbankGitVersioning.AssemblyVersion)
                .SetFileVersion(NerdbankGitVersioning.AssemblyFileVersion)
                .SetInformationalVersion(NerdbankGitVersioning.AssemblyInformationalVersion)
                .EnableNoBuild()
                .EnableNoLogo()
                .When(IsServerBuild, _ => _
                    .EnableContinuousIntegrationBuild())
                .CombineWith(frameworks, (_, v) => _
                    .SetProject(v.project)
                    .SetOutput(ArtifactsDirectory / v.framework)
                    .SetFramework(v.framework))
            );
        });

    Target Download => _ => _
        .Executes(() =>
        {
            //https://github.com/pulumi/esc/releases/download/v0.14.3/esc-v0.14.3-linux-arm64.tar.gz

            const string version = "v0.14.3";
            const string baseUrl = $"https://get.pulumi.com/esc/releases/esc-{version}";

            List<string> operatingSystems =
            [
                "windows",
                "linux",
                "darwin"
            ];

            List<string> architectures =
            [
                "x64",
                "arm64"
            ];

            foreach (var operatingSystem in operatingSystems)
            {
                foreach (var architecture in architectures)
                {
                    var extension = operatingSystem == "windows" ? "zip" : "tar.gz";

                    var url = $"{baseUrl}-{operatingSystem}-{architecture}.{extension}";
                    var filename = $"esc-{version}-{operatingSystem}-{architecture}.{extension}";

                    Log.Information("url: {0} filename: {1}", url, filename);

                    AbsolutePath releaseTempFilename = Path.Combine(TemporaryDirectory, filename);
                    HttpDownloadFile(url, releaseTempFilename, clientConfigurator: settings =>
                    {
                        settings.Timeout = TimeSpan.FromSeconds(15);
                        return settings;
                    });

                    var unCompressDirectory = TemporaryDirectory / operatingSystem / architecture;
                    releaseTempFilename.UncompressTo(unCompressDirectory);

                    unCompressDirectory.GlobFiles("**/esc*").ForEach(file =>
                    {
                        var contentFilesDirectoryName = operatingSystem == "windows"
                            ? $"win-{architecture}"
                            : $"{operatingSystem}-{architecture}";

                        file.Move(RootDirectory / "contentFiles" / contentFilesDirectoryName / file.Name);
                    });
                }
            }
        });

    [Parameter]
    string NuGetSource => "https://api.nuget.org/v3/index.json";

    Target Pack => _ => _
        .DependsOn(Download, Compile)
        .Produces(PackagesDirectory / "*.nupkg")
        .Executes(() =>
        {
            NuGetPack(_ => _
                .SetTargetPath("Esc.nuspec")
                .SetOutputDirectory(PackagesDirectory)
                .SetVersion(NerdbankGitVersioning.NuGetPackageVersion)
            );

            DotNetPack(_ => _
                .SetConfiguration(Configuration)
                .SetNoBuild(SucceededTargets.Contains(Compile))
                .SetOutputDirectory(PackagesDirectory)
                .SetRepositoryUrl(GitRepository.HttpsUrl)
                .SetVersion(NerdbankGitVersioning.NuGetPackageVersion)
            );

            ReportSummary(_ => _
                .AddPair("Packages", PackagesDirectory.GlobFiles("*.nupkg").Count.ToString()));
        });

    Configure<DotNetNuGetPushSettings> PackagePushSettings => _ => _;

    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";

    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            DotNetNuGetPush(_ => _
                    .SetSource(NuGetSource)
                    .SetApiKey(NuGetApiKey)
                    .CombineWith(PackagesDirectory.GlobFiles("*.nupkg"), (_, v) => _
                        .SetTargetPath(v))
                    .Apply(PackagePushSettings),
                5,
                true);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution.GetProject("Esc.Sdk.Cli")));
        });

    AbsolutePath SourceDirectory => RootDirectory / "src";

    AbsolutePath TestsDirectory => RootDirectory / "tests";

    public static int Main() => Execute<Build>(x => x.Compile);
}

