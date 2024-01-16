namespace Trdr;

/// <summary>
/// A wrapper class for an item to indicate whether a value has been set or not.
/// Useful if null is a valid value.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class ItemHolder<T>
{
    private T _value = default!;
    private bool _hasValue;

    public T Value
    {
        get
        {
            if (!_hasValue) throw new InvalidOperationException("No item.");
            return _value;
        }
        set
        {
            _value = value;
            _hasValue = true;
        }
    }

    public bool TryGetValue(out T value)
    {
        if (_hasValue)
        {
            value = _value;
            return true;
        }

        value = default!;
        return false;
    }

    public void Reset()
    {
        _hasValue = false;
    }
}