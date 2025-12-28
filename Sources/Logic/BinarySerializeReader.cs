namespace Logic;

public class BinarySerializeReader : Serialization.IReader
{
	public delegate void OnMsgHeaderErrorCallback(string message);

	private MemStream reader;

	private bool debug_headers;

	private OnMsgHeaderErrorCallback onMsgHeaderErrorCallback;

	public BinarySerializeReader(Game game, OnMsgHeaderErrorCallback onMsgHeaderErrorCallback)
		: base(game?.defs)
	{
		this.onMsgHeaderErrorCallback = onMsgHeaderErrorCallback;
	}

	public void Init(MemStream stream, bool useDebugHeaders)
	{
		reader = stream;
		debug_headers = useDebugHeaders;
	}

	public override int Position()
	{
		return reader.Position;
	}

	public override int Length()
	{
		return reader.Length;
	}

	private void ReadHeader(string type, string key, int key_idx = int.MaxValue)
	{
		if (key == null || !debug_headers)
		{
			return;
		}
		string text;
		string text2;
		if (unique_strings == null)
		{
			text = reader.ReadString();
			text2 = reader.ReadString();
		}
		else
		{
			int uid = reader.Read7BitUInt();
			int uid2 = reader.Read7BitUInt();
			text = unique_strings.Get(uid);
			text2 = unique_strings.Get(uid2);
		}
		if (text != type)
		{
			string text3 = "Received type \"" + text + "\" is different than the expected type \"" + type + "\" for key \"" + key + "\"";
			Error(text3);
			onMsgHeaderErrorCallback?.Invoke(text3);
		}
		if (text2 != key)
		{
			string text4 = "Received key \"" + text2 + "\" is different than the expected key \"" + key + "\"";
			Error(text4);
			onMsgHeaderErrorCallback?.Invoke(text4);
		}
		if (key_idx != int.MaxValue)
		{
			int num = reader.Read7BitUInt();
			if (num != key_idx)
			{
				string text5 = "Received key idx \"" + num + "\" is different than the expected key idx \"" + key_idx + "\" for key \"" + text2 + "\"";
				Error(text5);
				onMsgHeaderErrorCallback?.Invoke(text5);
			}
		}
	}

	public override void ReadMessageHeader(out byte id, out Serialization.ObjectType tid, out Serialization.ObjectTypeInfo ti, out int nid)
	{
		id = ReadByte(null);
		NID nID = ReadNID(null);
		ti = nID.ti;
		if (ti == null)
		{
			Error("Attempting to read object " + id + " of unknown object type: " + nID.ToString());
			nid = -1;
			tid = Serialization.ObjectType.COUNT;
		}
		else
		{
			nid = nID.nid;
			tid = nID.ti.tid;
		}
	}

	public override void ReadMessageHeader(out byte id, out Serialization.ObjectType tid, out Serialization.ObjectTypeInfo ti, out int nid, out byte substate_id, out int substate_index)
	{
		ReadMessageHeader(out id, out tid, out ti, out nid);
		substate_id = ReadByte(null);
		substate_index = Read7BitUInt(null);
	}

	public override Section OpenSection(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("begin_section", key, key_idx);
		return new Section(this, key, key_idx);
	}

	public override void CloseSection(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("end_section", key, key_idx);
	}

	public override bool ReadBool(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("bool", key, key_idx);
		byte b = reader.ReadByte();
		if (b != 0 && b != 1)
		{
			Error("Read " + b + " as boolean");
		}
		return b != 0;
	}

	public override byte ReadByte(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("byte", key, key_idx);
		return reader.ReadByte();
	}

	public override int Read7BitUInt(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("uint7", key, key_idx);
		return reader.Read7BitUInt();
	}

	public override int Read7BitSigned(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("sint7", key, key_idx);
		int num = reader.Read7BitUInt();
		int num2 = num >> 1;
		if ((num & 1) != 0)
		{
			num2 = -num2;
		}
		return num2;
	}

	public override string ReadStr(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("str", key, key_idx);
		if (unique_strings == null)
		{
			return reader.ReadString();
		}
		int uid = reader.Read7BitUInt();
		return unique_strings.Get(uid);
	}

	public override string ReadRawStr(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("string", key, key_idx);
		return reader.ReadString();
	}

	public override float ReadFloat(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("float", key, key_idx);
		return reader.ReadFloat();
	}

	public override Point ReadPoint(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("point", key, key_idx);
		float x = reader.ReadFloat();
		float y = reader.ReadFloat();
		return new Point(x, y);
	}

	public override PPos ReadPPos(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("point", key, key_idx);
		float x = reader.ReadFloat();
		float y = reader.ReadFloat();
		int paID = Read7BitSigned(key, key_idx);
		return new PPos(x, y, paID);
	}

	public override byte[] ReadBytes(string key, int key_idx = int.MaxValue)
	{
		ReadHeader("bytes", key, key_idx);
		int num = reader.Read7BitUInt() - 1;
		if (num < 0)
		{
			return null;
		}
		byte[] array = new byte[num];
		new MemStream(array, 0, num).CopyBytesFrom(ref reader, num);
		return array;
	}

	private static void Log(string msg)
	{
		Game.Log("[BinarySerializeReader]: " + msg, Game.LogType.Message);
	}

	private static void Error(string msg)
	{
		Game.Log("[BinarySerializeReader]: " + msg, Game.LogType.Error);
	}

	public override NID ReadNID(Serialization.ObjectTypeInfo ti, int pid, string type, string key, int key_idx = int.MaxValue)
	{
		int num = Read7BitUInt(null);
		if (num == 0)
		{
			return NID.Null;
		}
		if (ti == null)
		{
			ti = Serialization.ObjectTypeInfo.Get((Serialization.ObjectType)Read7BitUInt(null));
		}
		if (pid < 0)
		{
			pid = ((ti != null && ti.dynamic) ? Read7BitUInt(null) : 0);
		}
		return new NID(ti, pid, num);
	}
}
