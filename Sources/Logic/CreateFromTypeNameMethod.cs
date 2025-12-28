using System;
using System.Collections;

namespace Logic;

public abstract class Data
{
	public delegate Data CreateFromObjectMethod(object obj);

	public delegate Data CreateFromTypeNameMethod(string type_name);

	private Reflection.TypeInfo _rtti;

	public Reflection.TypeInfo rtti
	{
		get
		{
			if (_rtti == null)
			{
				_rtti = Reflection.GetTypeInfo(GetType());
			}
			return _rtti;
		}
		set
		{
			_rtti = value;
		}
	}

	public Data()
	{
	}

	public override string ToString()
	{
		return rtti.full_name;
	}

	public abstract bool InitFrom(object obj);

	public abstract void Save(Serialization.IWriter ser);

	public abstract void Load(Serialization.IReader ser);

	public abstract object GetObject(Game game);

	public abstract bool ApplyTo(object obj, Game game);

	public virtual Value GetValue(Game game)
	{
		object obj = GetObject(game);
		if (!ApplyTo(obj, game))
		{
			Game.Log("Could not apply " + ToString() + " to " + (obj?.ToString() ?? "null"), Game.LogType.Error);
			return Value.Unknown;
		}
		return new Value(obj);
	}

	public static T RestoreObject<T>(Data data, Game game) where T : class
	{
		if (data == null)
		{
			return null;
		}
		object obj = data.GetObject(game);
		T val = obj as T;
		if (val == null && obj != null)
		{
			Game.Log("Could not cast the object of " + data.ToString() + " from " + (obj?.ToString() ?? "null") + " to " + typeof(T).ToString(), Game.LogType.Error);
			return null;
		}
		if (!data.ApplyTo(val, game))
		{
			Game.Log("Could not apply " + data.ToString() + " to " + (val?.ToString() ?? "null"), Game.LogType.Error);
		}
		return val;
	}

	public static bool IsRefSerializable(object obj)
	{
		if (obj == null)
		{
			return true;
		}
		if (!(obj is BaseObject baseObject))
		{
			return false;
		}
		return baseObject.IsRefSerializable();
	}

	public static Data CreateRef(object obj)
	{
		if (obj == null)
		{
			return null;
		}
		if (!(obj is BaseObject baseObject))
		{
			Game.Log("Cannot create reference data for non-BaseObject " + obj.ToString(), Game.LogType.Error);
			return null;
		}
		if (!baseObject.IsRefSerializable())
		{
			Game.Log("Creating data for non-reference serializable object " + obj.ToString(), Game.LogType.Error);
		}
		return baseObject.CreateRefData();
	}

	public static bool IsFullSerializable(object obj)
	{
		if (obj == null)
		{
			return true;
		}
		if (obj is int || obj is float || obj is string)
		{
			return true;
		}
		if (obj is Value value)
		{
			return value.IsFullSerializable();
		}
		if (obj is BaseObject baseObject)
		{
			return baseObject.IsFullSerializable();
		}
		if (obj is IList { Count: var count } list)
		{
			for (int i = 0; i < count; i++)
			{
				object obj2 = list[i];
				if (obj2 is Value value2)
				{
					if (!value2.IsSerializable())
					{
						return false;
					}
				}
				else if (!IsSerializable(obj2))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public static Data CreateFull(object obj)
	{
		if (obj == null)
		{
			return null;
		}
		if (obj is int || obj is float || obj is string || obj is Value)
		{
			return new Value(obj).CreateFullData();
		}
		if (obj is BaseObject baseObject)
		{
			return baseObject.CreateFullData();
		}
		if (obj is IList lst)
		{
			return new Value.ListData(lst);
		}
		Game.Log("Could not create full data for non-full serializable object " + obj.ToString(), Game.LogType.Error);
		return null;
	}

	public static bool IsSerializable(Type type)
	{
		if (type == null || type == typeof(int) || type == typeof(float) || type == typeof(string))
		{
			return true;
		}
		if (type == typeof(Value))
		{
			return true;
		}
		if (typeof(BaseObject).IsAssignableFrom(type))
		{
			return true;
		}
		if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
		{
			Type[] genericTypeArguments = type.GenericTypeArguments;
			if (genericTypeArguments == null || genericTypeArguments.Length != 1)
			{
				return false;
			}
			if (!IsSerializable(genericTypeArguments[0]))
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public static bool IsSerializable(object obj)
	{
		if (obj == null)
		{
			return true;
		}
		if (IsRefSerializable(obj))
		{
			return true;
		}
		if (IsFullSerializable(obj))
		{
			return true;
		}
		return false;
	}

	public static Data Create(object obj)
	{
		if (obj == null)
		{
			return null;
		}
		if (IsRefSerializable(obj))
		{
			return CreateRef(obj);
		}
		return CreateFull(obj);
	}

	public static Data CreateByTypename(string type_name)
	{
		Reflection.TypeInfo typeInfo = Reflection.GetTypeInfo(type_name);
		if (typeInfo == null || typeInfo.create == null || !typeof(Data).IsAssignableFrom(typeInfo.type))
		{
			return null;
		}
		if (!(typeInfo.create() is Data data))
		{
			return null;
		}
		data.rtti = typeInfo;
		return data;
	}
}
