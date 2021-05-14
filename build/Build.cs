using System;
using System.Linq;
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
    public static int Main() => Execute<Build>(x => x.compile_backend);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution BackendSolution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath VSCodeExtensionDir => RootDirectory / "vscode-extension";
    AbsolutePath VSCodeExtensionBinDir => VSCodeExtensionDir / "bin";
    AbsolutePath BackendDirectory => RootDirectory / "backend";

    Target clean => _ => _
        .Before(restore_backend)
        .Executes(() =>
        {
        });

    Target restore_backend => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(BackendSolution));
        });

    Target compile_backend => _ => _
        .DependsOn(restore_backend)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(BackendSolution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target publish_backend => _ => _
        .Executes(() =>
        {
            EnsureCleanDirectory(VSCodeExtensionBinDir);
            DotNetPublish(s => s
                .SetProject(BackendDirectory / "src" / "ILSpy.Backend")
                .SetConfiguration(Configuration.Release)
                .SetRuntime("win-x64")
                .DisableSelfContained()
                .SetOutput(VSCodeExtensionBinDir / "ilspy-backend"));
        });

    Target test_backend => _ => _
        .DependsOn(compile_backend)
        .Executes(() =>
        {
            //DotNetTest(s => s
            //    .SetProjectFile("ILSpy.Backend.Tests"));
        });

    Target backend => _ => _
        .DependsOn(publish_backend);

    Target compile_extension => _ => _
        .Executes(() =>
        {
            NpmInstall(s => s
                .SetProcessWorkingDirectory(VSCodeExtensionDir));
            NpmRun(s => s
                .SetProcessWorkingDirectory(VSCodeExtensionDir)
                .SetCommand("compile"));
        });

    Target test_extension => _ => _
        .DependsOn(compile_extension)
        .Executes(() =>
        {
            Npm("test", VSCodeExtensionDir);
        });

    Target vsix => _ => _
        .DependsOn(backend, compile_extension)
        .Executes(() =>
        {
            StartProcess("vsce", "package -o ilspy-vscode-0.0.0.vsix", VSCodeExtensionDir);
        });
}
