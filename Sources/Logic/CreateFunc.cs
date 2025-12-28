using System.Collections.Generic;

namespace Logic;

public class Challenge : BaseObject, IVars, ISetVar, IListener
{
	public class Def : Logic.Def
	{
		public DT.Field enabled;

		public DT.Field weight;

		public List<Query> args;

		public List<DT.Field> set_vars;

		public DT.Field generations_limit;

		public DT.Field minutes_limit;

		public GameRule.Def rules;

		public GameRule.Def success_rule;

		public GameRule.Def fail_rule;

		public OutcomeDef outcomes;

		public Trigger activate_trigger;

		public Trigger deactivate_trigger;

		public static Vars tmp_vars = new Vars();

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			enabled = field.FindChild("enabled");
			weight = field.FindChild("weight");
			args = Query.LoadAll(game, field.FindChild("args"));
			List<DT.Field> list = field.FindChild("set_vars")?.Children();
			if (list != null && list.Count > 0)
			{
				set_vars = new List<DT.Field>(list.Count);
				for (int i = 0; i < list.Count; i++)
				{
					DT.Field field2 = list[i];
					if (!string.IsNullOrEmpty(field2.key))
					{
						set_vars.Add(field2);
					}
				}
			}
			else
			{
				set_vars = null;
			}
			DT.Field field3 = field.FindChild("time_limit");
			if (field3 != null)
			{
				generations_limit = field3.FindChild("generations");
				minutes_limit = field3.FindChild("minutes");
			}
			else
			{
				generations_limit = null;
				minutes_limit = null;
			}
			rules = null;
			activate_trigger = new Trigger(null, null, "activate_challenge");
			deactivate_trigger = new Trigger(null, null, "deactivate_challenge");
			success_rule = null;
			fail_rule = null;
			DT.Field field4 = field.FindChild("outcomes");
			if (field4 != null)
			{
				DT.Field defaults = field.FindChild("outcome_defaults");
				outcomes = new OutcomeDef(game, field4, defaults);
			}
			else
			{
				outcomes = null;
			}
			return true;
		}

		public override bool Validate(Game game)
		{
			DT.Field field = base.field;
			rules = Logic.Def.Get<GameRule.Def>(field.FindChild("rules"));
			if (rules == null)
			{
				Game.Log(base.field.Path(include_file: true) + ": Challenge has no rules", Game.LogType.Error);
				return false;
			}
			if (rules.start_triggers != null && rules.start_triggers.Count > 0)
			{
				activate_trigger.def = rules.start_triggers[0];
			}
			if (rules.stop_triggers != null && rules.stop_triggers.Count > 0)
			{
				deactivate_trigger.def = rules.stop_triggers[0];
			}
			success_rule = rules.FindChildRule("success");
			if (success_rule == null)
			{
				Game.Log(base.field.Path(include_file: true) + ": Challenge has no success rule", Game.LogType.Error);
			}
			fail_rule = rules.FindChildRule("fail");
			if (fail_rule == null)
			{
				Game.Log(base.field.Path(include_file: true) + ": Challenge has no fail rule", Game.LogType.Error);
			}
			return true;
		}

		public float CalcWeight(Kingdom owner, bool validate_activation = true)
		{
			tmp_vars.obj = owner;
			tmp_vars.Set("own_kingdom", owner);
			if (validate_activation && enabled != null && !enabled.Value(tmp_vars).Bool())
			{
				return -1000f;
			}
			float num = 1f;
			if (weight != null)
			{
				num = weight.Float(tmp_vars);
			}
			if (num <= 0f)
			{
				return -100f;
			}
			if (validate_activation && rules != null)
			{
				activate_trigger.sender = owner;
				activate_trigger.param = null;
				if (!rules.ValidateActivate(activate_trigger, out var _))
				{
					return -10f;
				}
			}
			return num;
		}
	}

	public enum State
	{
		Invalid,
		Inactive,
		Active,
		Succeeded,
		Failed
	}

	public delegate Challenge CreateFunc(Def def, Kingdom owner);

	public class RefData : Data
	{
		public NID owner_nid;

		public string challenge_def_id;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(" + challenge_def_id + " of " + owner_nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Challenge challenge))
			{
				return false;
			}
			owner_nid = challenge.owner;
			challenge_def_id = challenge.def.id;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID<Kingdom>(owner_nid, "owner");
			ser.WriteStr(challenge_def_id, "def");
		}

		public override void Load(Serialization.IReader ser)
		{
			owner_nid = ser.ReadNID<Kingdom>("owner");
			challenge_def_id = ser.ReadStr("def");
		}

		public override object GetObject(Game game)
		{
			Kingdom kingdom = owner_nid.Get<Kingdom>(game);
			if (kingdom == null)
			{
				return null;
			}
			return Find(kingdom, challenge_def_id);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Challenge challenge))
			{
				return false;
			}
			if (challenge.def.id != challenge_def_id)
			{
				return false;
			}
			return true;
		}
	}

	public class FullData : RefData
	{
		public State state;

		public Data args;

		public Data set_vars;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Challenge challenge))
			{
				return false;
			}
			base.InitFrom(obj);
			state = challenge.state;
			args = challenge.args.CreateFullData();
			set_vars = challenge.set_vars.CreateFullData();
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.Write7BitUInt((int)state, "state");
			ser.WriteData(args, "args");
			ser.WriteData(set_vars, "set_vars");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			state = (State)ser.Read7BitUInt("state");
			args = ser.ReadData("args");
			set_vars = ser.ReadData("set_vars");
		}

		public override object GetObject(Game game)
		{
			Kingdom kingdom = owner_nid.Get<Kingdom>(game);
			if (kingdom == null)
			{
				return null;
			}
			Challenge challenge = Find(kingdom, challenge_def_id);
			if (challenge != null)
			{
				return challenge;
			}
			Def def = kingdom.game.defs.Get<Def>(challenge_def_id);
			if (def == null)
			{
				return null;
			}
			challenge = new Challenge
			{
				def = def,
				owner = kingdom,
				state = State.Invalid
			};
			kingdom.challenges.Add(challenge);
			return challenge;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Challenge challenge))
			{
				return false;
			}
			if (challenge.def.id != challenge_def_id)
			{
				Game.Log("Attempting to apply " + challenge_def_id + " data to " + challenge.ToString(), Game.LogType.Error);
				return false;
			}
			base.ApplyTo(obj, game);
			challenge.state = state;
			challenge.args = Data.RestoreObject<Vars>(args, challenge.game);
			challenge.set_vars = Data.RestoreObject<Vars>(set_vars, challenge.game);
			return true;
		}
	}

	public Def def;

	public Kingdom owner;

	public State state;

	public Vars args = new Vars();

	public Vars set_vars = new Vars();

	public GameRule rules;

	public string reason;

	private static List<OutcomeDef> tmp_forced_outcomes = new List<OutcomeDef> { null, null };

	public Game game => owner?.game;

	public override string ToString()
	{
		string s = $"{state} Challenge {owner.Name}.{def.id}";
		if (!args.Empty())
		{
			s += " (";
			int i = 0;
			args.EnumerateAll(delegate(string key, Value val)
			{
				int num = i + 1;
				i = num;
				if (i > 1)
				{
					s += ", ";
				}
				if (i < 3)
				{
					s += $"{key}: '{val}'";
				}
				else if (i == 3)
				{
					s += "...";
				}
			});
			s += ")";
		}
		return s;
	}

	public string Dump()
	{
		string text = ToString();
		if (!args.Empty())
		{
			text = text + "\nArgs:" + args.Dump("\n    ", "\n    ");
		}
		if (!set_vars.Empty())
		{
			text = text + "\nSet Vars:" + set_vars.Dump("\n    ", "\n    ");
		}
		return text;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "obj":
		case "challenge":
			return this;
		case "owner":
		case "own_kingdom":
		case "src_kingdom":
			return owner;
		case "state":
			return state.ToString();
		case "is_active":
			return state == State.Active;
		case "succeeded":
			return state == State.Succeeded;
		case "failed":
			return state == State.Failed;
		case "rules":
			return rules;
		case "reason":
			return reason;
		case "outcomes_def":
			return new Value(def.outcomes);
		default:
		{
			Value result = set_vars.Get(key, as_value);
			if (!result.is_unknown)
			{
				return result;
			}
			result = args.Get(key, as_value);
			if (!result.is_unknown)
			{
				return result;
			}
			result = owner.GetVar(key, vars, as_value);
			if (!result.is_unknown)
			{
				return result;
			}
			return Value.Unknown;
		}
		}
	}

	public void SetVar(string key, Value value)
	{
		set_vars.Set(key, value);
		owner.SendState<Kingdom.ChallengesState>();
		owner.NotifyListeners("challenge_changed", this);
	}

	public static Challenge Find(Kingdom owner, Def def)
	{
		if (owner?.challenges == null)
		{
			return null;
		}
		for (int i = 0; i < owner.challenges.Count; i++)
		{
			Challenge challenge = owner.challenges[i];
			if (challenge?.def == def)
			{
				return challenge;
			}
		}
		return null;
	}

	public static Challenge Find(Kingdom owner, string def_id)
	{
		if (owner?.challenges == null)
		{
			return null;
		}
		for (int i = 0; i < owner.challenges.Count; i++)
		{
			Challenge challenge = owner.challenges[i];
			if (challenge?.def?.id == def_id)
			{
				return challenge;
			}
		}
		return null;
	}

	public static Challenge Create(Def def, Kingdom owner)
	{
		Challenge challenge = new Challenge
		{
			def = def,
			owner = owner,
			state = State.Invalid
		};
		if (!Query.FillVars(challenge.args, def.args, challenge))
		{
			return null;
		}
		if (def.rules?.start_condition != null && !def.rules.start_condition.GetValue(challenge).Bool())
		{
			return null;
		}
		if (def.set_vars != null)
		{
			for (int i = 0; i < def.set_vars.Count; i++)
			{
				DT.Field field = def.set_vars[i];
				Value val = field.Value(challenge);
				challenge.set_vars.Set(field.key, val);
			}
		}
		challenge.InitTimeLimit();
		challenge.state = State.Inactive;
		return challenge;
	}

	public static void DestroyAll(Kingdom owner)
	{
		if (owner?.challenges != null)
		{
			for (int num = owner.challenges.Count - 1; num >= 0; num--)
			{
				owner.challenges[num].Deactivate();
			}
		}
	}

	private void InitTimeLimit()
	{
		int num = 0;
		if (def.generations_limit != null)
		{
			num = def.generations_limit.Int(this);
		}
		float num2 = 0f;
		if (def.minutes_limit != null)
		{
			num2 = def.minutes_limit.Float(this);
		}
		if (num > 0 && num2 > 0f)
		{
			if (game.Random(0, 100) < 50)
			{
				num = 0;
			}
			else
			{
				num2 = 0f;
			}
		}
		if (num > 0)
		{
			set_vars.Set("generations_limit", num);
			set_vars.Set("start_generation", owner.generationsPassed);
		}
		if (num2 > 0f)
		{
			set_vars.Set("minutes_limit", num2);
			set_vars.Set("seconds_limit", num2 * 60f);
		}
	}

	public bool Activate()
	{
		if (def.rules == null)
		{
			return false;
		}
		Object target_obj = owner;
		GameRule.Def.Target target = def.rules.ResolveTarget(ref target_obj);
		def.activate_trigger.sender = owner;
		def.activate_trigger.param = this;
		rules = def.rules.Activate(target_obj, target, def.activate_trigger, null, null, this);
		if (rules == null)
		{
			return false;
		}
		state = State.Active;
		owner.challenges.Add(this);
		Game.Log($"{this} activated", Game.LogType.Message);
		owner.SendState<Kingdom.ChallengesState>();
		owner.NotifyListeners("challenge_activated", this);
		return true;
	}

	public void Deactivate(State state = State.Invalid, Trigger trigger = null)
	{
		if (!owner.IsValid())
		{
			state = State.Invalid;
		}
		this.state = state;
		if (state != State.Invalid)
		{
			reason = GetReason(trigger);
		}
		if (rules != null)
		{
			rules.context = null;
			def.deactivate_trigger.sender = owner;
			def.deactivate_trigger.param = this;
			rules?.Deactivate(def.deactivate_trigger);
		}
		rules = null;
		owner.challenges.Remove(this);
		Game.Log($"{this} deactivated", Game.LogType.Message);
		if (state != State.Invalid)
		{
			ApplyOutcomes();
			owner.SendState<Kingdom.ChallengesState>();
			owner.NotifyListeners("challenge_deactivated", this);
		}
	}

	private string GetReason(Trigger trigger)
	{
		if (trigger == null)
		{
			return null;
		}
		if (trigger.def?.field == null)
		{
			return trigger.message;
		}
		return trigger.def.field.GetString("reason", this, trigger.message);
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (!(obj is GameRule gameRule))
		{
			return;
		}
		if (message == "executed")
		{
			if (gameRule.def.id == def.success_rule.id)
			{
				Deactivate(State.Succeeded, param as Trigger);
			}
			else if (gameRule.def.id == def.fail_rule.id)
			{
				Deactivate(State.Failed, param as Trigger);
			}
		}
		else if (message == "stopped" && gameRule.def.id == def.rules.id)
		{
			Deactivate(State.Failed, param as Trigger);
		}
	}

	private void SetAddText(Vars outcome_vars, string main_outcome_key, string reason)
	{
		DT.Field field = def.field.FindChild(main_outcome_key + "_add_texts");
		if (field != null)
		{
			DT.Field field2 = null;
			if (!string.IsNullOrEmpty(reason))
			{
				field2 = field.FindChild(reason);
			}
			if (field2 == null)
			{
				field2 = field.FindChild(main_outcome_key);
			}
			if (field2 != null)
			{
				outcome_vars.Set(main_outcome_key + "_add_text", field2.Path());
			}
		}
	}

	private void ApplyOutcomes()
	{
		if (def.outcomes == null)
		{
			return;
		}
		string text;
		if (state == State.Succeeded)
		{
			text = "success";
		}
		else
		{
			if (state != State.Failed)
			{
				return;
			}
			text = "fail";
		}
		OutcomeDef outcomeDef = def.outcomes.Find(text);
		if (outcomeDef == null)
		{
			return;
		}
		tmp_forced_outcomes[1] = outcomeDef;
		if (!string.IsNullOrEmpty(reason))
		{
			OutcomeDef outcomeDef2 = outcomeDef.Find(reason);
			if (outcomeDef2 != null)
			{
				tmp_forced_outcomes[1] = outcomeDef2;
			}
		}
		List<OutcomeDef> outcomes = def.outcomes.DecideOutcomes(game, this, tmp_forced_outcomes);
		List<OutcomeDef> list = OutcomeDef.UniqueOutcomes(outcomes);
		Vars vars = new Vars(this);
		vars.Set("reason", reason);
		SetAddText(vars, text, reason);
		OutcomeDef.PrecalculateValues(list, game, vars, vars);
		Event obj = new Event(owner, "challenge_outcomes", this);
		obj.outcomes = outcomes;
		obj.vars = vars;
		owner.FireEvent(obj);
		for (int i = 0; i < list.Count; i++)
		{
			OutcomeDef outcomeDef3 = list[i];
			if (!(outcomeDef3.key == "success") && !(outcomeDef3.key == "fail") && !outcomeDef3.Apply(game, this))
			{
				Game.Log($"{this}: unhandled outcome: {outcomeDef3.id}", Game.LogType.Warning);
			}
		}
	}

	public static void RebindRules(Kingdom owner)
	{
		if (owner?.challenges == null)
		{
			return;
		}
		for (int i = 0; i < owner.challenges.Count; i++)
		{
			Challenge challenge = owner.challenges[i];
			if (challenge.rules == null && challenge.state == State.Active)
			{
				challenge.rules = ObjRules.Find(owner, challenge.def.rules);
				if (challenge.rules != null)
				{
					challenge.rules.context = challenge;
				}
			}
		}
	}

	public override bool IsRefSerializable()
	{
		if (owner != null)
		{
			return def != null;
		}
		return false;
	}

	public override bool IsFullSerializable()
	{
		if (!IsRefSerializable())
		{
			return false;
		}
		if (!args.IsFullSerializable())
		{
			return false;
		}
		if (!set_vars.IsFullSerializable())
		{
			return false;
		}
		return true;
	}
}
