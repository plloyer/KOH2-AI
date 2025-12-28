using System;
using System.Collections.Generic;
using System.Text;

namespace Logic;

public static class Container
{
	public static int Count<T>(List<T> lst)
	{
		return lst?.Count ?? 0;
	}

	public static int Count<T>(T[] arr)
	{
		if (arr == null)
		{
			return 0;
		}
		return arr.Length;
	}

	public static T GetSafe<T>(T[] arr, int idx, T def_val = default(T))
	{
		if (arr == null || idx < 0)
		{
			return def_val;
		}
		int num = arr.Length;
		if (idx >= num)
		{
			return def_val;
		}
		return arr[idx];
	}

	public static T GetSafe<T>(List<T> lst, int idx, T def_val = default(T))
	{
		if (lst == null || idx < 0)
		{
			return def_val;
		}
		int count = lst.Count;
		if (idx >= count)
		{
			return def_val;
		}
		return lst[idx];
	}

	public static V GetSafe<K, V>(Dictionary<K, V> dict, K key, V def_val = default(V)) where K : struct
	{
		if (dict == null)
		{
			return def_val;
		}
		if (!dict.TryGetValue(key, out var value))
		{
			return def_val;
		}
		return value;
	}

	public static V GetSafe_Class<K, V>(Dictionary<K, V> dict, K key, V def_val = default(V)) where K : class
	{
		if (dict == null || key == null)
		{
			return def_val;
		}
		if (!dict.TryGetValue(key, out var value))
		{
			return def_val;
		}
		return value;
	}

	public static bool Contains<T>(T[] arr, T val) where T : struct, IEquatable<T>
	{
		if (arr == null)
		{
			return false;
		}
		foreach (T other in arr)
		{
			if (val.Equals(other))
			{
				return true;
			}
		}
		return false;
	}

	public static bool Contains<T>(List<T> lst, T val) where T : struct, IEquatable<T>
	{
		if (lst == null)
		{
			return false;
		}
		return lst.IndexOf(val) >= 0;
	}

	public static bool Contains<K, V>(Dictionary<K, V> dict, K key)
	{
		if (dict == null || key == null)
		{
			return false;
		}
		return dict.ContainsKey(key);
	}

	public static bool Contains_Class<T>(T[] arr, T val) where T : class
	{
		if (arr == null)
		{
			return false;
		}
		int num = arr.Length;
		for (int i = 0; i < num; i++)
		{
			T val2 = arr[i];
			if (val == val2)
			{
				return true;
			}
		}
		return false;
	}

	public static bool Contains_Class<T>(List<T> lst, T val) where T : class
	{
		if (lst == null)
		{
			return false;
		}
		int count = lst.Count;
		for (int i = 0; i < count; i++)
		{
			T val2 = lst[i];
			if (val == val2)
			{
				return true;
			}
		}
		return false;
	}

	public static int IndexOf<T>(T[] arr, T val) where T : struct, IEquatable<T>
	{
		if (arr == null)
		{
			return -2;
		}
		int num = arr.Length;
		for (int i = 0; i < num; i++)
		{
			T other = arr[i];
			if (val.Equals(other))
			{
				return i;
			}
		}
		return -1;
	}

	public static int IndexOf<T>(List<T> lst, T val) where T : struct, IEquatable<T>
	{
		return lst?.IndexOf(val) ?? (-2);
	}

	public static int IndexOf_Class<T>(T[] arr, T val) where T : class
	{
		if (arr == null)
		{
			return -2;
		}
		for (int i = 0; i < arr.Length; i++)
		{
			T val2 = arr[i];
			if (val == val2)
			{
				return i;
			}
		}
		return -1;
	}

	public static int IndexOf_Class<T>(List<T> lst, T val) where T : class
	{
		return lst?.IndexOf(val) ?? (-2);
	}

	public static int Add<T>(ref List<T> lst, T val, int prealloc = 0)
	{
		if (lst != null)
		{
			lst.Add(val);
			return -1;
		}
		if (prealloc <= 0)
		{
			lst = new List<T>();
		}
		else
		{
			lst = new List<T>(prealloc);
		}
		lst.Add(val);
		return -2;
	}

	public static int AddUniqueUnsafe<T>(List<T> lst, T val) where T : struct, IEquatable<T>
	{
		int num = IndexOf(lst, val);
		if (num >= 0)
		{
			return num;
		}
		lst.Add(val);
		return -1;
	}

	public static int AddUniqueUnsafe_Class<T>(List<T> lst, T val) where T : class
	{
		int num = IndexOf_Class(lst, val);
		if (num >= 0)
		{
			return num;
		}
		lst.Add(val);
		return -1;
	}

	public static int AddUniqueSafe<T>(List<T> lst, T val) where T : struct, IEquatable<T>
	{
		if (lst == null)
		{
			return -2;
		}
		int num = IndexOf(lst, val);
		if (num >= 0)
		{
			return num;
		}
		lst.Add(val);
		return -1;
	}

	public static int AddUniqueSafe_Class<T>(List<T> lst, T val) where T : class
	{
		if (lst == null)
		{
			return -2;
		}
		int num = IndexOf_Class(lst, val);
		if (num >= 0)
		{
			return num;
		}
		lst.Add(val);
		return -1;
	}

	public static int AddUnique<T>(ref List<T> lst, T val, int prealloc = 0) where T : struct, IEquatable<T>
	{
		int num = IndexOf(lst, val);
		if (num >= 0)
		{
			return num;
		}
		if (lst != null)
		{
			lst.Add(val);
			return -1;
		}
		if (prealloc <= 0)
		{
			lst = new List<T>();
		}
		else
		{
			lst = new List<T>(prealloc);
		}
		lst.Add(val);
		return -2;
	}

	public static int AddUnique_Class<T>(ref List<T> lst, T val, int prealloc = 0) where T : class
	{
		int num = IndexOf_Class(lst, val);
		if (num >= 0)
		{
			return num;
		}
		if (lst != null)
		{
			lst.Add(val);
			return -1;
		}
		if (prealloc <= 0)
		{
			lst = new List<T>();
		}
		else
		{
			lst = new List<T>(prealloc);
		}
		lst.Add(val);
		return -2;
	}

	public static int AddToMultiMapUnsafe<K, V>(Dictionary<K, List<V>> dict, K key, V val, int list_prealloc = 0) where V : struct, IEquatable<V>
	{
		dict.TryGetValue(key, out var value);
		int num = Add(ref value, val, list_prealloc);
		if (num == -2)
		{
			dict.Add(key, value);
		}
		return num;
	}

	public static int AddToMultiMapUniqueUnsafe<K, V>(Dictionary<K, List<V>> dict, K key, V val, int list_prealloc = 0) where V : struct, IEquatable<V>
	{
		dict.TryGetValue(key, out var value);
		int num = AddUnique(ref value, val, list_prealloc);
		if (num == -2)
		{
			dict.Add(key, value);
		}
		return num;
	}

	public static int AddToMultiMapUniqueUnsafe_Class<K, V>(Dictionary<K, List<V>> dict, K key, V val, int list_prealloc = 0) where V : class
	{
		dict.TryGetValue(key, out var value);
		int num = AddUnique_Class(ref value, val, list_prealloc);
		if (num == -2)
		{
			dict.Add(key, value);
		}
		return num;
	}

	public static void Benchmark()
	{
		int size = 1000;
		Value[] arr = null;
		List<Value> lst = null;
		List<string> lst_s = null;
		HashSet<Value> set = null;
		Dictionary<Value, Value> dict = null;
		StringBuilder txt = new StringBuilder(1024);
		txt.AppendLine("Benchmark results:");
		using (Game.Profile("TestContantersPerformance"))
		{
			Benchmark("NOP", delegate
			{
			});
			Benchmark("Alloc and fill array", delegate
			{
				arr = new Value[size];
				for (int i = 0; i < size; i++)
				{
					arr[i] = i;
				}
			});
			Benchmark("Fill array", delegate
			{
				for (int i = 0; i < size; i++)
				{
					arr[i] = i;
				}
			});
			Benchmark("Array.IndexOf", delegate
			{
				Array.IndexOf(arr, (Value)(-1));
			});
			Benchmark("Array lookup", delegate
			{
				Value value = new Value(-1);
				for (int i = 0; i < arr.Length && !(arr[i] == value); i++)
				{
				}
			});
			Benchmark("Alloc and fill list", delegate
			{
				lst = new List<Value>();
				for (int i = 0; i < size; i++)
				{
					lst.Add(i);
				}
			});
			Benchmark("Alloc and fill preallocated list", delegate
			{
				lst = new List<Value>(size);
				for (int i = 0; i < size; i++)
				{
					lst.Add(i);
				}
			});
			Benchmark("Clear and fill list", delegate
			{
				lst.Clear();
				for (int i = 0; i < size; i++)
				{
					lst.Add(i);
				}
			});
			Benchmark("List.Contains", delegate
			{
				lst.Contains(-1);
			});
			Benchmark("List.IndexOf", delegate
			{
				lst.IndexOf(-1);
			});
			Benchmark("Logic.Container.Contains(List, val)", delegate
			{
				Contains(lst, -1);
			});
			Benchmark("List lookup", delegate
			{
				Value value = new Value(-1);
				for (int i = 0; i < size && !(lst[i] == value); i++)
				{
				}
			});
			Benchmark("List.Foreach", delegate
			{
				foreach (Value item in lst)
				{
					_ = item;
				}
			});
			Benchmark("Alloc and fill string list", delegate
			{
				lst_s = new List<string>();
				for (int i = 0; i < size; i++)
				{
					string text2 = "AAAAAAAAAAA";
					text2 += 65 + i;
					lst_s.Add(text2);
				}
			});
			Benchmark("List<string>.Contains", delegate
			{
				lst_s.Contains("-1");
			});
			Benchmark("List<string> lookup", delegate
			{
				string text2 = "AAAAAAAAAAA0";
				for (int i = 0; i < size && !(lst_s[i] == text2); i++)
				{
				}
			});
			Benchmark("List<string> lookup culture-insensitive", delegate
			{
				string value = "AAAAAAAAAAA0";
				for (int i = 0; i < size && !lst_s[i].Equals(value, StringComparison.Ordinal); i++)
				{
				}
			});
			Benchmark("List<string>.Foreach", delegate
			{
				foreach (string item2 in lst_s)
				{
					_ = item2;
				}
			});
			Benchmark("Alloc and fill set", delegate
			{
				set = new HashSet<Value>();
				for (int i = 0; i < size; i++)
				{
					set.Add(i);
				}
			});
			Benchmark("Clear and fill set", delegate
			{
				set.Clear();
				for (int i = 0; i < size; i++)
				{
					set.Add(i);
				}
			});
			Benchmark("Set.Contais", delegate
			{
				set.Contains(100);
			});
			Benchmark("Set.Foreach", delegate
			{
				foreach (Value item3 in set)
				{
					_ = item3;
				}
			});
			Benchmark("Alloc and fill dictionary", delegate
			{
				dict = new Dictionary<Value, Value>();
				for (int i = 0; i < size; i++)
				{
					dict.Add(i, i);
				}
			});
			Benchmark("Clear and fill dictionary", delegate
			{
				dict.Clear();
				for (int i = 0; i < size; i++)
				{
					dict.Add(i, i);
				}
			});
			Benchmark("Dictionary.ContainsKey", delegate
			{
				dict.ContainsKey(100);
			});
			Benchmark("Dictionary.TryGetValue", delegate
			{
				dict.TryGetValue(100, out var _);
			});
			Benchmark("Dictionary.Foreach", delegate
			{
				foreach (KeyValuePair<Value, Value> item4 in dict)
				{
					_ = item4.Key;
					_ = item4.Value;
				}
			});
			string text = txt.ToString();
			Game.CopyToClipboard(text);
			Game.Log(text, Game.LogType.Message);
		}
		void Benchmark(string name, System.Action action)
		{
			Game.BenchmarkResult benchmarkResult = Game.Benchmark(name, action, -100L, use_profiler: true, log: false);
			txt.AppendLine(benchmarkResult.ToString());
		}
	}
}
