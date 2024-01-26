using System.CommandLine;
using Trdr.Aggr;

var rootCmd = new RootCommand();
var filePathArg = new Argument<string> { Arity = ArgumentArity.ExactlyOne };
rootCmd.AddArgument(filePathArg);

string filePath = null!;
IReadOnlyList<string> columns = null!;

// "0,ts,rndup{00:00:00.01}"
// "1,d,mean"
// "2,d,sum"
var columnsOption = new Option<IEnumerable<string>>("-c")
{
    Arity = ArgumentArity.OneOrMore,
    IsRequired = true,
    Description = "The column index and operation format."
};
rootCmd.AddOption(columnsOption);
rootCmd.SetHandler(
    (fp, cols) =>
    {
        filePath = fp;
        columns = cols.ToList();
    },
    filePathArg, columnsOption);

try
{
    var result = rootCmd.Invoke(args);
    if (result != 0)
        return result;

    using var source = new FileStream(filePath, FileMode.Open);
    using var reader = new StreamReader(source);

    var rows = Rows.Create(columns);
    while (reader.ReadLine() is { } line)
    {
        var row = rows.OnNext(line).ToList();
        if (row.Count != 0)
            Console.WriteLine(string.Join(',', row));
    }

    var flushed = rows.Flush().ToList();
    if (flushed.Count != 0)
        Console.WriteLine(string.Join(',', flushed));

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return -1;
}