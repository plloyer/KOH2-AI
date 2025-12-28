namespace Logic;

public class BinarySerializeWriter : Serialization.IWriter
{
	private MemStream writer;

	private bool debug_headers;

	public BinarySerializeWriter(UniqueStrings unique_strings, MemStream stream, bool useDebugHeaders)
	{
		base.unique_strings = unique_strings;
		writer = stream;
		debug_headers = useDebugHeaders;
	}

	private void WriteHeader(string type, string key, int key_idx = int.MaxValue)
	{
		if (key != null && debug_headers)
		{
			if (unique_strings == null)
			{
				writer.WriteString(type);
				writer.WriteString(key);
			}
			else
			{
				int i = unique_strings.Register(type);
				int i2 = unique_strings.Register(key);
				writer.Write7BitUInt(i);
				writer.Write7BitUInt(i2);
			}
			if (key_idx != int.MaxValue)
			{
				Write7BitUInt(key_idx, null);
			}
		}
	}

	public override void WriteMessageHeader(byte msg_id, NID nid)
	{
		WriteByte(msg_id, null);
		WriteNID(nid, null);
	}

	public override Section OpenSection(string type, string key, int key_idx = int.MaxValue, bool checkKeys = true)
	{
		WriteHeader("begin_section", key, key_idx);
		return new Section(this, key, key_idx);
	}

	public override void CloseSection(string key, int key_idx = int.MaxValue)
	{
		WriteHeader("end_section", key, key_idx);
	}

	public override void WriteBool(bool val, string key, int key_idx = int.MaxValue)
	{
		WriteHeader("bool", key, key_idx);
		writer.WriteByte((byte)(val ? 1 : 0));
	}

	public override void WriteByte(byte val, string key, int key_idx = int.MaxValue)
	{
		WriteHeader("byte", key, key_idx);
		writer.WriteByte(val);
	}

	public override void Write7BitUInt(int val, string key, int key_idx = int.MaxValue)
	{
		WriteHeader("uint7", key, key_idx);
		writer.Write7BitUInt(val);
	}

	public override void Write7BitSigned(int val, string key, int key_idx = int.MaxValue)
	{
		WriteHeader("sint7", key, key_idx);
		int num;
		int num2;
		if (val >= 0)
		{
			num = 0;
			num2 = val;
		}
		else
		{
			num2 = -val;
			num = 1;
		}
		if ((num2 & 0x80000000u) != 0L)
		{
			Game.Log("Serializing " + val + " as 7 bit integer", Game.LogType.Warning);
		}
		num2 = (num2 << 1) | num;
		writer.Write7BitUInt(num2);
	}

	public override void WriteStr(string val, string key, int key_idx = int.MaxValue)
	{
		WriteHeader("str", key, key_idx);
		if (unique_strings == null)
		{
			writer.WriteString(val);
			return;
		}
		int i = unique_strings.Register(val);
		writer.Write7BitUInt(i);
	}

	public override void WriteRawStr(string val, string key, int key_idx = int.MaxValue)
	{
		WriteHeader("string", key, key_idx);
		writer.WriteString(val);
	}

	public override void WriteFloat(float val, string key, int key_idx = int.MaxValue)
	{
		WriteHeader("float", key, key_idx);
		writer.WriteFloat(val);
	}

	public override void WritePoint(Point val, string key, int key_idx = int.MaxValue)
	{
		WriteHeader("point", key, key_idx);
		writer.WriteFloat(val.x);
		writer.WriteFloat(val.y);
	}

	public override void WritePPos(PPos val, string key, int key_idx = int.MaxValue)
	{
		WritePoint(val.pos, key, key_idx);
		Write7BitSigned(val.paID, key, key_idx);
	}

	public override void WriteBytes(byte[] bytes, string key, int key_idx = int.MaxValue)
	{
		WriteHeader("bytes", key);
		if (bytes == null)
		{
			writer.Write7BitUInt(0);
			return;
		}
		writer.Write7BitUInt(bytes.Length + 1);
		writer.WriteBytes(bytes);
	}

	public override int Position()
	{
		return writer.Position;
	}

	public override void Close()
	{
		writer.Close();
	}

	private static void Log(string msg)
	{
		Game.Log("[BinarySerializeWriter]: " + msg, Game.LogType.Message);
	}

	private static void Error(string msg)
	{
		Game.Log("[BinarySerializeWriter]: " + msg, Game.LogType.Error);
	}

	public override void WriteNID(int id, Serialization.ObjectTypeInfo ti, int pid, string type, string key, int key_idx = int.MaxValue)
	{
		Write7BitUInt(id, null);
		if (id != 0)
		{
			if (ti != null)
			{
				Write7BitUInt((int)ti.tid, null);
			}
			if (pid >= 0)
			{
				Write7BitUInt(pid, null);
			}
		}
	}
}
