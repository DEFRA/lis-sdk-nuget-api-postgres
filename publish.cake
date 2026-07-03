#load "version.cake"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var github_token = Argument<string>("github_token", "");

const string SolutionFileName = "Database.slnx";
string version = String.Empty;

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
        OutputDirectory = ".artifacts",
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
        foreach(var file in GetFiles("./.artifacts/*.nupkg"))
        {
            Information("Publishing {0}...", file.GetFilename().FullPath);
            DotNetNuGetPush(file, new DotNetNuGetPushSettings {
                ApiKey = github_token,
                Source = "https://nuget.pkg.github.com/defra/index.json"
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
