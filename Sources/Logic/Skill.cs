using System;
using System.Collections.Generic;

namespace Logic;

public class Skill : IVars
{
	public class Def : Logic.Def
	{
		public string name;

		public SkillsTable.RowDef row_def;

		public int max_rank = 3;

		public SkillsTable skills_table;

		private DT.Field learn_cost;

		private DT.Field upgrade_cost;

		public float ai_eval = 1f;

		public float ai_eval_stability;

		public bool IsApplicableTo(CharacterClass.Def class_def)
		{
			if (class_def == null)
			{
				return false;
			}
			if (row_def == null)
			{
				return false;
			}
			if (row_def.FindCell(class_def.id) == null)
			{
				return false;
			}
			return true;
		}

		public bool IsApplicableTo(string class_name)
		{
			if (class_name == null)
			{
				return false;
			}
			if (row_def == null)
			{
				return false;
			}
			if (row_def.FindCell(class_name) == null)
			{
				return false;
			}
			return true;
		}

		public int GetRank(Object owner)
		{
			if (owner == null)
			{
				return 0;
			}
			if (owner is Character character)
			{
				int num = character.GetSkillRank(name);
				if (num == 0)
				{
					num = character.NewSkillRank(this);
				}
				return num;
			}
			return 0;
		}

		public Tradition.Type GetTraditionType()
		{
			SkillsTable.CellDef cellDef = row_def?.FindCell("Marshal");
			if (cellDef == null)
			{
				return Tradition.Type.NonMilitary;
			}
			if (cellDef.slot_type == "primary")
			{
				return Tradition.Type.Military;
			}
			return Tradition.Type.NonMilitary;
		}

		public bool IsAffinitySkill(Character c)
		{
			if (c == null)
			{
				return false;
			}
			if (c.new_skills == null)
			{
				return false;
			}
			for (int i = 0; i < c.new_skills.Count; i++)
			{
				if (c.new_skills[i] == this)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsInherited(Character c)
		{
			if (c == null)
			{
				return false;
			}
			Character father = c.GetFather();
			if (father == null)
			{
				return false;
			}
			if (father.skills == null)
			{
				return false;
			}
			for (int i = 0; i < father.skills.Count; i++)
			{
				if (father.skills[i] != null && father.skills[i].def == this)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsGrantedByTradition(Kingdom k)
		{
			if (k?.traditions == null)
			{
				return false;
			}
			for (int i = 0; i < k.traditions.Count; i++)
			{
				Tradition tradition = k.traditions[i];
				if (tradition?.def != null && tradition.def.GrantsSkill(this))
				{
					return true;
				}
			}
			return false;
		}

		public string GetSlotType(Character c)
		{
			return GetSlotType(c?.class_name);
		}

		public string GetSlotType(CharacterClass.Def class_def)
		{
			return GetSlotType(class_def?.id);
		}

		public string GetSlotType(string class_name)
		{
			if (class_name == null)
			{
				return null;
			}
			if (row_def == null)
			{
				return null;
			}
			return row_def.FindCell(class_name)?.slot_type;
		}

		public int GetClassLevelValue(CharacterClass.Def class_def)
		{
			if (!IsApplicableTo(class_def))
			{
				return 0;
			}
			return 1;
		}

		public Resource GetLearnCost(Character c)
		{
			if (learn_cost == null)
			{
				return new Resource();
			}
			if (!learn_cost.value.is_valid)
			{
				return new Resource();
			}
			Resource resource;
			if (learn_cost.value.obj_val is List<DT.SubValue> list)
			{
				int num = c?.GetSkillsCount() ?? 0;
				if (list.Count <= num)
				{
					return new Resource();
				}
				resource = Resource.Parse(list[num].value);
			}
			else
			{
				resource = Resource.Parse(learn_cost, c);
			}
			if (resource != null && c?.GetKingdom() != null)
			{
				resource.Mul(1f - c.GetKingdom().GetStat(Stats.ks_learn_skill_discount_perc) / 100f);
			}
			return resource;
		}

		public Resource GetUpgardeCost(Character c)
		{
			if (upgrade_cost == null || c == null)
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
				int skillRank = c.GetSkillRank(name);
				if (skillRank == 0 || skillRank > max_rank)
				{
					return new Resource();
				}
				List<DT.SubValue> list = upgrade_cost.value.obj_val as List<DT.SubValue>;
				if (list.Count < skillRank)
				{
					return new Resource();
				}
				resource = Resource.Parse(list[skillRank - 1].value);
			}
			else
			{
				resource = Resource.Parse(upgrade_cost, c);
			}
			if (resource != null && c.GetKingdom() != null)
			{
				resource.Mul(1f - c.GetKingdom().GetStat(Stats.ks_upgrade_skill_discount_perc) / 100f);
			}
			return resource;
		}

		public string GetClassGradientKey()
		{
			if (row_def?.cell_defs == null)
			{
				return null;
			}
			for (int i = 0; i < row_def.cell_defs.Count; i++)
			{
				SkillsTable.CellDef cellDef = row_def.cell_defs[i];
				if (cellDef.class_def != null && !(cellDef.slot_type != "primary"))
				{
					return cellDef.class_def.name;
				}
			}
			return null;
		}

		public override Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			switch (key)
			{
			case "learn_cost":
				if (vars != null)
				{
					if (vars is Character)
					{
						return GetLearnCost(vars as Character);
					}
					if (vars.GetVar("obj").obj_val is Character c3)
					{
						return GetLearnCost(c3);
					}
					if (vars.GetVar("target").obj_val is Character c4)
					{
						return GetLearnCost(c4);
					}
				}
				return GetLearnCost(null);
			case "upgrade_cost":
				if (vars != null)
				{
					if (vars is Character)
					{
						return GetUpgardeCost(vars as Character);
					}
					if (vars.GetVar("obj").obj_val is Character c)
					{
						return GetUpgardeCost(c);
					}
					if (vars.GetVar("target").obj_val is Character c2)
					{
						return GetUpgardeCost(c2);
					}
				}
				return GetUpgardeCost(null);
			case "class_gradient_key":
				return GetClassGradientKey();
			default:
				return base.GetVar(key, vars, as_value);
			}
		}

		public override bool Load(Game game)
		{
			name = base.field.key;
			if (name.EndsWith("Skill", StringComparison.Ordinal))
			{
				name = name.Substring(0, name.Length - 5);
			}
			if (IsBase())
			{
				max_rank = base.field.GetInt("max_rank", null, max_rank);
				skills_table = new SkillsTable();
			}
			else
			{
				Def def = game.defs.GetBase<Def>();
				max_rank = def.max_rank;
				skills_table = def.skills_table;
			}
			learn_cost = base.field.FindChild("learn_cost");
			upgrade_cost = base.field.FindChild("upgrade_cost");
			ai_eval = base.field.GetFloat("ai_eval", null, ai_eval);
			ai_eval_stability = base.field.GetFloat("ai_eval_stability", null, ai_eval_stability);
			return true;
		}

		public override bool Validate(Game game)
		{
			if (IsBase())
			{
				skills_table.Load(game);
			}
			return true;
		}

		public string GetNameKey(IVars vars = null, string form = "")
		{
			return base.field.Path() + ".name";
		}
	}

	public class StatModifier : Stat.Modifier, IVars, IListener
	{
		public class Def
		{
			public DT.Field field;

			public SkillsTable.CellDef cell_def;

			public string stat_name;

			public DT.Field condition;

			public bool is_expression;

			public List<string> recalc_on;

			public Type type;

			public int min_rank
			{
				get
				{
					if (cell_def != null)
					{
						return cell_def.min_rank;
					}
					return 1;
				}
			}

			public int max_rank
			{
				get
				{
					if (cell_def != null)
					{
						return cell_def.max_rank;
					}
					return 1;
				}
			}

			public Def(SkillsTable.CellDef cell_def, DT.Field mf, string stat_name)
			{
				this.field = mf;
				this.cell_def = cell_def;
				this.stat_name = stat_name;
				condition = mf.FindChild("condition");
				recalc_on = null;
				DT.Field field = mf.FindChild("recalc_on");
				if (field != null)
				{
					recalc_on = new List<string>();
					List<DT.Field> list = field.Children();
					if (list != null)
					{
						for (int i = 0; i < list.Count; i++)
						{
							DT.Field field2 = list[i];
							if (!string.IsNullOrEmpty(field2.key))
							{
								recalc_on.Add(field2.key);
							}
						}
					}
				}
				int num = mf.NumValues();
				for (int j = 0; j < num; j++)
				{
					if (this.field.Value(j, null, calc_expression: false).obj_val is Expression)
					{
						is_expression = true;
						break;
					}
				}
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

			public static bool IsMod(string field_type)
			{
				switch (field_type)
				{
				default:
					return field_type == "governed_realm_mod";
				case "mod":
				case "kingdom_mod":
				case "original_kingdom_mod":
				case "mission_kingdom_mod":
				case "special_court_kingdom_mod":
					return true;
				}
			}

			public static string TargetStatsType(string mod_type)
			{
				switch (mod_type)
				{
				case "mod":
					return "CharacterStats";
				case "kingdom_mod":
				case "original_kingdom_mod":
				case "mission_kingdom_mod":
				case "special_court_kingdom_mod":
					return "KingdomStats";
				case "governed_realm_mod":
					return "RealmStats";
				default:
					return null;
				}
			}

			public string TargetStatsType()
			{
				return TargetStatsType(field?.type);
			}

			public static Stats TargetStats(Object owner, string mod_type)
			{
				if (owner is Kingdom kingdom)
				{
					if (mod_type != "kingdom_mod")
					{
						return null;
					}
					return kingdom.stats;
				}
				if (!(owner is Character character))
				{
					return null;
				}
				return mod_type switch
				{
					"mod" => character.stats, 
					"kingdom_mod" => character.GetKingdom()?.stats, 
					"original_kingdom_mod" => character.GetOriginalKingdom()?.stats, 
					"special_court_kingdom_mod" => character.GetSpecialCourtKingdom()?.stats, 
					"mission_kingdom_mod" => character.mission_kingdom?.stats, 
					"governed_realm_mod" => character.governed_castle?.GetRealm()?.stats, 
					_ => null, 
				};
			}

			public int GetRank(Object target, bool check_tradition = true)
			{
				Tradition.Def def = cell_def?.GetTraditionDef();
				if (def != null)
				{
					if (!check_tradition)
					{
						return 1;
					}
					return (target?.GetKingdom())?.GetTraditionRank(def) ?? 0;
				}
				if (cell_def?.row_def?.skill_def == null)
				{
					return 1;
				}
				return cell_def.row_def.skill_def.GetRank(target);
			}

			public float CalcRankValue(DT.Field field, Object target, bool check_tradition = true)
			{
				int rank = GetRank(target, check_tradition);
				if (rank < min_rank)
				{
					return 0f;
				}
				int num = field.NumValues();
				if (num <= 1)
				{
					return field.Float(0, target);
				}
				if (num == max_rank - min_rank + 1)
				{
					return field.Float(rank - min_rank, target);
				}
				Game.Log($"Invalid number of values in skill stat modifier: {this}", Game.LogType.Error);
				return 0f;
			}

			public void CalcValueParams(DT.Field field, out float min_val, out float max_val, out int min_rank)
			{
				int num = field.NumValues();
				min_rank = cell_def.min_rank;
				if (num < 1)
				{
					min_val = (max_val = 0f);
					return;
				}
				if (num == 1)
				{
					min_val = (max_val = field.Float(0));
					return;
				}
				min_val = 0f;
				max_val = field.Float(num - 1);
				for (int i = 0; i < num; i++)
				{
					min_val = field.Float(i);
					if (min_val == 0f)
					{
						min_rank++;
						continue;
					}
					break;
				}
			}

			public float CalcValue(Object target, bool check_tradition = true)
			{
				return CalcRankValue(field, target, check_tradition);
			}

			public override string ToString()
			{
				return cell_def?.row_def?.name + "." + cell_def?.name + "." + stat_name + " = " + field.value_str;
			}
		}

		public Def def;

		public Object source;

		public float def_value;

		public static bool log;

		public string mod_type => def.field.type;

		public Stats tgt_stats => stat?.stats;

		public StatModifier(Def def, Object source)
		{
			this.def = def;
			this.source = source;
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
			if (def.cell_def?.tradition_def != null)
			{
				return def.cell_def.tradition_def.field;
			}
			if (def.cell_def?.row_def?.skill_def != null)
			{
				return def.cell_def.row_def.skill_def.field;
			}
			if (def.cell_def?.class_def != null)
			{
				return def.cell_def.class_def.field;
			}
			return def.cell_def.field;
		}

		public override string ToString()
		{
			return def.ToString() + Stat.Modifier.ToString(value, type);
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			if (key == "owner")
			{
				return new Value(base.owner);
			}
			if (!(base.owner is IVars vars2))
			{
				return Value.Unknown;
			}
			return vars2.GetVar(key, vars, as_value);
		}

		public override bool IsConst()
		{
			if (def.recalc_on != null)
			{
				return true;
			}
			if (def.condition == null)
			{
				return !def.is_expression;
			}
			return false;
		}

		public override float CalcValue(Stats stats, Stat stat)
		{
			if (def.condition != null && !def.condition.Bool(source))
			{
				return 0f;
			}
			if (def.is_expression)
			{
				def_value = def.CalcValue(source);
			}
			return def_value;
		}

		public void Apply(Object owner)
		{
			Stats stats = Def.TargetStats(owner, mod_type);
			if (stats != tgt_stats)
			{
				Revert(owner);
			}
			float num = value;
			def_value = def.CalcValue(owner);
			if (stats == null)
			{
				return;
			}
			if (stat != null && IsConst() && def_value != num)
			{
				Revert(owner);
			}
			if (stat == null)
			{
				stats.AddModifier(def.stat_name, this);
				if (log)
				{
					(stats.owner as Object)?.Log("Added modifier " + ToString());
				}
			}
			else if (num != value)
			{
				stats.NotifyChanged(stats.Find(def.stat_name));
				if (log)
				{
					(stats.owner as Object)?.Log("Changed modifier " + ToString());
				}
			}
		}

		public void Revert(Object owner)
		{
			if (stat != null)
			{
				if (log)
				{
					(tgt_stats?.owner as Object)?.Log("Deleting modifier " + ToString());
				}
				stat.DelModifier(this);
				stat = null;
			}
		}

		public override void OnActivate(Stats stats, Stat stat, bool from_state = false)
		{
			base.OnActivate(stats, stat, from_state);
			if (def.recalc_on != null && source != null)
			{
				source.AddListener(this);
			}
		}

		public override void OnDeactivate(Stats stats, Stat stat)
		{
			base.OnDeactivate(stats, stat);
			if (def.recalc_on != null && source != null)
			{
				source.DelListener(this);
			}
		}

		public void OnMessage(object obj, string message, object param)
		{
			Stat stat = base.stat;
			if (stat != null && def.recalc_on.IndexOf(message) >= 0)
			{
				float num = CalcValue(stat.stats, stat);
				if (num != value)
				{
					stat.DelModifier(this, notify_changed: false);
					value = num;
					stat.AddModifier(this);
				}
			}
		}
	}

	public Def def;

	public Object owner;

	public int rank;

	public SkillMods.Row mods_row;

	public Skill(Def def, Object owner, int rank = 1)
	{
		this.def = def;
		this.owner = owner;
		this.rank = rank;
	}

	public override string ToString()
	{
		if (def == null)
		{
			return "unknown";
		}
		string text = def.name;
		int num = MaxRank();
		if (num > 1)
		{
			text += $": {rank}/{num}";
		}
		return text;
	}

	public string GetNameKey(IVars vars = null, string form = "")
	{
		return def.id + ".name";
	}

	public int MaxRank()
	{
		return def.max_rank;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"owner" => owner, 
			"name" => GetNameKey(), 
			"rank" => def.GetRank(owner), 
			"max_rank" => MaxRank(), 
			"def" => def, 
			_ => Value.Unknown, 
		};
	}
}
