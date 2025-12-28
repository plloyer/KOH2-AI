using System;
using System.Collections.Generic;

namespace Logic;

public class BattleBonus
{
	public class StatModifier : Stat.Modifier
	{
		public class Def
		{
			public DT.Field field;

			public string stat_name;

			public float value;

			public Type type = Type.Perc;

			public Unit.Def unit_def;

			public Unit.Type unit_type;

			public int side = -1;

			public bool Load(Game game, DT.Field field)
			{
				this.field = field;
				stat_name = field.key;
				value = field.Float();
				if (value == 0f)
				{
					Game.Log(field.Path(include_file: true) + ": Invalid battle bonus value '" + field.ValueStr() + "', must be a non-zero number", Game.LogType.Error);
				}
				if (field.FindChild("perc") != null)
				{
					type = Type.Perc;
				}
				else if (field.FindChild("base") != null)
				{
					type = Type.Base;
				}
				else if (field.FindChild("unscaled") != null)
				{
					type = Type.Unscaled;
				}
				return true;
			}

			public bool Validate(int side, Unit.Def unit_def, string key)
			{
				if (this.field.key != key)
				{
					return false;
				}
				if (unit_type != unit_def.type && unit_type != unit_def.secondary_type && unit_type != Unit.Type.COUNT)
				{
					return false;
				}
				if (this.unit_def != null)
				{
					DT.Field field = unit_def?.field;
					bool flag = false;
					do
					{
						if (field == this.unit_def?.field)
						{
							flag = true;
							break;
						}
						field = field.based_on;
					}
					while (field != null);
					if (!flag)
					{
						return false;
					}
				}
				if (this.side != -1 && this.side != side)
				{
					return false;
				}
				return true;
			}
		}
	}

	public class Def : Logic.Def
	{
		public int min_tiles;

		public List<StatModifier.Def> mods;

		public DT.Field spawn_chance;

		private void LoadFromField(Game game, DT.Field f, Unit.Def def = null, int side = -1, Unit.Type type = Unit.Type.COUNT)
		{
			Unit.Type result;
			if (f.key == "Defenders")
			{
				side = 1;
			}
			else if (f.key == "Attackers")
			{
				side = 0;
			}
			else if (!Enum.TryParse<Unit.Type>(f.key, ignoreCase: false, out result))
			{
				def = game.defs.Find<Unit.Def>(f.key);
			}
			else
			{
				type = result;
			}
			if (f.children == null || f.children.Count == 0)
			{
				return;
			}
			for (int i = 0; i < f.children.Count; i++)
			{
				DT.Field field = f.children[i];
				if (field != null)
				{
					if (field.type == "mod")
					{
						StatModifier.Def def2 = new StatModifier.Def();
						def2.Load(game, field);
						def2.unit_type = type;
						def2.unit_def = def;
						def2.side = side;
						mods.Add(def2);
					}
					else
					{
						LoadFromField(game, field, def, side, type);
					}
				}
			}
		}

		public override bool Load(Game game)
		{
			mods = new List<StatModifier.Def>();
			DT.Field field = dt_def.field;
			min_tiles = field.GetInt("min_tiles");
			spawn_chance = field.FindChild("spawn_chance");
			LoadFromField(game, field);
			return true;
		}

		public bool CanSpawn(Battle battle)
		{
			if (spawn_chance == null)
			{
				return false;
			}
			return battle.game.Random(0, 100) < (int)spawn_chance.Value(battle);
		}
	}

	public Def def;
}
