using System;
using System.Collections.Generic;

namespace Logic;

public class Tradition : BaseObject, IVars
{
	public enum Type
	{
		Unknown,
		All,
		Military,
		NonMilitary
	}

	public class Def : Logic.Def
	{
		public class SkillInfo
		{
			public DT.Field field;

			public Skill.Def def;

			public bool grants = true;

			public bool granted_by = true;

			public List<DT.Field> bonus_fields;

			public List<SkillsTable.CellDef> bonus_cells;

			public SkillInfo(Game game, DT.Field field)
			{
				this.field = field;
				def = game.defs.Get<Skill.Def>(field.key);
				if (field.children == null)
				{
					return;
				}
				for (int i = 0; i < field.children.Count; i++)
				{
					DT.Field field2 = field.children[i];
					if (string.IsNullOrEmpty(field2.key))
					{
						continue;
					}
					if (field2.key == "grants")
					{
						grants = field2.Bool(null, grants);
						continue;
					}
					if (field2.key == "granted_by")
					{
						granted_by = field2.Bool(null, granted_by);
						continue;
					}
					if (bonus_fields == null)
					{
						bonus_fields = new List<DT.Field>();
					}
					bonus_fields.Add(field2);
				}
			}

			public override string ToString()
			{
				return field.Path();
			}
		}

		public string name;

		public int max_rank = 3;

		public List<StatModifier.Def> mods = new List<StatModifier.Def>();

		private DT.Field adopt_cost;

		private DT.Field upgrade_cost;

		public float ai_eval = 1f;

		public float ai_eval_stability;

		public List<SkillInfo> skills = new List<SkillInfo>();

		private Type _type;

		public Type type => GetTraditionType();

		public bool GrantsSkill(Skill.Def def)
		{
			if (skills == null)
			{
				return false;
			}
			for (int i = 0; i < skills.Count; i++)
			{
				SkillInfo skillInfo = skills[i];
				if (skillInfo.def == def)
				{
					return skillInfo.grants;
				}
			}
			return false;
		}

		public bool IsGrantedBySkill(Skill.Def def)
		{
			if (skills == null)
			{
				return false;
			}
			for (int i = 0; i < skills.Count; i++)
			{
				SkillInfo skillInfo = skills[i];
				if (skillInfo.def == def)
				{
					return skillInfo.granted_by;
				}
			}
			return false;
		}

		public bool IsGrantedBySkill(Character c)
		{
			if (c?.skills == null)
			{
				return false;
			}
			for (int i = 0; i < c.skills.Count; i++)
			{
				Skill skill = c.skills[i];
				if (skill != null && c.GetSkillRank(skill) >= skill.MaxRank() && IsGrantedBySkill(skill?.def))
				{
					return true;
				}
			}
			return false;
		}

		public bool IsGrantedBySkill(Kingdom k)
		{
			if (k?.court == null)
			{
				return false;
			}
			for (int i = 0; i < k.court.Count; i++)
			{
				Character character = k.court[i];
				if (character != null && character.IsAlive() && character.kingdom_id == k.id && IsGrantedBySkill(character))
				{
					return true;
				}
			}
			return false;
		}

		public bool BuffsSkill(Skill.Def def)
		{
			if (skills == null)
			{
				return false;
			}
			for (int i = 0; i < skills.Count; i++)
			{
				SkillInfo skillInfo = skills[i];
				if (skillInfo.def == def)
				{
					if (skillInfo.bonus_cells != null)
					{
						return skillInfo.bonus_cells.Count > 0;
					}
					return false;
				}
			}
			return false;
		}

		public SkillInfo FindSkillInfo(Skill.Def sdef)
		{
			if (sdef == null)
			{
				return null;
			}
			if (skills == null)
			{
				return null;
			}
			for (int i = 0; i < skills.Count; i++)
			{
				SkillInfo skillInfo = skills[i];
				if (skillInfo.def == sdef)
				{
					return skillInfo;
				}
			}
			return null;
		}

		public bool MatchTrditionType(Type tt)
		{
			if (tt == Type.All)
			{
				return true;
			}
			if (type == Type.All)
			{
				return true;
			}
			return type == tt;
		}

		public Type GetTraditionType()
		{
			if (_type != Type.Unknown)
			{
				return _type;
			}
			string value = base.field.GetString("type");
			if (!string.IsNullOrEmpty(value) && Enum.TryParse<Type>(value, out _type) && _type != Type.Unknown)
			{
				return _type;
			}
			bool flag = false;
			bool flag2 = false;
			if (skills != null)
			{
				for (int i = 0; i < skills.Count; i++)
				{
					if (skills[i].def.GetTraditionType() == Type.Military)
					{
						flag = true;
					}
					else
					{
						flag2 = true;
					}
				}
			}
			if (flag == flag2)
			{
				_type = Type.All;
			}
			else if (flag)
			{
				_type = Type.Military;
			}
			else
			{
				_type = Type.NonMilitary;
			}
			return _type;
		}

		public override bool Load(Game game)
		{
			name = base.field.key;
			if (name.EndsWith("Tradition", StringComparison.Ordinal))
			{
				name = name.Substring(0, name.Length - 9);
			}
			_type = Type.Unknown;
			max_rank = base.field.GetInt("max_rank", null, max_rank);
			ai_eval = base.field.GetFloat("ai_eval", null, ai_eval);
			ai_eval_stability = base.field.GetFloat("ai_eval_stability", null, ai_eval_stability);
			LoadSkills(game);
			LoadMods(game);
			adopt_cost = base.field.FindChild("adopt_cost");
			upgrade_cost = base.field.FindChild("upgrade_cost");
			return true;
		}

		private void LoadSkills(Game game)
		{
			skills.Clear();
			DT.Field field = base.field.FindChild("skills");
			if (field?.children == null)
			{
				return;
			}
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!string.IsNullOrEmpty(field2.key))
				{
					SkillInfo item = new SkillInfo(game, field2);
					skills.Add(item);
				}
			}
		}

		private void LoadMods(Game game)
		{
			mods.Clear();
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
					mods.Add(item);
				}
			}
		}

		public static void ParseSlots(DT.Field field, ref Type[] types)
		{
			if (field == null)
			{
				return;
			}
			int num = field.NumValues();
			if (num == 0)
			{
				return;
			}
			if (types == null)
			{
				types = new Type[num];
			}
			else if (types.Length != num)
			{
				Array.Resize(ref types, num);
			}
			for (int i = 0; i < num; i++)
			{
				string value = field.String(i);
				if (!string.IsNullOrEmpty(value) && Enum.TryParse<Type>(value, out var result) && result != Type.Unknown)
				{
					types[i] = result;
				}
			}
		}

		public static void ParseSlotTiers(DT.Field field, ref int[] tier)
		{
			if (field == null)
			{
				return;
			}
			int num = field.NumValues();
			if (num != 0)
			{
				if (tier == null)
				{
					tier = new int[num];
				}
				else if (tier.Length != num)
				{
					Array.Resize(ref tier, num);
				}
				for (int i = 0; i < num; i++)
				{
					tier[i] = field.Int(i);
				}
			}
		}

		public static void ParseSlotsFame(DT.Field field, ref int[] presige)
		{
			if (field == null)
			{
				return;
			}
			int num = field.NumValues();
			if (num != 0)
			{
				if (presige == null)
				{
					presige = new int[num];
				}
				else if (presige.Length != num)
				{
					Array.Resize(ref presige, num);
				}
				for (int i = 0; i < num; i++)
				{
					presige[i] = field.Int(i);
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
			if (mods.Count == 0)
			{
				if (!IsBase())
				{
					Game.Log(base.field.Path(include_file: true) + ": Tradition '" + name + "' has no mods", Game.LogType.Warning);
				}
				return;
			}
			Stats.Def def = game.defs.Get<Stats.Def>("KingdomStats");
			for (int i = 0; i < mods.Count; i++)
			{
				StatModifier.Def def2 = mods[i];
				if (!def.HasStat(def2.stat_name))
				{
					Game.Log(def2.field.Path(include_file: true) + ": unknown stat: '" + def2.stat_name + "'", Game.LogType.Error);
					mods.RemoveAt(i);
					i--;
					continue;
				}
				int num = def2.field.NumValues();
				if (num != 1 && num != max_rank - def2.min_rank + 1)
				{
					Game.Log(def2.field.Path(include_file: true) + ": invalid number of values", Game.LogType.Error);
				}
			}
		}

		public Resource GetAdoptCost(Kingdom k, int slot_index = -1)
		{
			if (adopt_cost == null)
			{
				return new Resource();
			}
			Vars vars = new Vars(this);
			vars.Set("adopted_num", k.NumTraditions());
			vars.Set("kingdom", k);
			vars.Set("tier", k.GetTraditionSlotTier(slot_index));
			Resource resource = Resource.Parse(adopt_cost, vars);
			if (k != null && resource != null)
			{
				resource.Mul(1f - k.GetStat(Stats.ks_adopt_tradition_discount_perc) / 100f);
			}
			return resource;
		}

		public Resource GetUpgardeCost(Kingdom k)
		{
			if (k == null)
			{
				return new Resource();
			}
			if (upgrade_cost == null)
			{
				return new Resource();
			}
			if (!upgrade_cost.value.is_valid)
			{
				return new Resource();
			}
			Resource resource = new Resource();
			if (upgrade_cost.value.obj_val is List<DT.SubValue>)
			{
				Tradition tradition = null;
				for (int i = 0; i < k.traditions.Count; i++)
				{
					Tradition tradition2 = k.traditions[i];
					if (tradition2 != null && tradition2.def == this)
					{
						tradition = tradition2;
						break;
					}
				}
				if (tradition == null)
				{
					return new Resource();
				}
				int rank = tradition.rank;
				if (rank >= max_rank)
				{
					return new Resource();
				}
				List<DT.SubValue> list = upgrade_cost.value.obj_val as List<DT.SubValue>;
				if (list.Count < rank)
				{
					return new Resource();
				}
				resource = Resource.Parse(list[rank - 1].value);
			}
			else
			{
				resource = Resource.Parse(upgrade_cost, k);
			}
			resource.Mul(1f - k.GetStat(Stats.ks_upgrade_tradition_discount_perc) / 100f);
			return resource;
		}

		public override Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			switch (key)
			{
			case "skill":
				if (skills == null || skills.Count != 1)
				{
					return Value.Null;
				}
				return new Value(skills[0].def);
			case "adopt_cost":
				if (vars != null)
				{
					if (vars is Character)
					{
						return GetAdoptCost(vars as Kingdom);
					}
					if (vars.GetVar("obj").obj_val is Kingdom k3)
					{
						return GetAdoptCost(k3);
					}
					if (vars.GetVar("target").obj_val is Kingdom k4)
					{
						return GetAdoptCost(k4);
					}
				}
				return GetAdoptCost(null);
			case "upgrade_cost":
				if (vars != null)
				{
					if (vars is Kingdom)
					{
						return GetUpgardeCost(vars as Kingdom);
					}
					if (vars.GetVar("obj").obj_val is Kingdom k)
					{
						return GetUpgardeCost(k);
					}
					if (vars.GetVar("target").obj_val is Kingdom k2)
					{
						return GetUpgardeCost(k2);
					}
				}
				return GetUpgardeCost(null);
			default:
				return base.GetVar(key, vars, as_value);
			}
		}

		public string GetNameKey(IVars vars = null, string form = "")
		{
			return base.field.Path() + ".name";
		}
	}

	public class StatModifier : Stat.Modifier, IVars
	{
		public class Def
		{
			public DT.Field field;

			public Tradition.Def tradition_def;

			public string stat_name;

			public int min_rank = 1;

			public Type type;

			public Def(Tradition.Def tradition_def, DT.Field mf, string stat_name)
			{
				field = mf;
				this.tradition_def = tradition_def;
				this.stat_name = stat_name;
				min_rank = mf.GetInt("min_rank", null, 1);
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

			public float CalcValue(int rank)
			{
				if (rank < min_rank)
				{
					return 0f;
				}
				int num = field.NumValues();
				int idx = 0;
				if (num == tradition_def.max_rank - min_rank + 1)
				{
					idx = rank - min_rank;
				}
				return field.Float(idx);
			}

			public override string ToString()
			{
				return tradition_def.name + "." + stat_name;
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
			return def?.tradition_def?.field;
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

	public class RefData : Data
	{
		public NID kingdom_nid;

		public int slot_id;

		public static RefData Create()
		{
			return new RefData();
		}

		public override string ToString()
		{
			return base.ToString() + "(Tradition #" + slot_id + " in " + kingdom_nid.ToString() + ")";
		}

		public override bool InitFrom(object obj)
		{
			if (!(obj is Tradition { owner: not null } tradition))
			{
				return false;
			}
			kingdom_nid = tradition.owner;
			slot_id = tradition.owner.traditions.IndexOf(tradition);
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			ser.WriteNID(kingdom_nid, "kingdom_nid");
			ser.Write7BitSigned(slot_id, "slot_id");
		}

		public override void Load(Serialization.IReader ser)
		{
			kingdom_nid = ser.ReadNID("kingdom_nid");
			slot_id = ser.Read7BitSigned("slot_id");
		}

		public override object GetObject(Game game)
		{
			return ((Kingdom)kingdom_nid.GetObj(game))?.traditions[slot_id];
		}

		public override bool ApplyTo(object obj, Game game)
		{
			Kingdom kingdom = (Kingdom)kingdom_nid.GetObj(game);
			if (kingdom == null || kingdom.traditions == null || kingdom.traditions.Count <= slot_id)
			{
				return false;
			}
			return true;
		}
	}

	public Def def;

	public Kingdom owner;

	public int rank;

	public List<StatModifier> mods = new List<StatModifier>();

	public Tradition(Def def, int rank)
	{
		this.def = def;
		this.rank = rank;
		CreateMods();
	}

	public void SetOwner(Kingdom owner, bool refresh_court = true)
	{
		DelMods(refresh_court && owner != this.owner);
		this.owner = owner;
		AddMods(refresh_court);
	}

	public void SetRank(int rank, bool send_state = true)
	{
		DelMods(refresh_court: false);
		this.rank = rank;
		AddMods(refresh_court: true);
		if (send_state)
		{
			owner.SendState<Kingdom.TraditionsState>();
		}
		if (owner != null)
		{
			owner.NotifyListeners("traditions_changed");
		}
	}

	private void CreateMods()
	{
		mods.Clear();
		for (int i = 0; i < def.mods.Count; i++)
		{
			StatModifier item = new StatModifier(def.mods[i]);
			mods.Add(item);
		}
	}

	private void DelMods(bool refresh_court)
	{
		if (owner == null)
		{
			return;
		}
		_ = owner.stats;
		for (int i = 0; i < mods.Count; i++)
		{
			StatModifier statModifier = mods[i];
			if (statModifier.stat != null)
			{
				statModifier.stat.DelModifier(statModifier);
			}
		}
		if (refresh_court)
		{
			owner.RefreshCourtSkills();
		}
	}

	private void AddMods(bool refresh_court)
	{
		if (owner == null)
		{
			return;
		}
		int traditionRank = owner.GetTraditionRank(def);
		if (traditionRank > 0)
		{
			Stats stats = owner.stats;
			for (int i = 0; i < mods.Count; i++)
			{
				StatModifier statModifier = mods[i];
				statModifier.value = statModifier.def.CalcValue(traditionRank);
				stats.AddModifier(statModifier.def.stat_name, statModifier);
			}
		}
		if (refresh_court)
		{
			owner.RefreshCourtSkills();
		}
		owner.InvalidateIncomes();
	}

	public override string ToString()
	{
		if (def == null)
		{
			return "<Unknown tradition>";
		}
		return def.id + " rank " + rank;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "def"))
		{
			if (key == "name")
			{
				return def?.GetNameKey();
			}
			return Value.Null;
		}
		return def;
	}
}
