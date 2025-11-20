#!/usr/bin/env dotnet run

using System;
using System.IO;
using System.Text.RegularExpressions;

string TransformDocsContent(string content)
{
    // Skip the title (first line)
    var lines = content.Split('\n');
    if (lines.Length > 0 && lines[0].StartsWith("# "))
    {
        lines = lines[1..];
    }
    content = string.Join('\n', lines).TrimStart('\n');

    // Transform the grid cards section into simpler markdown
    var gridPattern = @"<div class=""grid cards""[^>]*>.*?</div>";
    content = Regex.Replace(content, gridPattern, ReplaceGridCards, RegexOptions.Singleline);

    // Fix relative links for docs
    content = Regex.Replace(content, @"\]\(\./([^)]+)\.md\)", "](https://steveasleep.com/hyprwt/$1/)");

    return content;
}

string ReplaceGridCards(Match match)
{
    var gridContent = match.Value;
    var simplified = "**What hyprwt can do for you:**\n\n";

    // Extract each card's title and description
    var cardPattern = @"\s*-\s+__([^_]+)__\s+---\s+(.*?)(?=\n\s*-\s+__|\s*</div>)";
    var cards = Regex.Matches(gridContent, cardPattern, RegexOptions.Singleline);

    foreach (Match card in cards)
    {
        var title = card.Groups[1].Value.Trim();
        var description = card.Groups[2].Value.Trim();
        description = Regex.Replace(description, @"\s+", " ");
        simplified += $"- **{title}**: {description}\n";
    }

    return simplified.TrimEnd();
}

void SyncReadme()
{
    var projectRoot = Directory.GetCurrentDirectory();
    var docsIndex = Path.Combine(projectRoot, "docs", "index.md");
    var readme = Path.Combine(projectRoot, "README.md");

    // Read docs/index.md
    if (!File.Exists(docsIndex))
    {
        throw new FileNotFoundException($"docs/index.md not found at {docsIndex}");
    }
    var docsContent = File.ReadAllText(docsIndex);

    // Transform content
    var syncedContent = TransformDocsContent(docsContent);

    // Read current README.md
    if (!File.Exists(readme))
    {
        throw new FileNotFoundException($"README.md not found at {readme}");
    }
    var readmeContent = File.ReadAllText(readme);

    // Find markers
    const string beginMarker = "<!-- BEGIN SYNCED CONTENT -->";
    const string endMarker = "<!-- END SYNCED CONTENT -->";

    if (!readmeContent.Contains(beginMarker) || !readmeContent.Contains(endMarker))
    {
        throw new InvalidOperationException(
            $"README.md must contain sync markers:\n  {beginMarker}\n  {endMarker}"
        );
    }

    // Extract before, middle (to replace), and after sections
    var before = readmeContent.Split(new[] { beginMarker }, StringSplitOptions.None)[0];
    var after = readmeContent.Split(new[] { endMarker }, StringSplitOptions.None)[1];

    // Construct new README
    var newReadme = $"{before}{beginMarker}\n" +
                    "<!-- This content is synced from docs/index.md - do not edit directly -->\n" +
                    "<!-- Run 'mise run sync-readme' to update -->\n\n" +
                    $"{syncedContent}\n\n" +
                    $"{endMarker}{after}";

    // Write back
    File.WriteAllText(readme, newReadme);
    Console.WriteLine("✓ Synced README.md from docs/index.md");
}

SyncReadme();
