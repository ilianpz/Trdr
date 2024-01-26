namespace Trdr.Aggr;

internal sealed class Rows
{
    private readonly KeyColumn _keyColumn;
    private readonly IReadOnlyList<OtherColumn> _otherColumns;

    private Rows(KeyColumn keyColumn, IReadOnlyList<OtherColumn> otherColumns)
    {
        _keyColumn = keyColumn ?? throw new ArgumentNullException(nameof(keyColumn));
        _otherColumns = otherColumns ?? throw new ArgumentNullException(nameof(otherColumns));
    }

    public static Rows Create(IReadOnlyList<string> columns)
    {
        if (columns == null) throw new ArgumentNullException(nameof(columns));
        var keyColumn = Column.ParseKeyColumn(columns[0]);
        var otherColumns = columns.Skip(1).Select(Column.ParseOtherColumn).ToList();
        return new Rows(keyColumn, otherColumns);
    }

    public IEnumerable<string> OnNext(string row)
    {
        var tokens = row.Split(',');
        bool shouldFlush = _keyColumn.OnNext(tokens);
        if (shouldFlush)
        {
            yield return _keyColumn.Flush();
            foreach (var otherColumn in _otherColumns)
            {
                yield return otherColumn.Flush();
            }

            // We flushed the first item of the next group. Just add it again.
            _keyColumn.OnNext(tokens);
        }

        foreach (var otherColumn in _otherColumns)
        {
            otherColumn.OnNext(tokens);
        }
    }

    public IEnumerable<string> Flush()
    {
        yield return _keyColumn.Flush();
        foreach (var otherColumn in _otherColumns)
        {
            yield return otherColumn.Flush();
        }
    }
}