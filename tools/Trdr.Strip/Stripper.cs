using System.Text;
using Newtonsoft.Json.Linq;
using Trdr.App;

namespace Trdr.Strip;

internal static class Stripper
{
    /// <summary>
    /// Strips each line from a stream into its timestamp and JSON components. Output will be in CSV format.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="tokenPaths">
    /// Optionally, deconstruct the JSON into tokens using the given paths.
    /// Uses Newtonsoft.Json.Linq.JToken.SelectToken() format.
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Strip(Stream stream, IEnumerable<string>? tokenPaths)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        var tokenPathsList = tokenPaths?.ToList();

        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            int idx = line.IndexOfNthOccurence("]", 3, StringComparison.Ordinal);

            string timeStamp = line.Substring(0, Application.TimestampFormat.Length);
            string payload = line.Substring(idx + 1);

            var stripped = new StringBuilder();
            stripped.Append(timeStamp + ", ");

            if (tokenPathsList == null || tokenPathsList.Count == 0)
            {
                stripped.Append(payload);
            }
            else
            {
                var obj = JObject.Parse(payload);

                foreach (var tokenPath in tokenPathsList)
                {
                    var token = (string?)obj.SelectToken(tokenPath);
                    if (token != null)
                        stripped.Append(token + ", ");
                }

                // Remove last ", "
                stripped.Length -= 2;
            }

            Console.WriteLine(stripped.ToString());
        }
    }
}