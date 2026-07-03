#load "version.cake"

var productName = Argument<string>("product_name", "DEFRA_identity-service-helper");
var sonarToken = EnvironmentVariable("SONAR_TOKEN");
var sonarVersion = EnvironmentVariable("SONAR_VERSION");
var SonarScannerPath = Argument("sonarscannerpath", "./.sonar/scanner/dotnet-sonarscanner");
var DotNetCoveragePath = Argument("dotnetcoveragepath", "./.sonar/coverage/dotnet-coverage");
const string SonarCoverageFile = "coverage.xml";
var target = Argument("target", "Sonar");
var configuration = Argument("configuration", "Release");

const string SolutionFileName = "Database.slnx";
string version = String.Empty;

Task("Version")
    .Does(() => {
    version = CalculateVersion();
    Information($"Version: { version }");
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
    .IsDependentOn("Version")
    .Description("Starts SonarCloud analysis")
    .Does(() => {
        if (string.IsNullOrWhiteSpace(sonarToken))
        {
            throw new Exception("SONAR_TOKEN environment variable is required to run SonarCloud analysis.");
        }

        StartProcess(SonarScannerPath, new ProcessSettings {
            Arguments = string.Join(" ", new [] {
                "begin",
                $"/k:\"{productName}\"",
                "/o:\"defra\"",
                $"/d:sonar.token=\"{sonarToken}\"",
                "/d:sonar.host.url=\"https://sonarcloud.io\"",
                $"/v:\"{version}\"",
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
            NoIncremental = true,
            ArgumentCustomization = args => args.Append($"/p:Version={version}")
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

RunTarget(target);