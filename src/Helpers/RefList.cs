namespace Atmo.Helpers;
/// <summary>
/// A byref list.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RefList<T> : IEnumerable
{
	/// <summary>
	/// Constructs a new instance with default capacity.
	/// </summary>
	public RefList()
	{
		_arr = new T[DEFAULT_CAPACITY];
		len = 0;
	}
	/// <summary>
	/// Default capacity of a list.
	/// </summary>
	public const int DEFAULT_CAPACITY = 16;
	private T?[] _arr;
	private int len;
	/// <summary>
	/// Number of items the instance can contain without needing to resize.
	/// </summary>
	public int Capacity => _arr.Length;
	private void _EnsureCapacity(int l)
	{
		if (l > Capacity)
		{
			Array.Resize(ref _arr, Capacity * 2);
		}
	}
	/// <summary>
	/// Returns a reference for item under specified index.
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public ref T? this[int index] => ref _arr[index];
	/// <summary>
	/// Returns current number of elements.
	/// </summary>
	public int Count => len;
	/// <summary>
	/// Appends an item, resizing if necessary.
	/// </summary>
	/// <param name="item"></param>
	public void Add(T item)
	{
		_EnsureCapacity(len + 1);
		_arr[len] = item;
		len++;
	}
	/// <summary>
	/// Resets to default capacity and erases all elements.
	/// </summary>
	public void Clear()
	{
		Array.Resize(ref _arr, DEFAULT_CAPACITY);
		len = 0;
		for (int i = 0; i < Capacity; i++)
		{
			_arr[i] = default;
		}
	}
	/// <summary>
	/// Checks if a given item is in the list.
	/// </summary>
	public bool Contains(T item)
	{
		return _arr.Contains(item);
	}

	/// <summary>
	/// Copies all elements starting with <paramref name="arrayIndex"/> to specified <paramref name="array"/>.
	/// </summary>
	public void CopyTo(T?[] array, int arrayIndex)
	{
		_arr.CopyTo(array, arrayIndex);
	}
	/// <summary>
	/// Returns an object to enumerate this list.
	/// </summary>
	/// <returns></returns>
	public RefEn GetEnumerator()
	{
		return new(this);
	}

	/// <returns>Position of given item in list; negative if not found.</returns>
	public int IndexOf(T? item)
	{
		for (int i = 0; i < len; i++)
		{
			if (Equals(_arr[i], item)) return i;
		}
		return -1;
	}
	/// <summary>
	/// Inserts an item at position.
	/// </summary>
	/// <param name="index"></param>
	/// <param name="item"></param>
	public void Insert(int index, T? item)
	{
		_EnsureCapacity(len + 1);
		Array.Copy(_arr, index, _arr, index + 1, len - index);
		_arr[index] = item;
	}
	/// <summary>
	/// Removes a specified item.
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool Remove(T? item)
	{
		int index = IndexOf(item);
		if (index < 0 || index >= len) return false;
		RemoveAt(index);
		return true;
	}
	/// <summary>
	/// Removes an item at given index.
	/// </summary>
	/// <param name="index"></param>
	public bool RemoveAt(int index)
	{
		if (index < 0 || index >= len) return false;
		Array.Copy(_arr, index + 1, _arr, index, len - index);
		len--;
		return true;
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <summary>
	/// Enumerator to cycle list by ref
	/// </summary>
	public struct RefEn : IEnumerator
	{
		private readonly RefList<T> _owner;
		private int index = -1;
		/// <summary>
		/// Creates an instance wrapping a given reflist.
		/// </summary>
		/// <param name="owner"></param>
		public RefEn(RefList<T> owner)
		{
			_owner = owner;
		}
		/// <summary>
		/// Current item.
		/// </summary>
		public ref T? Current => ref _owner[index];
		object? IEnumerator.Current => Current;
		/// <summary>
		/// Advances enumeration; returns whether you can continue.
		/// </summary>
		public bool MoveNext()
		{
			index++;
			return index < _owner.Count;
		}
		/// <summary>
		/// Resets to start enumeration again.
		/// </summary>
		public void Reset()
		{
			index = -1;
		}
	}
}
