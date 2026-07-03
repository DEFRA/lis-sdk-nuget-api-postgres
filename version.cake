#addin nuget:?package=Cake.MinVer&version=3.0.0
#tool dotnet:?package=minver-cli&version=6.0.0

public string CalculateVersion()
{
    var result = MinVer(new MinVerSettings {
        TagPrefix = "v",
        DefaultPreReleasePhase = "preview",
    });
    
    var calculatedVersion = result.Version;

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

        var baseVersion = calculatedVersion.Split('-')[0];
        var height = "0";
        if (calculatedVersion.Contains("-"))
        {
            var preReleasePart = calculatedVersion.Split('-')[1];
            height = preReleasePart.Split('.').Last();
        }
        calculatedVersion = $"{baseVersion}-{lreg}-alpha.{height}";
    }
    else
    {
        calculatedVersion = calculatedVersion.Split('-')[0];
    }

    return calculatedVersion;
}
