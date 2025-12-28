using System;
using System.Collections.Generic;

namespace Logic;

[Serializable]
public class Resource : BaseObject, IVars
{
	public class Def : Logic.Def
	{
		public string Name;

		public string Category;

		public float building_commerce_upkeep;

		public DT.Field ai_eval_field;

		public List<StatModifier.Def> mdefs = new List<StatModifier.Def>();

		public List<Building.Def> produced_in = new List<Building.Def>();

		public List<string> required_for = new List<string>();

		public List<string> advantages = new List<string>();

		public List<string> province_features = new List<string>();

		public static int total;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			Name = field.GetString("name", null, dt_def.path);
			Category = field.GetString("category", null, dt_def.path);
			float num = field.GetFloat("import_commerce", null, 10f);
			float num2 = field.GetFloat("building_commerce_upkeep_mul", null, 1f);
			building_commerce_upkeep = (float)Math.Ceiling(num * num2);
			ai_eval_field = field.FindChild("ai_eval");
			LoadMods(game);
			produced_in.Clear();
			required_for.Clear();
			advantages.Clear();
			if (!IsBase())
			{
				total++;
			}
			return true;
		}

		private void LoadMods(Game game)
		{
			mdefs.Clear();
			DT.Field field = base.field;
			if (field?.children == null)
			{
				return;
			}
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!(field2.type != "mod"))
				{
					StatModifier.Def item = new StatModifier.Def(this, field2, field2.key);
					mdefs.Add(item);
				}
			}
		}

		public override bool Validate(Game game)
		{
			ValidateMods(game);
			return true;
		}

		private void ValidateMods(Game game)
		{
			if (mdefs.Count == 0)
			{
				return;
			}
			Stats.Def def = game.defs.Get<Stats.Def>("KingdomStats");
			for (int i = 0; i < mdefs.Count; i++)
			{
				StatModifier.Def def2 = mdefs[i];
				if (!def.HasStat(def2.stat_name))
				{
					Game.Log(def2.field.Path(include_file: true) + ": unknown stat: '" + def2.stat_name + "'", Game.LogType.Error);
					mdefs.RemoveAt(i);
					i--;
				}
				else
				{
					def2.field.NumValues();
				}
			}
		}
	}

	public class StatModifier : Stat.Modifier, IVars
	{
		public class Def
		{
			public DT.Field field;

			public Resource.Def resource_def;

			public string stat_name;

			public Type type;

			public Def(Resource.Def resource_def, DT.Field mf, string stat_name)
			{
				field = mf;
				this.resource_def = resource_def;
				this.stat_name = stat_name;
				if (mf.FindChild("perc") != null)
				{
					type = Type.Perc;
				}
				else if (mf.FindChild("base") != null)
				{
					type = Type.Base;
				}
				else if (mf.FindChild("unscaled") != null)
				{
					type = Type.Unscaled;
				}
			}

			public float CalcValue()
			{
				return field.Float(0);
			}

			public override string ToString()
			{
				return resource_def.Name + "." + stat_name;
			}
		}

		public Def def;

		public StatModifier(Def def)
		{
			this.def = def;
		}

		public override DT.Field GetField()
		{
			return def.field;
		}

		public override DT.Field GetNameField()
		{
			DT.Field nameField = base.GetNameField();
			if (nameField != null)
			{
				return nameField;
			}
			return def.field.parent;
		}

		public override bool IsConst()
		{
			return true;
		}

		public override string ToString()
		{
			return def.ToString() + Stat.Modifier.ToString(value, type);
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			if (!(base.owner is IVars vars2))
			{
				return Value.Unknown;
			}
			return vars2.GetVar(key, vars, as_value);
		}
	}

	public class FullData : Data
	{
		private Resource resource = new Resource();

		public static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			Resource resource = obj as Resource;
			if (resource == null)
			{
				return false;
			}
			this.resource = resource.Copy();
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteRawStr(resource.ToString(), "resource");
		}

		public override void Load(Serialization.IReader ser)
		{
			string text = ser.ReadRawStr("resource");
			resource = Parse(text) ?? resource;
		}

		public override object GetObject(Game game)
		{
			return new Resource();
		}

		public override bool ApplyTo(object obj, Game game)
		{
			Resource resource = obj as Resource;
			if (resource == null)
			{
				return false;
			}
			resource.Set(this.resource, 1f);
			return true;
		}
	}

	public float[] amounts = new float[13];

	public const ResourceType SPECIAL = ResourceType.Workers;

	private static Resource tmp_avail = new Resource();

	public float this[ResourceType rt]
	{
		get
		{
			return Get(rt);
		}
		set
		{
			Set(rt, value);
		}
	}

	private void CheckOwnerKingdom(Kingdom k)
	{
	}

	public float Get(ResourceType rt)
	{
		if ((int)rt >= amounts.Length)
		{
			return 0f;
		}
		float num = amounts[(int)rt];
		if (float.IsNaN(num))
		{
			num = (amounts[(int)rt] = 0f);
		}
		return num;
	}

	public void Set(ResourceType rt, float amount, Kingdom owner_kingdom = null)
	{
		CheckOwnerKingdom(owner_kingdom);
		if (float.IsNaN(amount))
		{
			if (!Game.isLoadingSaveGame)
			{
				Game.Log($"Setting resources {rt} to NaN!", Game.LogType.Error);
			}
			amount = 0f;
		}
		if ((int)rt < amounts.Length)
		{
			amounts[(int)rt] = amount;
		}
	}

	public Resource()
	{
	}

	public Resource(Resource res)
	{
		Set(res, 1f);
	}

	public Resource Copy()
	{
		return new Resource(this);
	}

	public void Clear()
	{
		for (int i = 0; i < amounts.Length; i++)
		{
			amounts[i] = 0f;
		}
	}

	public bool IsZero()
	{
		for (int i = 0; i < amounts.Length; i++)
		{
			if (amounts[i] != 0f)
			{
				return false;
			}
		}
		return true;
	}

	public static bool Eq(Resource r1, Resource r2)
	{
		if ((object)r1 == r2)
		{
			return true;
		}
		if ((object)r1 == null || (object)r2 == null)
		{
			return false;
		}
		for (int i = 0; i < r1.amounts.Length; i++)
		{
			if (r1.amounts[i] != r2.amounts[i])
			{
				return false;
			}
		}
		return true;
	}

	public static bool operator ==(Resource r1, Resource r2)
	{
		return Eq(r1, r2);
	}

	public static bool operator !=(Resource r1, Resource r2)
	{
		return !Eq(r1, r2);
	}

	public override bool Equals(object obj)
	{
		Resource r = obj as Resource;
		if (obj == null)
		{
			return false;
		}
		return Eq(this, r);
	}

	public override int GetHashCode()
	{
		throw new NotSupportedException();
	}

	public static implicit operator Value(Resource res)
	{
		return new Value((object)res);
	}

	public bool CanAfford(Resource cost, float mul = 1f, params ResourceType[] ignore)
	{
		if (cost == null)
		{
			return true;
		}
		bool flag = ignore.Length != 0;
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			if (resourceType != ResourceType.Hammers)
			{
				int num = (int)resourceType;
				if (!(cost.amounts[num] <= 0f) && amounts[num] < cost.amounts[num] * mul && (!flag || Array.IndexOf(ignore, resourceType) < 0))
				{
					return false;
				}
			}
		}
		return true;
	}

	public void Set(Resource res, float mul, Kingdom own_kingdom)
	{
		CheckOwnerKingdom(own_kingdom);
		if (res == null)
		{
			Clear();
			return;
		}
		for (int i = 0; i < amounts.Length; i++)
		{
			amounts[i] = res.amounts[i] * mul;
		}
	}

	public void Set(Resource res, float mul = 1f, params ResourceType[] ignore)
	{
		CheckOwnerKingdom(null);
		if (res == null)
		{
			Clear();
			return;
		}
		bool flag = ignore.Length != 0;
		for (int i = 0; i < amounts.Length; i++)
		{
			if (!flag || Array.IndexOf(ignore, (ResourceType)i) < 0)
			{
				amounts[i] = res.amounts[i] * mul;
			}
		}
	}

	public void Add(ResourceType rt, float amount, Kingdom owner_kingdom = null)
	{
		float num = Get(rt);
		Set(rt, num + amount, owner_kingdom);
	}

	public void Add(Resource res, float mul, Kingdom owner_kingdom)
	{
		CheckOwnerKingdom(owner_kingdom);
		if (!(res == null))
		{
			for (int i = 0; i < amounts.Length; i++)
			{
				amounts[i] += res.amounts[i] * mul;
			}
		}
	}

	public void Add(Resource res, float mul = 1f, params ResourceType[] ignore)
	{
		CheckOwnerKingdom(null);
		if (res == null)
		{
			return;
		}
		bool flag = ignore.Length != 0;
		for (int i = 0; i < amounts.Length; i++)
		{
			if (!flag || Array.IndexOf(ignore, (ResourceType)i) < 0)
			{
				amounts[i] += res.amounts[i] * mul;
			}
		}
	}

	public void Sub(ResourceType rt, float amount, Kingdom owner_kingdom = null)
	{
		float num = Get(rt);
		Set(rt, num - amount, owner_kingdom);
	}

	public void Sub(Resource res, float mul = 1f, Kingdom owner_kingdom = null)
	{
		CheckOwnerKingdom(owner_kingdom);
		if (!(res == null))
		{
			for (int i = 0; i < amounts.Length; i++)
			{
				amounts[i] -= res.amounts[i] * mul;
			}
		}
	}

	public void Mul(float mul, params ResourceType[] ignore)
	{
		CheckOwnerKingdom(null);
		bool flag = ignore.Length != 0;
		for (int i = 0; i < amounts.Length; i++)
		{
			if (!flag || Array.IndexOf(ignore, (ResourceType)i) < 0)
			{
				amounts[i] *= mul;
			}
		}
	}

	public static bool IsRealmLocalResource(ResourceType rt)
	{
		switch (rt)
		{
		case ResourceType.Hammers:
		case ResourceType.TownGuards:
		case ResourceType.Workers:
		case ResourceType.WorkerSlots:
		case ResourceType.Rebels:
		case ResourceType.RebelsSlots:
			return true;
		default:
			return false;
		}
	}

	public void ClearRealmLocalResources()
	{
		Set(ResourceType.Hammers, 0f);
		Set(ResourceType.TownGuards, 0f);
		Set(ResourceType.Workers, 0f);
		Set(ResourceType.WorkerSlots, 0f);
		Set(ResourceType.Rebels, 0f);
		Set(ResourceType.RebelsSlots, 0f);
	}

	public float Eval(Resource weights = null)
	{
		float num = 0f;
		for (ResourceType resourceType = ResourceType.Gold; resourceType < ResourceType.COUNT; resourceType++)
		{
			float num2 = Get(resourceType);
			if (weights != null)
			{
				float num3 = weights[resourceType];
				num2 *= num3;
			}
			num += num2;
		}
		return num;
	}

	public static ResourceType GetType(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return ResourceType.None;
		}
		for (ResourceType resourceType = ResourceType.Gold; resourceType < ResourceType.COUNT; resourceType++)
		{
			string text = resourceType.ToString();
			if (s.Equals(text, StringComparison.OrdinalIgnoreCase))
			{
				return resourceType;
			}
			if (resourceType < ResourceType.Workers && s.Length == 1 && char.ToLowerInvariant(text[0]) == char.ToLowerInvariant(s[0]))
			{
				return resourceType;
			}
		}
		if (s == "Goods")
		{
			return ResourceType.RebelsSlots;
		}
		if (s == "UniqueGoods")
		{
			return ResourceType.Rebels;
		}
		return ResourceType.None;
	}

	public static Resource Parse(string text, bool no_null = false)
	{
		if (string.IsNullOrEmpty(text))
		{
			if (!no_null)
			{
				return null;
			}
			return new Resource();
		}
		Resource resource = null;
		int i = 0;
		while (true)
		{
			Expression.SkipBlanks(text, ref i);
			if (i >= text.Length)
			{
				break;
			}
			Value value = Expression.ReadNumber(text, ref i);
			if (!value.is_number)
			{
				Game.Log("Invalid resources string: '" + text + "'", Game.LogType.Warning);
				if (!no_null)
				{
					return null;
				}
				return new Resource();
			}
			Expression.SkipBlanks(text, ref i);
			int num = i;
			for (; i < text.Length && char.IsLetter(text[i]); i++)
			{
			}
			if (i == num)
			{
				Game.Log("Invalid resources string: '" + text + "'", Game.LogType.Warning);
				if (!no_null)
				{
					return null;
				}
				return new Resource();
			}
			ResourceType type = GetType(text.Substring(num, i - num));
			if (type == ResourceType.None)
			{
				Game.Log("Invalid resources string: '" + text + "'", Game.LogType.Warning);
				continue;
			}
			float value2 = value;
			if (resource == null)
			{
				resource = new Resource();
			}
			else if (resource[type] != 0f)
			{
				Game.Log("Invalid resources string: '" + text + "'", Game.LogType.Warning);
				continue;
			}
			resource[type] = value2;
			if (i < text.Length && text[i] == ',')
			{
				i++;
			}
		}
		if (resource == null && no_null)
		{
			Game.Log("Invalid resources string: '" + text + "'", Game.LogType.Warning);
			return new Resource();
		}
		return resource;
	}

	public static Resource Parse(DT.Field field, IVars vars = null, bool no_null = false, bool parse_value = true)
	{
		if (field == null)
		{
			if (!no_null)
			{
				return null;
			}
			return new Resource();
		}
		Value value = (parse_value ? field.Value(vars) : Value.Unknown);
		if (!value.is_unknown)
		{
			Resource resource = value.obj_val as Resource;
			if (resource != null)
			{
				return resource;
			}
		}
		Resource resource2 = (parse_value ? Parse(field.String()) : null);
		List<string> list = field.Keys();
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			ResourceType type = GetType(text);
			if (type == ResourceType.None)
			{
				continue;
			}
			float num = field.FindChild(text).Float(vars);
			if (num == 0f)
			{
				continue;
			}
			if (resource2 == null)
			{
				resource2 = new Resource();
			}
			else if (resource2[type] != 0f)
			{
				if (!no_null)
				{
					return null;
				}
				return new Resource();
			}
			resource2[type] = num;
		}
		if (resource2 == null && no_null)
		{
			return new Resource();
		}
		return resource2;
	}

	public Value GetVar(string key, IVars vars, bool as_value)
	{
		if (key == "is_zero")
		{
			return IsZero();
		}
		ResourceType type = GetType(key);
		if (type == ResourceType.None)
		{
			return Value.Unknown;
		}
		return Get(type);
	}

	public string GetText(string form = null, string piety_type = null, Kingdom kingdom = null)
	{
		string text = "";
		for (ResourceType resourceType = ResourceType.Gold; resourceType < ResourceType.COUNT; resourceType++)
		{
			float num = Get(resourceType);
			if (num != 0f)
			{
				if (resourceType == ResourceType.Piety && !Religion.MatchPietyType(piety_type, kingdom))
				{
					num = 0f - num;
				}
				if (text != "")
				{
					text += " ";
				}
				text = text + "{@" + num;
				if (form != null)
				{
					text = text + ":" + form;
				}
				text = text + "}{" + resourceType.ToString().ToLowerInvariant() + "_icon}";
			}
		}
		if (text == "")
		{
			return "";
		}
		return "@" + text;
	}

	public override string ToString()
	{
		string text = "";
		for (ResourceType resourceType = ResourceType.Gold; resourceType < ResourceType.COUNT; resourceType++)
		{
			float num = Get(resourceType);
			if (num != 0f)
			{
				string text2 = resourceType switch
				{
					ResourceType.RebelsSlots => "goods", 
					ResourceType.Rebels => "unique_goods", 
					_ => resourceType.ToString().ToLowerInvariant(), 
				};
				if (text != "")
				{
					text += ", ";
				}
				text = text + DT.FloatToStr(num) + " " + text2;
			}
		}
		return text;
	}

	public string GetCostText(Resource available, Resource original, string type = "")
	{
		string text = "";
		for (ResourceType resourceType = ResourceType.Gold; resourceType < ResourceType.COUNT; resourceType++)
		{
			float num = Get(resourceType);
			if (num <= 0f)
			{
				continue;
			}
			if (text != "")
			{
				text += " ";
			}
			bool flag = true;
			float num2 = ((original == null) ? num : original[resourceType]);
			if (resourceType != ResourceType.Hammers)
			{
				float num3 = available[resourceType];
				if (num > num3)
				{
					flag = false;
				}
			}
			if (!flag)
			{
				text += ((type == "upkeep") ? "{requirement_not_met}" : "{insufficient_resource}");
			}
			else if (num < num2)
			{
				text += "{clr:active_buff}";
			}
			else if (num > num2)
			{
				text += "{clr:orange}";
			}
			text += num;
			text = text + "{" + resourceType.ToString().ToLowerInvariant() + "_icon}";
			if (!flag)
			{
				text += "{/insufficient_resource}";
			}
			else if (num < num2)
			{
				text += "{/clr}";
			}
			else if (num > num2)
			{
				text += "{/clr}";
			}
		}
		if (text == "")
		{
			return "";
		}
		return "@" + text;
	}

	public string GetUpkeepText(IVars vars)
	{
		Kingdom kingdom = Vars.Get<Kingdom>(vars, "kingdom");
		if (kingdom == null)
		{
			return GetText();
		}
		tmp_avail.Set(kingdom.income, 1f);
		tmp_avail.Sub(kingdom.expenses);
		return GetCostText(tmp_avail, null, "upkeep");
	}

	public string GetNameKey(IVars vars, string form = "")
	{
		if (vars == null || form == "plain")
		{
			return GetText();
		}
		if (form == "upkeep")
		{
			return GetUpkeepText(vars);
		}
		if (!string.IsNullOrEmpty(form) && form != "cost")
		{
			Kingdom kingdom = Vars.Get<Kingdom>(vars, "kingdom");
			string piety_type = Vars.Get<string>(vars, "piety_type");
			return GetText(form, piety_type, kingdom);
		}
		Resource resource = Vars.Get<Resource>(vars, "available");
		if (resource == null)
		{
			Kingdom kingdom2 = Vars.Get<Kingdom>(vars, "kingdom");
			if (kingdom2 != null)
			{
				resource = kingdom2.resources;
			}
			Character character = Vars.Get<Character>(vars, "own_character");
			if (character != null)
			{
				if (resource == null)
				{
					resource = character.GetKingdom()?.resources;
				}
				resource = new Resource(resource);
			}
		}
		if (resource == null)
		{
			return GetText();
		}
		Resource original = Vars.Get<Resource>(vars, "original_cost");
		return GetCostText(resource, original);
	}

	public bool IsValid()
	{
		if (amounts == null)
		{
			return false;
		}
		int i = 0;
		for (int num = amounts.Length; i < num; i++)
		{
			if (float.IsNaN(amounts[i]))
			{
				return false;
			}
		}
		return true;
	}

	public void Round()
	{
		Round(0);
	}

	public void Round(int decimals)
	{
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			float num = Get(resourceType);
			Set(resourceType, (float)Math.Round(num, decimals));
		}
	}

	public void Round(float precision)
	{
		if (precision <= 0f)
		{
			precision = 1f;
		}
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			float num = Get(resourceType);
			num /= precision;
			num = (float)Math.Round(num);
			num *= precision;
			Set(resourceType, num);
		}
	}
}
