using System.Collections.Generic;

namespace Logic;

public class Trigger : IVars
{
	public class Def : IVars
	{
		public DT.Field field;

		public string type;

		public string name;

		public DT.Field sender_field;

		public TargetType sender_type;

		public List<string> messages;

		public List<DT.Field> named_vars;

		public DT.Field condition_field;

		public DT.Field chance_field;

		public DT.Field chance_mul_field;

		public DT.Field target_field;

		public int calls;

		public int checks;

		public int activations;

		private Def()
		{
		}

		public Def(DT.Field f)
		{
			this.field = f;
			type = this.field.Type();
			name = this.field.key;
			sender_field = f.FindChild("sender");
			if (sender_field != null && !string.IsNullOrEmpty(type))
			{
				Game.Log(this.field.Path(include_file: true) + ": Target type (" + type + ") cannot be used with 'sender'", Game.LogType.Error);
			}
			if (type != "target")
			{
				sender_type = TargetType.Find(type);
				if (sender_type == null && sender_field == null)
				{
					Game.Log(this.field.Path(include_file: true) + ": Unknown trigger type: '" + type + "'", Game.LogType.Error);
				}
			}
			int num = f.NumValues();
			if (num > 0)
			{
				messages = new List<string>(num);
				for (int i = 0; i < num; i++)
				{
					string text = f.String(i);
					if (!string.IsNullOrEmpty(text))
					{
						messages.Add(text);
					}
				}
			}
			else
			{
				messages = new List<string> { name };
			}
			condition_field = f.FindChild("condition");
			chance_field = f.FindChild("chance");
			if (chance_field == null)
			{
				DT.Field parent = f.parent;
				if (parent != null && parent.value.is_valid)
				{
					chance_field = parent;
				}
			}
			chance_mul_field = f.FindChild("chance_mul");
			target_field = f.FindChild("target");
			List<DT.Field> list = f.Children();
			if (list == null)
			{
				return;
			}
			for (int j = 0; j < list.Count; j++)
			{
				DT.Field field = list[j];
				if (field.Type() == "var")
				{
					if (named_vars == null)
					{
						named_vars = new List<DT.Field>();
					}
					named_vars.Add(field);
				}
			}
		}

		public static Def Load(DT.Field f)
		{
			if (string.IsNullOrEmpty(f?.key))
			{
				return null;
			}
			return new Def(f);
		}

		public static Def CreateTimerDef(RuleTimer.Def tdef)
		{
			if (tdef == null)
			{
				return null;
			}
			Def def = new Def();
			def.type = "target";
			def.name = tdef.def.name;
			def.messages = new List<string> { def.name };
			return def;
		}

		public Object GetSenderObj(IVars vars)
		{
			if (sender_field == null)
			{
				return null;
			}
			Object obj = sender_field.Value(vars).Get<Object>();
			if (obj == null)
			{
				Game.Log($"Could not resolve sender for trigger {this}({vars})", Game.LogType.Warning);
				return null;
			}
			return obj;
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			if (named_vars != null)
			{
				for (int i = 0; i < named_vars.Count; i++)
				{
					DT.Field field = named_vars[i];
					if (key == field.key)
					{
						if (as_value)
						{
							return field.Value(vars);
						}
						return new Value(field);
					}
				}
			}
			if (this.field == null)
			{
				return Value.Unknown;
			}
			return this.field.GetVar(key, vars, as_value);
		}

		public override string ToString()
		{
			return type + " " + name;
		}
	}

	public Def def;

	public object sender;

	public string message;

	public object param;

	public int id;

	public Vars named_vars;

	private IVars tmp_vars;

	public Trigger(Def def = null, object sender = null, string name = null, object param = null, int id = 0, IVars vars = null)
	{
		Set(def, sender, name, param, id, vars);
	}

	public Trigger(Trigger t)
	{
		if (t == null)
		{
			Clear();
			return;
		}
		def = t.def;
		sender = t.sender;
		message = t.message;
		param = t.param;
		id = t.id;
		named_vars = t.named_vars?.Copy();
	}

	public void Clear()
	{
		def = null;
		sender = null;
		message = null;
		param = null;
		id = 0;
		named_vars = null;
	}

	public bool Equal(Trigger t)
	{
		if (t == null)
		{
			return false;
		}
		if (t.id == 0)
		{
			return false;
		}
		if (t.id == id && t.def == def && t.sender == sender && t.message == message)
		{
			return t.param == param;
		}
		return false;
	}

	public void Set(Trigger t, IVars vars)
	{
		if (t == null)
		{
			Clear();
		}
		else
		{
			Set(t.def, t.sender, t.message, t.param, t.id, vars);
		}
	}

	public void Set(Def def, object sender, string name, object param, int id, IVars vars)
	{
		this.def = def;
		this.sender = sender;
		message = name;
		this.param = param;
		this.id = id;
		if (vars == null || def?.named_vars == null)
		{
			named_vars = null;
			return;
		}
		tmp_vars = vars;
		named_vars = new Vars();
		for (int i = 0; i < def.named_vars.Count; i++)
		{
			DT.Field field = def.named_vars[i];
			Value val = field.Value(this);
			named_vars.Set(field.key, val);
		}
		tmp_vars = null;
	}

	public bool Validate(Game game, IVars vars)
	{
		if (def == null)
		{
			return true;
		}
		if (def.condition_field != null)
		{
			tmp_vars = vars;
			bool num = def.condition_field.Bool(this);
			tmp_vars = null;
			if (!num)
			{
				return false;
			}
		}
		if (def.chance_field != null)
		{
			tmp_vars = vars;
			float num2 = def.chance_field.Float(this);
			float num3 = ((def.chance_mul_field == null) ? 1f : def.chance_mul_field.Float(this, 1f));
			tmp_vars = null;
			num2 *= num3;
			if (game.Random(0f, 100f) > num2)
			{
				return false;
			}
		}
		return true;
	}

	public Object GetTarget(IVars vars)
	{
		if (def == null)
		{
			Game.Log($"{this}: Cannot decide target for trigger without def", Game.LogType.Error);
			return null;
		}
		if (def.target_field == null)
		{
			return sender as Object;
		}
		tmp_vars = vars;
		Value value = def.target_field.Value(this);
		tmp_vars = null;
		Object obj = value.Get<Object>();
		if (obj == null)
		{
			Game.Log($"{def.condition_field.Path(include_file: true)}: could not resolve target: {value}", Game.LogType.Error);
			return null;
		}
		return obj;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (named_vars != null)
		{
			Value result = named_vars.Get(key);
			if (!result.is_unknown)
			{
				return result;
			}
		}
		switch (key)
		{
		case "trigger":
			return new Value(this);
		case "def":
			return new Value(def);
		case "name":
			return def?.name ?? message;
		case "message":
			return message;
		case "sender":
			return new Value(sender);
		case "param":
			return new Value(param);
		case "vars":
			return new Value(named_vars);
		default:
			if (tmp_vars != null)
			{
				Value var = tmp_vars.GetVar(key, vars, as_value);
				if (!var.is_unknown)
				{
					return var;
				}
			}
			if (param is IVars vars2)
			{
				return vars2.GetVar(key, vars, as_value);
			}
			return Value.Unknown;
		}
	}

	public override string ToString()
	{
		if (sender == null)
		{
			return "null";
		}
		return string.Format("[{0}] {1} {2}({3}) from {4}", id, def?.type ?? "undefined", message, param, sender);
	}
}
