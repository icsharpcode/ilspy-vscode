using System;
using System.Linq;
using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Npm;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Npm.NpmTasks;
using static Nuke.Common.Tooling.ProcessTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.CompileBackend);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution BackendSolution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath VSCodeExtensionDir => RootDirectory / "vscode-extension";
    AbsolutePath VSCodeExtensionBinDir => VSCodeExtensionDir / "bin";
    AbsolutePath BackendDirectory => RootDirectory / "backend";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Versionize => _ => _
        .Executes(() =>
        {
            ProjectVersionWriter.WriteToVsProject(BackendDirectory / "src" / "ILSpy.Backend" / "ILSpy.Backend.csproj");
            ProjectVersionWriter.WriteToPackageJson(VSCodeExtensionDir / "package.json");
        });

    Target Clean => _ => _
        .Before(RestoreBackend)
        .Executes(() =>
        {
        });

    Target RestoreBackend => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(BackendSolution));
        });

    Target CompileBackend => _ => _
        .DependsOn(RestoreBackend)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(BackendSolution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target PublishBackend => _ => _
        .Executes(() =>
        {
            EnsureCleanDirectory(VSCodeExtensionBinDir);
            DotNetPublish(s => s
                .SetProject(BackendDirectory / "src" / "ILSpy.Backend")
                .SetConfiguration(Configuration.Release)
                .DisableSelfContained()
                .SetProperty("UseAppHost", "false")
                .SetOutput(VSCodeExtensionBinDir / "ilspy-backend"));
        });

    Target TestBackend => _ => _
        .DependsOn(CompileBackend)
        .Executes(() =>
        {
            //DotNetTest(s => s
            //    .SetProjectFile("ILSpy.Backend.Tests"));
        });

    Target Backend => _ => _
        .DependsOn(PublishBackend);

    Target CompileExtension => _ => _
        .Executes(() =>
        {
            NpmInstall(s => s
                .SetProcessWorkingDirectory(VSCodeExtensionDir));
            NpmRun(s => s
                .SetProcessWorkingDirectory(VSCodeExtensionDir)
                .SetCommand("compile"));
        });

    Target TestExtension => _ => _
        .DependsOn(CompileExtension)
        .Executes(() =>
        {
            Npm("test", VSCodeExtensionDir);
        });

    Target Vsix => _ => _
        .DependsOn(Versionize, Backend, CompileExtension)
        .Executes(() =>
        {
            NpmInstall(s => s
                .SetPackages("vsce")
                .SetGlobal(true));
            EnsureExistingDirectory(ArtifactsDirectory);
            var vsixFileName = $"ilspy-vscode-{ProjectVersion.Version.ToString(3) }.vsix";
            using var vsceProcess = StartProcess("vsce", $"package -o {ArtifactsDirectory / vsixFileName}", VSCodeExtensionDir);
            vsceProcess.AssertZeroExitCode();
        });
}
