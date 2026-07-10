#nullable enable
#tool dotnet:?package=GitVersion.Tool&version=6.5.1

public string CalculateVersion()
{
    var settings = new GitVersionSettings
    {
        NoFetch = true
    };

    var gitVersion = GitVersion(settings);
    
    var calculatedVersion = gitVersion.FullSemVer;
    var baseVersion = $"{gitVersion.Major}.{gitVersion.Minor}.{gitVersion.Patch}";

    var isGitHubActions = BuildSystem.GitHubActions.IsRunningOnGitHubActions;
    var isPullRequest = isGitHubActions && BuildSystem.GitHubActions.Environment.PullRequest.IsPullRequest;
    
    var branchName = Argument("branch", "");
    
    if (string.IsNullOrWhiteSpace(branchName))
    {
        if (isGitHubActions)
        {
            branchName = isPullRequest ? EnvironmentVariable("GITHUB_HEAD_REF") : BuildSystem.GitHubActions.Environment.Workflow.RefName;
        }
        else
        {
            try 
            {
                IEnumerable<string> outLines;
                var exitCode = StartProcess("git", new ProcessSettings {
                    Arguments = "rev-parse --abbrev-ref HEAD",
                    RedirectStandardOutput = true
                }, out outLines);
                if (exitCode == 0)
                {
                    branchName = outLines.FirstOrDefault();
                }
               
            } 
            catch 
            {
                // git not found or not a repo
            }
        }
    }

    var isMain = !string.IsNullOrWhiteSpace(branchName) && (
        branchName.Equals("main", StringComparison.OrdinalIgnoreCase) || 
        branchName.Equals("master", StringComparison.OrdinalIgnoreCase) ||
        branchName.EndsWith("/main", StringComparison.OrdinalIgnoreCase) ||
        branchName.EndsWith("/master", StringComparison.OrdinalIgnoreCase)
    );

    if (isPullRequest || !isGitHubActions || !isMain)
    {
        var lreg = "LREG-XX";
        if (!string.IsNullOrWhiteSpace(branchName))
        {
            var match = System.Text.RegularExpressions.Regex.Match(branchName, @"LREG-\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                lreg = match.Value.ToUpper();
            }
        }

        var height = gitVersion.CommitsSinceVersionSource ?? 0;
        calculatedVersion = $"{baseVersion}-{lreg}-alpha.{height}";
    }
    else
    {
        calculatedVersion = baseVersion;
    }
    return calculatedVersion;
}

Task("GetVersion")
    .Description("Calculates the version and sets it as a GitHub Actions output")
    .Does(() => {
        var version = CalculateVersion();
        Information($"Calculated Version: {version}");
        
        if (BuildSystem.GitHubActions.IsRunningOnGitHubActions)
        {
            var outputFile = EnvironmentVariable("GITHUB_OUTPUT");
            if (!string.IsNullOrEmpty(outputFile))
            {
                System.IO.File.AppendAllLines(outputFile, new[] { $"version={version}" });
            }
            else
            {
                Warning("GITHUB_OUTPUT environment variable not found.");
            }
        }
    });

var targetVersion = Argument("target", "");
if (targetVersion == "GetVersion")
{
    RunTarget("GetVersion");
}
