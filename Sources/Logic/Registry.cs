using System;
using System.Collections.Generic;

namespace Logic;

public class Defs : IVars
{
	public class Registry : IVars
	{
		public string dt_type;

		public Def base_def;

		public Dictionary<string, Def> defs = new Dictionary<string, Def>();

		public Registry(string dt_type)
		{
			this.dt_type = dt_type;
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			if (!(key == "base"))
			{
				if (key == "all")
				{
					return new Value(defs);
				}
				if (defs.TryGetValue(key, out var value))
				{
					return value;
				}
				return Value.Unknown;
			}
			return base_def;
		}
	}

	public Game game;

	public Dictionary<Type, Registry> registries = new Dictionary<Type, Registry>();

	public Defs(Game game)
	{
		this.game = game;
		RegisterDefTypes();
	}

	public Registry Get(Type def_type)
	{
		if (!typeof(Def).IsAssignableFrom(def_type))
		{
			game.Error(Object.TypeToStr(def_type) + " is not a Def");
			game.NotifyListeners("defs_malformed");
			return null;
		}
		Registry value = null;
		if (!registries.TryGetValue(def_type, out value))
		{
			return null;
		}
		return value;
	}

	public Def Get(Type def_type, string id, bool create_if_not_found)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		Registry registry = Get(def_type);
		if (registry == null)
		{
			if (create_if_not_found)
			{
				game.Error("Trying to get def of unregistered type: " + Object.TypeToStr(def_type));
				game.NotifyListeners("defs_malformed");
			}
			return null;
		}
		return Get(def_type, registry, id, create_if_not_found);
	}

	public T Find<T>(string id) where T : Def
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return Get(typeof(T), id, create_if_not_found: false) as T;
	}

	public T Get<T>(string id) where T : Def
	{
		return Get(typeof(T), id, create_if_not_found: true) as T;
	}

	public T GetBase<T>() where T : Def
	{
		Type typeFromHandle = typeof(T);
		Registry registry = Get(typeFromHandle);
		if (registry == null)
		{
			return null;
		}
		return GetBase(typeFromHandle, registry) as T;
	}

	public List<T> GetDefs<T>() where T : Def
	{
		Registry registry = Get(typeof(T));
		if (registry == null || registry.defs.Count == 0)
		{
			return null;
		}
		List<T> list = new List<T>(registry.defs.Count);
		foreach (KeyValuePair<string, Def> def in registry.defs)
		{
			T val = (T)def.Value;
			if (val != null && val.id != null)
			{
				list.Add(val);
			}
		}
		return list;
	}

	public T GetRandom<T>() where T : Def
	{
		Registry registry = Get(typeof(T));
		if (registry == null || registry.defs.Count == 0)
		{
			return null;
		}
		int num = game.Random(0, registry.defs.Count);
		foreach (KeyValuePair<string, Def> def in registry.defs)
		{
			if (num > 0)
			{
				num--;
				continue;
			}
			return (T)def.Value;
		}
		return null;
	}

	public void Load()
	{
		foreach (KeyValuePair<Type, Registry> registry in registries)
		{
			foreach (KeyValuePair<string, Def> def in registry.Value.defs)
			{
				def.Value.Unload(game);
			}
		}
		foreach (KeyValuePair<Type, Registry> registry2 in registries)
		{
			Type key = registry2.Key;
			Registry value = registry2.Value;
			Load(key, value);
		}
		Validate();
	}

	public void Validate()
	{
		foreach (KeyValuePair<Type, Registry> registry in registries)
		{
			Type key = registry.Key;
			Registry value = registry.Value;
			Validate(key, value);
		}
	}

	private Def Create(Type def_type)
	{
		Def def = null;
		try
		{
			return (Def)Activator.CreateInstance(def_type);
		}
		catch (Exception ex)
		{
			game.Error("Error creating new " + Object.TypeToStr(def_type) + ": " + ex.ToString());
			game.NotifyListeners("defs_malformed");
			return null;
		}
	}

	private Def Get(Type def_type, Registry reg, string id, bool create = true, bool mark_as_used = true)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		Def value = null;
		if (reg.base_def != null && id == reg.base_def.id)
		{
			value = reg.base_def;
		}
		else if (!reg.defs.TryGetValue(id, out value))
		{
			if (!create)
			{
				return null;
			}
			value = Create(def_type);
			reg.defs.Add(id, value);
		}
		if (mark_as_used)
		{
			value.used = true;
		}
		return value;
	}

	private Def GetBase(Type def_type, Registry reg, bool create = true, bool mark_as_used = true)
	{
		if (reg.base_def == null)
		{
			if (!create)
			{
				return null;
			}
			reg.base_def = Create(def_type);
			if (reg.base_def == null)
			{
				return null;
			}
		}
		if (mark_as_used)
		{
			reg.base_def.used = true;
		}
		return reg.base_def;
	}

	private void Load(Type def_type, Registry reg)
	{
		DT.Def def = game.dt.FindDef(reg.dt_type);
		if (def == null)
		{
			game.Error("Base def not found: " + reg.dt_type);
			game.NotifyListeners("defs_malformed");
			return;
		}
		Def def2 = GetBase(def_type, reg, create: true, mark_as_used: false);
		def2.dt_def = def;
		def2.dt_def.def = def2;
		try
		{
			def2.ResolveObjType(game);
			def2.Load(game);
		}
		catch (Exception ex)
		{
			game.Error("Exception while loading base def '" + def2.id + "': " + ex.ToString());
			game.NotifyListeners("defs_malformed");
		}
		def2.loaded = true;
		if (def.defs == null)
		{
			return;
		}
		for (int i = 0; i < def.defs.Count; i++)
		{
			DT.Def def3 = def.defs[i];
			Def def4 = Get(def_type, reg, def3.path, create: true, mark_as_used: false);
			def4.dt_def = def3;
			def4.dt_def.def = def4;
			try
			{
				def4.ResolveObjType(game);
				def4.Load(game);
			}
			catch (Exception ex2)
			{
				game.Error("Exception while loading def '" + def4.id + "': " + ex2.ToString());
				game.NotifyListeners("defs_malformed");
			}
			def4.loaded = true;
		}
	}

	private void Validate(Type def_type, Registry reg)
	{
		foreach (KeyValuePair<string, Def> def in reg.defs)
		{
			Def value = def.Value;
			if (value == null || (value.used && value.dt_def == null))
			{
				Game.Log(reg.dt_type + " def not found: " + def.Key, Game.LogType.Error);
			}
			try
			{
				value.Validate(game);
			}
			catch (Exception arg)
			{
				Game.Log($"Error validating def '{value.id}': {arg}", Game.LogType.Error);
				game.NotifyListeners("defs_malformed");
			}
		}
		if (reg.base_def != null)
		{
			try
			{
				reg.base_def.Validate(game);
			}
			catch (Exception arg2)
			{
				Game.Log($"Error validating def '{reg.base_def.id}': {arg2}", Game.LogType.Error);
				game.NotifyListeners("defs_malformed");
			}
		}
	}

	private void Register(Type def_type, string dt_type = null)
	{
		if (def_type == null)
		{
			game.Error("Cannot register def type null");
			game.NotifyListeners("defs_malformed");
			return;
		}
		if (!typeof(Def).IsAssignableFrom(def_type))
		{
			game.Error(Object.TypeToStr(def_type) + " is not derived from Logic.Def");
			game.NotifyListeners("defs_malformed");
			return;
		}
		if (def_type.Name != "Def")
		{
			game.Warning(Object.TypeToStr(def_type) + " is not named 'Def'");
		}
		if (dt_type == null)
		{
			Type declaringType = def_type.DeclaringType;
			if (declaringType == null)
			{
				game.Error(Object.TypeToStr(def_type) + " is not a nested class");
				game.NotifyListeners("defs_malformed");
				return;
			}
			if (declaringType.DeclaringType != null)
			{
				game.Warning(Object.TypeToStr(def_type) + ": object type is nested class");
			}
			dt_type = declaringType.Name;
		}
		if (registries.ContainsKey(def_type))
		{
			game.Error(Object.TypeToStr(def_type) + " is already registered");
			game.NotifyListeners("defs_malformed");
		}
		else
		{
			registries.Add(def_type, new Registry(dt_type));
		}
	}

	private void RegisterDefTypes()
	{
		Type typeFromHandle = typeof(Def);
		Type[] types = typeFromHandle.Assembly.GetTypes();
		foreach (Type type in types)
		{
			if (!(type == typeFromHandle) && typeFromHandle.IsAssignableFrom(type))
			{
				Register(type);
			}
		}
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "all")
		{
			return new Value(registries);
		}
		Type type = Type.GetType("Logic." + key + "+Def");
		if (type == null)
		{
			return Value.Unknown;
		}
		if (registries.TryGetValue(type, out var value))
		{
			return new Value(value);
		}
		return Value.Unknown;
	}
}
