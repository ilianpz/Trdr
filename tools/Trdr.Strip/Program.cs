using System.CommandLine;
using Trdr.Strip;

var rootCmd = new RootCommand();
var filePathArg = new Argument<string> { Arity = ArgumentArity.ExactlyOne };
rootCmd.AddArgument(filePathArg);

var tokensOption = new Option<IEnumerable<string>>("--tokens")
{
    AllowMultipleArgumentsPerToken = true,
    Description = "The JSON token path as per Newtonsoft.Json.Linq.Token.SelectToken()"
};
rootCmd.AddOption(tokensOption);

string filePath = null!;
IEnumerable<string>? tokenPaths = null;
rootCmd.SetHandler(
    (fp, tknPaths) =>
    {
        filePath = fp;
        tokenPaths = tknPaths;
    },
    filePathArg, tokensOption);

var result = rootCmd.Invoke(args);
if (result != 0)
    return result;

var stream = new FileStream(filePath, FileMode.Open);
Stripper.Strip(stream, tokenPaths);

return 0;