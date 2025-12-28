using System;
using System.Collections;
using System.Collections.Generic;

namespace Logic;

public struct Value : IEquatable<Value>
{
	public class NullData : Data
	{
		public static NullData Create()
		{
			return new NullData();
		}

		public override bool InitFrom(object obj)
		{
			return obj == null;
		}

		public override void Save(Serialization.IWriter ser)
		{
		}

		public override void Load(Serialization.IReader ser)
		{
		}

		public override object GetObject(Game game)
		{
			return null;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			return obj == null;
		}

		public override Value GetValue(Game game)
		{
			return Null;
		}
	}

	public class UnknownData : Data
	{
		public static UnknownData Create()
		{
			return new UnknownData();
		}

		public override bool InitFrom(object obj)
		{
			return false;
		}

		public override void Save(Serialization.IWriter ser)
		{
		}

		public override void Load(Serialization.IReader ser)
		{
		}

		public override object GetObject(Game game)
		{
			return null;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			return false;
		}

		public override Value GetValue(Game game)
		{
			return Unknown;
		}
	}

	public class IntData : Data
	{
		private int value;

		public IntData(int value)
		{
			this.value = value;
		}

		public static IntData Create()
		{
			return new IntData(0);
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is int))
			{
				return false;
			}
			value = (int)obj;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.Write7BitSigned(value, "value");
		}

		public override void Load(Serialization.IReader ser)
		{
			value = ser.Read7BitSigned("value");
		}

		public override object GetObject(Game game)
		{
			return value;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (obj is int)
			{
				return (int)obj == value;
			}
			return false;
		}

		public override Value GetValue(Game game)
		{
			return value;
		}
	}

	public class FloatData : Data
	{
		private float value;

		public FloatData(float value)
		{
			this.value = value;
		}

		public static FloatData Create()
		{
			return new FloatData(0f);
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is float))
			{
				return false;
			}
			value = (float)obj;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteFloat(value, "value");
		}

		public override void Load(Serialization.IReader ser)
		{
			value = ser.ReadFloat("value");
		}

		public override object GetObject(Game game)
		{
			return value;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (obj is float)
			{
				return (float)obj == value;
			}
			return false;
		}

		public override Value GetValue(Game game)
		{
			return value;
		}
	}

	public class StringData : Data
	{
		private string value;

		public StringData(string value)
		{
			this.value = value;
		}

		public static StringData Create()
		{
			return new StringData(null);
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is string))
			{
				return false;
			}
			value = obj as string;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteStr(value, "value");
		}

		public override void Load(Serialization.IReader ser)
		{
			value = ser.ReadStr("value");
		}

		public override object GetObject(Game game)
		{
			return value;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (obj is string)
			{
				return (string)obj == value;
			}
			return false;
		}

		public override Value GetValue(Game game)
		{
			return value;
		}
	}

	public class ListData : Data
	{
		public System.Type lst_type;

		public List<Data> lst;

		public ListData(IList lst)
		{
			InitFrom(lst);
		}

		public static ListData Create()
		{
			return new ListData(null);
		}

		public override bool InitFrom(object obj)
		{
			if (obj == null)
			{
				lst_type = null;
				lst = null;
				return true;
			}
			if (!(obj is IList list))
			{
				lst_type = null;
				lst = null;
				return false;
			}
			lst_type = obj.GetType();
			int count = list.Count;
			lst = new List<Data>(count);
			for (int i = 0; i < count; i++)
			{
				object val = list[i];
				Data item = new Value(val).CreateData();
				lst.Add(item);
			}
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			if (lst == null)
			{
				ser.WriteStr(null, "list_type");
				return;
			}
			ser.WriteStr(lst_type.FullName, "list_type");
			int count = lst.Count;
			ser.Write7BitUInt(count, "count");
			for (int i = 0; i < count; i++)
			{
				Data data = lst[i];
				ser.WriteData(data, "", i);
			}
		}

		public override void Load(Serialization.IReader ser)
		{
			string text = ser.ReadStr("list_type");
			if (string.IsNullOrEmpty(text))
			{
				lst_type = null;
				lst = null;
				return;
			}
			lst_type = System.Type.GetType(text);
			if (lst_type == null)
			{
				Game.Log("Could not resolve list type: " + text, Game.LogType.Error);
				lst = null;
				return;
			}
			int num = ser.Read7BitUInt("count");
			lst = new List<Data>(num);
			for (int i = 0; i < num; i++)
			{
				Data item = ser.ReadData("", i);
				lst.Add(item);
			}
		}

		public override object GetObject(Game game)
		{
			if (lst == null || lst_type == null)
			{
				return null;
			}
			return Activator.CreateInstance(lst_type, lst.Count);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			try
			{
				if (!(obj is IList list))
				{
					return false;
				}
				bool flag = false;
				if (lst_type.IsGenericType)
				{
					System.Type[] genericTypeArguments = lst_type.GenericTypeArguments;
					if (genericTypeArguments != null && genericTypeArguments.Length == 1 && genericTypeArguments[0] == typeof(Value))
					{
						flag = true;
					}
				}
				list.Clear();
				if (lst == null)
				{
					return false;
				}
				for (int i = 0; i < lst.Count; i++)
				{
					Data data = lst[i];
					if (data == null)
					{
						list.Add(null);
						continue;
					}
					Value value = data.GetValue(game);
					if (flag)
					{
						list.Add(value);
					}
					else
					{
						list.Add(value.Object());
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				Game.Log("Error in ListData.ApplyTo: " + ex.ToString(), Game.LogType.Error);
				return false;
			}
		}
	}

	public enum Type
	{
		Unknown,
		Null,
		Int,
		Float,
		String,
		Object
	}

	public Type type;

	public int int_val;

	public float float_val;

	public object obj_val;

	public static Value Unknown = new Value(Type.Unknown);

	public static Value Null = new Value(Type.Null);

	public static long boxed = 0L;

	public bool is_null => type == Type.Null;

	public bool is_unknown => type == Type.Unknown;

	public bool is_valid
	{
		get
		{
			if (type != Type.Null)
			{
				return type != Type.Unknown;
			}
			return false;
		}
	}

	public bool is_number
	{
		get
		{
			if (type != Type.Int)
			{
				return type == Type.Float;
			}
			return true;
		}
	}

	public bool is_string => type == Type.String;

	public bool is_object => type == Type.Object;

	public bool IsRefSerializable()
	{
		if (type != Type.Object)
		{
			return true;
		}
		return Data.IsRefSerializable(obj_val);
	}

	public Data CreateRefData()
	{
		return type switch
		{
			Type.Null => new NullData(), 
			Type.Unknown => new UnknownData(), 
			Type.Int => new IntData(int_val), 
			Type.Float => new FloatData(float_val), 
			Type.String => new StringData(obj_val as string), 
			_ => Data.CreateRef(obj_val), 
		};
	}

	public bool IsFullSerializable()
	{
		if (type != Type.Object)
		{
			return true;
		}
		return Data.IsFullSerializable(obj_val);
	}

	public Data CreateFullData()
	{
		if (type != Type.Object)
		{
			return CreateRefData();
		}
		return Data.CreateFull(obj_val);
	}

	public bool IsSerializable()
	{
		if (!IsRefSerializable())
		{
			return IsFullSerializable();
		}
		return true;
	}

	public Data CreateData()
	{
		if (IsRefSerializable())
		{
			return CreateRefData();
		}
		return CreateFullData();
	}

	public Value(Type type)
	{
		this.type = type;
		int_val = 0;
		float_val = 0f;
		obj_val = null;
	}

	public Value(bool val)
	{
		type = Type.Int;
		int_val = (val ? 1 : 0);
		float_val = 0f;
		obj_val = null;
	}

	public Value(int val)
	{
		type = Type.Int;
		int_val = val;
		float_val = 0f;
		obj_val = null;
	}

	public Value(float val)
	{
		type = Type.Float;
		int_val = 0;
		float_val = val;
		obj_val = null;
	}

	public Value(string val)
	{
		type = ((val == null) ? Type.Null : Type.String);
		int_val = 0;
		float_val = 0f;
		obj_val = val;
	}

	public Value(Value val)
	{
		type = val.type;
		int_val = val.int_val;
		float_val = val.float_val;
		obj_val = val.obj_val;
	}

	public Value(object val)
	{
		int_val = 0;
		float_val = 0f;
		obj_val = val;
		if (val == null)
		{
			type = Type.Null;
		}
		else if (val is bool)
		{
			type = Type.Int;
			int_val = (((bool)val) ? 1 : 0);
			inc_boxed();
		}
		else if (val is int)
		{
			type = Type.Int;
			int_val = (int)val;
			inc_boxed();
		}
		else if (val is float)
		{
			type = Type.Float;
			float_val = (float)val;
			inc_boxed();
		}
		else if (val is string)
		{
			type = Type.String;
		}
		else if (val is Value value)
		{
			type = value.type;
			int_val = value.int_val;
			float_val = value.float_val;
			obj_val = value.obj_val;
			inc_boxed();
		}
		else
		{
			type = Type.Object;
		}
	}

	public static Value Error(string err)
	{
		return new Value
		{
			type = Type.Null,
			obj_val = err
		};
	}

	public System.Type CSType()
	{
		switch (type)
		{
		case Type.Unknown:
		case Type.Null:
			return null;
		case Type.Int:
			return typeof(int);
		case Type.Float:
			return typeof(float);
		case Type.String:
			return typeof(string);
		default:
			return obj_val?.GetType();
		}
	}

	public static implicit operator Value(bool val)
	{
		return new Value(val);
	}

	public static implicit operator Value(int val)
	{
		return new Value(val);
	}

	public static implicit operator Value(float val)
	{
		return new Value(val);
	}

	public static implicit operator Value(string val)
	{
		return new Value(val);
	}

	public static implicit operator Value(BaseObject val)
	{
		return new Value((object)val);
	}

	public static implicit operator bool(Value val)
	{
		return val.Bool();
	}

	public static implicit operator int(Value val)
	{
		return val.Int();
	}

	public static implicit operator float(Value val)
	{
		return val.Float();
	}

	public static implicit operator string(Value val)
	{
		return val.String();
	}

	public bool Bool()
	{
		if (type == Type.Int)
		{
			return int_val != 0;
		}
		if (type == Type.Float)
		{
			return float_val != 0f;
		}
		return obj_val != null;
	}

	public int Int(int def_val = 0)
	{
		if (type == Type.Int)
		{
			return int_val;
		}
		if (type == Type.Float)
		{
			int num = (int)float_val;
			if ((float)num == float_val)
			{
				return num;
			}
			return def_val;
		}
		return def_val;
	}

	public float Float(float def_val = 0f)
	{
		if (type == Type.Int)
		{
			return int_val;
		}
		if (type == Type.Float)
		{
			return float_val;
		}
		return def_val;
	}

	public string String(string def_val = null)
	{
		if (type == Type.String)
		{
			return (string)obj_val;
		}
		return def_val;
	}

	public object Object(bool allow_boxing = true)
	{
		if (allow_boxing && obj_val == null)
		{
			if (type == Type.Int)
			{
				obj_val = int_val;
				inc_boxed();
			}
			else if (type == Type.Float)
			{
				obj_val = float_val;
				inc_boxed();
			}
		}
		return obj_val;
	}

	public T Get<T>() where T : class
	{
		return obj_val as T;
	}

	public static void inc_boxed()
	{
		boxed++;
	}

	public bool Match(Value v)
	{
		switch (type)
		{
		case Type.Unknown:
		case Type.Null:
			return !v.is_valid;
		case Type.Int:
			return v.type switch
			{
				Type.Int => int_val == v.int_val, 
				Type.Float => (float)int_val == v.float_val, 
				Type.String => int_val.ToString() == (string)v.obj_val, 
				_ => false, 
			};
		case Type.Float:
			return v.type switch
			{
				Type.Int => float_val == (float)v.int_val, 
				Type.Float => float_val == v.float_val, 
				Type.String => float_val.ToString() == (string)v.obj_val, 
				_ => false, 
			};
		case Type.String:
		{
			string text = (string)obj_val;
			return v.type switch
			{
				Type.Int => text == v.int_val.ToString(), 
				Type.Float => text == v.float_val.ToString(), 
				Type.String => text == (string)v.obj_val, 
				_ => false, 
			};
		}
		case Type.Object:
			if (v.type != Type.Object)
			{
				return false;
			}
			return obj_val == v.obj_val;
		default:
			return false;
		}
	}

	public static bool operator ==(Value v1, Value v2)
	{
		switch (v1.type)
		{
		case Type.Null:
			return v2.type == Type.Null;
		case Type.Unknown:
			return v2.type == Type.Unknown;
		case Type.Int:
			if (v2.type == Type.Int)
			{
				return v1.int_val == v2.int_val;
			}
			if (v2.type == Type.Float)
			{
				return (float)v1.int_val == v2.float_val;
			}
			return false;
		case Type.Float:
			if (v2.type == Type.Int)
			{
				return v1.float_val == (float)v2.int_val;
			}
			if (v2.type == Type.Float)
			{
				return v1.float_val == v2.float_val;
			}
			return false;
		case Type.String:
			if (v2.type != Type.String)
			{
				return false;
			}
			return (string)v1.obj_val == (string)v2.obj_val;
		case Type.Object:
			if (v2.type != Type.Object)
			{
				return false;
			}
			return v1.obj_val == v2.obj_val;
		default:
			throw new NotSupportedException();
		}
	}

	public static bool operator !=(Value v1, Value v2)
	{
		return !(v1 == v2);
	}

	public bool Equals(Value other)
	{
		return this == other;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is Value))
		{
			return false;
		}
		return this == (Value)obj;
	}

	public override int GetHashCode()
	{
		switch (type)
		{
		case Type.Unknown:
		case Type.Null:
			return 0;
		case Type.Int:
			return int_val.GetHashCode();
		case Type.Float:
			return float_val.GetHashCode();
		case Type.String:
			return ((string)obj_val).GetHashCode();
		case Type.Object:
			return obj_val.GetHashCode();
		default:
			throw new NotSupportedException();
		}
	}

	public override string ToString()
	{
		switch (type)
		{
		case Type.Null:
			if (obj_val is string text)
			{
				return "error: " + text;
			}
			return "null";
		case Type.Int:
			return int_val.ToString();
		case Type.Float:
			return DT.FloatToStr(float_val) + "f";
		case Type.String:
			return "'" + (string)obj_val + "'";
		case Type.Object:
			return obj_val.GetType().Name + "(" + Logic.Object.ToString(obj_val) + ")";
		case Type.Unknown:
			return "unknown";
		default:
			return "???";
		}
	}
}
