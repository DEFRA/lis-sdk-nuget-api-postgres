#addin nuget:?package=Cake.Coverlet&version=5.1.1
#addin nuget:?package=Cake.MinVer&version=3.0.0
#tool dotnet:?package=GitVersion.Tool&version=6.5.1
#tool dotnet:?package=minver-cli&version=6.0.0
#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.5.1

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

const string TEST_COVERAGE_OUTPUT_DIR = ".coverage";
const string SolutionFileName = "Database.slnx";
string version = String.Empty;
var github_token = Argument<string>("github_token", "");
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
    
    var result = MinVer(new MinVerSettings {
        TagPrefix = "v",
        DefaultPreReleasePhase = "preview",
        
    });
    
    version = result.Version;

    var isGitHubActions = BuildSystem.GitHubActions.IsRunningOnGitHubActions;
    var isPullRequest = BuildSystem.GitHubActions.Environment.PullRequest.IsPullRequest;
    var branchName = BuildSystem.GitHubActions.Environment.Workflow.RefName;
    var isMain = !string.IsNullOrWhiteSpace(branchName) && branchName.Equals("main", StringComparison.OrdinalIgnoreCase);

    if (isPullRequest || !isGitHubActions || !isMain)
    {
        var baseVersion = version.Split('-')[0];
        var height = "0";
        if (version.Contains("-"))
        {
            var preReleasePart = version.Split('-')[1];
            height = preReleasePart.Split('.').Last();
        }
        version = $"{baseVersion}-LREG-XX-alpha.{height}";
    }
    else
    {
        version = version.Split('-')[0];
    }

    Information($"Version: { version }");
});
Task("Restore")
    .IsDependentOn("Version")
    .Description("Restoring the solution dependencies")
    .Does(() => {
    
    Information("Restoring the solution dependencies");
      var settings =  new DotNetRestoreSettings
        {
          Verbosity = DotNetVerbosity.Minimal,
          Sources = new [] { 
             "https://api.nuget.org/v3/index.json",
          }
        };
   GetFiles("./**/**/*.csproj").ToList().ForEach(project => {
       Information($"Restoring {project.ToString()}");
       DotNetRestore(project.ToString(), settings);
     });
});

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Version")
    .Does(() => {
     var buildSettings = new DotNetBuildSettings {
                        Configuration = configuration,
                        ArgumentCustomization = args => args.Append($"/p:Version={version}")
                       };
     GetFiles("./**/**/*.csproj").ToList().ForEach(project => {
         Information($"Building {project.ToString()}");
         DotNetBuild(project.ToString(),buildSettings);
     });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
       
       var testSettings = new DotNetTestSettings  {
                 Configuration = configuration,
                 NoBuild = true,
       };
        var coverageOutput = Directory(TEST_COVERAGE_OUTPUT_DIR);             
     
       GetFiles("./tests/**/*.csproj").ToList().ForEach(project => {
          Information($"Testing Project : {project.ToString()}");
            
          var codeCoverageOutputName = $"{project.GetFilenameWithoutExtension()}.cobertura.xml";
          var coverletSettings = new CoverletSettings {
              CollectCoverage = true,
               CoverletOutputFormat = CoverletOutputFormat.cobertura,
               CoverletOutputDirectory =  coverageOutput,
               CoverletOutputName =codeCoverageOutputName,
               ArgumentCustomization = args => args.Append($"--logger trx")
          };
                  
          Information($"Running Tests : { project.ToString()}");
          DotNetTest(project.ToString(), testSettings, coverletSettings );        
        });
         Information($"Directory Path : { coverageOutput.ToString()}");
                  
              var glob = new GlobPattern($"./{ coverageOutput}/*.cobertura.xml");
                 
              Information($"globpattern : { glob.ToString()}");
              var outputDirectory = Directory($"./{TEST_COVERAGE_OUTPUT_DIR}/reports");
             
             Information($"output Directory : { outputDirectory}");
              var reportSettings = new ReportGeneratorSettings
              {
                 ArgumentCustomization = args => args.Append($"-reportTypes:HtmlInline_AzurePipelines_Dark;Cobertura")
              };
                 
              ReportGenerator(glob, outputDirectory, reportSettings);
});

Task("Pack")
 .IsDependentOn("Test")
 .Does(() => {
 
   var settings = new DotNetPackSettings
    {
        Configuration = configuration,
        OutputDirectory = ".artifacts",
        MSBuildSettings = new DotNetMSBuildSettings()
                        .WithProperty("PackageVersion", version)
                        .WithProperty("Copyright", $"© Copyright  {DateTime.Now.Year}")
                        .WithProperty("Version", version)
    };
    
    DotNetPack(SolutionFileName, settings);
 
 });
Task("Publish")
    .IsDependentOn("Test")
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
});

    
Task("Default")
       .IsDependentOn("Clean")
       .IsDependentOn("Version")
       .IsDependentOn("Restore")
       .IsDependentOn("Build")
       .IsDependentOn("Test")
       .IsDependentOn("Pack")
       .IsDependentOn("Publish");

RunTarget(target);