#load "version.cake"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var github_token = Argument<string>("github_token", "");

const string SolutionFileName = "Database.slnx";
string version = String.Empty;
const string PACK_OUTPUT_DIR = ".artifacts";
const string PUBLISH_URL = "https://nuget.pkg.github.com/defra/index.json"
Task("Clean")
    .Does(() => {
    if (BuildSystem.GitHubActions.IsRunningOnGitHubActions)
    {
        Information("Nothing to clean on Github Pipelines.");
    }
    else
    {
        DotNetClean(SolutionFileName);
    }
});

Task("Version")
    .IsDependentOn("Clean")
    .Description("Generate the version number for the assembly")
    .Does(() => {
    version = CalculateVersion();
    Information($"Version: { version }");
});

Task("Pack")
    .IsDependentOn("Version")
    .Does(() => {
    var settings = new DotNetPackSettings
    {
        Configuration = configuration,
        OutputDirectory = PACK_OUTPUT_DIR,
        MSBuildSettings = new DotNetMSBuildSettings()
                        .WithProperty("PackageVersion", version)
                        .WithProperty("Copyright", $"© Copyright {DateTime.Now.Year}")
                        .WithProperty("Version", version)
    };
    
    DotNetPack(SolutionFileName, settings);
});

Task("Publish")
    .IsDependentOn("Pack")
    .Does(() => {
    if (BuildSystem.GitHubActions.IsRunningOnGitHubActions)
    {
        foreach(var file in GetFiles($"./{PACK_OUTPUT_DIR}/*.nupkg"))
        {
            Information("Publishing {0}...", file.GetFilename().FullPath);
            DotNetNuGetPush(file, new DotNetNuGetPushSettings {
                ApiKey = github_token,
                Source = PUBLISH_URL
            });
        } 
    } 
    else
    {
        Information("Not running on GitHub Actions. Skipping Publish.");
    }
});

Task("Default")
    .IsDependentOn("Publish");

RunTarget(target);
