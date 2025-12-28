using System.Collections.Generic;

namespace Logic;

public class ObjRules : Component, IVars
{
	public List<GameRule> rules;

	public int last_urid;

	public ObjRules(Object obj)
		: base(obj)
	{
	}

	public static ObjRules Get(Object obj, bool create)
	{
		if (obj == null)
		{
			return null;
		}
		ObjRules component = obj.GetComponent<ObjRules>();
		if (component != null)
		{
			return component;
		}
		if (!create)
		{
			return null;
		}
		return new ObjRules(obj);
	}

	public static GameRule Find(Object obj, GameRule.Def def)
	{
		return Get(obj, create: false)?.Find(def);
	}

	public GameRule Find(GameRule.Def def)
	{
		if (rules == null)
		{
			return null;
		}
		for (int i = 0; i < rules.Count; i++)
		{
			GameRule gameRule = rules[i];
			if (gameRule.def == def)
			{
				return gameRule;
			}
		}
		return null;
	}

	private int DelInternal(GameRule rule, int idx, bool send_state = true)
	{
		int urid = rule.urid;
		if (send_state)
		{
			obj.SendSubstate<Object.RulesState.DelRuleState>(urid);
		}
		rules.RemoveAt(idx);
		if (rule.def.timeout > 0f)
		{
			Reschedule();
		}
		return urid;
	}

	private int Del(GameRule.Def def, bool send_state = true)
	{
		if (rules == null)
		{
			return -1;
		}
		for (int i = 0; i < rules.Count; i++)
		{
			GameRule gameRule = rules[i];
			if (gameRule.def == def)
			{
				return DelInternal(gameRule, i, send_state);
			}
		}
		return -1;
	}

	public int Del(int urid, bool send_state = true)
	{
		if (rules == null)
		{
			return -1;
		}
		for (int i = 0; i < rules.Count; i++)
		{
			GameRule gameRule = rules[i];
			if (gameRule.urid == urid)
			{
				return DelInternal(gameRule, i, send_state);
			}
		}
		return -1;
	}

	public int Del(GameRule rule, bool send_state = true)
	{
		if (rule == null)
		{
			return -1;
		}
		int result = Del(rule.urid, send_state);
		if (rules.Count == 0)
		{
			Destroy();
		}
		return result;
	}

	public GameRule Find(string name)
	{
		if (rules == null)
		{
			return null;
		}
		for (int i = 0; i < rules.Count; i++)
		{
			GameRule gameRule = rules[i];
			if (gameRule.name == name)
			{
				return gameRule;
			}
		}
		return null;
	}

	public GameRule Find(int urid)
	{
		if (rules == null)
		{
			return null;
		}
		for (int i = 0; i < rules.Count; i++)
		{
			GameRule gameRule = rules[i];
			if (gameRule.urid == urid)
			{
				return gameRule;
			}
		}
		return null;
	}

	public void Add(GameRule rule, bool send_state = true)
	{
		if (rule.urid == 0)
		{
			rule.urid = ++last_urid;
		}
		else if (rule.urid > last_urid)
		{
			last_urid = rule.urid;
		}
		if (Find(rule.urid) != null)
		{
			obj.Error($"Attempting to add existing rule: {rule}");
			return;
		}
		if (Del(rule.def) != -1 && rule.def.timeout > 0f && rule.IsActive())
		{
			Reschedule();
		}
		if (rules == null)
		{
			rules = new List<GameRule>();
		}
		rules.Add(rule);
		if (!rule.IsActive() && rule.def.timeout > 0f)
		{
			Reschedule();
		}
		if (send_state && obj.IsAuthority())
		{
			obj.SendSubstate<Object.RulesState.RuleState>(rule.urid);
		}
	}

	public static void OnActivate(GameRule rule, bool from_state = false)
	{
		Get(rule.target_obj, create: true)?.Add(rule, !from_state);
	}

	public static void OnDeactivate(GameRule rule, bool from_state = false)
	{
		ObjRules objRules = Get(rule.target_obj, create: false);
		if (objRules == null)
		{
			Game.Log($"Non-existing rule stopped: {rule}", Game.LogType.Error);
			return;
		}
		if (objRules.obj.IsValid())
		{
			if (!from_state && !rule.def.instant && objRules.obj.IsAuthority())
			{
				objRules.obj.SendSubstate<Object.RulesState.RuleState>(rule.urid);
			}
			if (rule.def.timeout > 0f)
			{
				objRules.Reschedule();
				return;
			}
		}
		objRules.Del(rule, !from_state);
	}

	public void Reschedule()
	{
		if (obj == null || !obj.IsValid())
		{
			return;
		}
		if (rules == null)
		{
			StopUpdating();
			return;
		}
		Time time = Time.Zero;
		for (int i = 0; i < rules.Count; i++)
		{
			GameRule gameRule = rules[i];
			if (!(gameRule.def.timeout <= 0f) && !float.IsPositiveInfinity(gameRule.def.timeout) && !(gameRule.stop_time == Time.Zero))
			{
				Time time2 = gameRule.stop_time + gameRule.def.timeout;
				if (time == Time.Zero || time2 <= time)
				{
					time = time2;
				}
			}
		}
		if (time == Time.Zero)
		{
			StopUpdating();
			return;
		}
		float num = time - obj.game.time;
		if (num <= 0f)
		{
			UpdateNextFrame();
		}
		else
		{
			UpdateAfter(num);
		}
	}

	private void ForgetTimedOutRules()
	{
		if (rules == null || !obj.AssertAuthority())
		{
			return;
		}
		Time time = obj.game.time;
		for (int num = rules.Count - 1; num >= 0; num--)
		{
			GameRule gameRule = rules[num];
			if (!(gameRule.def.timeout <= 0f) && !float.IsPositiveInfinity(gameRule.def.timeout) && !(gameRule.stop_time == Time.Zero) && !(gameRule.stop_time + gameRule.def.timeout > time))
			{
				Del(gameRule);
			}
		}
	}

	public override void OnUpdate()
	{
		Object obj = base.obj;
		if (obj != null && obj.IsAuthority())
		{
			ForgetTimedOutRules();
		}
		Reschedule();
	}

	public void DelAllRules(bool send_state = true)
	{
		while (rules != null && rules.Count > 0)
		{
			Del(rules[rules.Count - 1], send_state);
		}
	}

	public override void OnDestroy()
	{
		if (rules != null)
		{
			for (int num = rules.Count - 1; num >= 0; num--)
			{
				GameRule rule = rules[num];
				DelInternal(rule, num);
			}
		}
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		GameRule gameRule = Find(key);
		if (gameRule != null)
		{
			if (as_value)
			{
				return gameRule.IsActive();
			}
			return gameRule;
		}
		return Value.Unknown;
	}

	public string Dump()
	{
		int num = ((rules != null) ? rules.Count : 0);
		string text = $"rules of {obj}: {num}";
		for (int i = 0; i < num; i++)
		{
			GameRule gameRule = rules[i];
			text = text + "\n  " + gameRule.Dump(obj);
		}
		return text;
	}
}
