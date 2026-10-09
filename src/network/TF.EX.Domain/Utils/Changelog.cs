using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Globalization;
using System.Text;

namespace TF.EX.Domain.Utils
{
    public record ChangelogEntry(string Tag, string Text);

    public record ChangelogSection(string Title, List<ChangelogEntry> Entries);

    public record ChangelogRelease(Version Version, string Date, List<ChangelogSection> Sections);

    public static class Changelog
    {
        private static string _currentVersion = "";
        private static Action<string> _markSeen;

        public static IReadOnlyList<ChangelogRelease> Pending { get; private set; } = [];

        public static bool HasPending => Pending.Count > 0;

        public static void Configure(string markdown, string currentVersion, string lastSeenVersion, Action<string> markSeen)
        {
            _currentVersion = currentVersion;
            _markSeen = markSeen;
            Pending = SelectUnseen(Parse(markdown ?? ""), currentVersion, lastSeenVersion);

            if (!HasPending && lastSeenVersion != currentVersion)
            {
                markSeen(currentVersion);
            }
        }

        public static void MarkSeen()
        {
            if (!HasPending)
            {
                return;
            }

            Pending = [];
            _markSeen?.Invoke(_currentVersion);
        }

        public static List<ChangelogRelease> SelectUnseen(IEnumerable<ChangelogRelease> releases, string currentVersion, string lastSeenVersion)
        {
            if (!TryParseVersion(currentVersion, out var current))
            {
                return [];
            }

            var hasSeen = TryParseVersion(lastSeenVersion, out var lastSeen);

            return [.. releases
                .Where(release => hasSeen
                    ? release.Version > lastSeen && release.Version <= current
                    : release.Version == current)
                .OrderByDescending(release => release.Version)];
        }

        public static List<ChangelogRelease> Parse(string markdown)
        {
            var releases = new List<ChangelogRelease>();
            ChangelogRelease release = null;
            ChangelogSection section = null;

            foreach (var block in Markdown.Parse(markdown))
            {
                switch (block)
                {
                    case HeadingBlock { Level: 2 } heading:
                        section = null;
                        release = ToRelease(InlineText(heading.Inline));

                        if (release != null)
                        {
                            releases.Add(release);
                        }

                        break;
                    case HeadingBlock { Level: 3 } heading when release != null:
                        section = new ChangelogSection(ToDisplayText(InlineText(heading.Inline)), []);
                        release.Sections.Add(section);
                        break;
                    case ListBlock list when section != null:
                        AddEntries(list, section);
                        break;
                }
            }

            foreach (var parsed in releases)
            {
                parsed.Sections.RemoveAll(empty => empty.Entries.Count == 0);
            }

            releases.RemoveAll(empty => empty.Sections.Count == 0);

            return releases;
        }

        private static ChangelogRelease ToRelease(string heading)
        {
            var parts = heading.Split(" - ", 2);

            return TryParseVersion(parts[0].Trim(' ', '[', ']'), out var version)
                ? new ChangelogRelease(version, parts.Length > 1 ? parts[1].Trim() : "", [])
                : null;
        }

        private static void AddEntries(ListBlock list, ChangelogSection section)
        {
            foreach (var item in list.OfType<ListItemBlock>())
            {
                foreach (var child in item)
                {
                    if (child is ParagraphBlock paragraph)
                    {
                        section.Entries.Add(ToEntry(ToDisplayText(InlineText(paragraph.Inline))));
                    }
                    else if (child is ListBlock nested)
                    {
                        AddEntries(nested, section);
                    }
                }
            }
        }

        private static string InlineText(ContainerInline container)
        {
            if (container == null)
            {
                return "";
            }

            var text = new StringBuilder();

            foreach (var inline in container)
            {
                text.Append(inline switch
                {
                    LiteralInline literal => literal.Content.ToString(),
                    CodeInline code => code.Content,
                    AutolinkInline autolink => autolink.Url,
                    HtmlEntityInline entity => entity.Transcoded.ToString(),
                    LineBreakInline => " ",
                    ContainerInline nested => InlineText(nested),
                    _ => "",
                });
            }

            return text.ToString();
        }

        private static ChangelogEntry ToEntry(string text)
        {
            var tagEnd = text.IndexOf(']');

            return text.StartsWith('[') && tagEnd > 1
                ? new ChangelogEntry(text[1..tagEnd].Trim(), ToBracketFree(text[(tagEnd + 1)..].Trim()))
                : new ChangelogEntry("", ToBracketFree(text));
        }

        private static string ToBracketFree(string text) => text.Replace('[', '(').Replace(']', ')');

        private static bool TryParseVersion(string text, out Version version)
        {
            var core = (text ?? "").Trim().TrimStart('v', 'V').Split('-', '+')[0];

            return Version.TryParse(core, out version);
        }

        private static string ToDisplayText(string text)
        {
            var folded = text
                .Replace('—', '-')
                .Replace('–', '-')
                .Replace('’', '\'')
                .Replace('“', '"')
                .Replace('”', '"')
                .Normalize(NormalizationForm.FormD);

            var ascii = new StringBuilder(folded.Length);

            foreach (var character in folded)
            {
                if (character >= ' ' && character <= '~')
                {
                    ascii.Append(character);
                }
                else if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    ascii.Append(' ');
                }
            }

            return string.Join(' ', ascii.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
        }
    }
}
