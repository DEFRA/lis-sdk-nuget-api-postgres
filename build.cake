#nullable enable
#addin nuget:?package=Cake.Coverlet&version=5.1.1
#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.5.1
#load "base.cake" 
#load "version.cake"
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var PACK_OUTPUT_DIR = Argument("output_dir", "artifacts");
var TEST_COVERAGE_OUTPUT_DIR = ".coverage";
var solution_file_name = Argument<string>("solution_file_name", "");
var version = Argument<string>("package_version", "");
var github_token = Argument<string>("github_token", "");
Task("Clean")
    .Does(() => {
 
   if (BuildSystem.GitHubActions.IsRunningOnGitHubActions)
    {
      Information("Nothing to clean on Github Pipelines.");
    }
    else
    {
        var cleanSettings = new DotNetCleanSettings
        {
            Verbosity = DotNetVerbosity.Minimal,
            Configuration = configuration
        };
        
        if (!string.IsNullOrEmpty(solution_file_name))
        {
            DotNetClean(solution_file_name, cleanSettings);
        }
        else
        {
            var projects = GetFiles("./**/*.csproj");
            if (!projects.Any())
            {
                projects = GetFiles("./**/**/*.csproj");
            }
            projects.ToList().ForEach(project => {
                DotNetClean(project.ToString(), cleanSettings);
                Information($"Cleaning project {project.ToString()}");
            });
        }
    }
});

Task("Version")
     .IsDependentOn("Clean")
     .Description("Generate the version number for the assembly")
     .Does(() => {
      if (BuildSystem.GitHubActions.IsRunningOnGitHubActions)
         {
           if(string.IsNullOrEmpty(version))
           {
             version = CalculateVersion();
           }
         }
         else
         {
             if(string.IsNullOrEmpty(version))
             {
                version = CalculateVersion();
             }
         }
         Information($"Version {version}");
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
   var projects = GetFiles("./**/*.csproj");
   if (!projects.Any())
   {
       projects = GetFiles("./**/**/*.csproj");
   }
   projects.ToList().ForEach(project => {
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
     var projects = GetFiles("./**/*.csproj");
     if (!projects.Any())
     {
         projects = GetFiles("./**/**/*.csproj");
     }
     projects.ToList().ForEach(project => {
         Information($"Building {project.ToString()}");
         DotNetBuild(project.ToString(),buildSettings);
     });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
       
       var testSettings = new DotNetTestSettings  {
                 Configuration = configuration,
       };
        var coverageOutput = Directory(TEST_COVERAGE_OUTPUT_DIR);             
     
       var testProjects = GetFiles("./tests/**/*.csproj");
       if (!testProjects.Any())
       {
           testProjects = GetFiles("./tests/**/*.csproj");
       }

       testProjects.ToList().ForEach(project => {
          Information($"Testing Project : {project.ToString()}");
            
          var codeCoverageOutputName = $"{project.GetFilenameWithoutExtension()}.cobertura.xml";
          var coverletSettings = new CoverletSettings {
              CollectCoverage = true,
               CoverletOutputFormat = CoverletOutputFormat.cobertura,
               CoverletOutputDirectory =  coverageOutput,
               CoverletOutputName =codeCoverageOutputName,
               ArgumentCustomization = args => args.Append("--logger trx")
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
        OutputDirectory = MakeAbsolute(Directory(PACK_OUTPUT_DIR)),
        MSBuildSettings = new DotNetMSBuildSettings()
                        .WithProperty("PackageVersion", version)
                        .WithProperty("Copyright", $"© Copyright {DateTime.Now.Year}")
                        .WithProperty("Version", version)
    };
    
    if (string.IsNullOrEmpty(solution_file_name))
    {
        var projects = GetFiles("./src/**/*.csproj");
        if (!projects.Any())
        {
            projects = GetFiles("./src/*.csproj");
        }
        projects.ToList().ForEach(project => {
            Information($"Packing {project.ToString()}");
            DotNetPack(project.ToString(), settings);
        });
    }
    else
    {
        DotNetPack(solution_file_name, settings);
    }
});

Task("Default")
       .IsDependentOn("Clean")
       .IsDependentOn("Version")
       .IsDependentOn("Restore")
       .IsDependentOn("Build")
       .IsDependentOn("Test")
       .IsDependentOn("Pack");

RunTarget(target);