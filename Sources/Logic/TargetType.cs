using System;
using System.Collections.Generic;

namespace Logic;

public class TargetType : GameRules.IListener
{
	public delegate object ResolveFunc(object target);

	public string name;

	public Reflection.TypeInfo obj_type;

	public ResolveFunc validate_func;

	public List<string> trigger_messages;

	public static Dictionary<string, TargetType> all = new Dictionary<string, TargetType>();

	public static TargetType Add(string target_type, Type obj_type, ResolveFunc validate_func = null, params string[] trigger_messages)
	{
		TargetType targetType = new TargetType
		{
			name = target_type,
			obj_type = Reflection.GetTypeInfo(obj_type),
			validate_func = validate_func,
			trigger_messages = new List<string>(trigger_messages)
		};
		all.Add(target_type, targetType);
		return targetType;
	}

	public static TargetType Find(string target_type)
	{
		if (!all.TryGetValue(target_type, out var value))
		{
			return null;
		}
		return value;
	}

	public static object Resolve(string target_type, object obj)
	{
		return Find(target_type)?.Resolve(obj);
	}

	public object Resolve(object obj)
	{
		if (obj == null)
		{
			return null;
		}
		if (validate_func == null)
		{
			if (!Reflection.GetTypeInfo(obj.GetType()).IsKindOf(obj_type))
			{
				return null;
			}
			return obj;
		}
		return validate_func(obj);
	}

	public void OnTrigger(Trigger trigger, GameRule parent_rule = null, RuleTimer parent_timer = null)
	{
		if (Resolve(parent_rule.target_obj) != parent_rule.target_obj)
		{
			parent_rule.Deactivate(trigger);
		}
	}

	public override string ToString()
	{
		return name;
	}
}
