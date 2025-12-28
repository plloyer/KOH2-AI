using System.Collections.Generic;

namespace Logic;

public class SkillsTable
{
	public class RowDef
	{
		public DT.Field field;

		public string name;

		public Skill.Def skill_def;

		public CharacterClass.Def class_def;

		public List<CellDef> cell_defs;

		public CellDef default_cell_def;

		public RowDef(Game game, DT.Field field)
		{
			name = field.key;
			if (field.type == "skill")
			{
				skill_def = game.defs.Find<Skill.Def>(name);
				if (skill_def == null)
				{
					Game.Log(field.Path(include_file: true) + ": unknown skill: '" + name + "'", Game.LogType.Error);
					return;
				}
			}
			else if (field.type == "class")
			{
				class_def = game.defs.Find<CharacterClass.Def>(name);
				if (class_def == null)
				{
					Game.Log(field.Path(include_file: true) + ": unknown class: '" + name + "'", Game.LogType.Error);
					return;
				}
			}
			else if (field.type != "tag" && class_def == null)
			{
				Game.Log(field.Path(include_file: true) + ": unknown skill effect type, ignored: '" + field.type + "'", Game.LogType.Error);
				return;
			}
			this.field = field;
			if (skill_def != null)
			{
				skill_def.row_def = this;
			}
			if (class_def != null)
			{
				class_def.skills_row_def = this;
			}
			if (field.children == null)
			{
				return;
			}
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				if (!(field2.key == ""))
				{
					new CellDef(game, this, null, field2);
				}
			}
		}

		public CellDef FindCell(string name)
		{
			if (cell_defs == null)
			{
				return null;
			}
			for (int i = 0; i < cell_defs.Count; i++)
			{
				CellDef cellDef = cell_defs[i];
				if (cellDef != null && cellDef.name == name)
				{
					return cellDef;
				}
			}
			return null;
		}

		public bool Validate(Object target, bool assume_governing = false)
		{
			if (!(target is Character character))
			{
				return true;
			}
			if (skill_def != null)
			{
				return character.GetSkill(skill_def.name) != null;
			}
			if (class_def != null)
			{
				return character.class_def == class_def;
			}
			if (name == "Govern" && assume_governing)
			{
				return true;
			}
			return character.HasTag(name);
		}

		public override string ToString()
		{
			return name + " {" + ((cell_defs != null) ? cell_defs.Count : 0) + "}";
		}
	}

	public class CellDef
	{
		public RowDef row_def;

		public DT.Field field;

		public string name;

		public string slot_type;

		public CharacterClass.Def class_def;

		public Tradition.Def tradition_def;

		public List<Skill.StatModifier.Def> mod_defs;

		public List<ActionDef> action_defs;

		public List<BattleTactic.Def> tactic_defs;

		public List<CellDef> subcells;

		public CellDef parent;

		public int min_rank = 1;

		public int max_rank = 1;

		public CellDef(Game game, RowDef row_def, CellDef parent, DT.Field field, bool add_children = true)
		{
			name = field?.key;
			this.row_def = row_def;
			this.parent = parent;
			if (parent != null)
			{
				max_rank = parent.max_rank;
			}
			else if (row_def?.skill_def != null)
			{
				max_rank = row_def.skill_def.max_rank;
			}
			if (field != null)
			{
				if (field.type == "class")
				{
					class_def = game.defs.Find<CharacterClass.Def>(name);
					if (class_def == null)
					{
						Game.Log(field.Path(include_file: true) + ": unknown class: '" + name + "'", Game.LogType.Error);
						return;
					}
					if (row_def?.skill_def != null)
					{
						slot_type = field.GetString("slot_type");
						if (slot_type != "primary" && slot_type != "secondary")
						{
							Game.Log(field.Path(include_file: true) + ": missing / invalid slot_type", Game.LogType.Warning);
						}
					}
				}
				else if (field.type == "at_rank")
				{
					if (parent == null)
					{
						Game.Log(field.Path(include_file: true) + ": cannot have at_rank cell at row level", Game.LogType.Error);
						return;
					}
					int.TryParse(field.key, out min_rank);
					if (min_rank <= 1)
					{
						Game.Log(field.Path(include_file: true) + ": invalid at_rank key (must be integer): '" + field.key + "'", Game.LogType.Error);
					}
					else if (min_rank > max_rank)
					{
						Game.Log(field.Path(include_file: true) + ": invalid at_rank key (must be less than max_rank): '" + field.key + "'", Game.LogType.Error);
					}
				}
				else if (field.type != "tag" && field.key != "Default")
				{
					Game.Log(field.Path(include_file: true) + ": unknown skill cell type: '" + field.type + "', ignored", Game.LogType.Warning);
					return;
				}
			}
			this.field = field;
			if (parent != null)
			{
				max_rank = parent.max_rank;
				if (parent.subcells == null)
				{
					parent.subcells = new List<CellDef>();
				}
				parent.subcells.Add(this);
			}
			else if (row_def != null)
			{
				if (row_def.cell_defs == null)
				{
					row_def.cell_defs = new List<CellDef>();
				}
				row_def.cell_defs.Add(this);
				if (field.key == "Default")
				{
					row_def.default_cell_def = this;
				}
			}
			if (add_children)
			{
				AddChildren(game, field?.children);
			}
		}

		public void AddChildren(Game game, List<DT.Field> children)
		{
			if (children == null)
			{
				return;
			}
			for (int i = 0; i < children.Count; i++)
			{
				DT.Field field = children[i];
				if (Skill.StatModifier.Def.IsMod(field.type))
				{
					AddStatMod(game, field);
				}
				else if (field.type == "action" || field.type == "plot" || field.type == "opportunity")
				{
					AddAction(game, field);
				}
				else if (field.type == "tactic")
				{
					AddBattleTactic(game, field);
				}
				else if (field.type == "tag" || field.type == "class" || field.type == "at_rank")
				{
					AddSubcell(game, field);
				}
			}
		}

		private void AddStatMod(Game game, DT.Field mf)
		{
			AddStatMod(game, mf, mf.key);
		}

		private void AddStatMod(Game game, DT.Field mf, string stat_name)
		{
			string text = Skill.StatModifier.Def.TargetStatsType(mf.type);
			if (text == null)
			{
				Game.Log(mf.Path(include_file: true) + ": unknown modifier type: '" + mf.type + "'", Game.LogType.Error);
				return;
			}
			Stats.Def def = game.defs.Get<Stats.Def>(text);
			if (def == null || !def.HasStat(stat_name))
			{
				Game.Log(mf.Path(include_file: true) + ": unknown stat: '" + text + "." + stat_name + "'", Game.LogType.Error);
				return;
			}
			int num = mf.NumValues();
			if (num != 1 && num != max_rank - min_rank + 1)
			{
				Game.Log(mf.Path(include_file: true) + ": invalid number of values", Game.LogType.Error);
				return;
			}
			Skill.StatModifier.Def item = new Skill.StatModifier.Def(this, mf, stat_name);
			if (mod_defs == null)
			{
				mod_defs = new List<Skill.StatModifier.Def>();
			}
			mod_defs.Add(item);
		}

		private void AddAction(Game game, DT.Field af)
		{
			string key = af.key;
			Action.Def def = game.defs.Find<Action.Def>(key);
			if (def == null)
			{
				Game.Log(af.Path(include_file: true) + ": unknown action: '" + key + "'", Game.LogType.Error);
				return;
			}
			ActionDef actionDef = new ActionDef();
			actionDef.cell_def = this;
			actionDef.field = af;
			actionDef.name = key;
			actionDef.action_def = def;
			actionDef.condition_field = af;
			if (action_defs == null)
			{
				action_defs = new List<ActionDef>();
			}
			action_defs.Add(actionDef);
			if (def.skill_dependencies == null)
			{
				def.skill_dependencies = new List<ActionDef>();
			}
			def.skill_dependencies.Add(actionDef);
			CharacterClass.Def def2 = GetClassDef();
			if (def2 == null)
			{
				def2 = game.defs.GetBase<CharacterClass.Def>();
			}
			if (def2 != null)
			{
				if (def2.actions == null)
				{
					def2.actions = new List<(Action.Def, DT.Field)>();
				}
				if (!def2.HasAction(def))
				{
					def2.AddAction(def, new DT.Field(null)
					{
						value = false
					});
				}
			}
		}

		private void AddBattleTactic(Game game, DT.Field cf)
		{
			string key = cf.key;
			BattleTactic.Def def = game.defs.Find<BattleTactic.Def>(key);
			if (def == null)
			{
				Game.Log(cf.Path(include_file: true) + ": unknown battle tactic: '" + key + "'", Game.LogType.Error);
				return;
			}
			if (tactic_defs == null)
			{
				tactic_defs = new List<BattleTactic.Def>();
			}
			tactic_defs.Add(def);
			if (def.skill_dependencies == null)
			{
				def.skill_dependencies = new List<CellDef>();
			}
			def.skill_dependencies.Add(this);
		}

		public bool HasBonuses()
		{
			if (mod_defs != null)
			{
				return true;
			}
			if (action_defs != null)
			{
				return true;
			}
			if (tactic_defs != null)
			{
				return true;
			}
			return false;
		}

		private void AddSubcell(Game game, DT.Field cf)
		{
			new CellDef(game, row_def, this, cf);
		}

		public CellDef FindSubCell(string name)
		{
			if (subcells == null)
			{
				return null;
			}
			for (int i = 0; i < subcells.Count; i++)
			{
				CellDef cellDef = subcells[i];
				if (cellDef.name == name)
				{
					return cellDef;
				}
			}
			return null;
		}

		public int GetSkillRank(Object target)
		{
			if (row_def?.skill_def == null)
			{
				return 1;
			}
			return row_def.skill_def.GetRank(target);
		}

		public int GetSkillMaxRank(Object target)
		{
			if (row_def?.skill_def == null)
			{
				return 1;
			}
			return row_def.skill_def.max_rank;
		}

		public CharacterClass.Def GetClassDef()
		{
			for (CellDef cellDef = this; cellDef != null; cellDef = cellDef.parent)
			{
				if (cellDef.class_def != null)
				{
					return cellDef.class_def;
				}
			}
			return null;
		}

		public Tradition.Def GetTraditionDef()
		{
			for (CellDef cellDef = this; cellDef != null; cellDef = cellDef.parent)
			{
				if (cellDef.tradition_def != null)
				{
					return cellDef.tradition_def;
				}
			}
			return null;
		}

		public bool Validate(Object target, bool check_parent, bool check_row = true, bool assume_governing = false)
		{
			if (check_parent)
			{
				if (parent != null)
				{
					if (!parent.Validate(target, check_parent: true, check_row, assume_governing))
					{
						return false;
					}
				}
				else if (check_row && !row_def.Validate(target, assume_governing))
				{
					return false;
				}
			}
			if (name == "Default")
			{
				return true;
			}
			if (name == "Govern" && assume_governing)
			{
				return true;
			}
			Character character = target as Character;
			if (character != null && class_def != null)
			{
				return character.class_def == class_def;
			}
			int skillRank = GetSkillRank(target);
			Tradition.Def traditionDef = GetTraditionDef();
			if (traditionDef != null)
			{
				int skillMaxRank = GetSkillMaxRank(target);
				if (skillRank > 0 && skillRank < skillMaxRank)
				{
					return false;
				}
				Tradition tradition = (target?.GetKingdom())?.FindTradition(traditionDef);
				if (tradition == null && character != null)
				{
					return false;
				}
				if (tradition != null && tradition.rank < min_rank)
				{
					return false;
				}
			}
			else if (skillRank > 0 && skillRank < min_rank)
			{
				return false;
			}
			if (field?.type == "tag" && tradition_def == null)
			{
				return character?.HasTag(name) ?? true;
			}
			return true;
		}

		public string NamePath()
		{
			return ((parent != null) ? parent.NamePath() : row_def.name) + "/" + name;
		}

		public override string ToString()
		{
			string text = NamePath();
			if (mod_defs != null && mod_defs.Count > 0)
			{
				text = text + ", mods: " + mod_defs.Count;
			}
			if (action_defs != null && action_defs.Count > 0)
			{
				text = text + ", actions: " + action_defs.Count;
			}
			if (subcells != null && subcells.Count > 0)
			{
				text = text + ", subcells: " + subcells.Count;
			}
			return text;
		}
	}

	public class ActionDef
	{
		public CellDef cell_def;

		public DT.Field field;

		public string name;

		public Action.Def action_def;

		public DT.Field condition_field;

		public bool Validate(Character c, bool check_cell)
		{
			if (action_def == null)
			{
				return false;
			}
			if (check_cell && !cell_def.Validate(c, check_parent: true))
			{
				return false;
			}
			if (condition_field != null && condition_field.value.is_valid && !condition_field.Bool(c))
			{
				return false;
			}
			return true;
		}

		public override string ToString()
		{
			return "action " + cell_def.row_def.name + " / " + cell_def.name + " / " + name;
		}
	}

	public delegate void EnumGovernorBonusesCallback(Castle castle, Character governor, Skill.StatModifier.Def mdef);

	public struct GovernorModEval
	{
		public Skill.StatModifier.Def mdef;

		public Stat stat;

		public bool active;

		public float final_value;

		public float mod_value;

		public float multiplier;

		public override string ToString()
		{
			if (stat == null)
			{
				return "null  (" + mdef?.cell_def?.NamePath() + ")";
			}
			string text = (active ? "[+]" : "[-]");
			ResourceType resourceType = ((stat?.def != null) ? stat.def.GetResource() : ResourceType.None);
			string text2 = ((resourceType == ResourceType.None) ? "" : $"{resourceType}: ");
			string text3 = ((multiplier < 0f) ? "" : $" ({mod_value} x {multiplier})");
			return $"{text} {text2}{final_value}{text3} {stat.def.name} ({mdef?.cell_def?.NamePath()})";
		}
	}

	public List<RowDef> row_defs = new List<RowDef>();

	public List<RowDef> tag_rows = new List<RowDef>();

	private static Vars tmp_eval_governor_mod_vars = new Vars();

	public void Load(Game game)
	{
		row_defs.Clear();
		tag_rows.Clear();
		Skill.Def def = game.defs.GetBase<Skill.Def>();
		if (def == null || def.field == null)
		{
			return;
		}
		DT.Field field = def.field.FindChild("effects");
		if (field == null)
		{
			return;
		}
		List<string> list = field.Keys();
		for (int i = 0; i < list.Count; i++)
		{
			string path = list[i];
			DT.Field field2 = field.FindChild(path);
			if (field2 == null)
			{
				continue;
			}
			RowDef rowDef = new RowDef(game, field2);
			if (rowDef.field != null)
			{
				row_defs.Add(rowDef);
				if (rowDef.skill_def == null && rowDef.class_def == null)
				{
					tag_rows.Add(rowDef);
				}
			}
		}
		LoadTraditionBuffs(game);
	}

	private void LoadTraditionBuffs(Game game)
	{
		List<Tradition.Def> defs = game.defs.GetDefs<Tradition.Def>();
		if (defs != null)
		{
			for (int i = 0; i < defs.Count; i++)
			{
				Tradition.Def tdef = defs[i];
				LoadTraditionBuffs(game, tdef);
			}
		}
	}

	private void LoadTraditionBuffs(Game game, Tradition.Def tdef)
	{
		if (tdef.skills != null)
		{
			for (int i = 0; i < tdef.skills.Count; i++)
			{
				Tradition.Def.SkillInfo si = tdef.skills[i];
				LoadTraditionBuffs(game, tdef, si);
			}
		}
	}

	private void LoadTraditionBuffs(Game game, Tradition.Def tdef, Tradition.Def.SkillInfo si)
	{
		if (si.bonus_fields == null)
		{
			return;
		}
		if (si.def.row_def == null)
		{
			Game.Log(si.field.Path(include_file: true) + ": Cannot add buffs to skill with no effects", Game.LogType.Error);
			return;
		}
		for (int i = 0; i < si.bonus_fields.Count; i++)
		{
			DT.Field bf = si.bonus_fields[i];
			LoadTraditionBuffs(game, tdef, si, bf);
		}
	}

	private void LoadTraditionBuffs(Game game, Tradition.Def tdef, Tradition.Def.SkillInfo si, DT.Field bf)
	{
		CellDef cellDef = si.def.row_def?.FindCell(bf.key);
		if (cellDef == null)
		{
			cellDef = new CellDef(game, si.def.row_def, null, bf, add_children: false);
			if (cellDef.field == null)
			{
				return;
			}
		}
		CellDef cellDef2 = new CellDef(game, si.def.row_def, cellDef, null);
		cellDef2.name = tdef.id;
		cellDef2.field = bf;
		cellDef2.tradition_def = tdef;
		cellDef2.AddChildren(game, bf.children);
		if (si.bonus_cells == null)
		{
			si.bonus_cells = new List<CellDef>();
		}
		si.bonus_cells.Add(cellDef2);
	}

	public static RowDef FindGovernRowDef(Game game)
	{
		SkillsTable skillsTable = (game?.defs?.GetBase<Skill.Def>())?.skills_table;
		if (skillsTable?.tag_rows == null)
		{
			return null;
		}
		for (int i = 0; i < skillsTable.tag_rows.Count; i++)
		{
			RowDef rowDef = skillsTable.tag_rows[i];
			if (rowDef.name == "Govern")
			{
				return rowDef;
			}
		}
		return null;
	}

	public static void EnumGovernorBonuses(Castle castle, Character governor, EnumGovernorBonusesCallback callback)
	{
		if (castle == null)
		{
			castle = governor?.governed_castle;
			if (castle == null)
			{
				return;
			}
		}
		else if (governor == null)
		{
			governor = castle.governor;
			if (governor == null)
			{
				return;
			}
		}
		RowDef rowDef = FindGovernRowDef(governor.game);
		if (rowDef != null)
		{
			EnumGovernorBonuses(rowDef, "Default", castle, governor, callback);
			EnumGovernorBonuses(rowDef, governor.class_name, castle, governor, callback);
			if (governor.IsKing())
			{
				EnumGovernorBonuses(rowDef, "King", castle, governor, callback);
			}
			if (governor.IsCardinal())
			{
				EnumGovernorBonuses(rowDef, "Cardinal", castle, governor, callback);
			}
			if (governor.IsPatriarch())
			{
				EnumGovernorBonuses(rowDef, "Patriarch", castle, governor, callback);
			}
		}
		EnumGovernorSkillBonuses(castle, governor, callback);
	}

	private static void EnumGovernorBonuses(CellDef cell_def, Castle castle, Character governor, EnumGovernorBonusesCallback callback)
	{
		if (cell_def?.mod_defs != null)
		{
			for (int i = 0; i < cell_def.mod_defs.Count; i++)
			{
				Skill.StatModifier.Def def = cell_def.mod_defs[i];
				if (!(def.field?.type != "governed_realm_mod"))
				{
					callback(castle, governor, def);
				}
			}
		}
		if (cell_def.subcells != null)
		{
			for (int j = 0; j < cell_def.subcells.Count; j++)
			{
				EnumGovernorBonuses(cell_def.subcells[j], castle, governor, callback);
			}
		}
	}

	private static void EnumGovernorBonuses(RowDef row_def, string cell_name, Castle castle, Character governor, EnumGovernorBonusesCallback callback)
	{
		CellDef cellDef = row_def?.FindCell(cell_name);
		if (cellDef != null)
		{
			EnumGovernorBonuses(cellDef, castle, governor, callback);
		}
	}

	public static void EnumGovernorSkillBonuses(Castle castle, Character governor, EnumGovernorBonusesCallback callback)
	{
		if (governor?.skills == null)
		{
			return;
		}
		for (int i = 0; i < governor.skills.Count; i++)
		{
			Skill skill = governor.skills[i];
			if (skill?.def?.row_def == null)
			{
				continue;
			}
			EnumGovernorBonuses(skill.def.row_def, "Govern", castle, governor, callback);
			CellDef cellDef = skill.def.row_def.FindCell(governor.class_name);
			if (cellDef?.subcells == null)
			{
				continue;
			}
			for (int j = 0; j < cellDef.subcells.Count; j++)
			{
				CellDef cellDef2 = cellDef.subcells[j];
				if (cellDef2.name == "Govern")
				{
					EnumGovernorBonuses(cellDef2, castle, governor, callback);
				}
				else if (cellDef2.tradition_def != null)
				{
					CellDef cellDef3 = cellDef2.FindSubCell("Govern");
					if (cellDef3 != null)
					{
						EnumGovernorBonuses(cellDef3, castle, governor, callback);
					}
				}
			}
		}
	}

	public static void SetupEvalGovernorModVars(Vars vars, Castle castle, Character governor)
	{
		vars.obj = governor;
		vars.Set("governed_castle", castle);
		vars.Set("governor", governor);
		Realm val = castle?.GetRealm();
		vars.Set("realm", val);
		Kingdom kingdom = governor.GetKingdom();
		vars.Set("kingdom", kingdom);
		vars.Set("class_name", governor.class_name);
		vars.Set("class_level", governor.GetClassLevel());
	}

	public static GovernorModEval EvalGovernorMod(Castle castle, Character governor, Skill.StatModifier.Def mdef, Vars vars = null, Stat stat = null)
	{
		if (stat == null)
		{
			stat = castle?.GetRealm()?.stats?.Find(mdef.stat_name);
		}
		GovernorModEval result = new GovernorModEval
		{
			mdef = mdef,
			stat = stat
		};
		if (stat?.def == null)
		{
			return result;
		}
		if (mdef.type == Stat.Modifier.Type.Perc)
		{
			Game.Log($"Percent modifiers are not supported in governor bonuses: {mdef}", Game.LogType.Error);
			return result;
		}
		result.mod_value = mdef.CalcValue(governor, check_tradition: false);
		result.multiplier = -1f;
		result.final_value = result.mod_value;
		if (stat.def.HasMultipler())
		{
			Realm realm = castle.GetRealm();
			result.multiplier = stat.def.CalcMultiplier(realm, governor);
			if (result.multiplier >= 0f)
			{
				result.final_value *= result.multiplier;
			}
		}
		if (!mdef.cell_def.Validate(governor, check_parent: true, check_row: true, assume_governing: true))
		{
			return result;
		}
		if (mdef.condition != null)
		{
			if (vars == null)
			{
				vars = tmp_eval_governor_mod_vars;
				SetupEvalGovernorModVars(vars, castle, governor);
			}
			if (!mdef.condition.Bool(vars))
			{
				return result;
			}
		}
		result.active = true;
		return result;
	}
}
