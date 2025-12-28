using System;
using System.Collections.Generic;

namespace Logic;

public class Opportunity : BaseObject, IVars
{
	public class ClassDef
	{
		public DT.Field field;

		public float min_tick = 10f;

		public float max_tick = 15f;

		public int max_count;

		public float min_time_before_new_opportunity = 30f;

		public static ClassDef Load(DT.Field f, Game game)
		{
			if (f == null)
			{
				return null;
			}
			ClassDef classDef = new ClassDef();
			classDef.field = f;
			DT.Field field = f.FindChild("recalc_tick");
			if (field != null)
			{
				classDef.min_tick = field.Float(0, null, classDef.min_tick);
				if (classDef.min_tick < 1f)
				{
					classDef.min_tick = 1f;
				}
				if (field.NumValues() > 1)
				{
					classDef.max_tick = field.Float(1, null, classDef.min_tick);
					if (classDef.max_tick < classDef.min_tick)
					{
						classDef.max_tick = classDef.min_tick;
					}
				}
				else
				{
					classDef.max_tick = classDef.min_tick;
				}
			}
			classDef.max_count = f.GetInt("max_count", null, classDef.max_count);
			classDef.min_time_before_new_opportunity = f.GetFloat("min_time_before_new_opportunity", null, classDef.min_time_before_new_opportunity);
			return classDef;
		}
	}

	public class Def
	{
		public DT.Field field;

		public DT.Field chance_to_add_on_tick;

		public DT.Field chance_to_add_on_tick_player;

		public float min_active_time = 30f;

		public DT.Field chance_to_del_on_tick;

		public DT.Field cooldown;

		public int max_targets = 1;

		public static Def Load(Game game, DT.Field field)
		{
			if (field == null)
			{
				return null;
			}
			if (field.Value() == false)
			{
				return null;
			}
			DT.Field field2 = game.dt.Find("Opportunity");
			if (field.based_on == null)
			{
				field.based_on = field2;
			}
			else if (field2 != null && !field.IsBasedOn(field2))
			{
				Game.Log(field.Path(include_file: true) + ": opportunity field not based on Opportunity", Game.LogType.Error);
			}
			Def def = new Def();
			def.field = field;
			if (!def.Load(game))
			{
				return null;
			}
			return def;
		}

		public bool Load(Game game)
		{
			chance_to_add_on_tick = field.FindChild("chance_to_add_on_tick");
			chance_to_add_on_tick_player = field.FindChild("chance_to_add_on_tick_player");
			min_active_time = field.GetFloat("min_active_time", null, min_active_time);
			chance_to_del_on_tick = field.FindChild("chance_to_del_on_tick");
			cooldown = field.FindChild("cooldown");
			max_targets = field.GetInt("max_targets", null, max_targets);
			return true;
		}

		public float ChanceToAddOnTick(IVars vars, bool is_player = false)
		{
			float result = 100f;
			if (is_player && chance_to_add_on_tick_player != null)
			{
				result = chance_to_add_on_tick_player.Float(vars);
			}
			else if (chance_to_add_on_tick != null)
			{
				result = chance_to_add_on_tick.Float(vars);
			}
			return result;
		}

		public float ChanceToDelOnTick(IVars vars)
		{
			if (chance_to_del_on_tick == null)
			{
				return 0f;
			}
			return chance_to_del_on_tick.Float(vars);
		}

		public float Cooldown(IVars vars)
		{
			if (cooldown == null)
			{
				return 0f;
			}
			return cooldown.Float(vars);
		}

		private float CalcPermanentChance(DT.Field f, float def_val)
		{
			if (f == null)
			{
				return def_val;
			}
			Value value = f.Value(null, calc_expression: false);
			if (!value.is_valid)
			{
				return def_val;
			}
			if (value.is_number)
			{
				return value.Float();
			}
			return -1f;
		}

		public bool IsPermanent()
		{
			float num = CalcPermanentChance(chance_to_add_on_tick, 100f);
			float num2 = CalcPermanentChance(chance_to_del_on_tick, 0f);
			if (num >= 100f)
			{
				return num2 == 0f;
			}
			return false;
		}
	}

	public class TempActionArgs : IDisposable
	{
		public Action action;

		public Object originalTarget;

		public List<Value> originalArgs;

		public int side;

		public TempActionArgs(Action a, Object temp_target = null, List<Value> temp_args = null)
		{
			if (a != null)
			{
				action = a;
				originalTarget = action.target;
				originalArgs = action.args;
				action.target = temp_target;
				action.args = temp_args;
			}
		}

		public void Dispose()
		{
			if (action != null)
			{
				action.target = originalTarget;
				action.args = originalArgs;
			}
		}
	}

	public class RefData : Data
	{
		public NID owner_nid;

		public int idx;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(opportunity " + idx + " of " + owner_nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			Opportunity opportunity = obj as Opportunity;
			if (opportunity?.action == null)
			{
				return false;
			}
			owner_nid = opportunity.owner;
			idx = opportunity.index;
			return idx >= 0;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID(owner_nid, "owner");
			ser.Write7BitUInt(idx, "idx");
		}

		public override void Load(Serialization.IReader ser)
		{
			owner_nid = ser.ReadNID("owner");
			idx = ser.Read7BitUInt("idx");
		}

		public override object GetObject(Game game)
		{
			Object obj = owner_nid.GetObj(game);
			if (obj == null)
			{
				return null;
			}
			Actions component = obj.GetComponent<Actions>();
			if (component?.opportunities == null || component.opportunities.Count <= idx)
			{
				return null;
			}
			return component.opportunities[idx];
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Opportunity))
			{
				return false;
			}
			return true;
		}
	}

	public class FullData : RefData
	{
		public string action_def_id;

		public NID target_nid;

		public Data args;

		public bool active;

		public bool seen;

		public float last_time_elapsed;

		public bool dismissed_message;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Opportunity { action: not null } opportunity))
			{
				return false;
			}
			base.InitFrom(obj);
			action_def_id = opportunity.action.def.id;
			target_nid = opportunity.target;
			args = Data.CreateFull(opportunity.args);
			active = opportunity.active;
			seen = opportunity.seen;
			last_time_elapsed = opportunity.game.time - opportunity.last_time;
			dismissed_message = opportunity.dismissed_message;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteStr(action_def_id, "def");
			ser.WriteNID(target_nid, "target");
			ser.WriteData(args, "args");
			ser.WriteBool(active, "active");
			ser.WriteBool(seen, "seen");
			ser.WriteFloat(last_time_elapsed, "elapsed");
			ser.WriteBool(dismissed_message, "dismissed_message");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			action_def_id = ser.ReadStr("def");
			target_nid = ser.ReadNID("target");
			args = ser.ReadData("args");
			active = ser.ReadBool("active");
			seen = ser.ReadBool("seen");
			last_time_elapsed = ser.ReadFloat("elapsed");
			if (Serialization.cur_version >= 14)
			{
				dismissed_message = ser.ReadBool("dismissed_message");
			}
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Opportunity opportunity))
			{
				return false;
			}
			Actions actions = opportunity.owner?.GetComponent<Actions>();
			if (actions == null)
			{
				return false;
			}
			opportunity.action = actions.Find(action_def_id);
			opportunity.target = target_nid.GetObj(game);
			opportunity.args = Data.RestoreObject<List<Value>>(args, game);
			opportunity.active = active;
			opportunity.seen = seen;
			opportunity.last_time = game.time - last_time_elapsed;
			opportunity.owner?.NotifyListeners("opportunities_changed");
			opportunity.dismissed_message = dismissed_message;
			return true;
		}
	}

	public static int base_def_version;

	public Action action;

	public Object target;

	public List<Value> args;

	public bool forced;

	public bool active;

	public bool seen;

	public bool dismissed_message;

	public Time last_time;

	public Def def => action?.def?.opportunity;

	public Object owner => action?.owner;

	public Game game => action?.game;

	public int index
	{
		get
		{
			Actions actions = owner?.GetComponent<Actions>();
			if (actions?.opportunities == null)
			{
				return -1;
			}
			return actions.opportunities.IndexOf(this);
		}
	}

	public string Validate()
	{
		if (action == null)
		{
			return "uknown_action";
		}
		using (new TempActionArgs(action, action.target, args))
		{
			if (action.NeedsTarget() && !action.ValidateTarget(target))
			{
				return "invalid_target";
			}
			if (action.NeedsArgs() && !action.ValidateArgs())
			{
				return "invalid_args";
			}
			string text = action.Validate();
			if (text != "ok")
			{
				if (text == "_in_progress" && !MatchTarget(action.target, action.args))
				{
					text = "_another_action_in_progress";
				}
				return text;
			}
			if (!action.CheckCost(target))
			{
				return "_cost";
			}
		}
		return "ok";
	}

	public string AIValidate()
	{
		if (action == null)
		{
			return "uknown_action";
		}
		using (new TempActionArgs(action, target, args))
		{
			string text = action.AIValidate();
			if (text != "ok")
			{
				return text;
			}
			if (action.NeedsTarget() && !action.ValidateTarget(target))
			{
				return "invalid_target";
			}
			if (action.NeedsArgs() && !action.ValidateArgs())
			{
				return "invalid_args";
			}
		}
		return "ok";
	}

	public string ait()
	{
		return AIValidate();
	}

	public string GetValidateKey()
	{
		if (action == null)
		{
			return "uknown_action";
		}
		string text;
		using (new TempActionArgs(action, target, args))
		{
			text = action.GetValidateKey(this);
			if (text == "_in_progress" && !MatchTarget(action.target, action.args))
			{
				text = "_another_action_in_progress";
			}
		}
		return text;
	}

	public DT.Field GetValidatePrompt()
	{
		DT.Field field = action?.def.field?.FindChild("validate_prompts");
		if (field == null)
		{
			return null;
		}
		string validateKey = GetValidateKey();
		DT.Field field2 = field.FindChild(validateKey);
		if (field2 == null && validateKey != "ok")
		{
			field2 = field.FindChild("generic_invalid");
		}
		if (field2 == null)
		{
			return null;
		}
		return field2;
	}

	public bool MatchTarget(Object target, List<Value> args)
	{
		if (this.target != target)
		{
			return false;
		}
		if (this.args == args)
		{
			return true;
		}
		if (this.args == null || args == null)
		{
			return false;
		}
		if (this.args.Count != args.Count)
		{
			return false;
		}
		for (int i = 0; i < args.Count; i++)
		{
			Value value = this.args[i];
			Value value2 = args[i];
			if (value != value2)
			{
				return false;
			}
		}
		return true;
	}

	public bool IsRunning()
	{
		if (action == null)
		{
			return false;
		}
		if (action.state == Action.State.Inactive)
		{
			return false;
		}
		if (action.target != target)
		{
			return false;
		}
		if (action.args != args)
		{
			return false;
		}
		return true;
	}

	public override bool IsRefSerializable()
	{
		if (owner != null && owner.IsRefSerializable())
		{
			return index >= 0;
		}
		return false;
	}

	public string TargetStr()
	{
		string text = $"({target}";
		if (args != null)
		{
			for (int i = 0; i < args.Count; i++)
			{
				Value value = args[i];
				text += $", {value}";
			}
		}
		return text + ")";
	}

	public override string ToString()
	{
		string text = (active ? "" : "[Inactive]");
		return $"{text}Opportunity {owner}.{action?.def?.id}{TargetStr()}";
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "target":
			return target;
		case "args":
			return new Value(args);
		case "target_kingdom":
			return target?.GetKingdom() ?? action?.own_character?.mission_kingdom;
		case "mission_kingdom":
			return action?.own_character?.mission_kingdom;
		case "validate_key":
			return GetValidateKey();
		case "validate_prompt_text":
			return new Value(GetValidatePrompt());
		default:
			if (action == null)
			{
				return Value.Unknown;
			}
			using (new TempActionArgs(action, action.target, args))
			{
				return action.GetVar(key, this, as_value);
			}
		}
	}
}
