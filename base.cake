#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class RepositoryVersionInfo
{
    public string EventName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string StoryId { get; set; } = string.Empty;
    public string BumpType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string TagMessage { get; set; } = string.Empty;
    public bool IsPreRelease { get; set; }
}

var hotfixBranchPattern = new Regex(@"^hotfix/([a-z]+-[0-9]+)-.+$");
var featureBranchPattern = new Regex(@"^feature/([a-z]+-[0-9]+)-.+$");
var stableTagPattern = new Regex(@"^[0-9]+\.[0-9]+\.[0-9]+$");

IReadOnlyList<string> GetCommandOutput(string fileName, string arguments)
{
    IEnumerable<string> output;
    IEnumerable<string> error;

    var exitCode = StartProcess(
        fileName,
        new ProcessSettings
        {
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        },
        out output,
        out error);

    var outputLines = output.ToList();
    var errorLines = error.ToList();

    if (exitCode != 0)
    {
        foreach (var line in errorLines)
        {
            Error(line);
        }

        throw new Exception($"{fileName} {arguments} failed with exit code {exitCode}.");
    }

    return outputLines;
}

void RunCommand(string fileName, string arguments)
{
    GetCommandOutput(fileName, arguments);
}

bool CommandSucceeds(string fileName, string arguments)
{
    var exitCode = StartProcess(
        fileName,
        new ProcessSettings
        {
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

    return exitCode == 0;
}

string RequireEnvironmentVariable(string name)
{
    var value = EnvironmentVariable(name);

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new Exception($"Environment variable '{name}' is required.");
    }

    return value;
}

IReadOnlyList<string> GetGitOutput(string arguments) => GetCommandOutput("git", arguments);

void RunGit(string arguments)
{
    RunCommand("git", arguments);
}

bool GitTagExists(string value)
{
    return CommandSucceeds("git", $"rev-parse -q --verify refs/tags/{value}");
}

string ResolveRepositoryEventName(string? eventName = null)
{
    if (!string.IsNullOrWhiteSpace(eventName))
    {
        return eventName;
    }

    return EnvironmentVariable("GITHUB_EVENT_NAME") ?? "local";
}

string ResolveRepositoryBranchName(string? branchName = null)
{
    var resolvedBranchName = branchName;

    if (string.IsNullOrWhiteSpace(resolvedBranchName))
    {
        resolvedBranchName = EnvironmentVariable("GITHUB_HEAD_REF");
    }

    if (string.IsNullOrWhiteSpace(resolvedBranchName))
    {
        resolvedBranchName = EnvironmentVariable("GITHUB_REF_NAME");
    }

    if (string.IsNullOrWhiteSpace(resolvedBranchName))
    {
        resolvedBranchName = GetGitOutput("branch --show-current").FirstOrDefault();
    }

    if (string.IsNullOrWhiteSpace(resolvedBranchName))
    {
        throw new Exception("Unable to determine the branch name.");
    }

    return resolvedBranchName.ToLowerInvariant();
}

void FetchRepositoryVersionState()
{
    RunGit("fetch --force --tags");
    RunGit("fetch --force origin main");
}

RepositoryVersionInfo CalculateRepositoryVersion(string? branchName = null, string? eventName = null)
{
    var resolvedEventName = ResolveRepositoryEventName(eventName);
    var resolvedBranchName = ResolveRepositoryBranchName(branchName);

    var hotfixMatch = hotfixBranchPattern.Match(resolvedBranchName);
    string storyId;
    string bumpType;

    if (hotfixMatch.Success)
    {
        storyId = hotfixMatch.Groups[1].Value;
        bumpType = "patch";
    }
    else
    {
        var featureMatch = featureBranchPattern.Match(resolvedBranchName);

        if (!featureMatch.Success)
        {
            throw new Exception($"Branch '{resolvedBranchName}' does not have the correct name.");
        }

        storyId = featureMatch.Groups[1].Value;
        bumpType = "minor";
    }

    var latestMainTag = GetGitOutput("tag --sort=-v:refname")
        .FirstOrDefault(line => stableTagPattern.IsMatch(line));

    var major = 0;
    var minor = 0;
    var patch = 0;

    if (!string.IsNullOrWhiteSpace(latestMainTag))
    {
        var parts = latestMainTag.Split('.');
        major = int.Parse(parts[0]);
        minor = int.Parse(parts[1]);
        patch = int.Parse(parts[2]);
    }

    if (bumpType.Equals("minor", StringComparison.OrdinalIgnoreCase))
    {
        minor += 1;
        patch = 0;
    }
    else
    {
        patch += 1;
    }

    var baseVersion = $"{major}.{minor}.{patch}";
    var isPreRelease = resolvedEventName.Equals("push", StringComparison.OrdinalIgnoreCase);
    string version;
    string tagMessage;

    if (isPreRelease)
    {
        var mergeBase = GetGitOutput("merge-base HEAD origin/main").First();
        var depth = GetGitOutput($"rev-list --count {mergeBase}..HEAD").First();
        version = $"{baseVersion}-{storyId}-alpha.{depth}";
        tagMessage = $"Repository branch build {version} from {resolvedBranchName}";
    }
    else
    {
        version = baseVersion;
        tagMessage = $"Repository release {version} from {resolvedBranchName}";
    }

    Information($"Resolved branch '{resolvedBranchName}' with story '{storyId}' and bump '{bumpType}'.");
    Information($"Calculated version '{version}'.");

    return new RepositoryVersionInfo
    {
        EventName = resolvedEventName,
        BranchName = resolvedBranchName,
        StoryId = storyId,
        BumpType = bumpType,
        Version = version,
        TagMessage = tagMessage,
        IsPreRelease = isPreRelease
    };
}
