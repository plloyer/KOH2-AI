using System;

namespace Logic;

public class DTSerializeReader : Serialization.IReader
{
	private DT.Field dtField;

	public DTSerializeReader(Game game, DT.Field field = null)
		: base(game?.defs)
	{
		dtField = field;
	}

	public void SetDTField(DT.Field field)
	{
		dtField = field;
	}

	public override void ReadMessageHeader(out byte id, out Serialization.ObjectType tid, out Serialization.ObjectTypeInfo ti, out int nid)
	{
		Error("ReadMessageHeader() should not be called when serializing as DT");
		id = 0;
		tid = Serialization.ObjectType.COUNT;
		ti = null;
		nid = -1;
	}

	public override void ReadMessageHeader(out byte id, out Serialization.ObjectType tid, out Serialization.ObjectTypeInfo ti, out int nid, out byte substate_id, out int substate_index)
	{
		Error("ReadMessageHeader() should not be called when serializing as DT");
		id = 0;
		tid = Serialization.ObjectType.COUNT;
		ti = null;
		nid = -1;
		substate_id = 0;
		substate_index = -1;
	}

	public override Section OpenSection(string key, int key_idx = int.MaxValue)
	{
		if (key == null)
		{
			key = "";
		}
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("OpenSection for invalid key " + key);
		}
		else
		{
			dtField = field;
		}
		return new Section(this, key, key_idx);
	}

	public override void CloseSection(string key, int key_idx = int.MaxValue)
	{
		if (!dtField.key.StartsWith(key, StringComparison.Ordinal))
		{
			Warning("CloseSection for invalid key " + key + ", current is " + dtField.key);
		}
		else
		{
			dtField = dtField.parent;
		}
	}

	public override bool ReadBool(string key, int key_idx = int.MaxValue)
	{
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("ReadBool for invalid key " + key);
			return false;
		}
		return field.Bool();
	}

	public override byte ReadByte(string key, int key_idx = int.MaxValue)
	{
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("ReadByte for invalid key " + key);
			return 0;
		}
		int num = 0;
		if (field.value.type == Value.Type.Int)
		{
			num = field.value.int_val;
		}
		else if (field.value.type == Value.Type.Float)
		{
			num = (int)field.value.float_val;
			if ((float)num != field.value.float_val)
			{
				Warning("Rounding float value to integer for key " + key);
			}
		}
		else
		{
			Warning("Error while reading integer value for key " + key);
		}
		if (num < 0)
		{
			Warning("Clampimg negative value to 0 for key " + key);
			return 0;
		}
		if (num > 255)
		{
			Warning("Clampimg large value to 255 for key " + key);
			return byte.MaxValue;
		}
		return (byte)num;
	}

	public override int Read7BitUInt(string key, int key_idx = int.MaxValue)
	{
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("Read7BitInt for invalid key " + key);
			return 0;
		}
		if (field.value.type == Value.Type.Int)
		{
			return field.value.int_val;
		}
		if (field.value.type == Value.Type.Float)
		{
			int num = (int)field.value.float_val;
			if ((float)num != field.value.float_val)
			{
				Warning("Rounding float value to integer for key " + key);
			}
			return num;
		}
		Warning("Error while reading integer value for key " + key);
		return 0;
	}

	public override int Read7BitSigned(string key, int key_idx = int.MaxValue)
	{
		return Read7BitUInt(key, key_idx);
	}

	public override string ReadStr(string key, int key_idx = int.MaxValue)
	{
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("ReadStr for invalid key " + key);
			return null;
		}
		return field.String();
	}

	public override string ReadRawStr(string key, int key_idx = int.MaxValue)
	{
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("ReadRawStr for invalid key " + key);
			return null;
		}
		return DT.Unquote(field.value_str);
	}

	public override float ReadFloat(string key, int key_idx = int.MaxValue)
	{
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("ReadFloat for invalid key " + key);
			return 0f;
		}
		float f = 0f;
		if (!DT.ParseFloat(field.ValueStr(), out f))
		{
			Warning("Error while parsing float value for key " + key);
		}
		return f;
	}

	public override Point ReadPoint(string key, int key_idx = int.MaxValue)
	{
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("ReadPoint for invalid key " + key);
			return Point.Invalid;
		}
		Point res = Point.Invalid;
		if (!DT.Convert(field.value_str, ref res))
		{
			Warning("Error while parsing point value for key " + key);
		}
		return res;
	}

	public override PPos ReadPPos(string key, int key_idx = int.MaxValue)
	{
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("ReadPoint for invalid key " + key);
			return Point.Invalid;
		}
		PPos res = PPos.Invalid;
		if (!DT.Convert(field.value_str, ref res))
		{
			Warning("Error while parsing point value for key " + key);
		}
		return res;
	}

	public override byte[] ReadBytes(string key, int key_idx = int.MaxValue)
	{
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("ReadBytes for invalid key " + key);
			return null;
		}
		string[] array = field.value_str.Substring(1, field.value_str.Length - 2).Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
		byte[] array2 = new byte[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			byte result = 0;
			if (!byte.TryParse(array[i], out result))
			{
				Warning("Error while parsing byte value for key " + key);
			}
			array2[i] = result;
		}
		return array2;
	}

	public override int Position()
	{
		Error("Position() should not be called when serializing as DT");
		return 0;
	}

	public override int Length()
	{
		Error("Length() should not be called when serializing as DT");
		return 0;
	}

	private static void Log(string msg)
	{
		Game.Log("[DTSerializeReader]: " + msg, Game.LogType.Message);
	}

	private static void Warning(string msg)
	{
		Game.Log("[DTSerializeReader]: " + msg, Game.LogType.Warning);
	}

	private static void Error(string msg)
	{
		Game.Log("[DTSerializeReader]: " + msg, Game.LogType.Error);
	}

	public override NID ReadNID(Serialization.ObjectTypeInfo ti, int pid, string type, string key, int key_idx = int.MaxValue)
	{
		if (pid > 0)
		{
			Error("ReadNID called with pid " + pid + " (should be <= 0)");
		}
		if (key_idx != int.MaxValue)
		{
			key += key_idx;
		}
		DT.Field field = dtField.FindChild(key);
		if (field == null)
		{
			Warning("ReadNID for invalid key " + key);
			return NID.Null;
		}
		if (field.type != type)
		{
			Warning("Attempting to read '" + field.type + "' as '" + type + "'");
		}
		int num = field.Int(0);
		int idx = 1;
		if (ti == null && num != 0)
		{
			idx = 2;
			string text = field.String(1);
			if (!Enum.TryParse<Serialization.ObjectType>(text, out var result))
			{
				Warning("Read unknown type: '" + text + "'");
				return NID.Null;
			}
			ti = Serialization.ObjectTypeInfo.Get(result);
			if (ti == null)
			{
				Warning("Could not resolve type info: '" + text + "'");
				return NID.Null;
			}
		}
		if (pid < 0)
		{
			pid = ((num != 0 && ti.dynamic) ? field.Int(idx) : 0);
		}
		return new NID(ti, pid, num);
	}
}
