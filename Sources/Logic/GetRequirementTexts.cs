using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logic;

public class Action : Updateable, IVars, IListener
{
	public class Def : Logic.Def
	{
		public enum MissionType
		{
			Ignore,
			NotOnMission,
			OnMission
		}

		public string Name;

		public Type owner_type;

		public MissionType mission = MissionType.NotOnMission;

		public bool prisoner;

		public bool while_leading_army;

		public bool secondary;

		public int severity_id;

		public string target;

		public List<string> arg_types;

		public Condition condition;

		public Condition enabled_condition;

		public string unique_id;

		public string unique_status_id;

		public DT.Field prepare_duration;

		public DT.Field prepare_message;

		public DT.Field prepare_tick;

		public float initial_progress;

		public DT.Field progress_on_tick;

		public SuccessAndFail.Def progress_factors;

		public float max_progress;

		public DT.Field cost;

		public bool can_be_canceled = true;

		public Opportunity.Def opportunity;

		public List<DT.Field> requirements;

		public bool has_sf_points_requirement;

		public SuccessAndFail.Def success_fail;

		public SuccessAndFail.Def reveal_factors;

		public OutcomeDef outcomes;

		public DT.Field ai_validate;

		public DT.Field ai_eval_target;

		public ProsAndCons.Def use_pc_def;

		public KingdomAI.Expense.Category expense_category;

		public KingdomAI.Expense.Priority ai_expense_priority = KingdomAI.Expense.Priority.Normal;

		public string upkeep_subcategory;

		public List<ActiveListener.Def> active_listeners;

		public List<CharacterClass.Def> available_in_class_defs;

		public List<SkillsTable.ActionDef> skill_dependencies;

		public Status.Def show_in_status;

		public int ui_order;

		public bool invalidate_incomes;

		private static string[] valid_arg_types = new string[14]
		{
			"int", "float", "string", "Offer", "skill", "tradition", "own_town", "kingdom", "other_kingdom", "enemy_kingdom",
			"prisoner", "fiancee", "trade_center", "importable_good"
		};

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			Name = field.GetString("name", null, dt_def.path);
			string text = field.GetString("owner_type");
			if (!string.IsNullOrEmpty(text))
			{
				owner_type = Type.GetType("Logic." + text);
				if (owner_type == null)
				{
					Game.Log(field.Path(include_file: true) + ".owner_type: unknown type '" + text + "'", Game.LogType.Error);
				}
			}
			if (!Enum.TryParse<MissionType>(field.GetString("mission"), out mission))
			{
				mission = MissionType.NotOnMission;
			}
			prisoner = field.GetBool("prisoner", null, prisoner);
			while_leading_army = field.GetBool("while_leading_army", null, while_leading_army);
			secondary = field.GetBool("secondary", null, secondary);
			switch (field.GetString("severity", null, "Low"))
			{
			case "Low":
				severity_id = 0;
				break;
			case "Mid":
				severity_id = 1;
				break;
			case "High":
				severity_id = 2;
				break;
			default:
				Game.Log(field.Path(include_file: true) + ": Unknown action severity (must be 'Low', 'Mid' or 'High')", Game.LogType.Error);
				severity_id = 0;
				break;
			}
			target = field.GetString("target", null, target);
			LoadArgs(game);
			condition = Condition.Load(field.FindChild("condition"));
			enabled_condition = Condition.Load(field.FindChild("enabled_condition"));
			DT.Field field2 = field.FindChild("unique");
			if (field2 != null)
			{
				unique_id = field2.String(null, base.id);
			}
			else
			{
				unique_id = null;
			}
			unique_status_id = field.GetString("unique_status");
			prepare_duration = field.FindChild("prepare_duration");
			prepare_tick = field.FindChild("prepare_tick");
			prepare_message = field.FindChild("prepare_message");
			initial_progress = field.GetFloat("initial_progress");
			progress_on_tick = field.FindChild("progress_on_tick");
			DT.Field field3 = field.FindChild("progress_factors");
			if (field3 != null)
			{
				progress_factors = new SuccessAndFail.Def(game, field3);
				if (!progress_factors.valid)
				{
					progress_factors = null;
				}
			}
			else
			{
				progress_factors = null;
			}
			max_progress = field.GetFloat("max_progress");
			if (max_progress > 0f && prepare_duration != null && (!prepare_duration.value.is_number || prepare_duration.Float() != 0f))
			{
				Game.Log(prepare_duration.Path(include_file: true) + ": Action '" + Name + "' has both prepare_duration and max_progress", Game.LogType.Warning);
			}
			can_be_canceled = field.GetBool("can_be_canceled", null, can_be_canceled);
			cost = field.FindChild("cost");
			opportunity = Opportunity.Def.Load(game, field.FindChild("opportunity"));
			requirements = null;
			has_sf_points_requirement = false;
			DT.Field field4 = field.FindChild("requirements");
			if (field4 != null)
			{
				List<string> list = field4.Keys();
				if (list.Count > 0)
				{
					requirements = new List<DT.Field>(list.Count);
					for (int i = 0; i < list.Count; i++)
					{
						string text2 = list[i];
						DT.Field field5 = field4.FindChild(text2);
						if (field5.based_on == null)
						{
							field5.based_on = field.FindChild("requirement_defaults." + field5.key);
						}
						requirements.Add(field5);
						if (text2 == "sf_points")
						{
							has_sf_points_requirement = true;
						}
					}
				}
			}
			DT.Field field6 = field.FindChild("success_fail");
			if (field6 != null)
			{
				success_fail = new SuccessAndFail.Def(game, field6);
				if (!success_fail.valid)
				{
					success_fail = null;
				}
			}
			else
			{
				success_fail = null;
			}
			DT.Field field7 = field.FindChild("reveal_factors");
			if (field7 != null)
			{
				reveal_factors = new SuccessAndFail.Def(game, field7);
				if (!reveal_factors.valid)
				{
					reveal_factors = null;
				}
			}
			else
			{
				reveal_factors = null;
			}
			DT.Field field8 = field.FindChild("outcomes");
			if (field8 != null)
			{
				DT.Field defaults = field.FindChild("outcome_defaults");
				outcomes = new OutcomeDef(game, field8, defaults);
			}
			else
			{
				outcomes = null;
			}
			ai_validate = field.FindChild("ai_validate");
			ai_eval_target = field.FindChild("ai_eval_target");
			expense_category = KingdomAI.Expense.Category.None;
			string text3 = field.GetString("expense_category");
			if (!string.IsNullOrEmpty(text3) && !Enum.TryParse<KingdomAI.Expense.Category>(text3, out expense_category))
			{
				Game.Log(field.Path(include_file: true) + ": Unknown expense category: '" + text3 + "'", Game.LogType.Error);
			}
			ai_expense_priority = KingdomAI.Expense.Priority.Normal;
			string text4 = field.GetString("ai_expense_priority");
			if (!string.IsNullOrEmpty(text4) && !Enum.TryParse<KingdomAI.Expense.Priority>(text4, out ai_expense_priority))
			{
				Game.Log(field.Path(include_file: true) + ": Unknown expense priority: '" + text4 + "'", Game.LogType.Error);
			}
			upkeep_subcategory = field.GetString("upkeep_subcategory", null, null);
			active_listeners = null;
			List<DT.Field> list2 = field.FindChild("cancel_events")?.Children();
			if (list2 != null)
			{
				for (int j = 0; j < list2.Count; j++)
				{
					DT.Field field9 = list2[j];
					if (string.IsNullOrEmpty(field9.key))
					{
						continue;
					}
					ActiveListener.Def def = LoadActiveListenerDef(field9, this);
					if (def != null)
					{
						if (active_listeners == null)
						{
							active_listeners = new List<ActiveListener.Def>();
						}
						active_listeners.Add(def);
					}
				}
			}
			invalidate_incomes = field.GetBool("invalidate_incomes", null, invalidate_incomes);
			ui_order = field.GetInt("ui_order");
			skill_dependencies = null;
			Tracker.AddActionDef(this);
			return true;
		}

		private void LoadArgs(Game game)
		{
			DT.Field field = base.field;
			DT.Field field2 = field.FindChild("args");
			if (field2 == null)
			{
				return;
			}
			int num = field2.NumValues();
			if (num <= 0)
			{
				return;
			}
			arg_types = new List<string>(num);
			for (int i = 0; i < num; i++)
			{
				string text = field2.String(i);
				arg_types.Add(text);
				if (text.EndsWith("?", StringComparison.Ordinal))
				{
					text = text.Substring(0, text.Length - 1);
				}
				if (Array.IndexOf(valid_arg_types, text) < 0 && Serialization.ObjectTypeInfo.Get(text) == null)
				{
					game.Error(field.Path(include_file: true) + ": Invalid action argument type '" + text + "'");
				}
			}
		}

		public override bool Validate(Game game)
		{
			if (base.field == null)
			{
				return false;
			}
			ResolveProsAndCons(game);
			DT.Field field = base.field.GetRef("show_in_status");
			if (field == null)
			{
				return true;
			}
			show_in_status = field.def?.def as Status.Def;
			if (show_in_status == null)
			{
				Game.Log(base.field.Path(include_file: true) + ".show_in_status: invalid status reference", Game.LogType.Error);
				return false;
			}
			if (show_in_status.actions == null)
			{
				show_in_status.actions = new List<Def>();
			}
			show_in_status.actions.Add(this);
			return true;
		}

		private ProsAndCons.Def FindPCDef(Game game, string threshold)
		{
			ProsAndCons.Def def = game.defs.Find<ProsAndCons.Def>(base.id);
			if (def != null && def.thresholds_field?.FindChild(threshold) != null)
			{
				return def;
			}
			def = game.defs.Find<ProsAndCons.Def>("PC_" + base.id);
			if (def != null && def.thresholds_field?.FindChild(threshold) != null)
			{
				return def;
			}
			return null;
		}

		private void ResolveProsAndCons(Game game)
		{
			use_pc_def = FindPCDef(game, "use");
		}
	}

	public enum State
	{
		Inactive,
		PickingTarget,
		PickingArgs,
		Preparing,
		Running,
		Finishing
	}

	public struct ActiveListener
	{
		public class Def
		{
			public DT.Field field;

			public List<string> messages = new List<string>();

			public string obj_type;

			public DT.Field obj_field;

			public DT.Field cancel_condition_field;

			public string cancelled_message;

			public string cancel_reason;

			public List<OutcomeDef> cancel_outcomes;

			public string MessagesToString()
			{
				string text = "";
				for (int i = 0; i < messages.Count; i++)
				{
					string text2 = messages[i];
					if (i > 0)
					{
						text += " | ";
					}
					text += text2;
				}
				return text;
			}

			public override string ToString()
			{
				return (obj_field?.value_str ?? obj_type) + "." + MessagesToString() + " = " + cancel_condition_field?.ValueStr();
			}
		}

		public Def def;

		public Object obj;

		public override string ToString()
		{
			return $"{obj}.{def.MessagesToString()}";
		}
	}

	public static class Tracker
	{
		public struct Track_ActionStats
		{
			public bool has_outcomes;

			public bool disabled_in_ai;

			public long count;

			public long count_success;

			public long count_fail;

			public long count_running;
		}

		public static bool enabled = true;

		public static Dictionary<string, Track_ActionStats> stats = new Dictionary<string, Track_ActionStats>();

		public static void AddActionDef(Def def)
		{
			if (def?.field != null)
			{
				stats.TryGetValue(def.field.key, out var value);
				if (def.ai_validate != null && def.ai_validate.value.type == Value.Type.Int && def.ai_validate.value.int_val == 0)
				{
					value.disabled_in_ai = true;
				}
				value.has_outcomes = def.outcomes != null;
				stats[def.field.key] = value;
			}
		}

		private static void Record(Dictionary<string, Track_ActionStats> stats, Action action, bool success)
		{
			string text = action.def?.field?.key;
			if (stats == null || text == null)
			{
				return;
			}
			if (!stats.TryGetValue(text, out var value))
			{
				value.count = 0L;
				value.count_success = 0L;
				value.count_fail = 0L;
				stats.Add(text, value);
			}
			if (action.state == State.Preparing)
			{
				value.count++;
				value.count_running++;
			}
			else
			{
				value.count_running--;
				if (success)
				{
					value.count_success++;
				}
				else
				{
					value.count_fail++;
				}
			}
			stats[text] = value;
		}

		private static bool IsFromPlayer(Action a)
		{
			if (a == null)
			{
				return false;
			}
			if (a.own_kingdom != null && a.own_kingdom.is_player)
			{
				return true;
			}
			if (a.owner?.GetKingdom() != null && a.owner.GetKingdom().is_player)
			{
				return true;
			}
			return false;
		}

		public static void Track(Action a, bool success = true)
		{
			if (enabled && a != null && !IsFromPlayer(a))
			{
				Record(stats, a, success);
			}
		}

		public static string Dump(Game game, Dictionary<string, Track_ActionStats> stats)
		{
			if (stats == null)
			{
				return "no stats";
			}
			StringBuilder stringBuilder = new StringBuilder();
			List<KeyValuePair<string, Track_ActionStats>> list = stats.ToList();
			list.Sort(delegate(KeyValuePair<string, Track_ActionStats> a, KeyValuePair<string, Track_ActionStats> b)
			{
				int num2 = b.Value.count.CompareTo(a.Value.count);
				if (num2 != 0)
				{
					return num2;
				}
				num2 = a.Value.disabled_in_ai.CompareTo(b.Value.disabled_in_ai);
				return (num2 != 0) ? num2 : a.Key.CompareTo(b.Key);
			});
			long num = 0L;
			if (game != null)
			{
				num = game.session_time.milliseconds;
			}
			TimeSpan timeSpan = TimeSpan.FromMilliseconds(num);
			string text = $"{timeSpan.Hours:D2}h {timeSpan.Minutes:D2}m {timeSpan.Seconds:D2}s";
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Actions stats after: " + text);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Action Name: Count_Total (Successful_count; Failed_count; CurrentlyRunning_count)");
			stringBuilder.AppendLine();
			foreach (KeyValuePair<string, Track_ActionStats> item in list)
			{
				stringBuilder.Append($"{item.Key} : {item.Value.count}");
				if (item.Value.count > 0 && item.Value.has_outcomes)
				{
					stringBuilder.Append($" (s-{item.Value.count_success}; f-{item.Value.count_fail}; r-{item.Value.count_running})");
				}
				if (item.Value.disabled_in_ai)
				{
					stringBuilder.Append(" disabled as random ai action");
				}
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}
	}

	public delegate Action CreateAction(Object owner, Def def);

	public class RefData : Data
	{
		public NID owner_nid;

		public string action_def_id;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(" + action_def_id + " of " + owner_nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Action action))
			{
				return false;
			}
			owner_nid = action.owner;
			action_def_id = action.def.id;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID(owner_nid, "owner");
			ser.WriteStr(action_def_id, "def");
		}

		public override void Load(Serialization.IReader ser)
		{
			owner_nid = ser.ReadNID("owner");
			action_def_id = ser.ReadStr("def");
		}

		public override object GetObject(Game game)
		{
			return owner_nid.GetObj(game)?.GetComponent<Actions>()?.Find(action_def_id);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Action action))
			{
				return false;
			}
			if (action.def.id != action_def_id)
			{
				return false;
			}
			return true;
		}
	}

	public class FullData : RefData
	{
		public State state;

		public NID target_nid;

		public List<Data> args;

		public float state_elapsed = -1f;

		public float state_duration = -1f;

		public float tick_after = -1f;

		public float progress;

		public float best_progress;

		public NID target_kingdom_nid;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Action action))
			{
				return false;
			}
			base.InitFrom(obj);
			state = action.state;
			action.GetStateTimes(out state_elapsed, out state_duration);
			if (action.next_tick_time > action.game.time)
			{
				tick_after = action.next_tick_time - action.game.time;
			}
			else
			{
				tick_after = 0f;
			}
			if (action.def.max_progress > 0f)
			{
				progress = action.progress / action.def.max_progress;
				best_progress = action.best_progress / action.def.max_progress;
			}
			else
			{
				best_progress = (progress = 0f);
			}
			target_nid = action.target;
			if (action.args != null && action.args.Count > 0)
			{
				args = new List<Data>();
				for (int i = 0; i < action.args.Count; i++)
				{
					args.Add(action.args[i].CreateData());
				}
			}
			target_kingdom_nid = action.target_kingdom;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.Write7BitUInt((int)state, "state");
			if (state >= State.Preparing)
			{
				ser.WriteNID(target_nid, "target");
				int num = ((args != null) ? args.Count : 0);
				ser.Write7BitUInt(num, "args_count");
				for (int i = 0; i < num; i++)
				{
					ser.WriteData(args[i], "args_", i);
				}
				ser.WriteFloat(state_duration, "state_duration");
				if (state_duration > 0f)
				{
					ser.WriteFloat(state_elapsed, "state_elapsed");
				}
				ser.WriteFloat(tick_after, "tick_after");
				ser.WriteFloat(progress, "progress");
				ser.WriteFloat(best_progress, "best_progress");
				ser.WriteNID<Kingdom>(target_kingdom_nid, "target_kingdom");
			}
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			state = (State)ser.Read7BitUInt("state");
			if (state < State.Preparing)
			{
				return;
			}
			target_nid = ser.ReadNID("target");
			int num = ser.Read7BitUInt("args_count");
			if (num > 0)
			{
				args = new List<Data>();
				for (int i = 0; i < num; i++)
				{
					Data data = ser.ReadData("args_", i);
					if (data != null)
					{
						args.Add(data);
					}
				}
			}
			state_duration = ser.ReadFloat("state_duration");
			if (state_duration > 0f)
			{
				state_elapsed = ser.ReadFloat("state_elapsed");
			}
			tick_after = ser.ReadFloat("tick_after");
			progress = ser.ReadFloat("progress");
			best_progress = ser.ReadFloat("best_progress");
			target_kingdom_nid = ser.ReadNID<Kingdom>("target_kingdom");
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Action action))
			{
				return false;
			}
			if (action.def.id != action_def_id)
			{
				Game.Log("Attempting to apply " + action_def_id + " data to " + action.ToString(), Game.LogType.Error);
				return false;
			}
			base.ApplyTo(obj, game);
			if (this.state < State.Preparing)
			{
				action.Cancel();
				return true;
			}
			State state = action.state;
			action.OnApplyLeaveState();
			action.NotifyListeners("leave_state", this.state);
			action.state = this.state;
			if (state_duration > 0f && state_elapsed < state_duration)
			{
				action.state_start_time = action.game.time - state_elapsed;
				action.state_end_time = action.state_start_time + state_duration;
			}
			else
			{
				action.state_end_time = (action.state_start_time = Time.Zero);
			}
			if (tick_after > 0f)
			{
				action.next_tick_time = action.game.time + tick_after;
			}
			else
			{
				action.next_tick_time = Time.Zero;
			}
			action.progress = progress * action.def.max_progress;
			action.best_progress = best_progress * action.def.max_progress;
			action.Reschedule();
			action.target = target_nid.GetObj(action.game);
			if (args != null)
			{
				action.args = new List<Value>();
				for (int i = 0; i < args.Count; i++)
				{
					Value value = args[i].GetValue(game);
					if (!(value == Value.Unknown))
					{
						action.args.Add(value);
					}
				}
			}
			action.target_kingdom = target_kingdom_nid.Get<Kingdom>(action.game);
			action.NotifyListeners("state_time_changed", this.state);
			action.NotifyListeners("enter_state", state);
			action.OnApplyEnterState();
			action.UpdateActiveListeners();
			return true;
		}
	}

	public delegate string GetRequirementTexts(Action action);

	public delegate string ListTexts(List<Character> characters);

	public Def def;

	public Actions actions;

	public State state;

	public Time state_start_time = Time.Zero;

	public Time state_end_time = Time.Zero;

	public Time next_tick_time = Time.Zero;

	public float progress;

	public float best_progress;

	private Object _target;

	public List<Value> args;

	public Kingdom target_kingdom;

	public IListener visuals;

	private List<IListener> listeners;

	private List<ActiveListener> active_listeners;

	public List<OutcomeDef> forced_outcomes;

	public List<OutcomeDef> outcomes;

	public List<OutcomeDef> unique_outcomes;

	public Vars outcome_vars;

	public State cancel_voice_first_state = State.Preparing;

	public State cancel_voice_last_state = State.Running;

	private static Vars tmp_vars = new Vars();

	public static bool enable_outcome_effects = true;

	public static float force_prepare_duration = 0f;

	public static bool no_requirement_checks = false;

	public static GetRequirementTexts get_requirement_texts = null;

	public static ListTexts get_prisoners_text = null;

	public Game game => owner.game;

	public Object owner => actions?.obj;

	public Character own_character => owner as Character;

	public virtual Kingdom own_kingdom => owner?.GetKingdom();

	public Status own_status { get; set; }

	public bool is_active => state >= State.Preparing;

	public Object target
	{
		get
		{
			return _target;
		}
		set
		{
			_target = value;
			target_kingdom = CalcTargetKingdom(_target);
		}
	}

	public Action(Object owner, Def def)
	{
		actions = owner?.GetComponent<Actions>();
		this.def = def;
	}

	public Kingdom GetKingdom()
	{
		return own_kingdom;
	}

	public override string ToString()
	{
		return "[" + state.ToString() + "]" + owner.ToString() + "." + def.field.key;
	}

	public static Action Create(Object owner, Def def)
	{
		if (def == null)
		{
			return null;
		}
		Reflection.TypeInfo obj_type = def.obj_type;
		if (obj_type == null)
		{
			Game.Log("Action def has no C# type: " + def.ToString(), Game.LogType.Error);
			return null;
		}
		if (obj_type.type == typeof(Action))
		{
			return new Action(owner, def);
		}
		CreateAction createAction = obj_type.FindCreateMethod(typeof(Action), typeof(Object), typeof(Def))?.func as CreateAction;
		try
		{
			if (createAction != null)
			{
				return createAction(owner, def);
			}
			return Reflection.CreateObjectViaReflection<Action>(obj_type.type, new object[2] { owner, def });
		}
		catch (Exception ex)
		{
			owner.game.Error("Error creating " + obj_type.name + ": " + ex);
			return null;
		}
	}

	public virtual bool NeedsTarget()
	{
		if (!string.IsNullOrEmpty(def.target))
		{
			return def.target != "none";
		}
		return false;
	}

	public virtual bool HasTarget(IVars vars)
	{
		if (!NeedsTarget())
		{
			return true;
		}
		if (GetTarget(vars) != null)
		{
			return true;
		}
		return false;
	}

	public virtual bool NeedsArgs()
	{
		if (def.arg_types != null)
		{
			return def.arg_types.Count > 0;
		}
		return false;
	}

	public bool HasAllArgs()
	{
		if (!NeedsArgs())
		{
			return true;
		}
		if (args == null)
		{
			return false;
		}
		if (args.Count != def.arg_types.Count)
		{
			return false;
		}
		for (int i = 0; i < args.Count; i++)
		{
			if (args[i].is_unknown)
			{
				return false;
			}
		}
		return true;
	}

	public void FinishStateAfter(float seconds)
	{
		if (seconds < 0f)
		{
			state_end_time = Time.Zero;
		}
		else if (seconds == 0f)
		{
			state_end_time = state_start_time;
		}
		else
		{
			state_end_time = game.time + seconds;
		}
		Reschedule();
		NotifyListeners("state_time_changed");
	}

	public void TickAfter(float seconds)
	{
		if (seconds <= 0f)
		{
			next_tick_time = Time.Zero;
		}
		else
		{
			next_tick_time = game.time + seconds;
		}
		Reschedule();
	}

	public void Reschedule()
	{
		Time time = game.time;
		if (state_end_time != Time.Zero)
		{
			if (next_tick_time > time && next_tick_time < state_end_time)
			{
				UpdateAt(next_tick_time);
			}
			else if (state_end_time > time)
			{
				UpdateAt(state_end_time);
			}
			else
			{
				StopUpdating();
			}
		}
		else if (next_tick_time > time)
		{
			UpdateAt(next_tick_time);
		}
		else
		{
			StopUpdating();
		}
	}

	public virtual void GetProgress(out float cur, out float max)
	{
		if (state == State.Preparing && def.max_progress > 0f)
		{
			cur = progress;
			max = def.max_progress;
		}
		else
		{
			GetStateTimes(out cur, out max);
		}
	}

	public void GetStateTimes(out float elapsed, out float duration)
	{
		Time time = game.time;
		if (time >= state_end_time)
		{
			elapsed = (duration = 0f);
			return;
		}
		elapsed = time - state_start_time;
		duration = state_end_time - state_start_time;
	}

	public virtual float CalcValue(DT.Field field)
	{
		if (field == null)
		{
			return 0f;
		}
		float num = field.Float(0, this);
		if (field.NumValues() <= 1)
		{
			return num;
		}
		float num2 = field.Float(1, this);
		if (num2 <= num)
		{
			return num;
		}
		return game.Random(num, num2);
	}

	public virtual float PrepareDuration()
	{
		if (def.max_progress > 0f)
		{
			return -1f;
		}
		float num = CalcValue(def.prepare_duration);
		if (num != 0f)
		{
			return num;
		}
		return 0f;
	}

	private void OnCharacterActionAnalytics()
	{
		if (own_character == null || own_kingdom == null || !own_kingdom.is_player)
		{
			return;
		}
		Vars vars = new Vars();
		vars.Set("characterName", own_character.Name);
		vars.Set("characterClass", own_character.class_name);
		vars.Set("characterLevel", own_character.GetClassLevel());
		vars.Set("taskChosen", def.id);
		Resource upkeep = GetUpkeep();
		if (upkeep != null)
		{
			vars.Set("upkeep", (int)upkeep[ResourceType.Gold]);
		}
		string val = ((target_kingdom != null) ? target_kingdom.Name : own_kingdom.Name);
		vars.Set("targetKingdom", val);
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(own_kingdom, target_kingdom, calc_fade: true, create_if_not_found: true);
		vars.Set("kingdomRelation", (kingdomAndKingdomRelation != null) ? ((int)kingdomAndKingdomRelation.GetRelationship()) : 0);
		Resource cost = GetCost();
		int val2 = ((cost != null) ? ((int)cost[ResourceType.Gold]) : 0);
		vars.Set("goldCost", val2);
		if (target != null)
		{
			vars.Set("actionTarget", target.ToString());
		}
		if (args != null)
		{
			for (int i = 0; i < 3 && args.Count < i; i++)
			{
				vars.Set($"actionArgument{i + 1}", args[i].ToString());
			}
		}
		own_kingdom.FireEvent("analytics_on_action_prepare", vars, own_kingdom.id);
	}

	protected void OnMissionChangedAnalytics()
	{
		if (own_character == null || own_kingdom == null)
		{
			return;
		}
		if (!own_kingdom.is_player)
		{
			Kingdom kingdom = target_kingdom;
			if (kingdom == null || !kingdom.is_player)
			{
				return;
			}
		}
		if (!Game.isLoadingSaveGame)
		{
			Vars vars = new Vars();
			KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(own_kingdom, target_kingdom, calc_fade: true, create_if_not_found: true);
			string val = target_kingdom?.Name ?? "none";
			vars.Set("targetKingdom", val);
			string val2 = own_kingdom?.Name ?? "none";
			vars.Set("originatingKingdom", val2);
			vars.Set("kingdomRelation", (kingdomAndKingdomRelation != null) ? ((int)kingdomAndKingdomRelation.GetRelationship()) : 0);
			vars.Set("characterName", own_character.Name);
			vars.Set("characterClass", own_character.class_name);
			vars.Set("characterLevel", own_character.GetClassLevel());
			Resource cost = GetCost();
			int val3 = ((cost != null) ? ((int)cost[ResourceType.Gold]) : 0);
			vars.Set("goldCost", val3);
			Value value = SuccessChanceValue(non_trivial_only: false);
			if (value.is_number)
			{
				vars.Set("successChance", value.Float());
			}
			Value value2 = RevealChanceValue(non_trivial_only: false);
			if (value2.is_number)
			{
				vars.Set("revealChance", value2.Int());
			}
			vars.Set("actionName", def.Name);
			if (own_kingdom.is_player)
			{
				own_kingdom.FireEvent("analytics_on_mission_changed", vars, own_kingdom.id);
			}
			if (target_kingdom != null && target_kingdom != own_kingdom && target_kingdom.is_player)
			{
				target_kingdom.FireEvent("analytics_on_mission_changed", vars, target_kingdom.id);
			}
		}
	}

	public virtual void Prepare()
	{
		OnCharacterActionAnalytics();
		best_progress = (progress = def.initial_progress);
		float num = PrepareDuration();
		if (num > 0f && force_prepare_duration > 0f)
		{
			num = force_prepare_duration;
		}
		if (num != 0f)
		{
			FinishStateAfter(num);
			if (!def.secondary)
			{
				own_character?.SetStatus<OngoingActionStatus>();
			}
		}
	}

	public virtual float PrepareTickDuration()
	{
		float num = CalcValue(def.prepare_tick);
		if (num != 0f)
		{
			return num;
		}
		if (def.max_progress > 0f)
		{
			return 1f;
		}
		return 0f;
	}

	private void StartTick()
	{
		if (state == State.Preparing)
		{
			float seconds = PrepareTickDuration();
			TickAfter(seconds);
		}
		else
		{
			TickAfter(-1f);
		}
	}

	public virtual void Run()
	{
	}

	public virtual void Finish()
	{
	}

	private State NextState(State state)
	{
		if (state != State.Finishing)
		{
			return state + 1;
		}
		return State.Inactive;
	}

	public void SetState(State state, bool send_state = true)
	{
		while (true)
		{
			next_tick_time = Time.Zero;
			StopUpdating();
			NotifyListeners("leave_state", state);
			OnLeaveState(state);
			State state2 = this.state;
			this.state = state;
			state_start_time = game.time;
			state_end_time = state_start_time;
			UpdateActiveListeners();
			OnEnterState(send_state);
			NotifyListeners("enter_state", state2);
			actions?.StateChanged(this);
			if (state != this.state)
			{
				return;
			}
			if (state == State.Inactive)
			{
				if (def.field.FindChild("show_in_status.target", this) == null)
				{
					target = null;
				}
				args = null;
				if (actions?.current == this)
				{
					actions.current = null;
					owner?.UpdateAutomaticStatuses();
				}
				break;
			}
			if (state_end_time != state_start_time)
			{
				break;
			}
			state = NextState(state);
		}
		owner?.UpdateAutomaticStatuses();
	}

	public virtual void OnLeaveState(State new_state)
	{
		if (state == State.Preparing && own_character?.status is OngoingActionStatus ongoingActionStatus && ongoingActionStatus.GetAction() == this)
		{
			own_character.ClearStatus();
		}
	}

	private static ActiveListener.Def LoadActiveListenerDef(DT.Field field, Def adef)
	{
		if (field == null)
		{
			return null;
		}
		ActiveListener.Def def = new ActiveListener.Def();
		def.field = field;
		DT.Field field2 = field.FindChild("message");
		if (field2 == null)
		{
			def.messages.Add(field.key);
		}
		else
		{
			int num = field2.NumValues();
			for (int i = 0; i < num; i++)
			{
				string text = field2.String(i);
				if (!string.IsNullOrEmpty(text))
				{
					def.messages.Add(text);
				}
			}
		}
		DT.Field field3 = field.FindChild("sender");
		if (field3 != null)
		{
			def.obj_field = field3;
		}
		else
		{
			def.obj_type = field.Type();
		}
		def.cancel_condition_field = field.FindChild("cancel_condition") ?? field;
		DT.Field field4 = field.FindChild("cancelled_message");
		if (field4 != null)
		{
			def.cancelled_message = field4.String();
		}
		else
		{
			def.cancelled_message = "ActionCancelledMessage";
		}
		def.cancel_reason = field.GetString("cancel_reason", null, field.key);
		DT.Field field5 = field.FindChild("cancel_outcome");
		if (field5 != null)
		{
			if (adef.outcomes == null)
			{
				Game.Log(field5.Path(include_file: true) + ": " + adef.id + " has no outcomes", Game.LogType.Error);
			}
			else
			{
				string text2 = field5.String();
				if (!string.IsNullOrEmpty(text2))
				{
					def.cancel_outcomes = adef.outcomes.Parse(text2);
				}
			}
		}
		else if (adef.outcomes != null)
		{
			OutcomeDef outcomeDef = adef.outcomes.Find("cancel_event");
			if (outcomeDef != null)
			{
				def.cancel_outcomes = new List<OutcomeDef>();
				OutcomeDef outcomeDef2 = outcomeDef.Find(def.cancel_reason);
				if (outcomeDef2 != null)
				{
					def.cancel_outcomes.Add(outcomeDef2);
				}
				else
				{
					def.cancel_outcomes.Add(null);
					def.cancel_outcomes.Add(outcomeDef);
				}
			}
		}
		return def;
	}

	private void UpdateActiveListeners()
	{
		if (state == State.Preparing)
		{
			AddActiveListeners();
		}
		else
		{
			DelActiveListeners();
		}
	}

	private void AddActiveListeners()
	{
		if ((active_listeners == null || active_listeners.Count <= 0) && def.active_listeners != null)
		{
			for (int i = 0; i < def.active_listeners.Count; i++)
			{
				ActiveListener.Def ld = def.active_listeners[i];
				AddActiveListener(ld);
			}
		}
	}

	private void DelActiveListeners()
	{
		if (active_listeners != null)
		{
			for (int i = 0; i < active_listeners.Count; i++)
			{
				ActiveListener l = active_listeners[i];
				DelActiveListener(l);
			}
			active_listeners.Clear();
		}
	}

	private Object ResolveActiveListenerObj(ActiveListener.Def ld)
	{
		if (ld.obj_field != null)
		{
			return ld.obj_field.Value(this).Get<Object>();
		}
		switch (ld.obj_type)
		{
		case "":
		case "owner":
			return owner;
		case "target":
			return target;
		case "arg":
		case "arg0":
			return GetArg(0, null).obj_val as Object;
		case "arg1":
			return GetArg(1, null).obj_val as Object;
		case "arg2":
			return GetArg(2, null).obj_val as Object;
		case "arg3":
			return GetArg(3, null).obj_val as Object;
		case "target_realm":
		{
			Object obj = target;
			if (obj != null)
			{
				if (obj is Realm result3)
				{
					return result3;
				}
				if (obj is Settlement settlement2)
				{
					return settlement2.GetRealm();
				}
			}
			return null;
		}
		case "owner_realm":
		{
			Object obj = owner;
			if (obj != null)
			{
				if (obj is Character character3)
				{
					return character3?.GetArmy()?.realm_in;
				}
				if (obj is Settlement settlement3)
				{
					return settlement3.GetRealm();
				}
			}
			return null;
		}
		case "owner_castle":
		{
			Object obj = owner;
			if (obj != null)
			{
				if (obj is Character character2)
				{
					return character2?.GetArmy()?.castle;
				}
				if (obj is Castle result4)
				{
					return result4;
				}
			}
			return null;
		}
		case "owner_army":
		{
			Object obj = owner;
			if (obj != null)
			{
				if (obj is Character character)
				{
					return character?.GetArmy();
				}
				if (obj is Castle result)
				{
					return result;
				}
			}
			return null;
		}
		case "target_castle":
		{
			Object obj = target;
			if (obj != null)
			{
				if (obj is Castle result2)
				{
					return result2;
				}
				if (obj is Realm realm)
				{
					return realm.castle;
				}
				if (obj is Settlement settlement)
				{
					return settlement.GetRealm()?.castle;
				}
			}
			return null;
		}
		case "src_kingdom":
		case "own_kingdom":
			return own_kingdom;
		case "tgt_kingdom":
		case "target_kingdom":
			return target_kingdom;
		case "mission_kingdom":
			return own_character?.mission_kingdom;
		case "catholic_hq_realm":
			return game.religions.catholic.hq_realm;
		default:
			return null;
		}
	}

	private void AddActiveListener(ActiveListener.Def ld)
	{
		Object obj = ResolveActiveListenerObj(ld);
		if (obj != null)
		{
			ActiveListener item = new ActiveListener
			{
				def = ld,
				obj = obj
			};
			if (active_listeners == null)
			{
				active_listeners = new List<ActiveListener>();
			}
			active_listeners.Add(item);
			obj.AddListener(this);
		}
	}

	private void DelActiveListener(ActiveListener l)
	{
		l.obj.DelListener(this);
	}

	public virtual void OnMessage(object obj, string message, object param)
	{
		if (owner.IsAuthority())
		{
			ProcessActiveListeners(obj, message, param);
		}
	}

	public virtual void ProcessActiveListeners(object obj, string message, object param)
	{
		if (active_listeners == null)
		{
			return;
		}
		for (int i = 0; i < active_listeners.Count; i++)
		{
			ActiveListener l = active_listeners[i];
			if (l.obj == obj && l.def.messages.Contains(message))
			{
				ProcessActiveListener(l, obj, message, param);
			}
		}
	}

	public virtual void ProcessActiveListener(ActiveListener l, object obj, string message, object param)
	{
		bool flag = true;
		if (l.def.cancel_condition_field != null)
		{
			tmp_vars.Clear();
			tmp_vars.obj = this;
			tmp_vars.Set("sender", obj);
			tmp_vars.Set("message", message);
			tmp_vars.Set("param", param);
			flag = l.def.cancel_condition_field.Bool(tmp_vars, flag);
		}
		if (flag)
		{
			CancelOnActiveListener(l, obj, message, param);
		}
	}

	public virtual void FillCancelOnActiveListenerVars(Vars vars, ActiveListener l, object obj, string message, object param)
	{
		vars.Clear();
		vars.obj = this;
		vars.Set("sender", obj);
		vars.Set("message", message);
		vars.Set("cancelled_message", l.def.cancelled_message);
		vars.Set("target", target);
		vars.Set("mission_kingdom", own_character?.mission_kingdom ?? target_kingdom);
		vars.Set("target_kingdom", target_kingdom);
		if (args != null)
		{
			vars.Set("args", args);
		}
		vars.Set("cancel_reason", l.def.cancel_reason);
		vars.Set("reason_text", def.id + ".cancelled_message_texts." + l.def.cancel_reason);
		vars.Set("default_text", def.id + ".cancelled_message_texts.default");
	}

	public virtual void CancelOnActiveListener(ActiveListener l, object obj, string message, object param)
	{
		if (l.def.cancel_outcomes != null)
		{
			forced_outcomes = l.def.cancel_outcomes;
			SetState(State.Running);
			return;
		}
		if (!string.IsNullOrEmpty(l.def.cancelled_message))
		{
			FillCancelOnActiveListenerVars(tmp_vars, l, obj, message, param);
			Event obj2 = new Event(owner, l.def.cancelled_message, this);
			obj2.vars = tmp_vars;
			obj2.send_to_kingdoms = new List<int> { own_kingdom.id };
			owner.FireEvent(obj2);
		}
		Cancel();
	}

	public virtual void ApplyOutcomes()
	{
		outcomes = DecideOutcomes();
		unique_outcomes = OutcomeDef.UniqueOutcomes(outcomes);
		if (enable_outcome_effects)
		{
			EarlyApplyOutcomes();
		}
		CreateOutcomeVars();
		OutcomeDef.PrecalculateValues(unique_outcomes, game, outcome_vars, outcome_vars);
		Event obj = new Event(owner, "action_outcomes", this);
		obj.outcomes = outcomes;
		obj.vars = outcome_vars;
		obj.send_to_kingdoms = GetSendToKingdoms();
		owner.FireEvent(obj);
		if (enable_outcome_effects)
		{
			ApplyOutcomeEffects();
		}
		forced_outcomes = null;
		outcomes = null;
		unique_outcomes = null;
		outcome_vars = null;
	}

	public virtual void OnEnterState(bool send_state = true)
	{
		if (state == State.PickingTarget)
		{
			if (NeedsTarget())
			{
				FinishStateAfter(-1f);
			}
		}
		else if (state == State.PickingArgs)
		{
			if (NeedsArgs())
			{
				FinishStateAfter(-1f);
			}
		}
		else if (state == State.Preparing)
		{
			if (!owner.IsAuthority())
			{
				return;
			}
			target_kingdom = CalcTargetKingdom(target);
			if (!def.secondary && actions != null)
			{
				Action current = actions.current;
				if (current != null && current != this)
				{
					current.Cancel(this is CancelMainAction);
				}
				actions.current = this;
				if (def.opportunity != null && !def.opportunity.IsPermanent())
				{
					actions.DelOpportunity(this, target, args);
				}
				owner?.UpdateAutomaticStatuses();
			}
			Prepare();
			if (state == State.Preparing)
			{
				StartTick();
			}
			Tracker.Track(this);
			if (send_state)
			{
				SendState();
			}
		}
		else if (state == State.Running)
		{
			if (owner.IsAuthority())
			{
				if (def.outcomes != null)
				{
					ApplyOutcomes();
				}
				else
				{
					Run();
					Tracker.Track(this);
				}
				if (send_state)
				{
					SendState();
				}
			}
		}
		else if (state == State.Finishing && owner.IsAuthority())
		{
			Finish();
			if (send_state)
			{
				SendState();
			}
		}
	}

	public virtual void OnApplyEnterState()
	{
	}

	public virtual void OnApplyLeaveState()
	{
	}

	public virtual bool CanBeCancelled(Action new_action = null)
	{
		return false;
	}

	public virtual void Cancel(bool manual = false, bool notify = true)
	{
		if (notify && owner.IsAuthority())
		{
			owner.FireEvent("cancelled", (Value)manual);
		}
		SetState(State.Inactive);
	}

	public bool SendExecuteEvent(Object target)
	{
		Object obj = owner;
		if (obj == null)
		{
			return false;
		}
		if (obj.IsAuthority())
		{
			return false;
		}
		obj.SendEvent(new Object.ExecuteActionEvent(this, target));
		return true;
	}

	public void SendState()
	{
		owner?.SendState<Object.ActionsState>();
	}

	public override bool IsRefSerializable()
	{
		if (owner != null && owner.IsRefSerializable())
		{
			return def != null;
		}
		return false;
	}

	public virtual string GetConfirmationMessageKey()
	{
		return "confirmation_message";
	}

	public virtual string GetTargetTableKey()
	{
		return "target_select_table";
	}

	public virtual string GetArgTableKey(int idx)
	{
		return "args_select_table";
	}

	public virtual string GetTargetConfirmationMessageKey(object target)
	{
		return "target_confirmation_message";
	}

	public virtual string ValidateSkillDependancies()
	{
		if (def.skill_dependencies == null)
		{
			return "ok";
		}
		Character character = own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		for (int i = 0; i < def.skill_dependencies.Count; i++)
		{
			if (def.skill_dependencies[i].Validate(character, check_cell: true))
			{
				return "ok";
			}
		}
		return "skill_dependancies";
	}

	public virtual string ValidateClassDependancies()
	{
		if (this.def.available_in_class_defs == null)
		{
			return "ok";
		}
		Character character = own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		for (int i = 0; i < this.def.available_in_class_defs.Count; i++)
		{
			CharacterClass.Def def = this.def.available_in_class_defs[i];
			if (character.class_def != def)
			{
				continue;
			}
			for (int j = 0; j < def.actions.Count; j++)
			{
				(Def, DT.Field) tuple = def.actions[j];
				if (tuple.Item1 == this.def)
				{
					if (tuple.Item2 != null && !tuple.Item2.Bool(character))
					{
						break;
					}
					return "ok";
				}
			}
			break;
		}
		return "class_dependancies";
	}

	public virtual string ValidateAlive()
	{
		Character character = own_character;
		if (character != null && !character.IsAlive())
		{
			return "dead";
		}
		return "ok";
	}

	public virtual string ValidateInPrison()
	{
		Character character = own_character;
		if (def.prisoner)
		{
			if (character == null)
			{
				return "not_a_character";
			}
			if (character.prison_kingdom == null)
			{
				return "not_in_prison";
			}
		}
		else if (character?.prison_kingdom != null)
		{
			return "in_prison";
		}
		return "ok";
	}

	public virtual string ValidateMissionKingdom()
	{
		if (def.mission == Def.MissionType.Ignore)
		{
			return "ok";
		}
		if (def.mission == Def.MissionType.OnMission && own_character?.mission_kingdom == null)
		{
			return "not_on_mission";
		}
		if (def.mission == Def.MissionType.NotOnMission && own_character?.mission_kingdom != null)
		{
			return "on_mission";
		}
		return "ok";
	}

	public virtual string ValidateWhileLeadingArmy()
	{
		if (def.secondary || def.prisoner)
		{
			return "ok";
		}
		if (own_character?.GetArmy() == null)
		{
			return "ok";
		}
		if (def.while_leading_army)
		{
			return "ok";
		}
		return "leading_army";
	}

	public virtual string ValidateIdle()
	{
		if (def.secondary || def.prisoner)
		{
			return "ok";
		}
		Character character = own_character;
		if (character != null && !character.IsIdle())
		{
			return "_not_idle";
		}
		return "ok";
	}

	public virtual string ValidateInProgress()
	{
		if (is_active)
		{
			return "_in_progress";
		}
		return "ok";
	}

	public virtual string ValidateCancelCurrentAction()
	{
		if (def.secondary)
		{
			return "ok";
		}
		Action action = actions?.current;
		if (action != this && action != null && !action.CanBeCancelled(this))
		{
			return "_another_action_in_progress";
		}
		return "ok";
	}

	public virtual string ValidateInCourt()
	{
		Character character = own_character;
		if (character == null)
		{
			return "ok";
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom == null || !kingdom.court.Contains(character))
		{
			return "not_in_court";
		}
		return "ok";
	}

	public virtual string ValidatePope()
	{
		Character character = own_character;
		if (character == null)
		{
			return "ok";
		}
		if (character.IsPope())
		{
			return "pope";
		}
		return "ok";
	}

	public virtual bool ValidateKingSkills(DT.Field rf)
	{
		Character character = own_kingdom?.GetKing();
		if (character == null)
		{
			return false;
		}
		int num = rf.NumValues();
		if (num <= 0)
		{
			return true;
		}
		for (int i = 0; i < num; i++)
		{
			string skill_name = rf.String(i);
			if (character.GetSkill(skill_name) == null)
			{
				return false;
			}
		}
		return true;
	}

	public virtual bool ValidateNotVassal(DT.Field rf)
	{
		return own_kingdom?.sovereignState == null;
	}

	public virtual bool ValidateNoWars(DT.Field rf)
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return false;
		}
		if (kingdom.wars.Count > 0)
		{
			return false;
		}
		return true;
	}

	public virtual bool ValidateInfluence(DT.Field rf)
	{
		Kingdom kingdom = own_kingdom;
		Kingdom kingdom2 = own_character?.mission_kingdom;
		if (kingdom == null || kingdom2 == null)
		{
			return true;
		}
		Value value = rf.Value(0);
		if (kingdom.GetInfluenceIn(kingdom2) < (float)value)
		{
			return false;
		}
		return true;
	}

	public virtual bool ValidateCrownAuthority(DT.Field rf)
	{
		if (own_kingdom == null)
		{
			return true;
		}
		Value value = rf.Value(0);
		Value value2 = rf.Value(1);
		int value3 = own_kingdom.GetCrownAuthority().GetValue();
		if (value3 < (int)value || value3 > (int)value2)
		{
			return false;
		}
		return true;
	}

	public virtual bool ValidateRequirement(DT.Field rf)
	{
		switch (rf.key)
		{
		case "king_skills":
			return ValidateKingSkills(rf);
		case "not_vassal":
			return ValidateNotVassal(rf);
		case "no_wars":
			return ValidateNoWars(rf);
		case "sf_points":
			return ValidateSuccessAndFail(rf.Int(this, 1)) == "ok";
		case "influence":
			return ValidateInfluence(rf);
		case "crown_authority":
			return ValidateCrownAuthority(rf);
		case "has_valid_target":
		{
			List<Object> possibleTargets = GetPossibleTargets();
			if (NeedsTarget() && possibleTargets != null)
			{
				return possibleTargets.Count != 0;
			}
			return false;
		}
		default:
			return rf.Bool(this);
		}
	}

	public virtual string ValidateRequirements()
	{
		if (def.requirements == null)
		{
			return "ok";
		}
		if (no_requirement_checks)
		{
			return "ok";
		}
		for (int i = 0; i < def.requirements.Count; i++)
		{
			DT.Field field = def.requirements[i];
			if (!ValidateRequirement(field))
			{
				return "_" + field.key;
			}
		}
		return "ok";
	}

	public virtual string ValidateCondition()
	{
		if (def.condition != null && !def.condition.GetValue(this).Bool())
		{
			return "condition";
		}
		if (def.enabled_condition != null && !def.enabled_condition.GetValue(this).Bool())
		{
			return "_enabled_condition";
		}
		return "ok";
	}

	public virtual string ValidateUnique()
	{
		if (own_kingdom?.GetCharacterWithActionOrStatus(def.unique_id, def.unique_status_id) != null)
		{
			return "_unique";
		}
		return "ok";
	}

	public virtual string ValidateTargetsAndArgs()
	{
		if (NeedsTarget())
		{
			List<Object> possibleTargets = GetPossibleTargets();
			if (possibleTargets == null || possibleTargets.Count == 0)
			{
				return "no_possible_targets";
			}
		}
		if (NeedsArgs() && target != null)
		{
			List<Value>[] possibleArgs = GetPossibleArgs();
			if (possibleArgs == null || possibleArgs.Length == 0)
			{
				return "no_possible_args";
			}
		}
		return "ok";
	}

	public static bool ShouldBeVisible(string validate_result)
	{
		if (string.IsNullOrEmpty(validate_result))
		{
			return false;
		}
		if (validate_result == "ok")
		{
			return true;
		}
		if (validate_result[0] == '_')
		{
			return true;
		}
		return false;
	}

	public virtual string ValidateSuccessAndFail(int min_chance = 0)
	{
		if (target == null && def.field.GetBool("sf_per_target"))
		{
			return "ok";
		}
		SuccessAndFail successAndFail = SuccessAndFail.Get(this, keep_factors: false);
		if (successAndFail == null)
		{
			return "ok";
		}
		if (successAndFail.value >= min_chance)
		{
			return "ok";
		}
		return "_sf_points";
	}

	public bool CheckValidateResult(ref string final_result, string cur_result, bool quick_out)
	{
		if (cur_result == "ok")
		{
			return true;
		}
		if (quick_out)
		{
			final_result = cur_result;
			return false;
		}
		if (string.IsNullOrEmpty(cur_result) || cur_result[0] != '_')
		{
			final_result = cur_result;
			return false;
		}
		if (final_result == "ok")
		{
			final_result = cur_result;
		}
		return true;
	}

	public virtual string CheckEnabled()
	{
		return "ok";
	}

	public string GetValidateKey(IVars vars = null)
	{
		string text = Validate();
		if (text != "ok")
		{
			return text;
		}
		text = CheckEnabled();
		if (text != "ok")
		{
			return text;
		}
		Object obj = GetTarget(vars);
		List<Value> list = GetArgs(vars);
		if (NeedsTarget())
		{
			if (obj != null)
			{
				if (!ValidateTarget(obj))
				{
					return "invalid_target";
				}
			}
			else
			{
				List<Object> possibleTargets = GetPossibleTargets();
				if (possibleTargets == null || possibleTargets.Count == 0)
				{
					return "_no_possible_targets";
				}
			}
		}
		if (NeedsArgs())
		{
			if (HasAllArgs())
			{
				if (!ValidateArgs())
				{
					return "invalid_args";
				}
			}
			else if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (!ValidateArg(list[i], i))
					{
						return "invalid_args";
					}
				}
			}
			else
			{
				List<Value>[] possibleArgs = GetPossibleArgs();
				if (possibleArgs == null || possibleArgs.Length == 0)
				{
					return "_no_possible_args";
				}
				foreach (List<Value> list2 in possibleArgs)
				{
					if (list2 == null || list2.Count == 0)
					{
						return "_no_possible_args";
					}
				}
			}
		}
		if (!CheckCost(obj))
		{
			return "_cost";
		}
		return "ok";
	}

	public DT.Field GetValidatePrompt(IVars vars = null)
	{
		DT.Field field = def.field?.FindChild("validate_prompts");
		if (field == null)
		{
			return null;
		}
		string validateKey = GetValidateKey(vars);
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

	public string ValidateSkillOrClassDependancies()
	{
		string final_result = "ok";
		return ValidateSkillOrClassDependancies(ref final_result);
	}

	public virtual string ValidateSkillOrClassDependancies(ref string final_result, bool quick_out = false)
	{
		string text = ValidateSkillDependancies();
		if (!CheckValidateResult(ref final_result, text, quick_out) && final_result != "ok")
		{
			text = ValidateClassDependancies();
			string final_result2 = "ok";
			if (!CheckValidateResult(ref final_result2, text, quick_out))
			{
				return text;
			}
			final_result = final_result2;
		}
		return text;
	}

	public virtual string Validate(bool quick_out = false)
	{
		string final_result = "ok";
		string cur_result = ValidateInProgress();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		cur_result = ValidateAlive();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		cur_result = ValidateInPrison();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		if (ValidateSkillOrClassDependancies(ref final_result, quick_out) != "ok")
		{
			return final_result;
		}
		cur_result = ValidateWhileLeadingArmy();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		cur_result = ValidateMissionKingdom();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		cur_result = ValidateInCourt();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		cur_result = ValidatePope();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		cur_result = ValidateRequirements();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		cur_result = ValidateCondition();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		if (!def.has_sf_points_requirement)
		{
			cur_result = ValidateSuccessAndFail();
			if (!CheckValidateResult(ref final_result, cur_result, quick_out))
			{
				return final_result;
			}
		}
		cur_result = ValidateCancelCurrentAction();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		cur_result = ValidateUnique();
		if (!CheckValidateResult(ref final_result, cur_result, quick_out))
		{
			return final_result;
		}
		cur_result = ValidateIdle();
		CheckValidateResult(ref final_result, cur_result, quick_out);
		return final_result;
	}

	public virtual bool ValidateTarget(Object target)
	{
		if (!NeedsTarget())
		{
			return true;
		}
		if (target == null)
		{
			return false;
		}
		switch (def.target)
		{
		case "own_town":
		{
			if (!(target is Castle castle))
			{
				return false;
			}
			Kingdom kingdom6 = castle.GetKingdom();
			if (kingdom6 == null || kingdom6 != own_kingdom)
			{
				return false;
			}
			return true;
		}
		case "own_town_army":
		{
			Kingdom kingdom8 = null;
			if (target is Army army)
			{
				if (army.IsHeadless())
				{
					kingdom8 = army.GetKingdom();
				}
			}
			else if (target is Castle castle2)
			{
				kingdom8 = castle2.GetKingdom();
			}
			if (kingdom8 == null || kingdom8 != own_kingdom)
			{
				return false;
			}
			return true;
		}
		case "kingdom":
			if (!(target is Kingdom kingdom5) || kingdom5.IsDefeated())
			{
				return false;
			}
			return true;
		case "other_kingdom":
			if (!(target is Kingdom kingdom4) || kingdom4.IsDefeated())
			{
				return false;
			}
			return kingdom4 != own_kingdom;
		case "enemy_kingdom":
			if (!(target is Kingdom kingdom2) || kingdom2.IsDefeated())
			{
				return false;
			}
			return kingdom2.IsEnemy(own_kingdom);
		case "prisoner":
			if (!(target is Character { prison_kingdom: not null }))
			{
				return false;
			}
			return true;
		case "book":
			if (!(target is Book))
			{
				return false;
			}
			return true;
		case "book_mission":
		{
			Book book = target as Book;
			if (book == null && book.GetKingdom() != own_character.mission_kingdom)
			{
				return false;
			}
			return true;
		}
		case "fiancee":
		{
			Character character2 = target as Character;
			if (character2 == null && character2.GetKingdom() != own_kingdom)
			{
				return false;
			}
			return true;
		}
		case "character":
			if (!(target is Character))
			{
				return false;
			}
			return true;
		case "realm":
			if (!(target is Realm))
			{
				return false;
			}
			return true;
		case "trade_center":
			if (!(target is Realm { tradeCenter: not null, tradeCenterDistance: 0 }))
			{
				return false;
			}
			return true;
		case "diplomatic_kingdom":
			if (!(target is Kingdom kingdom7) || kingdom7.IsDefeated() || kingdom7.IsEnemy(own_kingdom))
			{
				return false;
			}
			return kingdom7 != own_kingdom;
		case "war_kingdom":
			if (!(target is Kingdom kingdom3) || kingdom3.IsDefeated() || !kingdom3.IsEnemy(own_kingdom))
			{
				return false;
			}
			return kingdom3 != own_kingdom;
		case "trade_route_kingdom":
			if (!(target is Kingdom kingdom) || kingdom.IsEnemy(own_kingdom))
			{
				return false;
			}
			if (kingdom.tradeAgreementsWith.Contains(own_kingdom))
			{
				return true;
			}
			return false;
		default:
			return false;
		}
	}

	public virtual bool ValidateArgs()
	{
		if (!NeedsArgs())
		{
			return true;
		}
		if (args == null)
		{
			return false;
		}
		for (int i = 0; i < args.Count; i++)
		{
			if (!ValidateArg(args[i], i))
			{
				return false;
			}
		}
		return true;
	}

	public virtual bool ValidateArg(Value value, int def_type)
	{
		if (!NeedsArgs())
		{
			return true;
		}
		if (value == Value.Unknown || value == Value.Null)
		{
			return false;
		}
		if (def_type >= def.arg_types.Count)
		{
			return false;
		}
		Object obj = value.Get<Object>();
		switch (def.arg_types[def_type])
		{
		case "own_town":
		{
			if (!(obj is Castle castle2))
			{
				return false;
			}
			Kingdom kingdom2 = castle2.GetKingdom();
			if (kingdom2 == null || kingdom2 != own_kingdom)
			{
				return false;
			}
			return true;
		}
		case "own_town_army":
		{
			Kingdom kingdom = null;
			if (obj is Army army)
			{
				if (army.IsHeadless())
				{
					kingdom = army.GetKingdom();
				}
			}
			else if (obj is Castle castle)
			{
				kingdom = castle.GetKingdom();
			}
			if (kingdom == null || kingdom != own_kingdom)
			{
				return false;
			}
			return true;
		}
		case "Kingdom":
			if (!(obj is Kingdom kingdom3) || kingdom3.IsDefeated())
			{
				return false;
			}
			return true;
		case "Character":
			if (!(obj is Character character2) || character2.GetKingdom().IsDefeated())
			{
				return false;
			}
			return true;
		case "Realm":
			if (!(obj is Realm realm) || realm.GetKingdom() == null || realm.GetKingdom().IsDefeated())
			{
				return false;
			}
			return true;
		case "other_kingdom":
			if (!(obj is Kingdom kingdom5) || kingdom5.IsDefeated())
			{
				return false;
			}
			return kingdom5 != own_kingdom;
		case "enemy_kingdom":
			if (!(obj is Kingdom kingdom4) || kingdom4.IsDefeated())
			{
				return false;
			}
			return kingdom4.IsEnemy(own_kingdom);
		case "prisoner":
			if (!(obj is Character { prison_kingdom: not null }))
			{
				return false;
			}
			return true;
		case "book":
			if (!(obj is Book))
			{
				return false;
			}
			return true;
		case "skill":
			return value.is_string;
		case "tradition":
			return value.is_string;
		case "book_mission":
		{
			Book book = obj as Book;
			if (book == null && book.GetKingdom() != own_character.mission_kingdom)
			{
				return false;
			}
			return true;
		}
		case "fiancee":
		{
			Character character = obj as Character;
			if (character == null && character.GetKingdom() != own_kingdom)
			{
				return false;
			}
			return true;
		}
		case "realm":
			if (!(obj is Realm))
			{
				return false;
			}
			return true;
		case "trade_center":
			if (!(obj is Realm { tradeCenter: not null, tradeCenterDistance: 0 }))
			{
				return false;
			}
			return true;
		case "courte_slot_index":
			if (!value.is_number)
			{
				return false;
			}
			return true;
		case "int":
		case "float":
			if (!value.is_number)
			{
				return false;
			}
			return true;
		case "string":
		case "importable_good":
			if (!value.is_string)
			{
				return false;
			}
			return true;
		case "War":
			if (!(obj is War))
			{
				return false;
			}
			return true;
		case "Pact":
			if (!(obj is Pact))
			{
				return false;
			}
			return true;
		default:
			return false;
		}
	}

	public void AddTarget(ref List<Object> targets, Object target)
	{
		if (ValidateTarget(target))
		{
			if (targets == null)
			{
				targets = new List<Object>();
			}
			targets.Add(target);
		}
	}

	public void AddArg(ref List<Value> args, Value target, int arg_index)
	{
		if (ValidateArg(target, arg_index))
		{
			if (args == null)
			{
				args = new List<Value>();
			}
			args.Add(target);
			CalcTargetKingdom(_target);
		}
	}

	public virtual void SetStatus(Status s)
	{
		own_status = s;
	}

	public virtual List<Object> GetPossibleTargets()
	{
		if (!NeedsTarget())
		{
			return null;
		}
		List<Object> targets = null;
		switch (def.target)
		{
		case "own_town":
		{
			Kingdom kingdom16 = GetKingdom();
			if (kingdom16 == null)
			{
				return null;
			}
			for (int num12 = 0; num12 < kingdom16.realms.Count; num12++)
			{
				Realm realm5 = kingdom16.realms[num12];
				AddTarget(ref targets, realm5.castle);
			}
			break;
		}
		case "own_town_army":
		{
			Kingdom kingdom6 = GetKingdom();
			if (kingdom6 == null)
			{
				return null;
			}
			for (int m = 0; m < kingdom6.realms.Count; m++)
			{
				Realm realm = kingdom6.realms[m];
				AddTarget(ref targets, realm.castle);
			}
			for (int n = 0; n < kingdom6.armies.Count; n++)
			{
				Army army = kingdom6.armies[n];
				if (army.IsHeadless())
				{
					AddTarget(ref targets, army);
				}
			}
			break;
		}
		case "own_realm":
		{
			Kingdom kingdom12 = GetKingdom();
			if (kingdom12 == null)
			{
				return null;
			}
			for (int num6 = 0; num6 < kingdom12.realms.Count; num6++)
			{
				Realm realm2 = kingdom12.realms[num6];
				AddTarget(ref targets, realm2);
			}
			break;
		}
		case "own_realm_plus_vassals":
		{
			Kingdom kingdom14 = GetKingdom();
			if (kingdom14 == null)
			{
				return null;
			}
			for (int num8 = 0; num8 < kingdom14.realms.Count; num8++)
			{
				Realm realm3 = kingdom14.realms[num8];
				AddTarget(ref targets, realm3);
			}
			for (int num9 = 0; num9 < kingdom14.vassalStates.Count; num9++)
			{
				Kingdom kingdom15 = kingdom14.vassalStates[num9];
				if (kingdom15 != null)
				{
					for (int num10 = 0; num10 < kingdom15.realms.Count; num10++)
					{
						Realm realm4 = kingdom15.realms[num10];
						AddTarget(ref targets, realm4);
					}
				}
			}
			break;
		}
		case "kingdom":
		case "other_kingdom":
		case "enemy_kingdom":
		{
			if (game.kingdoms == null)
			{
				break;
			}
			for (int j = 0; j < game.kingdoms.Count; j++)
			{
				Kingdom kingdom2 = game.kingdoms[j];
				if (!kingdom2.IsDefeated())
				{
					AddTarget(ref targets, kingdom2);
				}
			}
			break;
		}
		case "vassal_kingdom":
		{
			Kingdom kingdom8 = GetKingdom();
			if (kingdom8 == null)
			{
				return null;
			}
			for (int num3 = 0; num3 < kingdom8.vassalStates.Count; num3++)
			{
				Kingdom kingdom9 = kingdom8.vassalStates[num3];
				AddTarget(ref targets, kingdom9);
			}
			break;
		}
		case "prisoner":
		{
			Kingdom kingdom13 = GetKingdom();
			if (kingdom13 == null)
			{
				return null;
			}
			for (int num7 = 0; num7 < kingdom13.prisoners.Count; num7++)
			{
				Character character4 = kingdom13.prisoners[num7];
				if (character4 != null && !character4.IsDead())
				{
					AddTarget(ref targets, character4);
				}
			}
			break;
		}
		case "book":
		{
			Kingdom kingdom5 = GetKingdom();
			if (kingdom5 == null)
			{
				return null;
			}
			for (int l = 0; l < kingdom5.books.Count; l++)
			{
				Book book = kingdom5.books[l];
				AddTarget(ref targets, book);
			}
			break;
		}
		case "book_mission":
		{
			Kingdom mission_kingdom = own_character.mission_kingdom;
			if (mission_kingdom == null)
			{
				return null;
			}
			for (int num11 = 0; num11 < mission_kingdom.books.Count; num11++)
			{
				Book book2 = mission_kingdom.books[num11];
				AddTarget(ref targets, book2);
			}
			break;
		}
		case "fiancee":
		{
			Kingdom kingdom7 = GetKingdom();
			if (kingdom7 == null)
			{
				return null;
			}
			if (targets == null)
			{
				targets = new List<Object>();
			}
			foreach (Kingdom neighbor in kingdom7.neighbors)
			{
				for (int num = 0; num < neighbor.royalFamily.Children.Count; num++)
				{
					Character character = neighbor.royalFamily.Children[num];
					for (int num2 = 0; num2 < own_kingdom.royalFamily.Children.Count; num2++)
					{
						Character character2 = own_kingdom.royalFamily.Children[num2];
						if (!character2.IsMarried() && !character.IsMarried() && character2.sex != character.sex && !targets.Contains(character))
						{
							AddTarget(ref targets, character);
						}
					}
				}
			}
			break;
		}
		case "trade_center":
			if (targets == null)
			{
				targets = new List<Object>();
			}
			foreach (Realm tradeCenterRealm in game.economy.tradeCenterRealms)
			{
				AddTarget(ref targets, tradeCenterRealm);
			}
			break;
		case "diplomatic_kingdom":
		{
			Kingdom kingdom10 = GetKingdom();
			targets = new List<Object>();
			if (kingdom10 == null || game.kingdoms == null)
			{
				break;
			}
			for (int num4 = 0; num4 < game.kingdoms.Count; num4++)
			{
				Kingdom kingdom11 = game.kingdoms[num4];
				if (!kingdom11.IsDefeated() && !kingdom11.IsEnemy(kingdom10))
				{
					AddTarget(ref targets, kingdom11);
				}
			}
			for (int num5 = 0; num5 < kingdom10.court.Count; num5++)
			{
				Character character3 = kingdom10.court[num5];
				if (character3 != null && character3.IsDiplomat())
				{
					if ((character3.cur_action as ImproveRelationsAction)?.target != null)
					{
						targets.Remove((character3.cur_action as ImproveRelationsAction)?.target as Kingdom);
					}
					if (character3.mission_kingdom != null)
					{
						targets.Remove(character3.mission_kingdom);
					}
				}
			}
			return targets;
		}
		case "war_kingdom":
		{
			Kingdom kingdom3 = GetKingdom();
			targets = new List<Object>();
			if (kingdom3 == null || game.kingdoms == null)
			{
				break;
			}
			for (int k = 0; k < game.kingdoms.Count; k++)
			{
				Kingdom kingdom4 = game.kingdoms[k];
				if (!kingdom4.IsDefeated() && kingdom4.IsEnemy(kingdom3))
				{
					AddTarget(ref targets, kingdom4);
				}
			}
			return targets;
		}
		case "trade_route_kingdom":
		{
			Kingdom kingdom = GetKingdom();
			targets = new List<Object>();
			for (int i = 0; i < kingdom.tradeAgreementsWith.Count; i++)
			{
				Kingdom item = kingdom.tradeAgreementsWith[i];
				if (!targets.Contains(item))
				{
					AddTarget(ref targets, item);
				}
			}
			return targets;
		}
		}
		return targets;
	}

	public virtual List<Vars> GetPossibleTargetsVars(List<Object> possibleTargets = null)
	{
		if (possibleTargets == null || possibleTargets.Count == 0)
		{
			return null;
		}
		DT.Field field = def?.field?.FindChild("target_vars", this);
		List<string> list = field?.Keys();
		List<Vars> list2 = new List<Vars>(possibleTargets.Count);
		Object obj = target;
		for (int i = 0; i < possibleTargets.Count; i++)
		{
			target = possibleTargets[i];
			Vars vars = new Vars(this);
			vars.Set("target", target);
			vars.Set("picker_tooltip", val: true);
			DT.Field field2 = def.field.FindChild("picker_text");
			if (field2 != null)
			{
				vars.Set("rightTextKey", field2.Path());
			}
			if (list != null)
			{
				for (int j = 0; j < list.Count; j++)
				{
					string key = list[j];
					Value value = field.GetValue(key, vars);
					vars.Set(key, value);
				}
			}
			FillPossibleTargetVars(vars);
			list2.Add(vars);
		}
		target = obj;
		return list2;
	}

	public virtual void FillPossibleTargetVars(Vars vars)
	{
	}

	public virtual List<Vars> GetPossibleArgVars(List<Value> possibleTargets = null, int arg_type = 0)
	{
		return null;
	}

	public virtual List<Value>[] GetPossibleArgs()
	{
		if (!NeedsArgs())
		{
			return null;
		}
		List<Value>[] array = new List<Value>[def.arg_types.Count];
		for (int i = 0; i < def.arg_types.Count; i++)
		{
			string text = def.arg_types[i];
			List<Value> list = null;
			switch (text)
			{
			case "own_town":
			{
				Kingdom kingdom5 = GetKingdom();
				if (kingdom5 == null)
				{
					return null;
				}
				for (int num2 = 0; num2 < kingdom5.realms.Count; num2++)
				{
					Realm realm = kingdom5.realms[num2];
					AddArg(ref list, realm.castle, i);
				}
				break;
			}
			case "kingdom":
			case "other_kingdom":
			case "enemy_kingdom":
			{
				if (game.kingdoms == null)
				{
					break;
				}
				for (int l = 0; l < game.kingdoms.Count; l++)
				{
					Kingdom kingdom2 = game.kingdoms[l];
					if (!kingdom2.IsDefeated())
					{
						AddArg(ref list, kingdom2, i);
					}
				}
				break;
			}
			case "prisoner":
			{
				Kingdom kingdom4 = GetKingdom();
				if (kingdom4 == null)
				{
					return null;
				}
				for (int num = 0; num < kingdom4.prisoners.Count; num++)
				{
					Character character3 = kingdom4.prisoners[num];
					if (character3 != null && !character3.IsDead())
					{
						AddArg(ref list, character3, i);
					}
				}
				break;
			}
			case "book":
			{
				Kingdom kingdom3 = GetKingdom();
				if (kingdom3 == null)
				{
					return null;
				}
				for (int m = 0; m < kingdom3.books.Count; m++)
				{
					Book book = kingdom3.books[m];
					AddArg(ref list, book, i);
				}
				break;
			}
			case "book_mission":
			{
				Kingdom mission_kingdom = own_character.mission_kingdom;
				if (mission_kingdom == null)
				{
					return null;
				}
				for (int n = 0; n < mission_kingdom.books.Count; n++)
				{
					Book book2 = mission_kingdom.books[n];
					AddArg(ref list, book2, i);
				}
				break;
			}
			case "fiancee":
			{
				Kingdom kingdom = GetKingdom();
				if (kingdom == null)
				{
					return null;
				}
				foreach (Kingdom neighbor in kingdom.neighbors)
				{
					for (int j = 0; j < neighbor.royalFamily.Children.Count; j++)
					{
						Character character = neighbor.royalFamily.Children[j];
						if (!character.IsAlive())
						{
							continue;
						}
						for (int k = 0; k < own_kingdom.royalFamily.Children.Count; k++)
						{
							Character character2 = own_kingdom.royalFamily.Children[k];
							if (character2.IsAlive() && !character2.IsMarried() && !character.IsMarried() && character2.sex != character.sex && !list.Contains(character) && character2.IsAlive() && character.IsAlive())
							{
								AddArg(ref list, character, i);
							}
						}
					}
				}
				break;
			}
			case "trade_center":
				foreach (Realm tradeCenterRealm in game.economy.tradeCenterRealms)
				{
					if (tradeCenterRealm != null && !own_character.IsEnemy(tradeCenterRealm))
					{
						AddArg(ref list, tradeCenterRealm, i);
					}
				}
				break;
			}
			array[i] = list;
		}
		return array;
	}

	public virtual string GetTargetPickerKey()
	{
		return def?.id + "TargetPicker";
	}

	public virtual string GetArgPickerKey(int arg_idx)
	{
		return def?.id + "ArgPicker" + arg_idx;
	}

	public virtual bool Execute(Object target)
	{
		if (Validate() != "ok")
		{
			Cancel();
			return false;
		}
		if (!ValidateTarget(target))
		{
			Cancel();
			return false;
		}
		this.target = target;
		if (!HasAllArgs())
		{
			SetState(State.PickingArgs);
			return false;
		}
		if (!ValidateArgs())
		{
			Cancel();
			return false;
		}
		if (!CheckCost(target))
		{
			Cancel();
			return false;
		}
		if (SendExecuteEvent(target))
		{
			return true;
		}
		if (!ApplyCost(check_first: false))
		{
			Cancel();
			return false;
		}
		SetState(State.Preparing);
		own_kingdom?.RecalcIncomes();
		return true;
	}

	public virtual Kingdom CalcTargetKingdom(Object target)
	{
		if (target is Kingdom result)
		{
			return result;
		}
		if (target is Settlement settlement)
		{
			return settlement.GetKingdom();
		}
		if (target is Realm realm)
		{
			return realm.controller.GetKingdom();
		}
		if (target is Character character)
		{
			return character.GetKingdom();
		}
		Character character2 = own_character;
		if (character2 != null)
		{
			if (character2.mission_kingdom != null)
			{
				return character2.mission_kingdom;
			}
			if (character2.mission_realm != null)
			{
				return character2.mission_realm.controller.GetKingdom();
			}
			Army army = character2.GetArmy();
			if (army != null)
			{
				return army.realm_in?.GetKingdom();
			}
		}
		return own_kingdom;
	}

	public virtual bool IsDead()
	{
		return false;
	}

	public Resource GetCost()
	{
		return GetCost(null);
	}

	public virtual Resource GetCost(Object target, IVars vars = null)
	{
		if (def.cost == null)
		{
			return null;
		}
		if (vars == null)
		{
			if (target == null)
			{
				vars = this;
			}
			else
			{
				Vars vars2 = new Vars(this);
				vars2.Set("target", target);
				vars = vars2;
			}
		}
		else if (target != null && vars.GetVar("target").is_unknown)
		{
			Vars vars3 = new Vars(vars);
			vars3.Set("target", target);
			vars = vars3;
		}
		return Resource.Parse(def.cost, vars);
	}

	public virtual bool CheckCost(Object target)
	{
		Resource cost = GetCost(target);
		if (cost == null)
		{
			return true;
		}
		Resource upkeep = GetUpkeep();
		if (upkeep != null)
		{
			cost.Add(ResourceType.Trade, upkeep.Get(ResourceType.Trade));
			cost.Add(ResourceType.Food, upkeep.Get(ResourceType.Food));
		}
		if (!cost.IsValid())
		{
			return false;
		}
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return false;
		}
		_ = own_character;
		return kingdom.resources.CanAfford(cost, 1f);
	}

	public virtual bool ApplyCost(bool check_first = true)
	{
		Resource cost = GetCost(target);
		if (cost == null)
		{
			return true;
		}
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return false;
		}
		if (check_first)
		{
			if (!kingdom.resources.CanAfford(cost, 1f))
			{
				return false;
			}
			if (own_character != null)
			{
				return false;
			}
		}
		KingdomAI.Expense.Category expenseCategory = GetExpenseCategory();
		kingdom.SubResources(expenseCategory, cost);
		return true;
	}

	public virtual Resource GetUpkeep()
	{
		DT.Field field = def?.field?.FindChild("upkeep", this);
		if (field == null)
		{
			return null;
		}
		if (field.Type() == "resources")
		{
			return Resource.Parse(field, this);
		}
		float num = field.Float(this);
		if (num != 0f)
		{
			Resource resource = new Resource();
			resource.Add(ResourceType.Gold, num);
			return resource;
		}
		return null;
	}

	public KingdomAI.Expense.Category GetExpenseCategory()
	{
		if (def != null && def.expense_category != KingdomAI.Expense.Category.None)
		{
			return def.expense_category;
		}
		Character character = own_character;
		if (character?.class_def != null && character.class_def.ai_category != KingdomAI.Expense.Category.None)
		{
			return character.class_def.ai_category;
		}
		return KingdomAI.Expense.Category.Economy;
	}

	public void ForceOutcomes(string str)
	{
		if (str == null || def.outcomes == null)
		{
			forced_outcomes = null;
		}
		else
		{
			forced_outcomes = def.outcomes.Parse(str);
		}
	}

	public virtual void AlterOutcomeChance(OutcomeDef outcome, IVars vars)
	{
		if (!ValidateOutcome(outcome))
		{
			outcome.chance = 0f;
		}
		else if (def.success_fail != null && outcome.key == "success" && !outcome.field.Value().is_valid)
		{
			SuccessAndFail successAndFail = SuccessAndFail.Get(this, keep_factors: false, vars);
			if (successAndFail != null)
			{
				outcome.chance = successAndFail.Chance();
			}
		}
	}

	public virtual List<OutcomeDef> DecideOutcomes()
	{
		return def.outcomes.DecideOutcomes(game, this, forced_outcomes, AlterOutcomeChance);
	}

	public virtual List<int> GetSendToKingdoms()
	{
		return null;
	}

	public virtual void CreateOutcomeVars()
	{
		outcome_vars = new Vars(this);
		outcome_vars.Set("target", target);
		outcome_vars.Set("mission_kingdom", own_character?.mission_kingdom ?? target_kingdom);
		outcome_vars.Set("target_kingdom", target_kingdom);
		if (args != null)
		{
			outcome_vars.Set("args", args);
		}
	}

	public virtual void EarlyApplyOutcomes()
	{
		for (int i = 0; i < unique_outcomes.Count; i++)
		{
			OutcomeDef outcome = unique_outcomes[i];
			ApplyEarlyOutcome(outcome);
		}
	}

	public virtual void ApplyOutcomeEffects()
	{
		for (int i = 0; i < unique_outcomes.Count; i++)
		{
			OutcomeDef outcome = unique_outcomes[i];
			ApplyOutcome(outcome);
		}
	}

	public virtual bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		return true;
	}

	public virtual bool ApplyOutcome(OutcomeDef outcome)
	{
		switch (outcome.key)
		{
		case "success":
			Run();
			Tracker.Track(this);
			return true;
		case "fail":
			Tracker.Track(this, success: false);
			return true;
		case "progress_failed":
			return true;
		case "no_results":
			return true;
		default:
			if (outcome.Apply(game, outcome_vars))
			{
				return true;
			}
			game.Warning(ToString() + ": unhandled outcome: " + outcome.id);
			return false;
		}
	}

	public virtual bool ValidateOutcome(OutcomeDef outcome)
	{
		return outcome.Validate(game, this);
	}

	public override void OnUpdate()
	{
		Time time = game.time;
		bool flag = false;
		if (next_tick_time != Time.Zero && next_tick_time <= time)
		{
			flag = true;
			StartTick();
			if (owner.IsAuthority())
			{
				State state = this.state;
				OnTick();
				if (this.state != state)
				{
					return;
				}
			}
		}
		if (state_end_time != Time.Zero && state_end_time <= time)
		{
			flag = true;
			if (owner.IsAuthority())
			{
				SetState(NextState(this.state));
			}
		}
		if (!flag)
		{
			Game.Log($"{this}: Unhandled update, now: {time}, next tick: {next_tick_time}, state end: {state_end_time}", Game.LogType.Error);
			Reschedule();
		}
	}

	public virtual void OnTick()
	{
		State state = this.state;
		ProcessActiveListeners(owner, "prepare_tick", this);
		if (this.state == state)
		{
			UpdateProgress();
		}
	}

	public virtual void UpdateProgress()
	{
		if (def.max_progress <= 0f)
		{
			return;
		}
		float num = CalcProgressAdvance();
		if (num != 0f)
		{
			progress += num;
			if (progress > best_progress)
			{
				best_progress = progress;
			}
			if (progress < 0f)
			{
				OnProgressFail();
			}
			else if (progress >= def.max_progress)
			{
				OnProgressDone();
			}
			else
			{
				SendState();
			}
		}
	}

	public virtual float CalcProgressAdvance()
	{
		float num = ((def.progress_on_tick == null) ? 1f : CalcValue(def.progress_on_tick));
		if (def.progress_factors == null)
		{
			return num;
		}
		SuccessAndFail successAndFail = SuccessAndFail.Get(game, def.progress_factors, this, keep_factors: false);
		if (successAndFail == null)
		{
			return num;
		}
		float num2 = successAndFail.SP;
		float num3 = successAndFail.FP;
		num3 *= best_progress / def.max_progress;
		float num4 = num2 + num3;
		if (num4 <= 0f)
		{
			num2 = 0f;
			num3 = 100f;
		}
		else
		{
			float num5 = 100f / num4;
			num2 *= num5;
			num3 *= num5;
		}
		if (game.Random(0f, 100f) <= num2)
		{
			return num * num2 / 100f;
		}
		return (0f - num) * num3 / 100f;
	}

	public virtual void OnProgressFail()
	{
		if (def.outcomes == null)
		{
			Cancel();
			return;
		}
		OutcomeDef outcomeDef = def.outcomes.Find("proggress_failed");
		if (outcomeDef == null)
		{
			outcomeDef = def.outcomes.Find("fail");
			if (outcomeDef == null)
			{
				Cancel();
				return;
			}
		}
		forced_outcomes = new List<OutcomeDef>();
		forced_outcomes.Add(null);
		forced_outcomes.Add(outcomeDef);
		SetState(NextState(state));
	}

	public virtual void OnProgressDone()
	{
		SetState(NextState(state));
	}

	private void StopUpdating()
	{
		if (next_tick_time != Time.Zero)
		{
			Game.Log($"{this}: Stopped updating while ticking", Game.LogType.Error);
		}
		game.scheduler.Unregister(this);
	}

	private void UpdateAt(Time time)
	{
		float after_seconds = time - game.time;
		game.scheduler.RegisterAfterSeconds(this, after_seconds, exact: false);
	}

	public void AddListener(IListener listener)
	{
		if (listeners == null)
		{
			listeners = new List<IListener>();
		}
		listeners.Add(listener);
	}

	public void DelListener(IListener listener)
	{
		if (listeners != null)
		{
			listeners.Remove(listener);
		}
	}

	public void NotifyListeners(string message, object param = null, bool profile = true)
	{
		if (visuals == null && listeners == null)
		{
			return;
		}
		string text = null;
		if (profile && visuals != null)
		{
			text = visuals.GetType().ToString() + ".on " + message;
			Game.BeginProfileSection(text);
		}
		if (visuals != null)
		{
			try
			{
				visuals.OnMessage(this, message, param);
			}
			catch (Exception ex)
			{
				Game.Log("Error in NotifyListeners('" + message + "'): " + ex.ToString(), Game.LogType.Error);
			}
		}
		if (listeners != null)
		{
			for (int i = 0; i < listeners.Count; i++)
			{
				IListener listener = listeners[i];
				try
				{
					listener.OnMessage(this, message, param);
				}
				catch (Exception ex2)
				{
					Game.Log("Error in NotifyListeners('" + message + "'): " + ex2.ToString(), Game.LogType.Error);
				}
			}
		}
		if (profile && text != null)
		{
			Game.EndProfileSection(text);
		}
	}

	public void Destroy()
	{
		next_tick_time = Time.Zero;
		StopUpdating();
	}

	public virtual List<Value> GetArgs(IVars vars)
	{
		List<Value> result = args;
		if (vars != null)
		{
			Value var = vars.GetVar("args");
			if (!var.is_unknown)
			{
				result = var.Get<List<Value>>();
			}
		}
		return result;
	}

	public virtual Value GetArg(int idx, IVars vars)
	{
		List<Value> list = args;
		if (vars != null)
		{
			Value var = vars.GetVar("args");
			if (!var.is_unknown)
			{
				list = var.Get<List<Value>>();
			}
		}
		if (list == null)
		{
			return Value.Unknown;
		}
		if (idx < 0 || idx >= list.Count)
		{
			return Value.Unknown;
		}
		return list[idx];
	}

	public virtual Object GetTarget(IVars vars = null)
	{
		if (vars == null)
		{
			return target;
		}
		if (vars is Vars vars2)
		{
			Object obj = vars2.GetRaw("target").Get<Object>();
			if (obj != null)
			{
				return obj;
			}
		}
		return target;
	}

	public static string ChanceColorText(Game game, string field_path, float chance)
	{
		DT.Field field = game?.dt?.Find(field_path);
		if (field == null)
		{
			return "";
		}
		List<DT.Field> list = field.Children();
		if (list == null)
		{
			return "";
		}
		for (int i = 0; i < list.Count; i++)
		{
			DT.Field field2 = list[i];
			if (field2.NumValues() == 2)
			{
				float num = field2.Float(0);
				if (!(chance > num))
				{
					string arg = field2.String(1);
					return $"@{'{'}clr:{arg}{'}'}";
				}
			}
		}
		return "";
	}

	public virtual Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		vars = vars ?? this;
		switch (key)
		{
		case "action":
			return this;
		case "owner":
			return owner;
		case "own_character":
			return own_character;
		case "kingdom":
		case "own_kingdom":
		case "src_kingdom":
		case "our_kingdom":
			return own_kingdom;
		case "target":
			return target;
		case "args":
			return new Value(args);
		case "arg":
		case "arg0":
			return GetArg(0, vars);
		case "arg1":
			return GetArg(1, vars);
		case "arg2":
			return GetArg(2, vars);
		case "arg3":
			return GetArg(3, vars);
		case "needs_target":
			return NeedsTarget();
		case "neds_arg":
			return NeedsArgs();
		case "needs_target_or_arg":
			return NeedsTarget() || NeedsArgs();
		case "has_target":
			return HasTarget(vars);
		case "has_all_args":
			return HasAllArgs();
		case "has_target_and_args":
			return HasTarget(vars) && HasAllArgs();
		case "mission_kingdom":
		{
			Kingdom kingdom9 = own_character?.mission_kingdom;
			if (kingdom9 != null)
			{
				return kingdom9;
			}
			if (target_kingdom != null)
			{
				return target_kingdom;
			}
			Kingdom kingdom10 = GetTarget(vars)?.GetKingdom();
			if (kingdom10 != null)
			{
				return kingdom10;
			}
			return Value.Null;
		}
		case "target_kingdom":
		case "tgt_kingdom":
		case "their_kingdom":
		{
			if (target_kingdom != null)
			{
				return target_kingdom;
			}
			Kingdom kingdom3 = GetTarget(vars)?.GetKingdom();
			if (kingdom3 != null)
			{
				return kingdom3;
			}
			Kingdom kingdom4 = own_character?.mission_kingdom;
			if (kingdom4 != null)
			{
				return kingdom4;
			}
			return Value.Null;
		}
		case "severity_id":
			return def.severity_id;
		case "cost":
			return GetCost(GetTarget(vars), vars);
		case "upkeep":
			return GetUpkeep();
		case "active":
			return state != State.Inactive;
		case "preparing":
			return state == State.Preparing;
		case "running":
			return state == State.Running;
		case "state":
			return state.ToString();
		case "state_elapsed":
			return game.time - state_start_time;
		case "progress":
			return progress;
		case "progress_factors_text":
			return GetProgressFactorsText(vars);
		case "progress_factors":
			return new Value(GetProgressFactorsTextVars(vars));
		case "progress_success_factors":
			return new Value(GetProgressFactorsTextVars(vars, success_factors: true, no_factors: false, fail_factors: false));
		case "progress_no_factors":
			return new Value(GetProgressFactorsTextVars(vars, success_factors: false, no_factors: true, fail_factors: false));
		case "progress_fail_factors":
			return new Value(GetProgressFactorsTextVars(vars, success_factors: false, no_factors: false));
		case "outcomes_def":
			return new Value(def.outcomes);
		case "requirements_text":
			return (def.requirements == null) ? null : "Action.requirement_defaults.requirements_text";
		case "requirement_texts":
			return (get_requirement_texts == null) ? null : get_requirement_texts(this);
		case "sf_factors_text":
			return GetSFText(vars);
		case "success_factors":
			return new Value(SuccessAndFail.Get(this, keep_factors: true, vars));
		case "success_chance_text":
			return SuccessChanceValue(non_trivial_only: true, vars);
		case "success_chance":
			return SuccessChanceValue(non_trivial_only: false, vars);
		case "success_chance_clr":
		{
			float chance2 = SuccessChanceValue(non_trivial_only: false, vars).Float();
			return ChanceColorText(game, "SpyPlot.success_chance_colors", chance2);
		}
		case "fail_reveal_chance_clr":
		{
			float chance = GetVar("fail_reveal_chance", vars).Float();
			return ChanceColorText(game, "SpyPlot.reveal_chance_colors", chance);
		}
		case "success_reveal_chance_clr":
		{
			float chance3 = GetVar("success_reveal_chance", vars).Float();
			return ChanceColorText(game, "SpyPlot.reveal_chance_colors", chance3);
		}
		case "reveal_factors_text":
			return GetRFText(vars);
		case "reveal_factors":
			return new Value(SuccessAndFail.Get(game, def.reveal_factors, vars, keep_factors: true));
		case "reveal_chance_text":
			return RevealChanceValue(non_trivial_only: true, vars);
		case "reveal_chance":
			return RevealChanceValue(non_trivial_only: false, vars);
		case "final_reveal_chance":
		{
			float num = SuccessChanceValue(non_trivial_only: false, vars).Float();
			float num2 = GetVar("fail_reveal_chance", vars).Float();
			float num3 = GetVar("success_reveal_chance", vars).Float();
			float num4 = num / 100f;
			return num2 * (1f - num4) + num3 * num4;
		}
		case "target_valid":
		{
			Object obj2 = GetTarget(vars);
			if (!ValidateTarget(obj2))
			{
				return false;
			}
			if (!ValidateArgs())
			{
				return false;
			}
			return true;
		}
		case "target_is_neighbor_kingdom":
		{
			Kingdom kingdom = GetTarget(vars)?.GetKingdom();
			if (kingdom == null)
			{
				return Value.Null;
			}
			Kingdom kingdom2 = own_kingdom;
			if (kingdom2 == null)
			{
				return Value.Null;
			}
			if (kingdom2.neighbors.Contains(kingdom))
			{
				return true;
			}
			return false;
		}
		case "target_is_nearby_kingdom":
		{
			Kingdom kingdom11 = GetTarget(vars)?.GetKingdom();
			if (kingdom11 == null)
			{
				return Value.Null;
			}
			Kingdom kingdom12 = own_kingdom;
			if (kingdom12 == null)
			{
				return Value.Null;
			}
			if (kingdom12.HasNeighbor(kingdom11))
			{
				return true;
			}
			if (kingdom12.HasSecondNeighbor(kingdom11))
			{
				return true;
			}
			return false;
		}
		case "target_kingdom_nearby_eval":
		{
			Kingdom kingdom7 = GetTarget(vars)?.GetKingdom();
			if (kingdom7 == null)
			{
				return 0;
			}
			Kingdom kingdom8 = own_kingdom;
			if (kingdom8 == null)
			{
				return Value.Null;
			}
			if (kingdom8.HasNeighbor(kingdom7))
			{
				return 2;
			}
			if (kingdom8.HasSecondNeighbor(kingdom7))
			{
				return 1;
			}
			return 0;
		}
		case "target_relationship":
		{
			Kingdom kingdom5 = GetTarget(vars)?.GetKingdom();
			if (kingdom5 == null)
			{
				return Value.Null;
			}
			Kingdom kingdom6 = own_kingdom;
			if (kingdom6 == null)
			{
				return Value.Null;
			}
			return kingdom6.GetRelationship(kingdom5);
		}
		case "other_character_doing_the_same":
			return own_kingdom?.GetCharacterWithActionOrStatus(def.unique_id ?? def.id, def.unique_status_id);
		case "validate_key":
			return GetValidateKey(vars);
		case "validate_prompt_text":
			return new Value(GetValidatePrompt(vars));
		case "opportunity_active":
		{
			if (def.opportunity == null)
			{
				return false;
			}
			Object obj = GetTarget(vars);
			Opportunity opportunity = own_character?.actions?.FindOpportunity(this, obj);
			if (opportunity == null)
			{
				return false;
			}
			return opportunity.active;
		}
		case "prepare_voice_line":
		case "done_voice_line":
		case "decline_voice_line":
		case "cancel_voice_line":
		case "cancelled_voice_line":
			return GetActionVoiceLine(key);
		case "success_voice_line":
		case "fail_voice_line":
			return GetActionOutcomeVoiceLine(key, vars, as_value);
		case "def_id":
			return def?.id;
		default:
			if (def?.field != null)
			{
				DT.Field field = def.field.FindChild(key, this);
				if (field != null)
				{
					if (!as_value)
					{
						return new Value(field);
					}
					Value result = field.Value(vars ?? this);
					if (!result.is_unknown)
					{
						return result;
					}
				}
			}
			if (owner != null)
			{
				return owner.GetVar(key, vars, as_value);
			}
			return Value.Unknown;
		}
	}

	public string GetActionOutcomeVoiceLine(string key, IVars vars, bool as_value)
	{
		if (!string.IsNullOrEmpty(GetVar("outcome_voice_line", vars, as_value).String()))
		{
			return Value.Null;
		}
		return GetActionVoiceLine(key);
	}

	public string GetActionVoiceLine(string key)
	{
		bool num = def.field.GetBool("voice_from_character");
		string randomString = def.field.GetRandomString(key, this);
		if (num)
		{
			string text = GetVoicingCharacter()?.GetVoiceLine(randomString);
			if (text != null)
			{
				return text;
			}
		}
		return randomString;
	}

	public virtual Character GetVoicingCharacter()
	{
		return own_character;
	}

	public virtual IVars GetVoiceVars()
	{
		return owner;
	}

	public string GetProgressFactorsText(IVars vars)
	{
		if (def?.progress_factors == null)
		{
			return null;
		}
		return SuccessAndFail.Get(game, def.progress_factors, vars ?? this, keep_factors: true)?.FactorsText();
	}

	public List<Vars> GetProgressFactorsTextVars(IVars vars, bool success_factors = true, bool no_factors = true, bool fail_factors = true)
	{
		if (def?.progress_factors == null)
		{
			return null;
		}
		return SuccessAndFail.Get(game, def.progress_factors, vars ?? this, keep_factors: true)?.FactorsTextVars(success_factors, no_factors, fail_factors);
	}

	public string GetSFText(IVars vars)
	{
		if (def?.field == null)
		{
			return null;
		}
		if (def.field.GetBool("sf_per_target") && GetTarget(vars) == null)
		{
			return null;
		}
		return SuccessAndFail.Get(this, keep_factors: true, vars)?.FactorsText();
	}

	public string GetRFText(IVars vars)
	{
		if (def?.field == null)
		{
			return null;
		}
		return SuccessAndFail.Get(game, def.reveal_factors, vars, keep_factors: true)?.FactorsText();
	}

	public Value SuccessChanceValue(bool non_trivial_only, IVars vars = null)
	{
		if (def?.field != null && def.field.GetBool("sf_per_target") && GetTarget(vars) == null)
		{
			return Value.Null;
		}
		SuccessAndFail successAndFail = SuccessAndFail.Get(this, keep_factors: false, vars);
		OutcomeDef outcomeDef = def?.outcomes?.Find("success");
		if (outcomeDef != null)
		{
			if (non_trivial_only && outcomeDef.field.value.is_number && outcomeDef.field.value.Int() >= 100)
			{
				return Value.Null;
			}
			outcomeDef.CalcChance(vars ?? this, -1f, forced_outcomes, AlterOutcomeChance);
			if (outcomeDef.chance < 0f)
			{
				return Value.Null;
			}
			if (outcomeDef.chance >= 100f)
			{
				if (non_trivial_only && successAndFail == null)
				{
					return Value.Null;
				}
				return 100;
			}
			return outcomeDef.chance;
		}
		if (successAndFail != null)
		{
			return successAndFail.Chance();
		}
		if (non_trivial_only)
		{
			return Value.Null;
		}
		return 100;
	}

	public Value RevealChanceValue(bool non_trivial_only, IVars vars = null)
	{
		SuccessAndFail successAndFail = SuccessAndFail.Get(game, def.reveal_factors, vars, keep_factors: true);
		if (successAndFail == null)
		{
			return Value.Null;
		}
		int num = successAndFail.Chance();
		if (num <= 0 && non_trivial_only)
		{
			return Value.Null;
		}
		return num;
	}

	public string DebugSuccessFailPoints()
	{
		SuccessAndFail successAndFail = SuccessAndFail.Get(this, keep_factors: false);
		if (successAndFail == null)
		{
			return null;
		}
		string text = "#" + successAndFail.value + " (" + successAndFail.SP + " - " + successAndFail.FP;
		if ((float)successAndFail.SP_perc != 0f)
		{
			text = text + " + " + successAndFail.SP_perc + "%";
		}
		if ((float)successAndFail.FP_perc != 0f)
		{
			text = text + " - " + successAndFail.FP_perc + "%";
		}
		return text + ")";
	}

	public static Action Find(Object obj, string id)
	{
		return obj?.GetComponent<Actions>()?.Find(id);
	}

	public static Action Find(Object obj, Def def)
	{
		return obj?.GetComponent<Actions>()?.Find(def);
	}

	public virtual float Eval()
	{
		return ProsAndCons.Get(this)?.eval ?? 0f;
	}

	public virtual bool CheckProCons()
	{
		return ProsAndCons.Get(this)?.CheckThreshold("use") ?? true;
	}

	public virtual string AIValidate()
	{
		if (def.ai_validate == null)
		{
			return null;
		}
		if (def.ai_validate.value.type == Value.Type.Int && def.ai_validate.value.int_val == 0)
		{
			return null;
		}
		string text = Validate(quick_out: true);
		if (text != "ok")
		{
			if (text == null)
			{
				return "";
			}
			return text;
		}
		if (!def.ai_validate.Bool(this))
		{
			return "";
		}
		return "ok";
	}

	public virtual bool AIPickTarget(out Object target)
	{
		target = null;
		if (!NeedsTarget())
		{
			return true;
		}
		List<Object> possibleTargets = GetPossibleTargets();
		if (possibleTargets == null || possibleTargets.Count == 0)
		{
			return false;
		}
		if (def.ai_eval_target == null)
		{
			int index = game.Random(0, possibleTargets.Count);
			target = possibleTargets[index];
			return true;
		}
		Object obj = this.target;
		WeightedRandom<Object> temp = WeightedRandom<Object>.GetTemp();
		for (int i = 0; i < possibleTargets.Count; i++)
		{
			Object val = possibleTargets[i];
			if (ValidateTarget(val))
			{
				this.target = val;
				float num = def.ai_eval_target.Float(this);
				if (!(num <= 0f))
				{
					temp.AddOption(val, num);
				}
			}
		}
		this.target = obj;
		target = temp.Choose();
		return temp.options.Count > 0;
	}

	public virtual Value AIPickArg(int idx, List<Value> options)
	{
		if (options == null || options.Count == 0)
		{
			return Value.Unknown;
		}
		int index = game.Random(0, options.Count);
		return options[index];
	}

	public virtual bool AIPickArgs()
	{
		if (!NeedsArgs())
		{
			return true;
		}
		List<Value>[] possibleArgs = GetPossibleArgs();
		if (possibleArgs == null || possibleArgs.Length != def.arg_types.Count)
		{
			return false;
		}
		foreach (List<Value> list in possibleArgs)
		{
			if (list == null || list.Count == 0)
			{
				return false;
			}
		}
		if (args == null)
		{
			args = new List<Value>(def.arg_types.Count);
		}
		else
		{
			args.Clear();
		}
		for (int j = 0; j < def.arg_types.Count; j++)
		{
			List<Value> options = possibleArgs[j];
			Value item = AIPickArg(j, options);
			if (item.is_unknown)
			{
				return false;
			}
			args.Add(item);
		}
		return true;
	}

	public string AIThink(out Object target)
	{
		target = null;
		string text = AIValidate();
		if (text != "ok")
		{
			return text;
		}
		if (!AIPickTarget(out target))
		{
			return "no_target";
		}
		if (!AIPickArgs())
		{
			return "no_args";
		}
		return "ok";
	}

	public string ait()
	{
		Object obj;
		string text = AIThink(out obj);
		if (text != "ok")
		{
			return text;
		}
		if (obj == null && !NeedsTarget())
		{
			return text;
		}
		return "ok: " + (obj?.ToString() ?? "null");
	}

	public string aie()
	{
		Object obj;
		string text = AIThink(out obj);
		if (text != "ok")
		{
			return text;
		}
		if (!Execute(obj))
		{
			return "failed:" + (obj?.ToString() ?? "null");
		}
		return "ok: " + (obj?.ToString() ?? "null");
	}
}
