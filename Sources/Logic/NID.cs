using System;
using System.Collections.Generic;

namespace Logic;

public struct NID
{
	public Serialization.ObjectTypeInfo ti;

	public int nid;

	private const int id_bits = 22;

	private const int id_mask = 4194303;

	public const int MAX_PID = 1023;

	public const int MAX_OBJ_ID = 4194303;

	public static NID Null = new NID((Serialization.ObjectTypeInfo)null, 0);

	public int pid
	{
		get
		{
			return nid >> 22;
		}
		set
		{
			nid = (value << 22) | (nid & 0x3FFFFF);
		}
	}

	public int id
	{
		get
		{
			return nid & 0x3FFFFF;
		}
		set
		{
			nid = (nid & -4194304) | (value & 0x3FFFFF);
		}
	}

	public NID(Serialization.ObjectTypeInfo ti, int nid)
	{
		if (ti == null && nid != 0)
		{
			Game.Log("Attempting to create NID for unknown object type", Game.LogType.Error);
		}
		this.ti = ti;
		this.nid = nid;
	}

	public NID(Serialization.ObjectTypeInfo ti, int pid, int id)
	{
		if (ti == null && id != 0)
		{
			Game.Log("Attempting to create NID for unknown object type", Game.LogType.Error);
		}
		this.ti = ti;
		nid = (pid << 22) | id;
	}

	public NID(Type type, int nid)
	{
		ti = Serialization.ObjectTypeInfo.Get(type);
		if (ti == null)
		{
			Game.Log("Attempting to create NID for unknown object type: " + type.Name, Game.LogType.Error);
		}
		this.nid = nid;
	}

	public NID(Serialization.ObjectType tid, int nid)
	{
		ti = Serialization.ObjectTypeInfo.Get(tid);
		if (ti == null)
		{
			Game.Log("Attempting to create NID for unknown object type: " + tid, Game.LogType.Error);
		}
		this.nid = nid;
	}

	public static implicit operator NID(Object obj)
	{
		if (obj == null)
		{
			return Null;
		}
		Serialization.ObjectTypeInfo objectTypeInfo = Serialization.ObjectTypeInfo.Get(obj);
		int num = obj.GetNid();
		return new NID(objectTypeInfo, num);
	}

	public static NID FromDTValue(Value value)
	{
		if (value.type == Value.Type.Int)
		{
			return new NID((Serialization.ObjectTypeInfo)null, value.int_val);
		}
		if (!value.is_object)
		{
			return Null;
		}
		if (!(value.obj_val is List<DT.SubValue> { Count: not 0 } list))
		{
			return Null;
		}
		int num = list[0].value.Int();
		Serialization.ObjectTypeInfo objectTypeInfo = null;
		int num2 = 0;
		if (list.Count > 1)
		{
			string value2 = list[1].value.String();
			if (!string.IsNullOrEmpty(value2) && Enum.TryParse<Serialization.ObjectType>(value2, out var result))
			{
				objectTypeInfo = Serialization.ObjectTypeInfo.Get(result);
			}
			num2 = list[list.Count - 1].value.Int();
		}
		return new NID(objectTypeInfo, num2, num);
	}

	public Value ToDTValue()
	{
		if (ti == null && pid <= 0)
		{
			return id;
		}
		List<DT.SubValue> list = new List<DT.SubValue>();
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
		if (pid != 0)
		{
			list.Add(new DT.SubValue
			{
				value = pid
			});
		}
		return new Value(list);
	}

	public Object GetObj(Game game)
	{
		if (id == 0 || ti == null)
		{
			return null;
		}
		return game.multiplayer?.objects.Get(ti.tid, nid);
	}

	public T Get<T>(Game game) where T : Object
	{
		if (ti != null && ti.type != typeof(T))
		{
			Game.Log("Attempting to get " + ti.name + " as " + typeof(T).Name, Game.LogType.Error);
		}
		if (id == 0)
		{
			return null;
		}
		Multiplayer multiplayer = game.multiplayer;
		if (multiplayer == null)
		{
			return null;
		}
		return multiplayer.objects.Get<T>(nid);
	}

	public static int Encode(int pid, int id)
	{
		return (pid << 22) | id;
	}

	public static void Decode(int nid, out int pid, out int id)
	{
		pid = nid >> 22;
		id = nid & 0x3FFFFF;
	}

	public static string ToString(int nid)
	{
		Decode(nid, out var num, out var num2);
		string text = num2.ToString();
		if (num > 0)
		{
			text = text + "|" + num;
		}
		return text;
	}

	public static int Decode(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return 0;
		}
		int num = s.IndexOf('|');
		int.TryParse((num < 0) ? s : s.Substring(0, num), out var result);
		int result2;
		if (num < 0)
		{
			result2 = 0;
		}
		else
		{
			int.TryParse(s.Substring(num + 1), out result2);
		}
		return Encode(result2, result);
	}

	public static NID FromString(string s, Serialization.ObjectTypeInfo ti = null, int pid = -1)
	{
		if (string.IsNullOrEmpty(s))
		{
			return Null;
		}
		int num = s.IndexOf('|');
		int.TryParse((num < 0) ? s : s.Substring(0, num), out var result);
		if (num > 0)
		{
			num++;
			int num2 = s.IndexOf('|', num);
			if (num2 < 0)
			{
				num2 = s.Length;
			}
			if (!char.IsDigit(s[num]) && ti == null)
			{
				if (Enum.TryParse<Serialization.ObjectType>(s.Substring(num, num2 - num), out var result2))
				{
					ti = Serialization.ObjectTypeInfo.Get(result2);
				}
			}
			else if (pid < 0)
			{
				int.TryParse(s.Substring(num, num2 - num), out pid);
			}
			if (num2 > 0 && pid < 0)
			{
				num2++;
				int.TryParse(s.Substring(num2), out pid);
			}
		}
		return new NID(ti, pid, result);
	}

	public override string ToString()
	{
		string text = id.ToString();
		if (ti != null)
		{
			text = text + "|" + ti.name;
		}
		if (pid > 0)
		{
			text = text + "|" + pid;
		}
		return text;
	}
}
