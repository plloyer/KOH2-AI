using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Logic;

public class UniqueStrings
{
	public class Info
	{
		public string str;

		public int lookups;

		public Info(string str)
		{
			this.str = str;
		}

		public override string ToString()
		{
			return ((str == null) ? "null" : ("'" + str + "'")) + ": " + lookups;
		}
	}

	public List<Info> strings;

	public Dictionary<string, int> map;

	public int size;

	public int Count => strings.Count;

	public UniqueStrings()
	{
		Clear();
	}

	public override string ToString()
	{
		return "Count: " + Count + ", Size: " + size;
	}

	public string Get(int uid)
	{
		return GetInfo(uid)?.str;
	}

	public int Register(string s)
	{
		GetInfo(s, add: true, out var uid);
		return uid;
	}

	public string Resolve(string s)
	{
		int uid;
		Info info = GetInfo(s, add: true, out uid);
		if (info == null)
		{
			return s;
		}
		return info.str;
	}

	public int Find(string s)
	{
		if (s == null)
		{
			return 0;
		}
		if (s == "")
		{
			return 1;
		}
		if (map.TryGetValue(s, out var value))
		{
			GetInfo(value).lookups++;
			return value;
		}
		return 0;
	}

	public Info GetInfo(int uid)
	{
		if (uid < 0 || uid >= strings.Count)
		{
			return null;
		}
		return strings[uid];
	}

	public Info GetInfo(string s, bool add, out int uid)
	{
		if (s == null)
		{
			return GetInfo(uid = 0);
		}
		if (s == "")
		{
			return GetInfo(uid = 1);
		}
		Info info;
		if (map.TryGetValue(s, out uid))
		{
			info = GetInfo(uid);
			info.lookups++;
			return strings[uid];
		}
		if (!add)
		{
			return null;
		}
		info = new Info(string.IsInterned(s) ?? s);
		uid = strings.Count;
		map.Add(info.str, uid);
		strings.Add(info);
		size += info.str.Length;
		return info;
	}

	public void Replace(int uid, string new_str)
	{
		if (new_str == null)
		{
			return;
		}
		string text = null;
		foreach (KeyValuePair<string, int> item in map)
		{
			if (item.Value == uid)
			{
				text = item.Key;
				break;
			}
		}
		if (text == null)
		{
			Game.Log($"Can't find unique string with id '{uid}' in order to replace it with string '{new_str}'", Game.LogType.Warning);
			return;
		}
		map.Remove(text);
		size -= text.Length;
		if (map.ContainsKey(new_str))
		{
			Game.Log("Map already contains key " + new_str, Game.LogType.Warning);
			int num = map[new_str];
			if (uid > num)
			{
				Game.Log($"Received uid {uid} for key {new_str} is larger than the already found uid {num} for that key. This will certainly cause an error.", Game.LogType.Error);
			}
			else if (num != uid)
			{
				Game.Log($"Contained key {new_str} uid is: {num}, new uid to replace is: {uid}. Replacing.", Game.LogType.Warning);
				map[new_str] = uid;
			}
			return;
		}
		map.Add(new_str, uid);
		size += new_str.Length;
		for (int i = 0; i < strings.Count; i++)
		{
			if (strings[i].str == text)
			{
				strings[i] = new Info(string.IsInterned(new_str) ?? new_str);
				break;
			}
		}
	}

	public void Clear()
	{
		size = 0;
		map = new Dictionary<string, int>();
		strings = new List<Info>();
		strings.Add(new Info(null));
		strings.Add(new Info(""));
	}

	public int LoadFromTextFile(string file_name)
	{
		string[] array;
		try
		{
			array = File.ReadAllLines(file_name);
		}
		catch (Exception ex)
		{
			Game.Log("Error loading " + file_name + ": " + ex.ToString(), Game.LogType.Error);
			return 0;
		}
		foreach (string s in array)
		{
			Register(s);
		}
		return array.Length;
	}

	public void SaveToTXTFile(string file_name)
	{
		StringBuilder stringBuilder = new StringBuilder(size + Count * 2);
		for (int i = 2; i < strings.Count; i++)
		{
			string str = strings[i].str;
			stringBuilder.AppendLine(str);
		}
		File.WriteAllText(file_name, stringBuilder.ToString());
	}

	public void SaveToCSVFile(string file_name)
	{
		StringBuilder stringBuilder = new StringBuilder(size + Count * 16);
		for (int i = 0; i < strings.Count; i++)
		{
			Info info = strings[i];
			string text = info.str ?? "<null>";
			text = i + ";\"" + text + "\";" + info.lookups;
			stringBuilder.AppendLine(text);
		}
		File.WriteAllText(file_name, stringBuilder.ToString());
	}

	public static void Test(UniqueStrings us)
	{
		us.SaveToTXTFile("../unique_strings.txt");
		us.SaveToCSVFile("../unique_strings.csv");
	}
}
