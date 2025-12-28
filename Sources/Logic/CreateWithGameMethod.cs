using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logic;

public static class Reflection
{
	public class VarInfo
	{
		public enum VarType
		{
			Property,
			Method,
			Field,
			Func,
			Unknown
		}

		public delegate Value GetFunc(object obj);

		public Type obj_type;

		public string var_name;

		public VarType var_type = VarType.Unknown;

		public PropertyInfo prop;

		public MethodInfo method;

		public FieldInfo field;

		public GetFunc func;

		private static Type[] NoParamTypes = new Type[0];

		private static object[] NoParamValues = new object[0];

		public bool is_valid => var_type != VarType.Unknown;

		public VarInfo(Type type, string key)
		{
			obj_type = type;
			var_name = key;
			if (key.EndsWith("()", StringComparison.Ordinal))
			{
				method = type.GetMethod(key.Substring(0, key.Length - 2), NoParamTypes);
				if (method != null)
				{
					var_type = VarType.Method;
					ResolveMethod(method);
				}
				return;
			}
			prop = type.GetProperty(key);
			if (prop != null)
			{
				var_type = VarType.Property;
				return;
			}
			method = type.GetMethod("Get" + key.Substring(0, 1).ToUpperInvariant() + key.Substring(1), NoParamTypes);
			if (method != null)
			{
				var_type = VarType.Method;
				ResolveMethod(method);
				return;
			}
			field = type.GetField(key);
			if (field != null)
			{
				var_type = VarType.Field;
			}
		}

		private static Func<object, TRes> CreateGetFuncT<TObj, TRes>(MethodInfo method) where TObj : class
		{
			Func<TObj, TRes> d = Delegate.CreateDelegate(typeof(Func<TObj, TRes>), method) as Func<TObj, TRes>;
			return (object obj) => d((TObj)obj);
		}

		private static Func<object, TRes> CreateGetFunc<TRes>(Type obj_type, MethodInfo method)
		{
			return typeof(VarInfo).GetMethod("CreateGetFuncT", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(obj_type, typeof(TRes)).Invoke(null, new object[1] { method }) as Func<object, TRes>;
		}

		private static Func<object, object> CreateGetFunc(Type obj_type, MethodInfo method)
		{
			return typeof(VarInfo).GetMethod("CreateGetFuncT", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(obj_type, typeof(object)).Invoke(null, new object[1] { method }) as Func<object, object>;
		}

		private void ResolveMethod(MethodInfo method)
		{
			try
			{
				if (method.ReturnType == typeof(bool))
				{
					Func<object, bool> f = CreateGetFunc<bool>(obj_type, method);
					func = (object arg) => new Value(f(arg));
				}
				else if (method.ReturnType == typeof(int))
				{
					Func<object, int> f2 = CreateGetFunc<int>(obj_type, method);
					func = (object arg) => new Value(f2(arg));
				}
				else if (method.ReturnType == typeof(float))
				{
					Func<object, float> f3 = CreateGetFunc<float>(obj_type, method);
					func = (object arg) => new Value(f3(arg));
				}
				else if (method.ReturnType == typeof(Value))
				{
					Func<object, Value> f4 = CreateGetFunc<Value>(obj_type, method);
					func = (object arg) => f4(arg);
				}
				else
				{
					Func<object, object> f5 = CreateGetFunc(obj_type, method);
					func = (object arg) => new Value(f5(arg));
				}
				var_type = VarType.Func;
			}
			catch
			{
				Game.Log("Could not create delegate for " + obj_type.FullName + "." + method.Name + "()", Game.LogType.Warning);
			}
		}

		public Value Extract(object obj)
		{
			object val = null;
			try
			{
				switch (var_type)
				{
				case VarType.Property:
					val = prop.GetValue(obj, null);
					break;
				case VarType.Method:
					val = method.Invoke(obj, NoParamValues);
					break;
				case VarType.Field:
					val = field.GetValue(obj);
					break;
				case VarType.Func:
					return func(obj);
				case VarType.Unknown:
					return Value.Unknown;
				}
			}
			catch (Exception ex)
			{
				Game.Log("Error extracting '" + var_name + "' from " + Object.ToString(obj) + "'): " + ex.ToString(), Game.LogType.Error);
				return Value.Null;
			}
			return new Value(val);
		}

		public override string ToString()
		{
			return obj_type.FullName + "." + var_name + "(" + var_type.ToString() + ")";
		}
	}

	public class TypeInfo : IVars
	{
		public class CreateMethodInfo
		{
			public Type delegate_type;

			public Type return_type;

			public Type[] parameter_types;

			public MethodInfo method;

			public Delegate func;
		}

		public delegate object CreateMethod();

		public delegate object CreateFromMultiplayeMethod(Multiplayer multiplayer);

		public delegate object CreateWithGameMethod(Game game);

		public delegate void CreateVisuals(Object obj);

		private class DelegateInfo
		{
			public Type type;

			public MethodInfo invoke;

			public Type return_type;

			public ParameterInfo[] parameters;
		}

		public Type type;

		public string name;

		public string full_name;

		public TypeInfo base_rtti;

		public TypeInfo parent_rtti;

		public TypeInfo ref_data_rtti;

		public TypeInfo full_data_rtti;

		public Serialization.ObjectTypeInfo ti;

		public Serialization.Event event_attr;

		public Serialization.State state_attr;

		public Serialization.Substate substate_attr;

		public CreateMethod create;

		public CreateFromMultiplayeMethod create_from_multiplayer;

		public CreateWithGameMethod create_with_game;

		public List<CreateMethodInfo> create_methods = new List<CreateMethodInfo>();

		public Dictionary<string, VarInfo> vars = new Dictionary<string, VarInfo>();

		public VarInfo stats;

		public VarInfo def;

		public VarInfo field;

		public string update_section_name;

		public static CreateVisuals no_create_visuals = delegate
		{
		};

		public CreateVisuals create_visuals;

		public bool IsKindOf(TypeInfo base_rtti)
		{
			for (TypeInfo typeInfo = this; typeInfo != null; typeInfo = typeInfo.base_rtti)
			{
				if (typeInfo == base_rtti)
				{
					return true;
				}
			}
			return false;
		}

		public TypeInfo(Type type, string full_name)
		{
			this.type = type;
			name = type.Name;
			this.full_name = full_name;
			update_section_name = full_name + ".OnUpdate";
		}

		public void Init()
		{
			Type baseType = type.BaseType;
			if (baseType != null)
			{
				base_rtti = GetTypeInfo(baseType);
			}
			Type declaringType = type.DeclaringType;
			if (declaringType != null)
			{
				parent_rtti = GetTypeInfo(declaringType);
			}
			InitCreateMethods();
			InitVars();
			InitData();
		}

		private void InitCreateMethods()
		{
			create = CreateDelegate(typeof(CreateMethod), type, "Create", type) as CreateMethod;
			create_from_multiplayer = CreateDelegate(typeof(CreateFromMultiplayeMethod), type, "Create", typeof(object), typeof(Multiplayer)) as CreateFromMultiplayeMethod;
			create_with_game = CreateDelegate(typeof(CreateWithGameMethod), type, "Create", typeof(object), typeof(Game)) as CreateWithGameMethod;
			List<DelegateInfo> delegates = new List<DelegateInfo>();
			ExtractDelegates(type, delegates);
			ExtractDelegates(typeof(TypeInfo), delegates);
			MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.Name != "Create")
				{
					continue;
				}
				DelegateInfo delegateInfo = FindMatchingDelegate(delegates, methodInfo);
				if (delegateInfo == null)
				{
					if (full_name.StartsWith("Logic.", StringComparison.Ordinal))
					{
						Game.Log($"{methodInfo.ToString()}({type}) does not have matching delegate defined, ignored", Game.LogType.Warning);
					}
					continue;
				}
				Delegate func;
				try
				{
					func = Delegate.CreateDelegate(delegateInfo.type, methodInfo);
				}
				catch (Exception ex)
				{
					Game.Log("Error creating delegate for " + methodInfo.ToString() + ": " + ex.ToString(), Game.LogType.Error);
					continue;
				}
				Type[] array = new Type[delegateInfo.parameters.Length];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = delegateInfo.parameters[j].ParameterType;
				}
				CreateMethodInfo item = new CreateMethodInfo
				{
					delegate_type = delegateInfo.type,
					return_type = methodInfo.ReturnType,
					parameter_types = array,
					method = methodInfo,
					func = func
				};
				create_methods.Add(item);
			}
		}

		private void ExtractDelegates(Type type, List<DelegateInfo> delegates)
		{
			while (type != null)
			{
				Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public);
				Type typeFromHandle = typeof(Delegate);
				foreach (Type type2 in nestedTypes)
				{
					if (typeFromHandle.IsAssignableFrom(type2))
					{
						MethodInfo method = type2.GetMethod("Invoke");
						Type returnType = method.ReturnType;
						ParameterInfo[] parameters = method.GetParameters();
						delegates.Add(new DelegateInfo
						{
							type = type2,
							invoke = method,
							return_type = returnType,
							parameters = parameters
						});
					}
				}
				type = type.BaseType;
			}
		}

		private DelegateInfo FindMatchingDelegate(List<DelegateInfo> delegates, MethodInfo method)
		{
			_ = method.ReturnType;
			ParameterInfo[] parameters = method.GetParameters();
			for (int i = 0; i < delegates.Count; i++)
			{
				DelegateInfo delegateInfo = delegates[i];
				if (!delegateInfo.return_type.IsAssignableFrom(method.ReturnType) || delegateInfo.parameters.Length != parameters.Length)
				{
					continue;
				}
				bool flag = true;
				for (int j = 0; j < parameters.Length; j++)
				{
					ParameterInfo obj = delegateInfo.parameters[j];
					ParameterInfo parameterInfo = parameters[j];
					if (!obj.ParameterType.IsAssignableFrom(parameterInfo.ParameterType))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					return delegateInfo;
				}
			}
			return null;
		}

		private void InitVars()
		{
			stats = GetVarInfo("stats");
			def = GetVarInfo("def");
			field = GetVarInfo("field");
		}

		public CreateMethodInfo FindCreateMethod(Type return_type, params Type[] parameter_types)
		{
			for (int i = 0; i < create_methods.Count; i++)
			{
				CreateMethodInfo createMethodInfo = create_methods[i];
				if (createMethodInfo.parameter_types.Length != parameter_types.Length)
				{
					continue;
				}
				bool flag = true;
				for (int j = 0; j < parameter_types.Length; j++)
				{
					Type obj = createMethodInfo.parameter_types[j];
					Type c = parameter_types[j];
					if (!obj.IsAssignableFrom(c))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					return createMethodInfo;
				}
			}
			return null;
		}

		public VarInfo GetVarInfo(string key)
		{
			if (vars.TryGetValue(key, out var value))
			{
				return value;
			}
			value = new VarInfo(type, key);
			vars.Add(key, value);
			return value;
		}

		private void InitData()
		{
			Type nestedType = type.GetNestedType("RefData");
			if (nestedType != null && typeof(Data).IsAssignableFrom(nestedType))
			{
				ref_data_rtti = GetTypeInfo(nestedType);
			}
			Type nestedType2 = type.GetNestedType("FullData");
			if (nestedType2 != null && typeof(Data).IsAssignableFrom(nestedType2))
			{
				full_data_rtti = GetTypeInfo(nestedType2);
			}
			if (parent_rtti != null && typeof(Data).IsAssignableFrom(type) && create == null && !type.IsAbstract)
			{
				Game.Log(full_name + " has no 'public static " + name + " Create()' metod", Game.LogType.Warning);
			}
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			return GetVarInfo(key)?.Extract(null) ?? Value.Unknown;
		}

		public override string ToString()
		{
			return "TypeInfo(" + full_name + ")";
		}

		public string Dump(string prefix, string new_line)
		{
			string text = prefix + ToString();
			if (create_visuals != null)
			{
				text = text + new_line + "  create_visuals: " + ((create_visuals == no_create_visuals) ? "Unknown" : "Func");
			}
			foreach (KeyValuePair<string, VarInfo> var in vars)
			{
				VarInfo value = var.Value;
				text = text + new_line + "  " + value.var_name + ": " + value.var_type;
			}
			return text;
		}

		public string Dump()
		{
			return Dump("", "\n");
		}
	}

	public static Dictionary<string, TypeInfo> types = new Dictionary<string, TypeInfo>();

	public static Func<string, Type> fn_resolve_type = null;

	public static string GetTypeKey(Type type)
	{
		if (type == null)
		{
			return null;
		}
		string text = type.FullName;
		if (!text.StartsWith("Logic.", StringComparison.Ordinal))
		{
			text = "Visuals_" + text;
		}
		return text;
	}

	public static TypeInfo GetTypeInfo(Type type, string key = null)
	{
		if (type == null)
		{
			return null;
		}
		if (key == null)
		{
			key = GetTypeKey(type);
		}
		if (types.TryGetValue(key, out var value))
		{
			return value;
		}
		value = new TypeInfo(type, key);
		types.Add(key, value);
		value.Init();
		return value;
	}

	public static TypeInfo GetTypeInfo(string type_name)
	{
		if (types.TryGetValue(type_name, out var value))
		{
			return value;
		}
		string text = type_name;
		Type type = null;
		if (type_name.IndexOf('.') > 0)
		{
			type = Type.GetType(type_name);
		}
		else
		{
			if (type_name.StartsWith("Visuals_", StringComparison.Ordinal))
			{
				type_name = type_name.Substring(8);
			}
			else
			{
				if (type_name.StartsWith("Logic_", StringComparison.Ordinal))
				{
					type_name = type_name.Substring(6);
				}
				type = Type.GetType("Logic." + type_name);
			}
			if (type == null && fn_resolve_type != null)
			{
				type = fn_resolve_type(type_name);
			}
		}
		if (type == null)
		{
			types.Add(text, null);
			return null;
		}
		string typeKey = GetTypeKey(type);
		value = GetTypeInfo(type, typeKey);
		if (typeKey != text)
		{
			types.Add(text, value);
		}
		return value;
	}

	public static T Create<T>(TypeInfo rtti) where T : class
	{
		if (rtti == null)
		{
			return null;
		}
		if (rtti.create != null)
		{
			return rtti.create() as T;
		}
		return CreateObjectViaReflection<T>(rtti.type, Array.Empty<object>());
	}

	public static T Create<T>(Type type) where T : class
	{
		return Create<T>(GetTypeInfo(type));
	}

	public static T Create<T>(TypeInfo rtti, Multiplayer multiplayer) where T : class
	{
		if (rtti == null)
		{
			return null;
		}
		if (rtti.create_from_multiplayer != null)
		{
			return rtti.create_from_multiplayer(multiplayer) as T;
		}
		return CreateObjectViaReflection<T>(rtti.type, new object[1] { multiplayer });
	}

	public static T Create<T>(Type type, Multiplayer multiplayer) where T : class
	{
		return Create<T>(GetTypeInfo(type), multiplayer);
	}

	public static T Create<T>(TypeInfo rtti, Game game) where T : class
	{
		if (rtti == null)
		{
			return null;
		}
		if (rtti.create_with_game != null)
		{
			return rtti.create_with_game(game) as T;
		}
		return CreateObjectViaReflection<T>(rtti.type, new object[1] { game });
	}

	public static T Create<T>(Type type, Game game) where T : class
	{
		return Create<T>(GetTypeInfo(type), game);
	}

	public static T CreateObjectViaReflection<T>(Type derived_type, params object[] args)
	{
		Game.Log("Creating " + derived_type.Name + " using reflection!", Game.LogType.Warning);
		Type typeFromHandle = typeof(T);
		if (derived_type == null || !typeFromHandle.IsAssignableFrom(derived_type))
		{
			Game.Log("Attempting to create " + typeFromHandle.Name + " of non-derived type " + derived_type.Name, Game.LogType.Error);
			return default(T);
		}
		try
		{
			return (T)Activator.CreateInstance(derived_type, args);
		}
		catch (Exception ex)
		{
			Game.Log("Error creating " + typeFromHandle.Name + " of type " + derived_type.Name + ": " + ex.ToString(), Game.LogType.Error);
			return default(T);
		}
	}

	public static VarInfo GetVarInfo(Type type, string key)
	{
		return GetTypeInfo(type).GetVarInfo(key);
	}

	public static Value GetVar(object obj, string key)
	{
		if (obj == null)
		{
			return Value.Null;
		}
		return GetVarInfo(obj.GetType(), key).Extract(obj);
	}

	public static bool Call(object instance, string method_name, object[] args, out Value result)
	{
		if (instance is TypeInfo typeInfo)
		{
			return Call(typeInfo.type, method_name, args, out result);
		}
		result = Value.Unknown;
		if (instance == null)
		{
			return false;
		}
		Type type = instance.GetType();
		if (type == null)
		{
			return false;
		}
		object[] actual_args;
		MethodInfo method = FindMatchingmethod(type.GetMethods(BindingFlags.Instance | BindingFlags.Public), method_name, args, out actual_args);
		return Call(instance, method, actual_args, out result);
	}

	public static bool Call(Type class_type, string method_name, object[] args, out Value result)
	{
		result = Value.Unknown;
		if (class_type == null)
		{
			return false;
		}
		object[] actual_args;
		MethodInfo method = FindMatchingmethod(class_type.GetMethods(BindingFlags.Static | BindingFlags.Public), method_name, args, out actual_args);
		return Call(null, method, actual_args, out result);
	}

	public static bool Call(object instance, MethodInfo method, object[] args, out Value result)
	{
		result = Value.Unknown;
		if (method == null)
		{
			return false;
		}
		try
		{
			object val = method.Invoke(instance, args);
			if (method.ReturnType != typeof(void))
			{
				result = new Value(val);
			}
			return true;
		}
		catch
		{
		}
		return false;
	}

	private static MethodInfo FindMatchingmethod(MethodInfo[] methods, string name, object[] args, out object[] actual_args)
	{
		actual_args = null;
		if (methods == null)
		{
			return null;
		}
		foreach (MethodInfo methodInfo in methods)
		{
			if (!(methodInfo.Name != name))
			{
				actual_args = MatchArgs(methodInfo, args);
				if (actual_args != null)
				{
					return methodInfo;
				}
			}
		}
		return null;
	}

	public static object[] MatchArgs(MethodInfo method, object[] args)
	{
		ParameterInfo[] parameters = method.GetParameters();
		if (args.Length > parameters.Length)
		{
			return null;
		}
		if (args.Length < parameters.Length)
		{
			for (int i = args.Length; i < parameters.Length; i++)
			{
				ParameterInfo parameterInfo = parameters[i];
				if (!parameterInfo.HasDefaultValue)
				{
					return null;
				}
				if (parameterInfo.IsOut)
				{
					return null;
				}
			}
		}
		object[] array = null;
		for (int j = 0; j < parameters.Length; j++)
		{
			ParameterInfo parameterInfo2 = parameters[j];
			if (parameterInfo2.IsOut)
			{
				return null;
			}
			int num;
			object actual_arg;
			if (j < args.Length)
			{
				object arg = args[j];
				num = MatchArg(parameterInfo2, arg, out actual_arg);
			}
			else
			{
				actual_arg = parameterInfo2.DefaultValue;
				num = 2;
			}
			if (num == 0)
			{
				return null;
			}
			if (num > 1 && array == null)
			{
				array = new object[parameters.Length];
				for (int k = 0; k < j; k++)
				{
					array[k] = args[k];
				}
			}
			if (array != null)
			{
				array[j] = actual_arg;
			}
		}
		if (array != null)
		{
			return array;
		}
		return args;
	}

	private static int MatchArg(ParameterInfo method_arg, object arg, out object actual_arg)
	{
		actual_arg = arg;
		if (arg == null)
		{
			if (method_arg.ParameterType.IsClass)
			{
				return 1;
			}
			return 0;
		}
		Type type = arg.GetType();
		if (method_arg.ParameterType.IsAssignableFrom(type))
		{
			return 1;
		}
		int num = 0;
		bool flag = false;
		if (arg is int num2)
		{
			flag = true;
			num = num2;
		}
		else if (arg is float num3)
		{
			num = (int)num3;
			flag = (float)num == num3;
		}
		if (method_arg.ParameterType == typeof(bool) && flag)
		{
			actual_arg = num != 0;
			return 2;
		}
		if (method_arg.ParameterType == typeof(float) && flag)
		{
			actual_arg = (float)num;
			return 2;
		}
		if (method_arg.ParameterType == typeof(Value))
		{
			actual_arg = new Value(arg);
			return 2;
		}
		if (method_arg.ParameterType.IsEnum)
		{
			if (arg is string value)
			{
				try
				{
					actual_arg = Enum.Parse(method_arg.ParameterType, value);
					return 2;
				}
				catch
				{
				}
				return 0;
			}
			if (flag)
			{
				try
				{
					actual_arg = Enum.ToObject(method_arg.ParameterType, num);
					if (!method_arg.ParameterType.IsDefined(typeof(FlagsAttribute), inherit: false) && !Enum.IsDefined(method_arg.ParameterType, actual_arg))
					{
						return 0;
					}
					return 2;
				}
				catch
				{
				}
				return 0;
			}
			return 0;
		}
		return 0;
	}

	public static Delegate CreateDelegate(Type delegate_type, Type class_type, string method_name, Type return_type, params Type[] param_types)
	{
		if (class_type == null)
		{
			return null;
		}
		MethodInfo method = class_type.GetMethod(method_name, param_types);
		if (method == null || !method.IsStatic || !method.IsPublic || !return_type.IsAssignableFrom(method.ReturnType))
		{
			return null;
		}
		try
		{
			Delegate obj = Delegate.CreateDelegate(delegate_type, method);
			if ((object)obj != null)
			{
				return obj;
			}
		}
		catch (Exception ex)
		{
			Game.Log("Error creating delegate for " + class_type.FullName + "." + method_name + ": " + ex.ToString(), Game.LogType.Error);
		}
		return null;
	}

	public static string Dump()
	{
		string text = "";
		foreach (KeyValuePair<string, TypeInfo> type in types)
		{
			TypeInfo value = type.Value;
			if (text != "")
			{
				text += "\n";
			}
			text += value.Dump();
		}
		return text;
	}
}
