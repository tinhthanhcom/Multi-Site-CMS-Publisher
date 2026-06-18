using System.Globalization;
using System.Text;

namespace Publisher.Web.Services;

/// <summary>
/// Generates URL-friendly slugs from titles. Handles Vietnamese diacritics
/// (đ/Đ -> d, combining marks stripped via Unicode normalization), lowercases,
/// converts whitespace/non-alphanumeric to single dashes, and trims dashes.
/// Static + pure so it needs no DI (Program.cs is off-limits to this agent).
/// </summary>
public static class SlugGenerator
{
    public static string Generate(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        // 1. Normalize Vietnamese đ/Đ before diacritic stripping (they have no combining-mark form).
        var pre = title.Replace('đ', 'd').Replace('Đ', 'D');

        // 2. Decompose (FormD) and drop non-spacing combining marks (the diacritics).
        var decomposed = pre.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(ch);
        }

        // 3. Recompose, lowercase invariantly.
        var ascii = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // 4. Build slug: keep [a-z0-9], everything else becomes a dash boundary.
        var slug = new StringBuilder(ascii.Length);
        var lastWasDash = false;
        foreach (var ch in ascii)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
            {
                slug.Append(ch);
                lastWasDash = false;
            }
            else
            {
                // Collapse any run of non-alphanumeric (spaces, punctuation) into a single dash.
                if (!lastWasDash)
                {
                    slug.Append('-');
                    lastWasDash = true;
                }
            }
        }

        // 5. Trim leading/trailing dashes.
        return slug.ToString().Trim('-');
    }
}
