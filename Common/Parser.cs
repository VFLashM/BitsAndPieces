using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.UI.VSIntegration;

namespace Common
{
    public class Parser
    {
        static readonly Regex useDatabaseRegex = new Regex(String.Join("|", new string[] {
            @"\buse\s+\[(?<db>[^\]]+)\]", // brackets identifier
            @"\buse\s+""(?<db>[^""]+)""", // quoted identifier
            @"\buse\s+(?<db>[a-zA-Z_@#][a-zA-Z_@#$0-9]*)\b", // regular identifier
        }), RegexOptions.RightToLeft);

        public static string ParseUseDatabase(string text, int? atPos = null)
        {
            if (atPos.HasValue)
            {
                text = text.Substring(0, atPos.Value);
            }
            var match = useDatabaseRegex.Match(text);
            return match.Success ? match.Groups["db"].Value : null;
        }
    }
}
