#addin nuget:?package=Cake.Coverlet&version=5.1.1
#tool dotnet:?package=GitVersion.Tool&version=6.5.1
#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.5.1

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var sonarToken = EnvironmentVariable("SONAR_TOKEN");
var sonarVersion = EnvironmentVariable("SONAR_VERSION");
var SonarScannerPath = Argument("sonarscannerpath", "./.sonar/scanner/dotnet-sonarscanner");
var DotNetCoveragePath = Argument("dotnetcoveragepath", "./.sonar/coverage/dotnet-coverage");
const string SonarCoverageFile = "coverage.xml";

const string TEST_COVERAGE_OUTPUT_DIR = "coverage";
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
    .Does(() => {
 /*   var result = GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true
    }); */
    
    version = "1.02.0";
    Information($"Version: { version }");
});
Task("Restore")
    .IsDependentOn("Clean")
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
    .Does(() => {
     var buildSettings = new DotNetBuildSettings {
                        Configuration = configuration,
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
              var outputDirectory = Directory("./coverage/reports");
             
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
Task("Sonar-Install")
    .Description("Installs the SonarCloud scanner and dotnet-coverage tools")
    .Does(() => {
        EnsureDirectoryExists("./.sonar/scanner");
        EnsureDirectoryExists("./.sonar/coverage");

        StartProcess("dotnet", new ProcessSettings {
            Arguments = "tool update dotnet-sonarscanner --tool-path ./.sonar/scanner"
        });

        StartProcess("dotnet", new ProcessSettings {
            Arguments = "tool update dotnet-coverage --tool-path ./.sonar/coverage"
        });
    });

Task("Sonar-Begin")
    .IsDependentOn("Sonar-Install")
    .Description("Starts SonarCloud analysis")
    .Does(() => {
        if (string.IsNullOrWhiteSpace(sonarToken))
        {
            throw new Exception("SONAR_TOKEN environment variable is required to run SonarCloud analysis.");
        }

        StartProcess(SonarScannerPath, new ProcessSettings {
            Arguments = string.Join(" ", new [] {
                "begin",
                "/k:\"DEFRA_identity-service-helper\"",
                "/o:\"defra\"",
                $"/d:sonar.token=\"{sonarToken}\"",
                "/d:sonar.host.url=\"https://sonarcloud.io\"",
                $"/v:\"{sonarVersion}\"",
                "/d:sonar.cs.vscoveragexml.reportsPaths=coverage.xml",
                "/d:sonar.exclusions=\"changelog/**,.github/**\"",
                "/d:sonar.dotnet.excludeTestProjects=true"
            })
        });
    });

Task("Sonar-Build")
    .IsDependentOn("Sonar-Begin")
    .Description("Builds the solution for SonarCloud analysis")
    .Does(() => {
        DotNetBuild(SolutionFileName, new DotNetBuildSettings {
            Configuration = configuration,
            NoIncremental = true
        });
    });

Task("Sonar-Test")
    .IsDependentOn("Sonar-Build")
    .Description("Runs tests and collects coverage for SonarCloud")
    .Does(() => {
        StartProcess(DotNetCoveragePath, new ProcessSettings {
            Arguments = $"collect \"dotnet test --configuration {configuration} --no-build\" -f xml -o \"{SonarCoverageFile}\""
        });
    });

Task("Sonar-End")
    .IsDependentOn("Sonar-Test")
    .Description("Completes SonarCloud analysis")
    .Does(() => {
        if (string.IsNullOrWhiteSpace(sonarToken))
        {
            throw new Exception("SONAR_TOKEN environment variable is required to run SonarCloud analysis.");
        }

        StartProcess(SonarScannerPath, new ProcessSettings {
            Arguments = $"end /d:sonar.token=\"{sonarToken}\""
        });
    });

Task("Sonar")
    .IsDependentOn("Sonar-End")
    .Description("Runs the full SonarCloud analysis pipeline");
    
Task("Default")
       .IsDependentOn("Clean")
       .IsDependentOn("Restore")
       .IsDependentOn("Version")
       .IsDependentOn("Build")
       .IsDependentOn("Test")
       .IsDependentOn("Pack")
       .IsDependentOn("Publish");

RunTarget(target);