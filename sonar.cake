
#load "base.cake"
#load "version.cake"

var target = Argument("target", "Sonar");
var configuration = Argument("configuration", "Release");

var productName = Argument<string>("product_name", EnvironmentVariable("PRODUCT_NAME") ?? "");
var solution_file_name = Argument<string>("solution_file_name", EnvironmentVariable("SOLUTION_FILE_NAME") ?? "");
var version = Argument<string>("package_version", EnvironmentVariable("PACKAGE_VERSION") ?? "");
var sonarToken = Argument<string>("sonar_token", EnvironmentVariable("SONAR_TOKEN") ?? "");
const string SonarHostUrl = "https://sonarcloud.io";
const string SonarOrganization = "defra";
const string SonarCoverageFile = "coverage.xml";

var SonarScannerPath = "./.sonar/scanner/dotnet-sonarscanner";
var DotNetCoveragePath = "./.sonar/coverage/dotnet-coverage";

Task("Version")
    .Does(() => {
        if (string.IsNullOrEmpty(version))
        {
            version = CalculateVersion();
        }
        Information($"Version: {version}");
    });

Task("Sonar-Install")
    .Description("Installs the SonarCloud scanner and dotnet-coverage tools")
    .Does(() => {
         if (string.IsNullOrWhiteSpace(sonarToken))
         {
           throw new Exception("Sonar Cloud token is required to run SonarCloud analysis.");
         }
         else
         {
           Information($"A valid token was provided ending {sonarToken[^Math.Min(4, sonarToken.Length)..]}");
         }
        EnsureDirectoryExists("./.sonar/scanner");
        EnsureDirectoryExists("./.sonar/coverage");

        var scannerExitCode = StartProcess("dotnet", new ProcessSettings {
            Arguments = "tool update dotnet-sonarscanner --tool-path ./.sonar/scanner"
        });
        if (scannerExitCode != 0)
        {
            throw new Exception($"Failed to install dotnet-sonarscanner tool. Exit code: {scannerExitCode}");
        }

        var coverageToolExitCode = StartProcess("dotnet", new ProcessSettings {
            Arguments = "tool update dotnet-coverage --tool-path ./.sonar/coverage"
        });
        if (coverageToolExitCode != 0)
        {
            throw new Exception($"Failed to install dotnet-coverage tool. Exit code: {coverageToolExitCode}");
        }
    });

Task("Sonar-Begin")
    .IsDependentOn("Sonar-Install")
    .IsDependentOn("Version")
    .Description("Starts SonarCloud analysis")
    .Does(() => {
          var beginExitCode = StartProcess(SonarScannerPath, new ProcessSettings {
            Arguments = string.Join(" ", new [] {
                "begin",
                $"/k:\"{productName}\"",
                $"/o:\"{SonarOrganization}\"",
                $"/d:sonar.token=\"{sonarToken}\"",
                $"/d:sonar.host.url=\"{SonarHostUrl}\"",
                $"/v:\"{version}\"",
                "/d:sonar.cs.vscoveragexml.reportsPaths=coverage.xml",
                "/d:sonar.exclusions=\"changelog/**,.github/**\"",
                "/d:sonar.dotnet.excludeTestProjects=true"
            })
        });
        if (beginExitCode != 0)
        {
            throw new Exception($"SonarCloud 'begin' step failed. Exit code: {beginExitCode}");
        }
    });

Task("Sonar-Build")
    .IsDependentOn("Sonar-Begin")
    .Description("Builds the solution for SonarCloud analysis")
    .Does(() => {
        DotNetBuild(solution_file_name, new DotNetBuildSettings {
            Configuration = configuration,
            NoIncremental = true,
            ArgumentCustomization = args => args.Append($"/p:Version={version}")
        });
    });

Task("Sonar-Test")
    .IsDependentOn("Sonar-Build")
    .Description("Runs tests and collects coverage for SonarCloud")
    .Does(() => {
        var coverageExitCode = StartProcess(DotNetCoveragePath, new ProcessSettings {
            Arguments = $"collect \"dotnet test --configuration {configuration} --no-build\" -f xml -o \"{SonarCoverageFile}\""
        });
        if (coverageExitCode != 0)
        {
            throw new Exception($"Test coverage collection failed. Exit code: {coverageExitCode}");
        }
    });

Task("Sonar-End")
    .IsDependentOn("Sonar-Test")
    .Description("Completes SonarCloud analysis")
    .Does(() => {
        var endExitCode = StartProcess(SonarScannerPath, new ProcessSettings {
            Arguments = $"end /d:sonar.token=\"{sonarToken}\""
        });
        if (endExitCode != 0)
        {
            throw new Exception($"SonarCloud 'end' step failed. Exit code: {endExitCode}");
        }
    });

Task("Sonar")
    .IsDependentOn("Sonar-End")
    .Description("Runs the full SonarCloud analysis pipeline");

Task("Default")
    .IsDependentOn("Sonar");

RunTarget(target);