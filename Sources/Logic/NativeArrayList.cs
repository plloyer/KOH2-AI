using System;
using Unity.Collections;

namespace Logic;

public struct NativeArrayList<T> : IDisposable where T : struct
{
	private NativeArray<T> array;

	private int count;

	public int Count => count;

	public int MaxCount => array.Length;

	public bool IsNotDefined => count == -1;

	public bool IsDefined => count != -1;

	public T this[int index]
	{
		get
		{
			SanityCheck(index);
			return array[index];
		}
		set
		{
			SanityCheck(index);
			array[index] = value;
		}
	}

	public NativeArrayList(int max_count, Allocator allocator = Allocator.Persistent)
	{
		array = new NativeArray<T>(max_count, allocator);
		count = 0;
	}

	public static NativeArrayList<T> CreateUndefined()
	{
		return new NativeArrayList<T>
		{
			count = -1
		};
	}

	public void Dispose()
	{
		if (array.IsCreated)
		{
			array.Dispose();
		}
	}

	public void Add(T item)
	{
		SanityCheck();
		array[count] = item;
		count++;
	}

	public void RemoveAt(int index)
	{
		SanityCheck(index);
		for (int i = index; i < count; i++)
		{
			array[i] = array[i + 1];
		}
		count--;
	}

	public void SetCount(int count)
	{
		SanityCheck();
		this.count = count;
		SanityCheck();
	}

	public void Clear()
	{
		if (count != 0)
		{
			SanityCheck();
			count = 0;
		}
	}

	public void RemoveRange(int index, int count_to_delete)
	{
		SanityCheck(index);
		SanityCheck(index + count_to_delete);
		for (int i = index; i < count; i++)
		{
			array[i] = array[i + count_to_delete];
		}
		count -= count_to_delete;
	}

	public void Insert(int index, T element)
	{
		InsertSanityCheck(index);
		for (int num = count - 1; num >= index; num--)
		{
			array[num + 1] = array[num];
		}
		array[index] = element;
		count++;
	}

	private void SanityCheck(int index = 0)
	{
		SanityCheck();
		if (index < 0)
		{
			throw new Exception("Index out of range. Index < 0");
		}
		if (index >= count)
		{
			throw new Exception("Index out of range. Index >= count");
		}
	}

	private void InsertSanityCheck(int index = 0)
	{
		SanityCheck();
		if (index < 0)
		{
			throw new Exception("Index out of range. Index < 0");
		}
		if (index > count)
		{
			throw new Exception("Index out of range. Index > count");
		}
	}

	private void SanityCheck()
	{
		if (!array.IsCreated)
		{
			throw new Exception("Array is not created");
		}
		if (count < 0 || count > array.Length)
		{
			throw new Exception("Invalid array count");
		}
	}
}
