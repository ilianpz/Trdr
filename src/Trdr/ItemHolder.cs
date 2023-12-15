namespace Trdr;

/// <summary>
/// A wrapper class for an item to indicate whether a value has been set or not.
/// Useful if null is a valid value.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ItemHolder<T>
{
    private T _item = default!;
    private bool _hasItem;

    public T Item
    {
        get
        {
            if (!_hasItem) throw new InvalidOperationException("No item.");
            return _item;
        }
    }

    public void SetItem(T item)
    {
        _item = item;
        _hasItem = true;
    }

    public bool TryGetItem(out T item)
    {
        if (_hasItem)
        {
            item = _item;
            return true;
        }

        item = default!;
        return false;
    }

    public void Reset()
    {
        _hasItem = false;
    }
}