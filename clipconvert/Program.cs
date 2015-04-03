using System;
using System.Windows;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using System.Windows.Automation.Peers;

namespace clipconvert {
	class MainClass {
		private static readonly Regex anchorRegex = new Regex(@"<a [^<>]*href=[""']([^""'>]*)[""'][^<>]*>(.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		private static readonly Regex colorRegex = new Regex(@"<span [^<>]*style=[""']color:([^""'>]*?);?[""'][^<>]*>(.*?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		private static readonly Regex tagRegex = new Regex(@"<[^<>]+>", RegexOptions.Singleline);
		private static readonly Regex whitespaceRegex = new Regex(@"\s+", RegexOptions.Singleline);
		private static readonly Regex brRegex = new Regex(@"<br[^<>]*>|<blockquote[^<>]*>|</blockquote[^<>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

		[STAThread]
		public static int Main(string[] args) {
			if (!Clipboard.ContainsText(TextDataFormat.Html)) {
				return 1;
			}

			// Retrieve clipboard contents as HTML
			string clipboardData = Clipboard.GetText(TextDataFormat.Html);

			// Remove header
			clipboardData = clipboardData.Remove(0, clipboardData.IndexOf('<'));

			// Convert simple tags
			clipboardData = ReplaceTag(clipboardData, "b", "B");
			clipboardData = ReplaceTag(clipboardData, "strong", "B");
			clipboardData = ReplaceTag(clipboardData, "i", "I");
			clipboardData = ReplaceTag(clipboardData, "em", "I");
			clipboardData = ReplaceTag(clipboardData, "u", "U");
			clipboardData = ReplaceTag(clipboardData, "s", "S");

			// Replace <br> by [INSERTNEWLINEHERE]
			clipboardData = brRegex.Replace(clipboardData, "[INSERTNEWLINEHERE]");

			// Convert HTML color tags to [COLOR]
			clipboardData = colorRegex.Replace(clipboardData, (Match match) => {
				string color = match.Groups[1].Value;
				string caption = match.Groups[2].Value;

				return string.Format("[COLOR={0}]{1}[/COLOR]", color, caption);
			});

			// Convert <a href> to [URL]
			clipboardData = anchorRegex.Replace(clipboardData, (Match match) => {
				string href = match.Groups[1].Value;
				string caption = match.Groups[2].Value;

				return string.Format("[URL={0}]{1}[/URL]", href, caption);
			});

			// Coalesce spaces
			clipboardData = whitespaceRegex.Replace(clipboardData, " ");

			// Parse HTML entities
			clipboardData = WebUtility.HtmlDecode(clipboardData);

			// Replace [INSERTNEWLINEHERE] back to newlines
			clipboardData = clipboardData.Replace("[INSERTNEWLINEHERE]", "\n");

			// Remove remaining tags
			clipboardData = tagRegex.Replace(clipboardData, "");

			Clipboard.SetText(clipboardData.Trim());

			return 0;
		}

		private static string ReplaceTag(string input, string htmlTagName, string bbcodeTagName) {
			string pattern = string.Format(@"<{0}[^<>]*>(.*?)</{0}>", htmlTagName);
			return Regex.Replace(input, pattern, (Match match) => {
				string caption = match.Groups[1].Value;

				return string.Format("[{0}]{1}[/{0}]", bbcodeTagName, caption);
			}, RegexOptions.Singleline | RegexOptions.IgnoreCase);
		}
	}
}
