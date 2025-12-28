using System;
using System.Collections.Generic;

namespace Logic;

public class BattleTactic
{
	public class Def : Logic.Def
	{
		public struct ReqUnitType
		{
			public Unit.Type type;

			public bool heavy;

			public ReqUnitType(string stype)
			{
				heavy = stype.StartsWith("Heavy", StringComparison.Ordinal);
				if (heavy)
				{
					stype = stype.Substring(5);
				}
				if (!Enum.TryParse<Unit.Type>(stype, out type))
				{
					type = Unit.Type.COUNT;
				}
			}

			public override string ToString()
			{
				if (type == Unit.Type.COUNT)
				{
					return "<invalid>";
				}
				if (heavy)
				{
					return $"Heavy {type}";
				}
				return type.ToString();
			}
		}

		public DT.Field condition_field;

		public List<ReqUnitType> unit_types;

		public List<SkillsTable.CellDef> skill_dependencies;

		public override bool Load(Game game)
		{
			unit_types = null;
			skill_dependencies = null;
			condition_field = base.field.FindChild("condition");
			LoadUnitTypes(base.field.FindChild("units"));
			return base.Load(game);
		}

		private void LoadUnitTypes(DT.Field f)
		{
			if (f == null)
			{
				return;
			}
			int num = f.NumValues();
			for (int i = 0; i < num; i++)
			{
				string stype = f.String(i);
				AddReqUnit(stype);
			}
			if (f.children != null)
			{
				for (int j = 0; j < f.children.Count; j++)
				{
					DT.Field field = f.children[j];
					AddReqUnit(field.key);
				}
			}
		}

		private void AddReqUnit(string stype)
		{
			if (string.IsNullOrEmpty(stype))
			{
				return;
			}
			ReqUnitType item = new ReqUnitType(stype);
			if (item.type == Unit.Type.COUNT)
			{
				Game.Log("Battle tactic " + base.id + " has invalid unity type requirement: '" + stype + "'", Game.LogType.Error);
			}
			else
			{
				if (unit_types == null)
				{
					unit_types = new List<ReqUnitType>();
				}
				unit_types.Add(item);
			}
		}

		public bool Validate(Army a)
		{
			if (!ValidateUnitTypes(a))
			{
				return false;
			}
			if (!ValidateSkillDependencies(a))
			{
				return false;
			}
			if (!ValidateCondition(a))
			{
				return false;
			}
			return true;
		}

		public bool ValidateUnitTypes(Army a)
		{
			if (unit_types == null)
			{
				return true;
			}
			for (int i = 0; i < a.units.Count; i++)
			{
				Unit u = a.units[i];
				if (MatchUnitType(u))
				{
					return true;
				}
			}
			return false;
		}

		public bool MatchUnitType(Unit u)
		{
			for (int i = 0; i < unit_types.Count; i++)
			{
				ReqUnitType ru = unit_types[i];
				if (MatchUnitType(u, ru))
				{
					return true;
				}
			}
			return false;
		}

		public static bool MatchUnitType(Unit u, ReqUnitType ru)
		{
			if (u.def.is_heavy != ru.heavy)
			{
				return false;
			}
			if (u.def.type == ru.type)
			{
				return true;
			}
			if (u.def.type == Unit.Type.Noble)
			{
				return false;
			}
			if (u.def.secondary_type == ru.type)
			{
				return true;
			}
			return false;
		}

		public bool ValidateSkillDependencies(Army a)
		{
			if (skill_dependencies == null)
			{
				return true;
			}
			if (a.leader == null)
			{
				return false;
			}
			for (int i = 0; i < skill_dependencies.Count; i++)
			{
				if (!skill_dependencies[i].Validate(a.leader, check_parent: true))
				{
					return false;
				}
			}
			return true;
		}

		public bool ValidateCondition(Army a)
		{
			if (condition_field == null)
			{
				return true;
			}
			if (!condition_field.Bool(a))
			{
				return false;
			}
			return true;
		}

		public static int AddValidTactics(Army a, List<Def> tactics)
		{
			List<Def> defs = a.game.defs.GetDefs<Def>();
			int num = 0;
			for (int i = 0; i < defs.Count; i++)
			{
				Def def = defs[i];
				if (!tactics.Contains(def) && def.Validate(a))
				{
					tactics.Add(def);
					num++;
				}
			}
			return num;
		}

		public static List<Def> GetValidTactics(Army a)
		{
			List<Def> list = new List<Def>();
			AddValidTactics(a, list);
			return list;
		}
	}
}
