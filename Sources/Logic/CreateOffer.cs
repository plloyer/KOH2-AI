using System;
using System.Collections;
using System.Collections.Generic;

namespace Logic;

public class Offer : BaseObject, IVars, RelationUtils.IRelChangeValidator
{
	public class Def : Logic.Def
	{
		public Offer instance;

		public Serialization.ObjectTypeInfo sender_ti;

		public Serialization.ObjectTypeInfo receiver_ti;

		public List<string> arg_types;

		public Dictionary<int, HashSet<string>> allowed_offers_per_arg;

		public ProsAndCons.Def propose_pc_def;

		public ProsAndCons.Def accept_pc_def;

		public OutcomeDef outcomes;

		public string default_outcome;

		public float timeout;

		public string rel_change_type = "neutral";

		public int min_args;

		public int max_args;

		public float pro_con_difference_cover_perc_min;

		public float pro_con_difference_cover_perc_max;

		public bool disabled;

		public float ai_propose_cooldown;

		public bool force_needs_answer;

		public bool can_be_sent_without_parent_offer = true;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			sender_ti = Serialization.ObjectTypeInfo.Get(field.GetString("from"));
			receiver_ti = Serialization.ObjectTypeInfo.Get(field.GetString("to"));
			timeout = field.GetFloat("timeout");
			rel_change_type = field.GetString("rel_change_type", null, "rel_change_type");
			disabled = field.GetBool("disabled", null, disabled);
			force_needs_answer = field.GetBool("force_needs_answer", null, force_needs_answer);
			can_be_sent_without_parent_offer = field.GetBool("can_be_sent_without_parent_offer", null, can_be_sent_without_parent_offer);
			ai_propose_cooldown = field.GetFloat("ai_propose_cooldown", null, ai_propose_cooldown);
			DT.Field field2 = field.FindChild("pro_con_difference_cover_perc");
			if (field2 != null)
			{
				pro_con_difference_cover_perc_min = field2.Value(0);
				pro_con_difference_cover_perc_max = field2.Value(1);
			}
			LoadArgs(game);
			LoadOutcomes(game);
			return true;
		}

		private void AddArgTypeName(Game game, DT.Field f, string type_name)
		{
			arg_types.Add(type_name);
			if (type_name.EndsWith("?", StringComparison.Ordinal))
			{
				type_name = type_name.Substring(0, type_name.Length - 1);
			}
			else
			{
				min_args++;
			}
			switch (type_name)
			{
			case "float":
				return;
			case "string":
				return;
			case "Offer":
				return;
			case "List":
				return;
			}
			if (Serialization.ObjectTypeInfo.Get(type_name) == null)
			{
				game.Error(f.Path(include_file: true) + ": Invalid offer argument type '" + type_name + "'");
			}
		}

		private void LoadArgs(Game game)
		{
			DT.Field field = base.field;
			DT.Field field2 = field.FindChild("args");
			min_args = 0;
			max_args = 0;
			if (field2 == null)
			{
				return;
			}
			int num = field2.NumValues();
			if (num > 0)
			{
				arg_types = new List<string>(num);
				for (int i = 0; i < num; i++)
				{
					string type_name = field2.String(i);
					AddArgTypeName(game, field, type_name);
				}
			}
			else
			{
				if (field2.children == null)
				{
					return;
				}
				num = field2.children.Count;
				if (num <= 0)
				{
					return;
				}
				arg_types = new List<string>(num);
				allowed_offers_per_arg?.Clear();
				for (int j = 0; j < num; j++)
				{
					DT.Field field3 = field2.children[j];
					if (string.IsNullOrEmpty(field3.key))
					{
						continue;
					}
					AddArgTypeName(game, field, field3.type);
					if (!field3.type.Contains("Offer"))
					{
						continue;
					}
					if (allowed_offers_per_arg == null)
					{
						allowed_offers_per_arg = new Dictionary<int, HashSet<string>>();
					}
					HashSet<string> hashSet = new HashSet<string>();
					if (field3.value.obj_val is DT.Field field4)
					{
						int num2 = field4.NumValues();
						for (int k = 0; k < num2; k++)
						{
							hashSet.Add(field4.String(k));
						}
						allowed_offers_per_arg.Add(j, hashSet);
					}
				}
			}
			max_args = arg_types.Count;
		}

		private void LoadOutcomes(Game game)
		{
			DT.Field field = base.field;
			default_outcome = field.GetString("default_outcome", null, null);
			DT.Field field2 = field.FindChild("outcomes");
			if (field2 != null)
			{
				DT.Field defaults = field.FindChild("outcome_defaults");
				outcomes = new OutcomeDef(game, field2, defaults);
				if (!ValidateOutcomes(game))
				{
					outcomes = null;
				}
			}
		}

		public bool ValidateOutcomes(Game game)
		{
			if (outcomes == null)
			{
				return false;
			}
			bool result = true;
			if (outcomes.alsos != null)
			{
				Game.Log(outcomes.field.Path(include_file: true) + ": alsos are not allower in Offer outcomes root sections", Game.LogType.Error);
				result = false;
			}
			if (outcomes.rolls_field != null)
			{
				Game.Log(outcomes.field.Path(include_file: true) + ": rolls are not allower in Offer outcomes root sections", Game.LogType.Error);
				result = false;
			}
			if (outcomes.options == null)
			{
				return false;
			}
			for (int i = 0; i < outcomes.options.Count; i++)
			{
				OutcomeDef outcomeDef = outcomes.options[i];
				string text = outcomeDef.field.Type();
				if (text != "main" && text != "option")
				{
					Game.Log(outcomes.field.Path(include_file: true) + "." + outcomeDef.key + ": invalid option type, must be 'main' or 'option'", Game.LogType.Error);
					result = false;
				}
				if (text == "main")
				{
					if (default_outcome != null)
					{
						Game.Log(outcomes.field.Path(include_file: true) + "." + outcomeDef.key + ": default outcome is already defined as '" + default_outcome + "'", Game.LogType.Error);
						result = false;
					}
					default_outcome = outcomeDef.key;
				}
				if (outcomeDef.chance_field != null && outcomeDef.chance_field.value.is_valid)
				{
					Game.Log(outcomes.field.Path(include_file: true) + "." + outcomeDef.key + ": chances are not allowed in Offer outcomes root sections", Game.LogType.Error);
					result = false;
				}
			}
			if (default_outcome == null)
			{
				Game.Log(outcomes.field.Path(include_file: true) + ": no default outcome is defined", Game.LogType.Error);
				result = false;
			}
			return result;
		}

		public override bool Validate(Game game)
		{
			ResolveProsAndCons(game);
			return true;
		}

		private ProsAndCons.Def FindPCDef(Game game, string prefix, string threshold)
		{
			ProsAndCons.Def def = game.defs.Find<ProsAndCons.Def>(prefix + base.id);
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
			propose_pc_def = FindPCDef(game, "PC_Propose_", "propose");
			accept_pc_def = FindPCDef(game, "PC_Accept_", "accept");
		}

		public Offer GetInstance(Object from, Object to, Offer parent = null, int parentArgIndex = 0, List<Value> args = null)
		{
			if (instance == null)
			{
				instance = Create(this, from, to);
			}
			instance.AI = false;
			instance.SetParent(parent);
			instance.from = from;
			instance.to = to;
			parent?.SetArg(parentArgIndex, instance);
			if (args != null)
			{
				instance.args = args;
			}
			else if (instance.args != null)
			{
				instance.args.Clear();
			}
			return instance;
		}

		public Offer GetInstance(Object from, Object to, Offer parent = null, int parentArgIndex = 0, params Value[] args)
		{
			if (instance == null)
			{
				instance = Create(this, from, to);
			}
			instance.AI = false;
			instance.SetParent(parent);
			instance.from = from;
			instance.to = to;
			parent?.SetArg(parentArgIndex, instance);
			if (instance.args != null)
			{
				instance.args.Clear();
			}
			for (int i = 0; i < args.Length; i++)
			{
				instance.SetArg(i, args[i]);
			}
			return instance;
		}
	}

	public class RefData : Data
	{
		public NID to_nid = NID.Null;

		public int uoid;

		public string answer;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(Offer " + uoid + " to " + to_nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Offer offer))
			{
				return false;
			}
			if (offer.to == null)
			{
				return false;
			}
			to_nid = offer.to;
			uoid = offer.uoid;
			answer = offer.answer;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID(to_nid, "to");
			ser.Write7BitUInt(uoid, "uoid");
			ser.WriteStr(answer, "answer");
		}

		public override void Load(Serialization.IReader ser)
		{
			to_nid = ser.ReadNID("to");
			uoid = ser.Read7BitUInt("uoid");
			answer = ser.ReadStr("answer");
		}

		public override object GetObject(Game game)
		{
			return Offers.Get(to_nid.GetObj(game), create: false)?.FindIncoming(uoid);
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Offer offer))
			{
				return false;
			}
			if (offer.uoid != uoid)
			{
				return false;
			}
			offer.answer = answer;
			return true;
		}
	}

	public class FullData : RefData
	{
		public string def_id;

		public NID from_nid = NID.Null;

		public List<Data> args = new List<Data>();

		public float exp_time_delta;

		public Data vars_data;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Offer offer))
			{
				return false;
			}
			base.InitFrom(obj);
			def_id = offer.def.id;
			if (offer.from != null)
			{
				from_nid = offer.from;
			}
			answer = offer.answer;
			if (offer.expire_time != Time.Zero)
			{
				exp_time_delta = offer.expire_time - offer.game.time;
				if (exp_time_delta < 0f)
				{
					exp_time_delta = 0f;
				}
			}
			else
			{
				exp_time_delta = float.NegativeInfinity;
			}
			if (offer.args != null)
			{
				for (int i = 0; i < offer.args.Count; i++)
				{
					Data item = offer.args[i].CreateData();
					args.Add(item);
				}
			}
			vars_data = Data.CreateFull(offer.vars);
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteStr(def_id, "def");
			ser.WriteNID(from_nid, "from");
			ser.WriteFloat(exp_time_delta, "exp_time_delta");
			int count = args.Count;
			ser.Write7BitUInt(count, "args_count");
			for (int i = 0; i < count; i++)
			{
				ser.WriteData(args[i], "arg_", i);
			}
			ser.WriteData(vars_data, "vars_data");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			def_id = ser.ReadStr("def");
			from_nid = ser.ReadNID("from");
			exp_time_delta = ser.ReadFloat("exp_time_delta");
			int num = ser.Read7BitUInt("args_count");
			for (int i = 0; i < num; i++)
			{
				Data item = ser.ReadData("arg_", i);
				args.Add(item);
			}
			vars_data = ser.ReadData("vars_data");
		}

		public override object GetObject(Game game)
		{
			object obj = base.GetObject(game);
			if (obj == null)
			{
				Object obj2 = to_nid.GetObj(game);
				Object obj3 = from_nid.GetObj(game);
				obj = Offer.Create(game.defs.Find<Def>(def_id), obj3, obj2);
			}
			return obj;
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!(obj is Offer offer))
			{
				return false;
			}
			offer.def = game.defs.Find<Def>(def_id);
			offer.uoid = uoid;
			offer.to = to_nid.GetObj(game);
			offer.from = from_nid.GetObj(game);
			offer.answer = answer;
			if (float.IsNegativeInfinity(exp_time_delta))
			{
				offer.expire_time = Time.Zero;
			}
			else
			{
				offer.expire_time = game.time + exp_time_delta;
			}
			if (offer.args == null)
			{
				offer.args = new List<Value>();
			}
			else
			{
				offer.args.Clear();
			}
			for (int i = 0; i < args.Count; i++)
			{
				Value value = args[i].GetValue(game);
				if (value.obj_val is Offer offer2)
				{
					offer2.parent = offer;
				}
				offer.args.Add(value);
			}
			vars_data?.ApplyTo(offer.vars, offer.game);
			return true;
		}
	}

	public delegate Offer CreateOffer(Def def, Object from, Object to);

	public delegate Offer CreateOfferWithParams(Def def, Object from, Object to, params Value[] args);

	public delegate Offer CreateOfferByNameWithParams(string def_id, Object from, Object to, params Value[] args);

	public delegate Offer CreateOfferByNameWithParentAndParams(string def_id, Object from, Object to, Offer parent, params Value[] args);

	public static Dictionary<string, DBGOffersData> dbg_offers_data = new Dictionary<string, DBGOffersData>();

	public int uoid;

	public Def def;

	public bool AI;

	public Object from;

	public Object to;

	public List<Value> args;

	public Offer parent;

	public Time expire_time = Time.Zero;

	public Vars vars = new Vars();

	public IListener visuals;

	private List<IListener> listeners;

	public object msg_icon;

	public object msg_icon_left;

	public string answer;

	public List<OutcomeDef> outcomes;

	public List<OutcomeDef> unique_outcomes;

	public Vars outcome_vars;

	public static List<Value> tmp_values = new List<Value>(1000);

	public static List<Offer> tmp_offers = new List<Offer>(1000);

	public Game game => to.game;

	public override bool IsRefSerializable()
	{
		if (to == null || !to.IsRefSerializable())
		{
			return false;
		}
		Offers offers = Offers.Get(to, create: false);
		if (offers == null)
		{
			return false;
		}
		if (uoid <= 0 || uoid > offers.last_uoid)
		{
			return false;
		}
		if (offers.FindIncoming(uoid) == null)
		{
			return false;
		}
		return true;
	}

	public Offer()
	{
	}

	public Offer(Def def, int uoid)
	{
		this.def = def;
		this.uoid = uoid;
	}

	public Offer(Def def, Object from, Object to)
	{
		this.def = def;
		this.from = from;
		this.to = to;
		if (!ValidateTypes(check_args: false))
		{
			game.Error("Invalid offer created: " + ToString());
		}
		SetArgs();
	}

	public Offer(Object from, Object to, params Value[] args)
	{
		this.from = from;
		this.to = to;
		DecideDef();
		SetArgs(args);
	}

	public Offer Copy()
	{
		Offer offer = OfferGenerator.instance?.CreateNewOffer(def, from, to);
		if (args != null)
		{
			for (int i = 0; i < args.Count; i++)
			{
				if (args[i].obj_val is Offer offer2)
				{
					Offer offer3 = offer2.Copy();
					offer3.SetParent(offer);
					offer.SetArg(i, offer3);
				}
				else
				{
					offer.SetArg(i, args[i]);
				}
			}
		}
		offer.AI = AI;
		offer.answer = answer;
		offer.vars = vars?.Copy();
		if (listeners != null)
		{
			for (int j = 0; j < listeners.Count; j++)
			{
				offer.AddListener(listeners[j]);
			}
		}
		return offer;
	}

	public virtual void SetArgs(params Value[] args)
	{
		if (args.Length != 0)
		{
			this.args = new List<Value>(args);
		}
		else
		{
			this.args = null;
		}
		if (!ValidateTypes(check_args: true))
		{
			game.Error("Invalid offer created: " + ToString());
		}
	}

	public void SetArg(int idx, Value value)
	{
		if (idx >= 0 && idx < def.max_args)
		{
			if (args == null)
			{
				args = new List<Value>(def.max_args);
			}
			while (args.Count <= idx)
			{
				args.Add(Value.Null);
			}
			args[idx] = value;
		}
	}

	public Value GetArg(int idx)
	{
		if (args == null)
		{
			return Value.Null;
		}
		if (idx < 0 || idx >= args.Count)
		{
			return Value.Null;
		}
		if (args[idx].obj_val is EmptyOffer)
		{
			return Value.Null;
		}
		return args[idx];
	}

	public int GetArgsCount()
	{
		if (args == null)
		{
			return 0;
		}
		return args.Count;
	}

	public T GetArg<T>(int idx) where T : class
	{
		return GetArg(idx).Get<T>();
	}

	public bool Equals(Offer o)
	{
		if (o == null)
		{
			return false;
		}
		if (o.def != def)
		{
			return false;
		}
		if (o.from != from)
		{
			return false;
		}
		if (o.to != to)
		{
			return false;
		}
		if (o.args == null != (args == null))
		{
			return false;
		}
		if (args == null)
		{
			return true;
		}
		if (o.args.Count != args.Count)
		{
			return false;
		}
		for (int i = 0; i < args.Count; i++)
		{
			Value value = o.args[i];
			Value value2 = args[i];
			if (value != value2)
			{
				return false;
			}
		}
		return true;
	}

	public override string ToString()
	{
		string text = GetType().Name;
		if (def != null && def.id != text)
		{
			text = text + " " + def.id;
		}
		text = text + " from " + ((from == null) ? "null" : from.ToString());
		text = text + " to " + to.ToString();
		if (args != null && args.Count > 0)
		{
			text += ": ";
			for (int i = 0; i < args.Count; i++)
			{
				Value value = args[i];
				if (i != 0)
				{
					text += ", ";
				}
				text += value.ToString();
			}
		}
		try
		{
			if (Validate() != "ok")
			{
				text = "[Invalid] " + text;
			}
		}
		catch
		{
		}
		return text;
	}

	public virtual void DecideDef()
	{
		Type type = rtti.type;
		while (type != null)
		{
			def = game.defs.Get<Def>(type.Name);
			if (def != null)
			{
				break;
			}
			type = type.BaseType;
		}
	}

	public static Offer Create(Def def, Object from, Object to)
	{
		Type type = null;
		for (DT.Field field = def.field; field != null; field = field.based_on)
		{
			type = Type.GetType("Logic." + field.key);
			if (type != null)
			{
				break;
			}
		}
		if (type == null)
		{
			return null;
		}
		if (type == typeof(Offer))
		{
			Game.Log("Trying to instantiate the base Offer class!", Game.LogType.Error);
			return new Offer(def, from, to);
		}
		Reflection.TypeInfo typeInfo = Reflection.GetTypeInfo(type);
		CreateOffer createOffer = typeInfo.FindCreateMethod(typeof(Offer), typeof(Def), typeof(Object), typeof(Object))?.func as CreateOffer;
		try
		{
			if (createOffer != null)
			{
				return createOffer(def, from, to);
			}
			return Reflection.CreateObjectViaReflection<Offer>(type, new object[3] { def, from, to });
		}
		catch (Exception ex)
		{
			Game.Log("Error creating " + typeInfo.name + ": " + ex, Game.LogType.Error);
			return null;
		}
	}

	public static Offer Create(Def def, Object from, Object to, params Value[] args)
	{
		Offer offer = Create(def, from, to);
		if (offer == null)
		{
			return null;
		}
		offer.SetArgs(args);
		return offer;
	}

	public static Offer Create(string def_id, Object from, Object to, params Value[] args)
	{
		Def def = to.game.defs.Get<Def>(def_id);
		if (def == null)
		{
			return null;
		}
		return Create(def, from, to, args);
	}

	public static Offer Create(string def_id, Object from, Object to, Offer parent, params Value[] args)
	{
		Offer offer = Create(def_id, from, to, args);
		offer.SetParent(parent);
		return offer;
	}

	public static Offer GetCachedOffer(string def_id, Object from, Object to, Offer parent = null, int parentArgIndex = 0, List<Value> args = null)
	{
		return OfferGenerator.instance.GetOfferDef(def_id)?.GetInstance(from, to, parent, parentArgIndex, args);
	}

	public static Offer GetCachedOffer(string def_id, Object from, Object to, Offer parent = null, int parentArgIndex = 0, params Value[] args)
	{
		return OfferGenerator.instance.GetOfferDef(def_id)?.GetInstance(from, to, parent, parentArgIndex, args);
	}

	public virtual bool MatchArg(int idx, Value val)
	{
		if (idx < 0 || idx >= def.max_args)
		{
			return false;
		}
		string text = def.arg_types[idx];
		if (text.EndsWith("?", StringComparison.Ordinal))
		{
			if (!val.is_valid)
			{
				return true;
			}
			text = text.Substring(0, text.Length - 1);
		}
		switch (text)
		{
		case "int":
			return val.type == Value.Type.Int;
		case "float":
			return val.type == Value.Type.Float;
		case "string":
			return val.type == Value.Type.String;
		case "Offer":
		{
			if (!val.is_valid)
			{
				return true;
			}
			if (!(val.obj_val is Offer offer))
			{
				return false;
			}
			HashSet<string> value = null;
			if (!def.allowed_offers_per_arg.TryGetValue(idx, out value))
			{
				return false;
			}
			return value.Contains(offer.def.field.key);
		}
		case "List":
			if (val.is_valid)
			{
				if (val.is_object)
				{
					return val.obj_val is IList;
				}
				return false;
			}
			return true;
		default:
			if (!val.is_object)
			{
				return false;
			}
			if (!(val.obj_val is Object obj))
			{
				return false;
			}
			return Serialization.ObjectTypeInfo.Get(text)?.type.IsAssignableFrom(obj.rtti.type) ?? false;
		}
	}

	public virtual bool ValidateTypes(bool check_args)
	{
		if (def == null)
		{
			return false;
		}
		if (to == null)
		{
			return false;
		}
		if (!def.receiver_ti.type.IsAssignableFrom(to.rtti.type))
		{
			return false;
		}
		if (from == null != (def.sender_ti == null))
		{
			return false;
		}
		if (from != null && !def.sender_ti.type.IsAssignableFrom(from.rtti.type))
		{
			return false;
		}
		if (!check_args)
		{
			return true;
		}
		if (def.min_args == 0)
		{
			return true;
		}
		if (args == null)
		{
			return true;
		}
		if (args.Count < def.min_args || args.Count > def.max_args)
		{
			return false;
		}
		for (int i = 0; i < args.Count; i++)
		{
			Value val = args[i];
			if (val.is_valid && !MatchArg(i, val))
			{
				return false;
			}
		}
		return true;
	}

	public virtual bool Expired()
	{
		if (expire_time == Time.Zero)
		{
			return false;
		}
		if (game.time < expire_time)
		{
			return false;
		}
		return true;
	}

	public virtual void GetProgress(out float cur, out float max)
	{
		if (expire_time == Time.Zero)
		{
			cur = (max = 0f);
			return;
		}
		max = def.timeout;
		cur = max - (expire_time - game.time);
		if (cur < 0f)
		{
			cur = 0f;
		}
		else if (cur > max)
		{
			cur = max;
		}
	}

	protected string ValidateCore()
	{
		if (def.disabled)
		{
			return "disabled";
		}
		if (!HasValidParent())
		{
			return "no_valid_parent";
		}
		if (Expired())
		{
			return "expired";
		}
		if (!IsValidForAI())
		{
			return "no_valid_for_ai";
		}
		if (parent == null && NeedsAnswer() && !(this is LeadCrusadeOffer) && !(this is DemandSovereignDefendOffer))
		{
			Offer offer = Offers.Find(from, to);
			if (offer == null)
			{
				offer = Offers.Find(to, from);
			}
			if (offer != null && offer != this)
			{
				return "pending_offer_exists";
			}
		}
		return "ok";
	}

	protected bool ShouldReturn(string validateStr)
	{
		if (validateStr != "ok")
		{
			return !validateStr.StartsWith("_", StringComparison.Ordinal);
		}
		return false;
	}

	protected bool ShouldSkip(string validateStr)
	{
		return validateStr.StartsWith("_", StringComparison.Ordinal);
	}

	public virtual string ValidateWithoutArgs()
	{
		string text = ValidateCore();
		if (ShouldReturn(text))
		{
			return text;
		}
		if (!ShouldSkip(text))
		{
			for (int i = 0; i < def.max_args; i++)
			{
				tmp_values.Clear();
				if (!GetPossibleArgValues(i, tmp_values))
				{
					text = "_no_possible_args";
					break;
				}
			}
		}
		return text;
	}

	public virtual string Validate()
	{
		string text = ValidateCore();
		if (ShouldReturn(text))
		{
			return text;
		}
		if (args == null)
		{
			if (def.max_args > 0)
			{
				return "no_args_set";
			}
			return "ok";
		}
		if (args.Count < def.min_args)
		{
			return "too_few_args_set";
		}
		if (args.Count > def.max_args)
		{
			return "too_many_args_set";
		}
		for (int i = 0; i < args.Count; i++)
		{
			Value val = args[i];
			if (!MatchArg(i, val))
			{
				return "_invalid_args";
			}
			Object obj = val.Get<Object>();
			if (obj != null && !obj.IsValid())
			{
				return "_invalid_args";
			}
		}
		return "ok";
	}

	public bool Update()
	{
		if (Validate() == "ok")
		{
			return true;
		}
		if (Expired())
		{
			OnExpire();
		}
		Offers.Del(from, this);
		Offers.Del(to, this);
		return false;
	}

	public virtual bool HasValidParent()
	{
		if (parent == null)
		{
			if (!def.can_be_sent_without_parent_offer)
			{
				return false;
			}
			return true;
		}
		if (parent.args == null)
		{
			return false;
		}
		for (int i = 0; i < parent.args.Count; i++)
		{
			if (parent.args[i].obj_val as Offer == this)
			{
				return parent.MatchArg(i, parent.args[i]);
			}
		}
		return false;
	}

	public virtual bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		lst.Clear();
		return true;
	}

	public virtual bool Send(bool create_uoid = true)
	{
		using (Game.Profile("Offer.Send"))
		{
			using (Game.Profile("Offer.Validate on Send"))
			{
				if (Validate() != "ok")
				{
					return false;
				}
			}
			if (def.instance == this)
			{
				return Copy().Send();
			}
			Offers offers = Offers.Get(to, create: true);
			if (create_uoid)
			{
				uoid = ++offers.last_uoid;
			}
			else
			{
				offers.last_uoid = uoid;
			}
			if (!to.IsAuthority())
			{
				to.SendEvent(new Object.SendOfferEvent(this));
				return false;
			}
			if (DBGOffersData.tracking_enabled)
			{
				using (Game.Profile("Offer.Debug RecordSending"))
				{
					dbg_offers_data[def.field.key].RecordSending(this);
				}
			}
			if (Resolve())
			{
				SendAnalytics();
				return false;
			}
			if (def.timeout >= 0f)
			{
				expire_time = game.time + def.timeout;
			}
			else
			{
				expire_time = Time.Zero;
			}
			Offers.Add(from, this);
			Offers.Add(to, this);
			SendAnalytics();
			return true;
		}
	}

	public void Cancel()
	{
		if (!to.IsAuthority())
		{
			to.SendEvent(new Object.CancelOfferEvent(this));
		}
		Offers.Del(from, this);
		Offers.Del(to, this);
	}

	public virtual bool NeedsAnswer()
	{
		if (def.force_needs_answer)
		{
			return true;
		}
		if (def.outcomes == null || def.outcomes.options == null || def.outcomes.options.Count < 2)
		{
			return false;
		}
		return true;
	}

	public virtual bool Resolve()
	{
		using (Game.Profile("Offer.Resolve"))
		{
			if (def.outcomes == null || def.outcomes.options == null || def.outcomes.options.Count == 0)
			{
				Answer(def.default_outcome ?? "accept");
				return true;
			}
			if (def.outcomes.options.Count == 1)
			{
				Answer(def.outcomes.options[0].key);
				return true;
			}
			if (answer != null)
			{
				Answer(answer);
				return true;
			}
			if (!(to is Kingdom kingdom))
			{
				return false;
			}
			if (kingdom.is_player || kingdom.ai == null || !kingdom.ai.Enabled(KingdomAI.EnableFlags.Diplomacy))
			{
				return false;
			}
			Offer counterOffer;
			string text = DecideAIAnswer(out counterOffer);
			if (text == "counter_offer")
			{
				answer = text;
				from.FireEvent("offer_answered", this);
				to.FireEvent("offer_answered", this);
				counterOffer.Send();
			}
			else
			{
				Answer(text ?? def.default_outcome ?? "decline");
			}
			return true;
		}
	}

	public virtual string DecideAIAnswer(out Offer counterOffer)
	{
		using (Game.Profile("Offer.DecideAIAnswer"))
		{
			counterOffer = null;
			if (answer != null)
			{
				return answer;
			}
			if (IsWar(sender: false))
			{
				Kingdom kingdom = to as Kingdom;
				if (kingdom?.ai == null || !kingdom.ai.Enabled(KingdomAI.EnableFlags.Wars))
				{
					return "decline";
				}
			}
			if (ProsAndCons.Get(this, "accept") == null)
			{
				return null;
			}
			float num = Eval("accept");
			if (num > 0f)
			{
				return "accept";
			}
			if (def.field.key.IndexOf("Peace", StringComparison.Ordinal) >= 0)
			{
				return "decline";
			}
			counterOffer = GetCachedOffer("CounterOffer", to, from);
			SetParent(counterOffer);
			counterOffer.SetArg(0, this);
			if (Validate() != "ok")
			{
				SetParent(null);
				return "decline";
			}
			float evalToCoverMin = num * counterOffer.def.pro_con_difference_cover_perc_min / 100f;
			float evalToCoverMax = num * counterOffer.def.pro_con_difference_cover_perc_max / 100f;
			OfferGenerator.instance.FillOfferArgs("propose", 1, counterOffer, evalToCoverMin, evalToCoverMax);
			if (counterOffer.Validate() == "ok")
			{
				return "counter_offer";
			}
			SetParent(null);
			return "decline";
		}
	}

	public virtual bool ConsiderAISweetenOffer()
	{
		using (Game.Profile("Offer.ConsiderAISweetenOffer"))
		{
			if (!(from is Kingdom { ai: not null } kingdom) || !kingdom.ai.Enabled(KingdomAI.EnableFlags.Diplomacy))
			{
				return false;
			}
			if (expire_time != Time.Zero && expire_time <= game.time)
			{
				return false;
			}
			float num = Eval("propose");
			if (num < 0f)
			{
				return false;
			}
			Offer cachedOffer = GetCachedOffer("SweetenOffer", from, to);
			SetParent(cachedOffer);
			cachedOffer.SetArg(0, this);
			if (Validate() != "ok")
			{
				SetParent(null);
				return false;
			}
			float evalToCoverMin = num * cachedOffer.def.pro_con_difference_cover_perc_min / 100f;
			float evalToCoverMax = num * cachedOffer.def.pro_con_difference_cover_perc_max / 100f;
			if (OfferGenerator.instance.TryGenerateRandomOfferHeavy("propose", from, to, cachedOffer, 1, forceParentArg: true, evalToCoverMin, evalToCoverMax) != null)
			{
				Cancel();
				if (cachedOffer.Validate() == "ok")
				{
					cachedOffer.Send();
					return true;
				}
				Offers.Add(from, this);
				Offers.Add(to, this);
			}
			else
			{
				SetParent(null);
			}
			return false;
		}
	}

	public void Accept()
	{
		Answer("accept");
	}

	public void Decline()
	{
		Answer("decline");
	}

	public virtual Object GetSourceObj()
	{
		return from;
	}

	public virtual Object GetTargetObj()
	{
		return to;
	}

	public void Answer(string answer, bool send_event = true)
	{
		using (Game.Profile("Offer.Answer"))
		{
			if (!to.IsAuthority() && send_event)
			{
				to.SendEvent(new Object.AnswerOfferEvent(this, answer));
				return;
			}
			if (Validate() == "ok")
			{
				OnAnswer(answer);
			}
			Offers.Del(from, this);
			Offers.Del(to, this);
			Offers.RemoveInvalid(from);
			Offers.RemoveInvalid(to);
		}
	}

	public virtual void OnExpire()
	{
		string text = def.default_outcome ?? "decline";
		if (!to.IsAuthority())
		{
			to.SendEvent(new Object.AnswerOfferEvent(this, text));
			return;
		}
		OnAnswer(text);
		Offers.Del(from, this);
		Offers.Del(to, this);
	}

	public virtual void OnAnswer(string answer)
	{
		using (Game.Profile("Offer.OnAnswer"))
		{
			LogOfferAnswered(answer);
			if (answer == "decline" && ConsiderAISweetenOffer())
			{
				return;
			}
			RecordOfferAnsweredForAI();
			RelationUtils.validators.AddLast(this);
			if (args != null)
			{
				for (int i = 0; i < args.Count; i++)
				{
					if (args[i].obj_val is Offer value)
					{
						RelationUtils.validators.AddLast(value);
					}
				}
			}
			this.answer = answer;
			if (parent == null)
			{
				DecideOutcomes(answer);
			}
			using (Game.Profile("Offer.FireEvent"))
			{
				Event obj = new Event(to, "offer_answered", this);
				obj.send_to_kingdoms = new List<int>(2);
				obj.send_to_kingdoms.Add(to.GetKingdom().id);
				if (from != null)
				{
					obj.send_to_kingdoms.Add(from.GetKingdom().id);
				}
				if (outcomes != null)
				{
					obj.outcomes = new List<OutcomeDef>(outcomes);
					obj.vars = outcome_vars;
				}
				to.FireEvent(obj);
			}
			switch (answer)
			{
			case "accept":
				using (Game.Profile("Offer.OnAccept"))
				{
					OnAccept();
				}
				break;
			case "decline":
				using (Game.Profile("Offer.OnDecline"))
				{
					OnDecline();
				}
				break;
			case "custom_counter_offer_answer":
				if (this is CounterOffer)
				{
					(args[0].obj_val as Offer).OnCustomCounterOfferAnswer();
				}
				break;
			}
			if (parent == null)
			{
				ApplyOutcomes(answer);
			}
			if ((this is CounterOffer || this is SweetenOffer) && args != null && args.Count > 0 && args[0].obj_val is Offer offer)
			{
				offer.DecideOutcomes(answer);
				offer.ApplyOutcomes(answer);
			}
			if (args != null)
			{
				for (int j = 0; j < args.Count; j++)
				{
					if (args[j].obj_val is Offer value2)
					{
						RelationUtils.validators.Remove(value2);
					}
				}
			}
			RelationUtils.validators.Remove(this);
		}
	}

	public void LogOfferAnswered(string answer)
	{
	}

	private void RecordOfferAnsweredForAI()
	{
		if (!AI)
		{
			return;
		}
		Offer offer = this;
		if (this is CounterOffer || this is SweetenOffer || this is AdditionalConditionOffer)
		{
			if (args == null || args.Count < 1)
			{
				return;
			}
			offer = args[0].obj_val as Offer;
			if (offer == null)
			{
				return;
			}
		}
		Kingdom kingdom = offer.from as Kingdom;
		Kingdom kingdom2 = offer.to as Kingdom;
		if (!kingdom.ai.Enabled(KingdomAI.EnableFlags.Diplomacy) || !kingdom2.is_player)
		{
			return;
		}
		for (int i = 0; i < kingdom.ai.offer_to_player.Length; i++)
		{
			KingdomAI.OfferToPlayer offerToPlayer = kingdom.ai.offer_to_player[i];
			if (offerToPlayer.kingdom_id == 0 || offerToPlayer.kingdom_id == kingdom2.id)
			{
				offerToPlayer.kingdom_id = kingdom2.id;
				if (offerToPlayer.t_last_answer_per_offer == null)
				{
					offerToPlayer.t_last_answer_per_offer = new Dictionary<string, Time>();
				}
				offerToPlayer.t_last_answer_per_offer[def.field.key] = game.time;
				kingdom.ai.offer_to_player[i] = offerToPlayer;
				break;
			}
		}
	}

	public virtual void OnAccept()
	{
		(from as Kingdom)?.NotifyListeners("sent_offer_accepted", this);
		(to as Kingdom)?.NotifyListeners("recieved_offer_accepted", this);
	}

	public virtual void OnDecline()
	{
		(from as Kingdom)?.NotifyListeners("sent_offer_declined", this);
		(to as Kingdom)?.NotifyListeners("recieved_offer_declined", this);
	}

	public virtual void OnCustomCounterOfferAnswer()
	{
	}

	public virtual void DecideOutcomes(string answer)
	{
		using (Game.Profile("Offer.DecideOutcomes"))
		{
			if (!string.IsNullOrEmpty(answer))
			{
				OutcomeDef outcomeDef = ((def.outcomes == null) ? null : def.outcomes.FindOption(answer));
				if (outcomeDef != null)
				{
					outcomes = outcomeDef.DecideOutcomes(game, this);
					unique_outcomes = OutcomeDef.UniqueOutcomes(outcomes);
					CreateOutcomeVars();
					OutcomeDef.PrecalculateValues(unique_outcomes, game, outcome_vars, outcome_vars);
				}
			}
		}
	}

	public virtual void ApplyOutcomes(string answer)
	{
		if (unique_outcomes == null)
		{
			return;
		}
		using (Game.Profile("Offer.ApplyOutcomes"))
		{
			for (int i = 0; i < unique_outcomes.Count; i++)
			{
				OutcomeDef outcome = unique_outcomes[i];
				ApplyOutcome(outcome);
			}
		}
	}

	public virtual void CreateOutcomeVars()
	{
		outcome_vars = new Vars(this);
	}

	public virtual bool ApplyOutcome(OutcomeDef outcome)
	{
		if (outcome.Apply(game, outcome_vars))
		{
			return true;
		}
		if (outcome.key == "accept" || outcome.key == "decline" || outcome.key.Contains("custom_counter_offer_answer"))
		{
			return true;
		}
		Game.Log(ToString() + ": unhandled outcome: " + outcome.id, Game.LogType.Warning);
		return false;
	}

	public virtual Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "name":
			return def?.id;
		case "from":
			return from;
		case "to":
			return to;
		case "args":
			return new Value(args);
		case "arg":
		case "arg0":
			return GetArg(0);
		case "arg1":
			return GetArg(1);
		case "arg2":
			return GetArg(2);
		case "arg3":
			return GetArg(3);
		case "parent":
			return parent;
		case "our_kingdom":
		case "own_kingdom":
		case "src_kingdom":
			return from?.GetKingdom();
		case "their_kingdom":
		case "target_kingdom":
		case "tgt_kingdom":
			return to?.GetKingdom();
		case "def":
			return new Value(def);
		case "outcomes_def":
			return new Value(def.outcomes);
		case "pc_accept_ratio_above_threshold":
			return GetRatioAboveThreshold("accept");
		case "answer":
			return answer;
		case "enemy_kingdom_leader":
			return (vars.GetVar("kingdom").obj_val as Kingdom)?.FindWarWith(game.GetLocalPlayerKingdom())?.GetEnemyLeader(game.GetLocalPlayerKingdom());
		default:
			return Value.Unknown;
		}
	}

	public string GetNameKey(IVars vars = null, string form = "")
	{
		return def.id + ".name";
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

	public void SetParent(Offer offer)
	{
		parent = offer;
	}

	public bool IsBetween(Object obj1, Object obj2)
	{
		if (from != obj1 || to != obj2)
		{
			if (from == obj2)
			{
				return to == obj1;
			}
			return false;
		}
		return true;
	}

	public virtual bool IsOfType(Type type)
	{
		return rtti.type == type;
	}

	public virtual bool IsOfferOfSimilarType(Offer offer)
	{
		return IsOfType(offer.rtti.type);
	}

	public virtual bool DoesOfferHasSameArgs(Offer offer)
	{
		bool flag = ((args == null) ? (-1) : args.Count) == ((offer.args == null) ? (-1) : offer.args.Count);
		if (flag && args != null)
		{
			for (int i = 0; i < args.Count; i++)
			{
				flag &= args[0].Match(offer.args[0]);
				if (!flag)
				{
					break;
				}
			}
		}
		return flag;
	}

	public virtual bool IsOfferSimilar(Offer offer, bool sameType = false, bool sameSourceTarget = false, bool sameArgs = false)
	{
		bool flag = true;
		flag = ((!sameType) ? (flag & IsOfferOfSimilarType(offer)) : (flag & IsOfType(offer.rtti.type)));
		if (sameSourceTarget)
		{
			flag &= offer.GetSourceObj() == GetSourceObj();
			flag &= offer.GetTargetObj() == GetTargetObj();
		}
		if (sameArgs)
		{
			flag &= DoesOfferHasSameArgs(offer);
		}
		return flag;
	}

	private List<Offer> GetSimilarOffersInOffer(Offer offer, bool sameType = false, bool sameSourceTarget = false, bool sameArgs = false, bool clearTmpOffers = true)
	{
		if (clearTmpOffers)
		{
			tmp_offers.Clear();
		}
		if (offer == null || offer == this)
		{
			return tmp_offers;
		}
		if (IsOfferSimilar(offer, sameType, sameSourceTarget, sameArgs))
		{
			tmp_offers.Add(offer);
		}
		if (offer.args != null && offer.args.Count > 0)
		{
			for (int i = 0; i < offer.args.Count; i++)
			{
				if (offer.args[i].obj_val is Offer offer2)
				{
					GetSimilarOffersInOffer(offer2, sameType, sameSourceTarget, sameArgs, clearTmpOffers: false);
				}
			}
		}
		return tmp_offers;
	}

	public List<Offer> GetSimilarOffersInParent(bool sameType = false, bool sameSourceTarget = false, bool sameArgs = false)
	{
		tmp_offers.Clear();
		if (parent != null)
		{
			GetSimilarOffersInOffer(parent, sameType, sameSourceTarget, sameArgs, clearTmpOffers: false);
		}
		return tmp_offers;
	}

	public int CountSimilarOffersInParent(bool sameType = false, bool sameSourceTarget = false, bool sameArgs = false)
	{
		return GetSimilarOffersInParent(sameType, sameSourceTarget, sameArgs).Count;
	}

	public virtual List<Value> ClearDuplicatesWithParent(int argumentIdx, List<Value> values)
	{
		List<Offer> similarOffersInParent = GetSimilarOffersInParent(sameType: false, sameSourceTarget: true);
		if (similarOffersInParent == null || similarOffersInParent.Count == 0)
		{
			return null;
		}
		if (argumentIdx < 0)
		{
			return values;
		}
		for (int i = 0; i < similarOffersInParent.Count; i++)
		{
			if (similarOffersInParent[i].args == null || argumentIdx >= similarOffersInParent[i].args.Count)
			{
				continue;
			}
			object obj_val = similarOffersInParent[i].args[argumentIdx].obj_val;
			for (int j = 0; j < values.Count; j++)
			{
				if (obj_val == values[j].obj_val)
				{
					values.RemoveAt(j--);
				}
			}
		}
		return values;
	}

	public virtual float Eval(string threshold_name, bool reverse_kingdoms = false)
	{
		return ProsAndCons.Get(this, threshold_name, reverse_kingdoms)?.eval ?? 0f;
	}

	public virtual bool CheckThreshold(string threshold_name, bool reverse_kingdoms = false)
	{
		return Eval(threshold_name, reverse_kingdoms) > 0f;
	}

	public virtual float GetRatioAboveThreshold(string threshold_name, bool reverse_kingdoms = false)
	{
		ProsAndCons prosAndCons = ProsAndCons.Get(this, threshold_name, reverse_kingdoms);
		if (prosAndCons == null)
		{
			return 0f;
		}
		return Eval(threshold_name, reverse_kingdoms) / prosAndCons.def.cost.Float(prosAndCons, 100000f);
	}

	public virtual bool IsWar(bool sender)
	{
		return false;
	}

	public virtual bool IsValidForAI()
	{
		if (!AI)
		{
			return true;
		}
		Kingdom kingdom = from.GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		Kingdom kingdom2 = to as Kingdom;
		if (kingdom2.is_player)
		{
			for (int i = 0; i < kingdom.ai.offer_to_player.Length; i++)
			{
				KingdomAI.OfferToPlayer offerToPlayer = kingdom.ai.offer_to_player[i];
				if (offerToPlayer.kingdom_id == kingdom2.id)
				{
					if ((parent != null && ((!(parent is CounterOffer) && !(parent is SweetenOffer) && !(parent is AdditionalConditionOffer)) || parent.args == null || parent.args.Count < 1 || parent.args[0].obj_val != this)) || offerToPlayer.t_last_answer_per_offer == null || !offerToPlayer.t_last_answer_per_offer.TryGetValue(def.field.key, out var value))
					{
						break;
					}
					if (value + def.ai_propose_cooldown > game.time)
					{
						return false;
					}
				}
			}
		}
		if (parent is CounterOffer || parent is SweetenOffer)
		{
			return true;
		}
		if (kingdom.inAudienceWith == to)
		{
			return false;
		}
		if (kingdom2.is_player)
		{
			Offers component = kingdom2.GetComponent<Offers>();
			if (component != null && component.incoming != null && !component.incoming.Contains(this) && component.incoming.Count >= 3)
			{
				return false;
			}
		}
		if (IsWar(sender: true) && !kingdom.ai.Enabled(KingdomAI.EnableFlags.Wars))
		{
			return false;
		}
		return true;
	}

	public Vars CreateOnSendAnalyticsVars()
	{
		if (def?.id == null)
		{
			return null;
		}
		Vars vars = new Vars();
		Kingdom kingdom = null;
		int num = 0;
		while (true)
		{
			List<Value> list = args;
			if (list == null)
			{
				break;
			}
			_ = list.Count;
			if (1 == 0 || num >= args.Count)
			{
				break;
			}
			kingdom = args[0].Get<Kingdom>();
			if (kingdom != null)
			{
				break;
			}
			num++;
		}
		vars.Set("targetKingdom", (kingdom != null) ? kingdom.Name : to.GetKingdom().Name);
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(from.GetKingdom(), to.GetKingdom(), calc_fade: true, create_if_not_found: true);
		vars.Set("kingdomRelation", (kingdomAndKingdomRelation != null) ? ((int)kingdomAndKingdomRelation.GetRelationship()) : 0);
		if (def.id.Contains("Demand"))
		{
			vars.Set("demandMade", def.id);
		}
		else
		{
			vars.Set("offerMade", def.id);
		}
		switch (def.id)
		{
		case "DeclareWar":
		case "PeaceDemandTribute":
		case "PeaceOfferTribute":
		case "WhitePeaceOffer":
			vars.Set("warChange", def.id);
			break;
		}
		if (def.id == "OfferGold")
		{
			vars.Set("goldCost", (this as OfferGold).GetGoldAmount());
		}
		else if (def.id == "DemandGold")
		{
			vars.Set("goldReceived", (this as DemandGold).GetGoldAmount());
		}
		if (def.id == "OfferLand")
		{
			string text = (this as OfferLand).GetArg<Realm>(0)?.name;
			if (!string.IsNullOrEmpty(text))
			{
				vars.Set("landGiven", text);
			}
		}
		else if (def.id == "DemandLand")
		{
			string text2 = (this as DemandLand).GetArg<Realm>(0)?.name;
			if (!string.IsNullOrEmpty(text2))
			{
				vars.Set("landReceived", text2);
			}
		}
		string text3 = GetSourceObj()?.GetKingdom()?.Name;
		string text4 = GetTargetObj()?.GetKingdom()?.Name;
		if (!string.IsNullOrEmpty(text3))
		{
			vars.Set("targetKingdomGiven", text3);
		}
		if (!string.IsNullOrEmpty(text4))
		{
			vars.Set("targetKingdomReceived", text4);
		}
		return vars;
	}

	public void SendAnalytics(Kingdom k)
	{
		if (k != null && k.is_player)
		{
			Vars vars = CreateOnSendAnalyticsVars();
			if (vars != null)
			{
				k.FireEvent("analytics_offer", vars, k.id);
			}
		}
	}

	public void SendAnalytics()
	{
		SendAnalytics(from?.GetKingdom());
		SendAnalytics(to?.GetKingdom());
	}

	public virtual bool ValidateRelChange(Kingdom kSrc, Kingdom kTgt, float perm, float temp, Kingdom indirectTarget)
	{
		return true;
	}
}
