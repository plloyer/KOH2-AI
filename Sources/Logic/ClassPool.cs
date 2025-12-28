using System.Collections.Generic;

namespace Logic;

internal static class ClassPool<T> where T : IPoolableClass, new()
{
	private static List<T> pool = new List<T>(512);

	public static T New()
	{
		if (pool.Count == 0)
		{
			return new T();
		}
		int index = pool.Count - 1;
		T result = pool[index];
		pool.RemoveAt(index);
		result.OnPoolAllocated();
		return result;
	}

	public static void Delete(T obj)
	{
		if (obj != null)
		{
			pool.Add(obj);
		}
	}

	public static void Delete(List<T> lst, int start_index = 0, int count = -1)
	{
		if (lst == null || start_index >= lst.Count)
		{
			return;
		}
		int num;
		if (count < 0)
		{
			num = lst.Count;
		}
		else
		{
			num = start_index + count;
			if (num > lst.Count)
			{
				num = lst.Count;
			}
		}
		for (int i = start_index; i < num; i++)
		{
			Delete(lst[i]);
		}
	}
}
