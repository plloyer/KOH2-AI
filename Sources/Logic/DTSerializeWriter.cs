using System;
using System.Collections.Generic;
using System.Text;

namespace Logic;

public class DTSerializeWriter : Serialization.IWriter
{
	private List<DT.Field> sections;

	public DTSerializeWriter(UniqueStrings unique_strings)
	{
		base.unique_strings = unique_strings;
	}

	public DT.Field GetRoot()
	{
		if (sections == null || sections.Count == 0)
		{
			return null;
		}
		return sections[0];
	}

	public void SetRootKey(string key, Value val = default(Value))
	{
		unique_strings?.Clear();
		if (key == null)
		{
			sections = null;
			return;
		}
		DT.Field field = new DT.Field(null);
		field.key = key;
		field.value = val;
		field.flags = DT.Field.Flags.StartsAtSameLine;
		sections = new List<DT.Field>();
		sections.Add(field);
	}

	public override Section OpenSection(string type, string key, int key_idx = int.MaxValue, bool checkKeys = true)
	{
		DT.Field item = CreateField(type, key, key_idx, Value.Unknown, checkKeys);
		sections.Add(item);
		return new Section(this, key, key_idx);
	}

	public override void CloseSection(string key, int key_idx = int.MaxValue)
	{
		DT.Field lastSection = GetLastSection();
		if (lastSection != null && !lastSection.key.StartsWith(key, StringComparison.Ordinal))
		{
			Error("Key mismatch while closing section with key " + key + ". Last opened section has a key " + lastSection.key);
		}
		RemoveLastSection();
	}

	public DT.Field CreateField(string type, string key, int key_idx, Value val, bool checkKeys = true)
	{
		if (unique_strings != null)
		{
			type = unique_strings.Resolve(type);
			if (sections.Count > 1)
			{
				key = unique_strings.Resolve(key);
			}
		}
		if (key == null)
		{
			key = "";
		}
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field lastSection = GetLastSection();
		if (lastSection == null)
		{
			return null;
		}
		if (checkKeys && lastSection.FindChild(key) != null)
		{
			Error("Field with key " + key + " already exists in section " + lastSection.ToString());
			return null;
		}
		DT.Field field = lastSection.AddChild(key);
		if (!string.IsNullOrEmpty(type))
		{
			field.type = type;
		}
		field.value = val;
		return field;
	}

	public override void WriteMessageHeader(byte msg_id, NID nid)
	{
		Error("WriteMessageHeader() should not be called when serializing as DT");
	}

	public override void WriteBool(bool val, string key, int key_idx = int.MaxValue)
	{
		CreateField("bool", key, key_idx, val);
	}

	public override void WriteByte(byte val, string key, int key_idx = int.MaxValue)
	{
		CreateField("byte", key, key_idx, val);
	}

	public override void Write7BitUInt(int val, string key, int key_idx = int.MaxValue)
	{
		if (val < 0)
		{
			Game.Log("Serializing " + val + " as 7 bit integer", Game.LogType.Warning);
		}
		CreateField("int", key, key_idx, val);
	}

	public override void Write7BitSigned(int val, string key, int key_idx = int.MaxValue)
	{
		if ((((val >= 0) ? val : (-val)) & 0x80000000u) != 0L)
		{
			Game.Log("Serializing " + val + " as 7 bit integer", Game.LogType.Warning);
		}
		CreateField("int", key, key_idx, val);
	}

	public override void WriteStr(string val, string key, int key_idx = int.MaxValue)
	{
		if (unique_strings != null)
		{
			val = unique_strings.Resolve(val);
		}
		CreateField("string", key, key_idx, val);
	}

	public override void WriteRawStr(string val, string key, int key_idx = int.MaxValue)
	{
		CreateField("string", key, key_idx, val);
	}

	public override void WriteFloat(float val, string key, int key_idx = int.MaxValue)
	{
		CreateField("float", key, key_idx, val);
	}

	public override void WritePoint(Point val, string key, int key_idx = int.MaxValue)
	{
		CreateField("point", key, key_idx, Value.Unknown).value_str = val.ToString();
	}

	public override void WritePPos(PPos val, string key, int key_idx = int.MaxValue)
	{
		CreateField("point", key, key_idx, Value.Unknown).value_str = val.ToString();
	}

	public override void WriteBytes(byte[] bytes, string key, int key_idx = int.MaxValue)
	{
		DT.Field field = CreateField("bytes", key, key_idx, Value.Unknown);
		StringBuilder stringBuilder = new StringBuilder("[", bytes.Length * 3);
		for (int i = 0; i < bytes.Length; i++)
		{
			stringBuilder.Append(bytes[i]);
			if (i < bytes.Length - 1)
			{
				stringBuilder.Append(", ");
			}
		}
		stringBuilder.Append("]");
		field.value_str = stringBuilder.ToString();
	}

	public override int Position()
	{
		Error("Position() should not be called when serializing as DT");
		return 0;
	}

	public override void Close()
	{
		Error("Close() should not be called when serializing as DT");
	}

	public DT.Field GetLastSection()
	{
		return sections[sections.Count - 1];
	}

	private void RemoveLastSection()
	{
		sections.RemoveAt(sections.Count - 1);
	}

	private int GetParentsCount(DT.Field field)
	{
		int num = 0;
		DT.Field field2 = field;
		while (field2.parent != null)
		{
			num++;
			field2 = field2.parent;
		}
		return num;
	}

	private static void Log(string msg)
	{
		Game.Log("[DTSerializeWriter]: " + msg, Game.LogType.Message);
	}

	private static void Error(string msg)
	{
		Game.Log("[DTSerializeWriter]: " + msg, Game.LogType.Error);
	}

	public override void WriteNID(int id, Serialization.ObjectTypeInfo ti, int pid, string type, string key, int key_idx = int.MaxValue)
	{
		if (id == 0 || (ti == null && pid < 0))
		{
			CreateField(type, key, key_idx, id);
			return;
		}
		List<DT.SubValue> list = new List<DT.SubValue>(3);
		list.Add(new DT.SubValue
		{
			value = id
		});
		if (ti != null)
		{
			list.Add(new DT.SubValue
			{
				value = ti.name
			});
		}
		if (pid >= 0)
		{
			list.Add(new DT.SubValue
			{
				value = pid
			});
		}
		CreateField(type, key, key_idx, new Value(list));
	}
}
