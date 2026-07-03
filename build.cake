#addin nuget:?package=Cake.Coverlet&version=5.1.1
#tool dotnet:?package=GitVersion.Tool&version=6.5.1
#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.5.1
#load "version.cake"

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
    version = CalculateVersion();
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

Task("Default")
       .IsDependentOn("Clean")
       .IsDependentOn("Version")
       .IsDependentOn("Restore")
       .IsDependentOn("Build")
       .IsDependentOn("Test");

RunTarget(target);