#nullable enable
#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.5.1
#load "base.cake"
#load "version.cake"

var target = Argument("target", "Default");
var version = Argument("package_version", EnvironmentVariable("PACKAGE_VERSION") ?? "");
private readonly string SOLUTION_FILE = GetSolutionFile();
private const string CONFIGURATION = "release";
private const DotNetVerbosity VERBOSITY = DotNetVerbosity.Minimal;

Task("Clean")
    .Does(() => 
    {
        var settings = new DotNetCleanSettings
        {
            Verbosity = VERBOSITY,
            Configuration = CONFIGURATION
        };
        DotNetClean(SOLUTION_FILE, settings);
    });

Task("Version")
    .IsDependentOn("Clean")
    .Description("Calculates the npm package version")
    .Does(() =>
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            version = CalculateVersion();
        }

        Information($"Version {version}");
    });

Task("Install")
    .IsDependentOn("Version")
    .Description("Restoring the solution dependencies")
    .Does(() => {
        Information("Restoring the solution dependencies");
        var settings = new DotNetRestoreSettings
        {
            Verbosity = VERBOSITY,
            Sources = new [] 
            { 
                "https://api.nuget.org/v3/index.json",
            }
        };
        DotNetRestore(SOLUTION_FILE, settings);
    });

Task("Format")
    .IsDependentOn("Install")
    .Description("Executing dotnet format")
    .Does(() => {
        var settings = new DotNetFormatSettings
        {
            VerifyNoChanges = false
        };

        DotNetFormat(SOLUTION_FILE, settings);
    });

Task("Build")
    .IsDependentOn("Format")
    .IsDependentOn("Version")
    .Does(() => {
        var settings = new DotNetBuildSettings {
            Verbosity = VERBOSITY,
            Configuration = CONFIGURATION,
            ArgumentCustomization = args => args
                .Append($"/p:Version={version}")
                .Append("/p:WarningsAsErrors=true")
            };
        DotNetBuild(SOLUTION_FILE, settings);
     });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
        var testSettings = new DotNetTestSettings  {
            Verbosity = VERBOSITY,
            Configuration = CONFIGURATION,
        };
        var coverageOutput = DirectoryPath.FromString("./coverage");
        
        var testProjects = GetFiles("./tests/**/*.csproj");
        if (!testProjects.Any())
        {
            testProjects = GetFiles("./tests/**/*.csproj");
        }

        testProjects.ToList().ForEach(project => {
            Information($"Testing Project : {project.ToString()}");
            
            Information($"Running Tests : { project.ToString()}");
            var projectCoverageDirectory = DirectoryPath.FromString(
                project.GetFilenameWithoutExtension().ToString());
            testSettings.ResultsDirectory = coverageOutput.Combine(projectCoverageDirectory);
            testSettings.ArgumentCustomization = args => args
                .AppendQuoted("--collect:XPlat Code Coverage")
                .Append("--logger trx");
            DotNetTest(project.ToString(), testSettings);
        });
        
        Information($"Directory Path : { coverageOutput.ToString()}");
        var glob = new GlobPattern($"./{coverageOutput}/**/coverage.cobertura.xml");
                 
        Information($"globpattern : { glob.ToString()}");
        var outputDirectory = Directory($"./coverage/reports");
        
        Information($"output Directory : { outputDirectory}");
        var reportSettings = new ReportGeneratorSettings
        {
            ArgumentCustomization = args => args.Append($"-reportTypes:HtmlInline_AzurePipelines_Dark;Cobertura")
        };
        
        ReportGenerator(glob, outputDirectory, reportSettings);
    });

Task("Default")
    .IsDependentOn("Test");

RunTarget(target);
