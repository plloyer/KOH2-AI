using System;
using System.Collections.Generic;

namespace Logic;

public class GameRule : BaseObject, IVars, ISetVar, GameRules.IListener
{
	public class Def : Logic.Def, IVars, GameRules.IListener
	{
		public class Target
		{
			public TargetType type;

			public string name;

			public override string ToString()
			{
				return $"{type} {name}";
			}
		}

		public Def parent_rule;

		public RuleTimer.Def parent_timer;

		public List<Target> targets;

		public List<Trigger.Def> start_triggers;

		public List<Trigger.Def> stop_triggers;

		public List<Condition> conditions;

		public Condition active_condition;

		public Condition execute_condition;

		public Condition start_condition;

		public Condition stop_condition;

		public float cooldown;

		public float timeout;

		public List<DT.Field> var_fields;

		public List<Def> child_rules;

		public List<RuleTimer.Def> timers;

		public DT.Field outcome_defaults;

		public OutcomeDef start_outcomes;

		public OutcomeDef stop_outcomes;

		public List<FadingModifier.Def> mods;

		public List<Status.Def> statuses;

		public int log;

		public int activation_checks;

		public int activations;

		public int deactivation_checks;

		public int deactivations;

		private Object tmp_target_obj;

		private string tmp_target_name;

		private Trigger tmp_trigger = new Trigger();

		private GameRule tmp_parent_rule;

		private RuleTimer tmp_parent_timer;

		public bool instant => execute_condition != null;

		public static Def Find(Game game, string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			Def def = game.defs.Find<Def>(id);
			if (def != null)
			{
				return def;
			}
			int num = id.IndexOf('.');
			if (num < 0)
			{
				return null;
			}
			string text = id.Substring(0, num);
			Def def2 = Find(game, text);
			if (def2 == null)
			{
				return null;
			}
			string path = id.Substring(num + 1);
			return def2.FindChildRuleDef(path);
		}

		public Def FindChildRuleDef(string path)
		{
			if (child_rules == null && timers == null)
			{
				return null;
			}
			int num = path.IndexOf('.');
			string text;
			string text2;
			if (num < 0)
			{
				text = path;
				text2 = null;
			}
			else
			{
				text = path.Substring(0, num);
				text2 = path.Substring(num + 1);
			}
			if (child_rules != null)
			{
				for (int i = 0; i < child_rules.Count; i++)
				{
					Def def = child_rules[i];
					if (!(def.field.key != text))
					{
						if (text2 == null)
						{
							return def;
						}
						return def.FindChildRuleDef(text);
					}
				}
			}
			if (text2 == null)
			{
				return null;
			}
			if (timers != null)
			{
				for (int j = 0; j < timers.Count; j++)
				{
					RuleTimer.Def def2 = timers[j];
					if (!(def2.def.field.key != text))
					{
						return FindTimerChildRuleDef(def2, text2);
					}
				}
			}
			return null;
		}

		public Def FindTimerChildRuleDef(RuleTimer.Def tdef, string path)
		{
			if (tdef.child_rules == null)
			{
				return null;
			}
			int num = path.IndexOf('.');
			string text;
			string text2;
			if (num < 0)
			{
				text = path;
				text2 = null;
			}
			else
			{
				text = path.Substring(0, num);
				text2 = path.Substring(num + 1);
			}
			for (int i = 0; i < tdef.child_rules.Count; i++)
			{
				Def def = tdef.child_rules[i];
				if (!(def.field.key != text))
				{
					if (text2 == null)
					{
						return def;
					}
					return def.FindChildRuleDef(text2);
				}
			}
			return null;
		}

		public override bool Load(Game game)
		{
			targets = null;
			start_triggers = null;
			stop_triggers = null;
			conditions = null;
			active_condition = null;
			start_condition = null;
			stop_condition = null;
			var_fields = null;
			child_rules = null;
			timers = null;
			start_outcomes = null;
			stop_outcomes = null;
			mods = null;
			statuses = null;
			if (IsBase() && parent_rule == null)
			{
				return true;
			}
			cooldown = base.field.GetFloat("cooldown");
			timeout = base.field.GetFloat("timeout");
			if (cooldown > timeout && timeout > 0f)
			{
				timeout = cooldown;
			}
			log = base.field.GetInt("log");
			LoadTargets(game);
			LoadTriggers(game);
			LoadOutcomes(game);
			List<DT.Field> list = base.field.Children();
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					DT.Field field = list[i];
					switch (field.Type())
					{
					case "condition":
						AddCondition(game, field);
						break;
					case "var":
					case "log":
						AddVar(game, field);
						break;
					case "rule":
						AddChildRule(game, field);
						break;
					case "timer":
						AddTimer(game, field);
						break;
					case "mod":
						AddMod(game, field);
						break;
					case "status":
						AddStatus(game, field);
						break;
					}
				}
			}
			active_condition = FindCondition("active");
			execute_condition = FindCondition("execute");
			start_condition = FindCondition("start");
			stop_condition = FindCondition("stop");
			if (active_condition != null)
			{
				if (execute_condition != null)
				{
					Game.Log(execute_condition.def.Path(include_file: true) + ": rule '" + base.id + "' has both 'active' and 'execute' conditions", Game.LogType.Error);
				}
				if (start_condition != null)
				{
					Game.Log(start_condition.def.Path(include_file: true) + ": rule '" + base.id + "' has both 'active' and 'start' conditions", Game.LogType.Error);
				}
				if (stop_condition != null)
				{
					Game.Log(stop_condition.def.Path(include_file: true) + ": rule '" + base.id + "' has both 'active' and 'stop' conditions", Game.LogType.Error);
				}
			}
			else if (execute_condition != null)
			{
				if (start_condition != null)
				{
					Game.Log(start_condition.def.Path(include_file: true) + ": rule '" + base.id + "' has both 'execute' and 'start' conditions", Game.LogType.Error);
				}
				if (stop_condition != null)
				{
					Game.Log(stop_condition.def.Path(include_file: true) + ": rule '" + base.id + "' has both 'execute' and 'stop' conditions", Game.LogType.Error);
				}
			}
			else if (start_condition != null)
			{
				if (stop_condition == null)
				{
					Game.Log(start_condition.def.Path(include_file: true) + ": rule '" + base.id + "' has 'start', but no 'stop' condition", Game.LogType.Error);
				}
			}
			else if (stop_condition != null)
			{
				Game.Log(stop_condition.def.Path(include_file: true) + ": rule '" + base.id + "' has 'stop', but no 'start' condition", Game.LogType.Error);
			}
			else
			{
				Game.Log(base.field.Path(include_file: true) + ": rule has no 'active', 'execute', 'start' or 'stop' conditions", Game.LogType.Error);
			}
			if (start_condition == null)
			{
				start_condition = active_condition ?? execute_condition;
			}
			if (targets == null && parent_rule == null)
			{
				Game.Log(base.field.Path(include_file: true) + ": rule has no targets", Game.LogType.Error);
			}
			if (instant)
			{
				if (stop_triggers != null && stop_triggers != start_triggers)
				{
					Game.Log(base.field.Path(include_file: true) + ": instant rule has stop triggers", Game.LogType.Error);
				}
				if (timers != null)
				{
					Game.Log(base.field.Path(include_file: true) + ": instant rule has timers", Game.LogType.Error);
				}
				if (mods != null)
				{
					Game.Log(base.field.Path(include_file: true) + ": instant rule has mods", Game.LogType.Error);
				}
				if (statuses != null)
				{
					Game.Log(base.field.Path(include_file: true) + ": instant rule has statuses", Game.LogType.Error);
				}
				if (stop_outcomes != null)
				{
					Game.Log(base.field.Path(include_file: true) + ": instant rule has stop outcomes", Game.LogType.Error);
				}
				if (child_rules != null)
				{
					for (int j = 0; j < child_rules.Count; j++)
					{
						Def def = child_rules[j];
						if (!def.instant)
						{
							Game.Log(def.field.Path(include_file: true) + ": instant rule has non-isntant children", Game.LogType.Error);
						}
					}
				}
			}
			if (start_triggers == null || start_triggers.Count == 0)
			{
				Game.Log(base.field.Path(include_file: true) + ": rule has no triggers", Game.LogType.Error);
			}
			return true;
		}

		private void LoadTargets(Game game)
		{
			DT.Field field = base.field.FindChild("target");
			if (parent_rule != null)
			{
				targets = parent_rule.targets;
				if (field != null)
				{
					Game.Log(field.Path(include_file: true) + ": child rule has target", Game.LogType.Warning);
				}
				return;
			}
			List<DT.Field> list = field?.Children();
			if (list == null)
			{
				return;
			}
			GameRules.RegisterTargetTypes();
			for (int i = 0; i < list.Count; i++)
			{
				DT.Field field2 = list[i];
				string text = field2.Type();
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				TargetType targetType = TargetType.Find(text);
				if (targetType == null)
				{
					Game.Log(field2.Path(include_file: true) + ": Unknown target type: '" + text + "'", Game.LogType.Error);
					continue;
				}
				if (targets == null)
				{
					targets = new List<Target>();
				}
				targets.Add(new Target
				{
					type = targetType,
					name = field2.key
				});
			}
		}

		private void LoadTriggers(Game game)
		{
			start_triggers = LoadTriggers(game, "start_triggers");
			bool flag = false;
			if (start_triggers == null)
			{
				start_triggers = LoadTriggers(game, "triggers");
				if (start_triggers != null)
				{
					flag = true;
				}
			}
			stop_triggers = LoadTriggers(game, "stop_triggers");
			if (stop_triggers == null && flag)
			{
				stop_triggers = start_triggers;
			}
			if (parent_timer == null)
			{
				return;
			}
			Trigger.Def def = Trigger.Def.CreateTimerDef(parent_timer);
			if (def == null)
			{
				return;
			}
			if (start_triggers == null)
			{
				start_triggers = new List<Trigger.Def>();
				if (stop_triggers == null)
				{
					stop_triggers = start_triggers;
				}
			}
			start_triggers.Add(def);
			if (stop_triggers != start_triggers)
			{
				if (stop_triggers == null)
				{
					stop_triggers = new List<Trigger.Def>();
				}
				stop_triggers.Add(def);
			}
		}

		private List<Trigger.Def> LoadTriggers(Game game, string key)
		{
			DT.Field field = base.field.FindChild(key);
			if (field == null)
			{
				return null;
			}
			List<Trigger.Def> list = new List<Trigger.Def>();
			List<DT.Field> list2 = field?.Children();
			if (list2 == null)
			{
				return list;
			}
			for (int i = 0; i < list2.Count; i++)
			{
				Trigger.Def def = Trigger.Def.Load(list2[i]);
				if (def != null)
				{
					list.Add(def);
					if (def.sender_field != null && parent_rule == null && key != "stop_triggers")
					{
						Game.Log(def.target_field.Path(include_file: true) + ": rule start trigger has sender", Game.LogType.Error);
					}
					if (def.target_field != null && parent_rule != null)
					{
						Game.Log(def.target_field.Path(include_file: true) + ": child rule trigger has target", Game.LogType.Error);
					}
				}
			}
			return list;
		}

		private void AddCondition(Game game, DT.Field f)
		{
			Condition condition = Condition.Load(f);
			if (condition != null)
			{
				if (conditions == null)
				{
					conditions = new List<Condition>();
				}
				conditions.Add(condition);
			}
		}

		private void LoadOutcomes(Game game)
		{
			outcome_defaults = base.field.FindChild("outcome_defaults");
			DT.Field field = base.field.FindChild("outcomes");
			if (field != null)
			{
				start_outcomes = new OutcomeDef(game, field, outcome_defaults);
			}
			field = base.field.FindChild("stop_outcomes");
			if (field != null)
			{
				stop_outcomes = new OutcomeDef(game, field, outcome_defaults);
			}
		}

		private void AddVar(Game game, DT.Field f)
		{
			if (var_fields == null)
			{
				var_fields = new List<DT.Field>();
			}
			var_fields.Add(f);
		}

		private void AddChildRule(Game game, DT.Field f, RuleTimer.Def parent_timer = null)
		{
			DT.Field based_on = base.field.BaseRoot();
			if (f.based_on == null)
			{
				f.based_on = based_on;
			}
			else if (!f.IsBasedOn(based_on))
			{
				Game.Log(f.Path(include_file: true) + ": rule not based on GameRule", Game.LogType.Warning);
			}
			Def def = new Def();
			def.dt_def = new DT.Def
			{
				path = f.Path(),
				field = f
			};
			def.dt_def.def = def;
			f.def = def.dt_def;
			def.parent_rule = this;
			def.parent_timer = parent_timer;
			if (!def.Load(game))
			{
				return;
			}
			if (parent_timer != null)
			{
				if (parent_timer.child_rules == null)
				{
					parent_timer.child_rules = new List<Def>();
				}
				parent_timer.child_rules.Add(def);
			}
			else
			{
				if (child_rules == null)
				{
					child_rules = new List<Def>();
				}
				child_rules.Add(def);
			}
		}

		private void AddTimer(Game game, DT.Field f)
		{
			Timer.Def def = new Timer.Def();
			def.repeating = true;
			if (!def.Load(game, f))
			{
				return;
			}
			RuleTimer.Def def2 = new RuleTimer.Def();
			def2.def = def;
			def2.rule = this;
			def.rtdef = def2;
			def2.trigger_parent = f.GetBool("trigger_parent");
			def2.stop_parent = f.GetBool("stop_parent");
			DT.Field field = f.FindChild("outcomes");
			if (field != null)
			{
				def2.outcomes = new OutcomeDef(game, field, outcome_defaults);
			}
			List<DT.Field> list = f.Children();
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					DT.Field field2 = list[i];
					if (field2.Type() == "rule")
					{
						AddChildRule(game, field2, def2);
					}
				}
			}
			if (timers == null)
			{
				timers = new List<RuleTimer.Def>();
			}
			timers.Add(def2);
		}

		private void AddMod(Game game, DT.Field f)
		{
			FadingModifier.Def def = FadingModifier.Def.Load(game, f);
			if (def != null)
			{
				if (def.duration != null)
				{
					Game.Log(f.Path(include_file: true) + ": rule mod has duration", Game.LogType.Error);
				}
				if (def.fade_out_time != null)
				{
					Game.Log(f.Path(include_file: true) + ": rule mod has fade out", Game.LogType.Error);
				}
				if (mods == null)
				{
					mods = new List<FadingModifier.Def>();
				}
				mods.Add(def);
			}
		}

		private void AddStatus(Game game, DT.Field f)
		{
			Status.Def item = game.defs.Get<Status.Def>(f.key);
			if (statuses == null)
			{
				statuses = new List<Status.Def>();
			}
			statuses.Add(item);
		}

		public bool ValidateActivate(Trigger trigger, out Target target, GameRule parent_rule = null, RuleTimer parent_timer = null)
		{
			target = null;
			if (start_condition == null)
			{
				return false;
			}
			tmp_trigger.Set(trigger, this);
			tmp_parent_rule = parent_rule;
			tmp_parent_timer = parent_timer;
			tmp_target_obj = DecideTargetObj(tmp_trigger, parent_rule);
			if (tmp_target_obj == null || !tmp_target_obj.IsValid())
			{
				return false;
			}
			target = ResolveTarget(ref tmp_target_obj);
			if (target == null)
			{
				return false;
			}
			tmp_target_name = target.name;
			if (!instant || cooldown > 0f)
			{
				GameRule gameRule = ObjRules.Find(tmp_target_obj, this);
				if (gameRule != null)
				{
					if (gameRule.IsActive())
					{
						return false;
					}
					if (cooldown > 0f && gameRule.game.time - gameRule.stop_time < cooldown)
					{
						return false;
					}
				}
			}
			if (trigger?.def != null)
			{
				trigger.def.checks++;
			}
			if (!tmp_trigger.Validate(tmp_target_obj.game, this))
			{
				return false;
			}
			if (trigger?.def != null)
			{
				trigger.def.calls++;
			}
			activation_checks++;
			if (log >= 3)
			{
				Game.Log($"checking rule {base.id}({tmp_target_obj}): trigger: {trigger}", Game.LogType.Message);
			}
			if (!start_condition.GetValue(this).Bool())
			{
				return false;
			}
			return true;
		}

		public void OnTrigger(Trigger trigger, GameRule parent_rule = null, RuleTimer parent_timer = null)
		{
			if (ValidateActivate(trigger, out var target, parent_rule, parent_timer))
			{
				if (trigger?.def != null)
				{
					trigger.def.activations++;
				}
				Activate(tmp_target_obj, target, tmp_trigger, parent_rule, parent_timer);
			}
		}

		public Target ResolveTarget(ref Object target_obj)
		{
			if (targets == null)
			{
				return null;
			}
			for (int i = 0; i < targets.Count; i++)
			{
				Target target = targets[i];
				object obj = target.type.Resolve(target_obj);
				if (obj != null)
				{
					target_obj = obj as Object;
					if (target_obj == null)
					{
						return null;
					}
					return target;
				}
			}
			return null;
		}

		public Object DecideTargetObj(Trigger trigger, GameRule parent_rule)
		{
			if (parent_rule != null)
			{
				return parent_rule.target_obj;
			}
			return trigger.GetTarget(this);
		}

		public Condition FindCondition(string name)
		{
			if (conditions == null)
			{
				return null;
			}
			for (int i = 0; i < conditions.Count; i++)
			{
				Condition condition = conditions[i];
				if (condition.def.key == name)
				{
					return condition;
				}
			}
			return null;
		}

		public Def FindChildRule(string key)
		{
			if (child_rules == null)
			{
				return null;
			}
			for (int i = 0; i < child_rules.Count; i++)
			{
				Def def = child_rules[i];
				if (def.field.key == key)
				{
					return def;
				}
			}
			return null;
		}

		public GameRule Activate(Object target_obj, Target target, Trigger trigger = null, GameRule parent_rule = null, RuleTimer parent_timer = null, IVars context = null)
		{
			if (target_obj == null)
			{
				return null;
			}
			activations++;
			GameRule gameRule = new GameRule(target_obj, target, this, parent_rule, parent_timer, context);
			gameRule.Activate(trigger);
			if (!gameRule.IsActive())
			{
				return null;
			}
			return gameRule;
		}

		public override Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			if (key == tmp_target_name)
			{
				return tmp_target_obj;
			}
			if (vars == null)
			{
				vars = this;
			}
			switch (key)
			{
			case "parent_rule":
				return tmp_parent_rule;
			case "parent_timer":
				return new Value(tmp_parent_timer);
			case "trigger":
				return new Value(tmp_trigger);
			case "active_condition":
			case "start_condition":
				if (start_condition == null)
				{
					return Value.Null;
				}
				if (!as_value)
				{
					return start_condition;
				}
				return start_condition.GetValue(vars);
			case "stop_condition":
				if (stop_condition == null)
				{
					return Value.Null;
				}
				if (!as_value)
				{
					return stop_condition;
				}
				return stop_condition.GetValue(vars);
			default:
			{
				Condition condition = FindCondition(key);
				if (condition != null)
				{
					if (as_value)
					{
						return condition.GetValue(vars);
					}
					return condition;
				}
				if (tmp_parent_rule?.vars != null)
				{
					Value var = tmp_parent_rule.vars.GetVar(key, vars, as_value);
					if (!var.is_unknown)
					{
						return var;
					}
				}
				for (GameRule gameRule = tmp_parent_rule; gameRule != null; gameRule = gameRule.parent_rule)
				{
					if (gameRule.context != null)
					{
						Value var2 = gameRule.context.GetVar(key, vars, as_value);
						if (!var2.is_unknown)
						{
							return var2;
						}
					}
				}
				return base.GetVar(key, vars, as_value);
			}
			}
		}

		public override string ToString()
		{
			string text = base.ToString();
			if (instant)
			{
				return text + $"[x{activations}/{activation_checks}]";
			}
			return text + $"[x{activations - deactivations} = {activations}/{activation_checks} - {deactivations}/{deactivation_checks}]";
		}

		public string Dump()
		{
			string text = base.id;
			text = ((!instant) ? (text + $": Active: {activations - deactivations} ({activations}/{activation_checks} - {deactivations}/{deactivation_checks})") : (text + $": Activations: {activations}/{activation_checks}"));
			if (start_triggers != null)
			{
				for (int i = 0; i < start_triggers.Count; i++)
				{
					Trigger.Def def = start_triggers[i];
					text += $"\n  {def.type} {def.name}: {def.activations}/{def.checks}/{def.calls}";
				}
			}
			return text;
		}
	}

	public class RefData : Data
	{
		public NID target_nid;

		public int urid;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(Rule " + urid + " of " + target_nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is GameRule gameRule))
			{
				return false;
			}
			target_nid = gameRule.target_obj;
			urid = gameRule.urid;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID(target_nid, "target");
			ser.Write7BitUInt(urid, "urid");
		}

		public override void Load(Serialization.IReader ser)
		{
			target_nid = ser.ReadNID("target");
			urid = ser.Read7BitUInt("urid");
		}

		public override object GetObject(Game game)
		{
			Object obj = target_nid.GetObj(game);
			if (obj == null)
			{
				return null;
			}
			return ObjRules.Get(obj, create: false)?.Find(urid);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is GameRule gameRule))
			{
				return false;
			}
			if (gameRule.urid != urid)
			{
				return false;
			}
			return true;
		}
	}

	public class FullData : RefData
	{
		public string rule_def_id;

		public float start_time_delta;

		public float stop_time_delta;

		public Data vars;

		public Data context;

		public List<Tuple<string, int>> fadingModsData;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is GameRule gameRule))
			{
				return false;
			}
			base.InitFrom(obj);
			rule_def_id = gameRule.def.id;
			Time time = gameRule.game.time;
			start_time_delta = ((gameRule.start_time == Time.Zero) ? (-1f) : (time - gameRule.start_time));
			stop_time_delta = ((gameRule.stop_time == Time.Zero) ? (-1f) : (time - gameRule.stop_time));
			vars = Data.Create(gameRule.vars);
			context = Data.Create(gameRule.context);
			if (gameRule.mods != null && gameRule.mods.Count > 0)
			{
				fadingModsData = new List<Tuple<string, int>>();
				for (int i = 0; i < gameRule.mods.Count; i++)
				{
					FadingModifier fadingModifier = gameRule.mods[i];
					if (!string.IsNullOrEmpty(fadingModifier?.stat?.def?.field?.key))
					{
						fadingModsData.Add(new Tuple<string, int>(fadingModifier.stat.def.field.key, fadingModifier.umid));
					}
				}
			}
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteStr(rule_def_id, "def");
			ser.WriteFloat(start_time_delta, "start_time");
			ser.WriteFloat(stop_time_delta, "stop_time");
			ser.WriteData(vars, "vars");
			ser.WriteData(context, "context");
			ser.Write7BitUInt((fadingModsData != null) ? fadingModsData.Count : 0, "mods_count");
			if (fadingModsData != null)
			{
				for (int i = 0; i < fadingModsData.Count; i++)
				{
					ser.WriteStr(fadingModsData[i].Item1, "mod_stat_", i);
					ser.Write7BitUInt(fadingModsData[i].Item2, "mod_umid_", i);
				}
			}
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			rule_def_id = ser.ReadStr("def");
			start_time_delta = ser.ReadFloat("start_time");
			stop_time_delta = ser.ReadFloat("stop_time");
			vars = ser.ReadData("vars");
			context = ser.ReadData("context");
			int num = ser.Read7BitUInt("mods_count");
			if (num > 0)
			{
				fadingModsData = new List<Tuple<string, int>>();
				for (int i = 0; i < num; i++)
				{
					string item = ser.ReadStr("mod_stat_", i);
					int item2 = ser.Read7BitUInt("mod_umid_", i);
					fadingModsData.Add(new Tuple<string, int>(item, item2));
				}
			}
		}

		public override object GetObject(Game game)
		{
			Object obj = target_nid.GetObj(game);
			if (obj == null)
			{
				return null;
			}
			GameRule gameRule = ObjRules.Get(obj, create: true).Find(urid);
			if (gameRule != null)
			{
				return gameRule;
			}
			Def def = Def.Find(game, rule_def_id);
			if (def == null)
			{
				return null;
			}
			gameRule = new GameRule(obj, null, def);
			gameRule.urid = urid;
			gameRule.ResolveMembers();
			return gameRule;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is GameRule gameRule))
			{
				return false;
			}
			if (gameRule.urid != urid)
			{
				Game.Log($"Attempting to apply rule data #{urid} to {gameRule}", Game.LogType.Error);
				return false;
			}
			if (gameRule.def.id != rule_def_id)
			{
				Game.Log("Attempting to apply " + rule_def_id + " data to " + gameRule.ToString(), Game.LogType.Error);
				return false;
			}
			if (gameRule.IsActive())
			{
				gameRule.Deactivate(null, from_state: true);
			}
			Time time = game.time;
			gameRule.start_time = ((start_time_delta < 0f) ? Time.Zero : (time - start_time_delta));
			gameRule.stop_time = ((stop_time_delta < 0f) ? Time.Zero : (time - stop_time_delta));
			gameRule.vars = Data.RestoreObject<Vars>(vars, game);
			gameRule.context = Data.RestoreObject<IVars>(context, game);
			gameRule.mods = null;
			Stats stats = gameRule.target_obj.GetStats();
			if (fadingModsData != null && stats != null)
			{
				gameRule.mods = new List<FadingModifier>();
				for (int i = 0; i < fadingModsData.Count; i++)
				{
					string item = fadingModsData[i].Item1;
					int item2 = fadingModsData[i].Item2;
					FadingModifier fadingModifier = stats.GetFadingModifier(item2);
					if (fadingModifier != null)
					{
						gameRule.mods.Add(fadingModifier);
						continue;
					}
					fadingModifier = new FadingModifier(game, null);
					stats.AddModifier(item, fadingModifier, from_state: true);
					fadingModifier.umid = item2;
					gameRule.mods.Add(fadingModifier);
				}
			}
			if (gameRule.IsActive())
			{
				gameRule.Activate(null, from_state: true);
			}
			return true;
		}
	}

	public int urid;

	public Def def;

	public Object target_obj;

	public Def.Target target_def;

	public GameRule parent_rule;

	public RuleTimer parent_timer;

	public Vars vars;

	public IVars context;

	public List<GameRule> child_rules;

	public List<RuleTimer> timers;

	public List<FadingModifier> mods;

	public List<Status> statuses;

	public Time start_time;

	public Time stop_time;

	public Trigger last_trigger = new Trigger();

	public Trigger start_trigger;

	public Trigger stop_trigger;

	public List<OutcomeDef> outcomes;

	private OutcomeDef tmp_outcome;

	public Game game => target_obj?.game;

	public GameRules game_rules => game?.game_rules;

	public string name => def?.id;

	public GameRule(Object target_obj, Def.Target target_def, Def def, GameRule parent_rule = null, RuleTimer parent_timer = null, IVars context = null)
	{
		this.def = def;
		this.target_obj = target_obj;
		this.target_def = target_def;
		this.parent_rule = parent_rule;
		this.parent_timer = parent_timer;
		this.context = context;
	}

	public void ResolveMembers()
	{
		if (target_def == null)
		{
			Object obj = target_obj;
			target_def = def.ResolveTarget(ref obj);
			if (target_def == null || obj != target_obj)
			{
				Game.Log($"{this}: Could not resolve target", Game.LogType.Error);
			}
		}
		if (parent_rule == null && def.parent_rule != null)
		{
			parent_rule = ObjRules.Find(target_obj, def.parent_rule);
			if (parent_rule == null)
			{
				Game.Log($"{this}: Could not resolve parent rule", Game.LogType.Error);
			}
		}
		if (parent_timer == null && def.parent_timer != null)
		{
			if (parent_rule != null)
			{
				parent_timer = parent_rule.FindTimer(def.parent_timer.def);
			}
			if (parent_timer == null)
			{
				Game.Log($"{this}: Could not resolve parent timer", Game.LogType.Error);
			}
		}
		ResolveTimers();
	}

	public bool IsActive()
	{
		if (stop_time == Time.Zero)
		{
			return start_time != Time.Zero;
		}
		return false;
	}

	public void Activate(Trigger trigger = null, bool from_state = false)
	{
		if (!from_state && IsActive())
		{
			Game.Log($"{this}: already started", Game.LogType.Error);
			return;
		}
		if (target_obj == null || !target_obj.IsValid())
		{
			Game.Log($"{this}: attempting to activate for invalid target: {target_obj}", Game.LogType.Error);
			return;
		}
		last_trigger.Set(trigger, null);
		last_trigger.named_vars = trigger?.named_vars;
		if (!from_state)
		{
			start_trigger = new Trigger(trigger);
			start_time = game.time;
			stop_time = Time.Zero;
		}
		if (def.instant)
		{
			if (def.log >= 1)
			{
				Log("executing ", $"trigger: {trigger}");
			}
			if (!from_state)
			{
				SetVars();
				ObjRules.OnActivate(this, from_state);
				ApplyOutcomes(def.start_outcomes);
				NotifyContexts("executed", trigger);
				target_obj.NotifyListeners(name, "executed");
			}
			stop_trigger = new Trigger(trigger);
			stop_time = game.time;
			if (!from_state)
			{
				ObjRules.OnDeactivate(this, from_state);
			}
			last_trigger.Clear();
			return;
		}
		if (def.log >= 1)
		{
			Log("starting ", $"trigger: {trigger}");
		}
		if (parent_timer != null)
		{
			if (parent_timer.child_rules == null)
			{
				parent_timer.child_rules = new List<GameRule>();
			}
			parent_timer.child_rules.Add(this);
		}
		else if (parent_rule != null)
		{
			if (parent_rule.child_rules == null)
			{
				parent_rule.child_rules = new List<GameRule>();
			}
			parent_rule.child_rules.Add(this);
		}
		if (!from_state)
		{
			SetVars();
			ObjRules.OnActivate(this);
		}
		if (parent_rule != null)
		{
			game_rules?.DelListeners(def, parent_rule, parent_timer);
		}
		game_rules?.AddListeners(this);
		if (!from_state)
		{
			StartTimers();
		}
		AddMods();
		if (!from_state)
		{
			AddStatuses();
			ApplyOutcomes(def.start_outcomes);
			NotifyContexts("started", trigger);
			target_obj.NotifyListeners(name, "started");
		}
		last_trigger.Clear();
	}

	public void Deactivate(Trigger trigger = null, bool from_state = false)
	{
		if (!from_state && !IsActive())
		{
			return;
		}
		def.deactivations++;
		if (def.log >= 1 && !def.instant)
		{
			Log("stopping ", $"trigger: {trigger}");
		}
		last_trigger.Set(trigger, null);
		last_trigger.named_vars = trigger?.named_vars;
		DelMods();
		if (!from_state)
		{
			DelStatuses();
			ApplyOutcomes(def.stop_outcomes);
		}
		game_rules?.DelListeners(this);
		if (!from_state)
		{
			stop_trigger = new Trigger(trigger);
			stop_time = game.time;
			DeactivateChildRules();
			StopTimers();
		}
		if (parent_timer != null)
		{
			parent_timer.child_rules.Remove(this);
			if (parent_timer.child_rules.Count == 0)
			{
				parent_timer.child_rules = null;
			}
		}
		else if (parent_rule != null)
		{
			parent_rule.child_rules.Remove(this);
			if (parent_rule.child_rules.Count == 0)
			{
				parent_rule.child_rules = null;
			}
		}
		if (!from_state)
		{
			NotifyContexts("stopped", trigger);
			target_obj.NotifyListeners(name, "stopped");
			ObjRules.OnDeactivate(this);
		}
		last_trigger.Clear();
		if (target_obj != null && target_obj.IsValid() && parent_rule != null && parent_rule.IsActive() && (parent_timer == null || parent_rule.timers.Contains(parent_timer)))
		{
			game_rules?.AddListeners(def, parent_rule, parent_timer);
		}
	}

	private void NotifyContexts(string message, object param)
	{
		for (GameRule gameRule = this; gameRule != null; gameRule = gameRule.parent_rule)
		{
			if (gameRule.context is IListener listener)
			{
				listener.OnMessage(this, message, param);
			}
		}
	}

	public void OnTrigger(Trigger trigger, GameRule parent_rule = null, RuleTimer parent_timer = null)
	{
		if (!IsActive() || def.instant)
		{
			return;
		}
		if (last_trigger.Equal(trigger))
		{
			Game.Log($"{this}: double check for trigger {trigger}", Game.LogType.Warning);
			return;
		}
		last_trigger.Set(trigger, this);
		if (!last_trigger.Validate(game, this))
		{
			return;
		}
		if (last_trigger.def != null)
		{
			Object obj = def.DecideTargetObj(last_trigger, parent_rule);
			def.ResolveTarget(ref obj);
			if (obj != target_obj)
			{
				return;
			}
		}
		def.deactivation_checks++;
		if (def.log >= 2)
		{
			Log("checking ", $"trigger: {last_trigger}");
		}
		if ((def.stop_condition != null) ? def.stop_condition.GetValue(this).Bool() : ((def.active_condition == null) ? def.instant : (!def.active_condition.GetValue(this).Bool())))
		{
			Deactivate(last_trigger);
		}
	}

	public static GameRule FindRule(List<GameRule> rules, Def def)
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

	private void DeactivateChildRules()
	{
		if (child_rules != null)
		{
			for (int num = child_rules.Count - 1; num >= 0; num--)
			{
				child_rules[num].Deactivate();
			}
			child_rules = null;
		}
	}

	public RuleTimer FindTimer(Timer.Def def)
	{
		if (timers == null)
		{
			return null;
		}
		for (int i = 0; i < timers.Count; i++)
		{
			RuleTimer ruleTimer = timers[i];
			if (ruleTimer.def.def.name == def.name)
			{
				return ruleTimer;
			}
		}
		return null;
	}

	private void StartTimers()
	{
		if (def.timers != null)
		{
			timers = new List<RuleTimer>(def.timers.Count);
			for (int i = 0; i < def.timers.Count; i++)
			{
				RuleTimer.Def tdef = def.timers[i];
				StartTimer(tdef);
			}
		}
	}

	private void ResolveTimers()
	{
		if (this.def.timers == null)
		{
			return;
		}
		timers = new List<RuleTimer>(this.def.timers.Count);
		for (int i = 0; i < this.def.timers.Count; i++)
		{
			RuleTimer.Def def = this.def.timers[i];
			Timer timer = Timer.Find(target_obj, def.def.name);
			if (timer != null)
			{
				SetupTimer(def, timer);
			}
		}
	}

	private void StopTimers()
	{
		if (timers != null)
		{
			for (int i = 0; i < timers.Count; i++)
			{
				RuleTimer timer = timers[i];
				StopTimer(timer, remove_from_list: false);
			}
			timers = null;
		}
	}

	private void StartTimer(RuleTimer.Def tdef)
	{
		new Timer(target_obj, tdef.def);
	}

	private RuleTimer SetupTimer(RuleTimer.Def tdef, Timer t)
	{
		RuleTimer ruleTimer = new RuleTimer();
		ruleTimer.def = tdef;
		ruleTimer.timer = t;
		timers.Add(ruleTimer);
		game_rules?.AddTimerChildRulesListeners(this, ruleTimer);
		return ruleTimer;
	}

	private void StopTimer(RuleTimer timer, bool remove_from_list = true)
	{
		if (remove_from_list)
		{
			timers?.Remove(timer);
		}
		timer.timer.Stop();
	}

	public void OnTimerStarted(Timer t)
	{
		if (IsActive() && FindTimer(t.def) == null)
		{
			SetupTimer(t.def.rtdef, t);
		}
	}

	public void OnTimerStopped(Timer t)
	{
		if (t == null || timers == null)
		{
			return;
		}
		bool flag = t.obj.IsAuthority();
		for (int i = 0; i < timers.Count; i++)
		{
			RuleTimer ruleTimer = timers[i];
			if (ruleTimer.timer == t)
			{
				timers.RemoveAt(i);
				game_rules?.DelTimerChildRulesListeners(this, ruleTimer);
				if (flag)
				{
					DeactivateChildRules(ruleTimer);
				}
				if (ruleTimer.def.stop_parent && flag)
				{
					last_trigger.Set(null, t.obj, "timer_stopped", t, 0, this);
					Deactivate(last_trigger);
				}
				game_rules?.CleanUp();
				break;
			}
		}
	}

	public void OnTimerTick(Timer t)
	{
		RuleTimer ruleTimer = FindTimer(t.def);
		if (ruleTimer == null)
		{
			Game.Log($"{this}: triggered from unknown timer: {t}", Game.LogType.Error);
			return;
		}
		if (ruleTimer.def.trigger_parent)
		{
			last_trigger.Set(null, t.obj, t.name, t, 0, this);
			OnTrigger(last_trigger, parent_rule, parent_timer);
			if (!IsActive())
			{
				return;
			}
		}
		ApplyOutcomes(ruleTimer.def.outcomes);
	}

	private void DeactivateChildRules(RuleTimer timer)
	{
		if (timer.child_rules != null)
		{
			for (int num = timer.child_rules.Count - 1; num >= 0; num--)
			{
				timer.child_rules[num].Deactivate();
			}
			timer.child_rules = null;
		}
	}

	private void SetVars()
	{
		if (def.var_fields == null)
		{
			return;
		}
		this.vars = null;
		Vars vars = null;
		for (int i = 0; i < def.var_fields.Count; i++)
		{
			DT.Field field = def.var_fields[i];
			if (field.key == "log" && def.log <= 0)
			{
				continue;
			}
			Value val = field.Value(this);
			if (field.key == "log")
			{
				if (vars == null)
				{
					vars = new Vars();
				}
				vars.Set(field.key, val);
			}
			else
			{
				if (this.vars == null)
				{
					this.vars = new Vars();
				}
				this.vars.Set(field.key, val);
			}
		}
		if (def.log > 0 && (this.vars != null || vars != null))
		{
			string text = "";
			if (this.vars != null)
			{
				text += this.vars.Dump("\n  ", "\n  ");
			}
			if (vars != null)
			{
				text += vars.Dump("\n  ", "\n  ");
			}
			Log("", "vars:" + text);
		}
	}

	private void AddMods()
	{
		if (this.def.mods == null || !target_obj.IsAuthority())
		{
			return;
		}
		mods = new List<FadingModifier>(this.def.mods.Count);
		for (int i = 0; i < this.def.mods.Count; i++)
		{
			FadingModifier.Def def = this.def.mods[i];
			FadingModifier fadingModifier = FadingModifier.Add(target_obj, def, this);
			if (fadingModifier != null)
			{
				mods.Add(fadingModifier);
			}
		}
		target_obj.SendSubstate<Object.RulesState.RuleState>(urid);
	}

	private void DelMods()
	{
		if (mods != null && target_obj.IsAuthority())
		{
			for (int i = 0; i < mods.Count; i++)
			{
				FadingModifier fadingModifier = mods[i];
				fadingModifier.stat?.DelModifier(fadingModifier);
			}
			mods = null;
			target_obj.SendSubstate<Object.RulesState.RuleState>(urid);
		}
	}

	private void AddStatuses()
	{
		if (this.def.statuses != null)
		{
			statuses = new List<Status>(this.def.statuses.Count);
			for (int i = 0; i < this.def.statuses.Count; i++)
			{
				Status.Def def = this.def.statuses[i];
				target_obj.AddStatus(def);
			}
		}
	}

	private void DelStatuses()
	{
		if (statuses != null)
		{
			for (int i = 0; i < statuses.Count; i++)
			{
				Status status = statuses[i];
				target_obj.DelStatus(status);
			}
			statuses = null;
		}
	}

	private void ApplyOutcomes(OutcomeDef outcomes_def)
	{
		if (outcomes_def == null)
		{
			return;
		}
		List<OutcomeDef> list = outcomes_def.DecideOutcomes(game, this);
		if (outcomes_def == def.start_outcomes)
		{
			outcomes = list;
		}
		if (list.Count == 0)
		{
			return;
		}
		List<OutcomeDef> list2 = OutcomeDef.UniqueOutcomes(list);
		Event obj = new Event(target_obj, "rule_outcomes", this);
		obj.outcomes = list;
		obj.vars = new Vars(this);
		if (outcomes_def == def.stop_outcomes)
		{
			obj.vars.Set("outcomes_def_key", "stop_outcomes_def");
		}
		else if (outcomes_def == def.start_outcomes)
		{
			obj.vars.Set("outcomes_def_key", "outcomes_def");
		}
		else
		{
			obj.vars.Set("outcomes_def_key", "timers");
			int val = -1;
			for (int i = 0; i < timers.Count; i++)
			{
				if (timers[i].def.outcomes == outcomes_def)
				{
					val = i;
					break;
				}
			}
			obj.vars.Set("outcome_timer_idx", val);
		}
		OutcomeDef.PrecalculateValues(list2, game, obj.vars, obj.vars);
		target_obj.FireEvent(obj);
		for (int j = 0; j < list2.Count; j++)
		{
			tmp_outcome = list2[j];
			if (!tmp_outcome.Apply(game, this) && tmp_outcome.key != "success")
			{
				Game.Log($"{this}: unhandled outcome: {tmp_outcome.id}", Game.LogType.Warning);
			}
		}
		tmp_outcome = null;
	}

	public Value GetVar(string key, IVars _vars = null, bool as_value = true)
	{
		if (key == target_def?.name)
		{
			return target_obj;
		}
		if (tmp_outcome != null)
		{
			Value var = tmp_outcome.GetVar(key, _vars, as_value);
			if (!var.is_unknown)
			{
				return var;
			}
		}
		if (vars != null)
		{
			Value var2 = vars.GetVar(key, _vars, as_value);
			if (!var2.is_unknown)
			{
				return var2;
			}
		}
		for (GameRule gameRule = this; gameRule != null; gameRule = gameRule.parent_rule)
		{
			if (gameRule.context != null)
			{
				Value var3 = gameRule.context.GetVar(key, _vars, as_value);
				if (!var3.is_unknown)
				{
					return var3;
				}
			}
		}
		switch (key)
		{
		case "rule":
			return this;
		case "target":
		case "owner":
			return target_obj;
		case "src_kingdom":
		case "tgt_kingdom":
			return target_obj.GetKingdom();
		case "is_active":
			return IsActive();
		case "parent_rule":
			return parent_rule;
		case "parent_timer":
			return new Value(parent_timer);
		case "outcomes_def":
			return new Value(def.start_outcomes);
		case "timer_defs":
			return new Value(def.timers);
		case "stop_outcomes_def":
			return new Value(def.stop_outcomes);
		case "trigger":
			return new Value(last_trigger);
		case "start_trigger":
			return new Value(start_trigger);
		case "stop_trigger":
			return new Value(stop_trigger);
		case "been_active":
			return start_time != Time.Zero;
		case "time_since_start":
			return game.time - start_time;
		case "time_since_stop":
			if (IsActive())
			{
				return Value.Null;
			}
			return game.time - stop_time;
		case "time_still_active":
			if (!IsActive())
			{
				return Value.Null;
			}
			return game.time - start_time;
		case "active_duration":
			if (IsActive())
			{
				return game.time - start_time;
			}
			return stop_time - start_time;
		default:
			return def.GetVar(key, this, as_value);
		}
	}

	public void SetVar(string key, Value value)
	{
		if (vars == null)
		{
			vars = new Vars();
		}
		vars.Set(key, value);
	}

	public override bool IsRefSerializable()
	{
		if (target_obj != null && target_obj.IsRefSerializable())
		{
			return urid > 0;
		}
		return false;
	}

	public void Log(string prefix, string msg)
	{
		if (def.log > 0)
		{
			Game.Log($"{prefix}{this}: {msg}", Game.LogType.Message);
		}
	}

	public override string ToString()
	{
		string text = (IsActive() ? "active" : "inactive");
		return $"[{urid}] {text} rule {name}({target_obj})";
	}

	public string Dump()
	{
		return Dump(null);
	}

	public string Dump(Object default_target)
	{
		string text = (IsActive() ? "active" : "inactive") + " rule " + name;
		if (default_target == null || target_obj != default_target)
		{
			text += $"({target_obj})";
		}
		if (context != null)
		{
			text += $"\n    context: {context}";
		}
		if (vars != null)
		{
			text += vars.Dump("\n    ", "\n    ");
		}
		if (target_obj == null)
		{
			return text;
		}
		Time time = game.time;
		if (start_time != Time.Zero)
		{
			text = ((!def.instant) ? (text + $"\n    started: {time - start_time}s ago, trigger: {start_trigger}") : (text + $"\n    executed: {time - start_time}s ago, trigger: {start_trigger}"));
		}
		if (stop_time != Time.Zero && !def.instant)
		{
			text += $"\n    stopped: {time - stop_time}s ago, trigger: {stop_trigger}";
		}
		if (stop_time != Time.Zero && def.timeout > 0f)
		{
			text += $"\n    timeout: after {stop_time + def.timeout - time}s";
		}
		return text;
	}
}
