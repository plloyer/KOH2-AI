using System.Collections.Generic;

namespace Logic;

public class RebelSpawnCondition
{
	public class Def : Logic.Def
	{
		public string name;

		public float refresh_period = 60f;

		public bool periodic;

		public bool hidden = true;

		public DT.Field spawn_chance;

		public float spawn_base;

		public float min_chance;

		public float max_chance = 100f;

		public bool enable_periodic_spawn = true;

		public string reason;

		public List<Rebel.Def> rebel_types;

		public List<RebelAgenda.Def> rebel_agendas;

		public float refresh_interval_duration_modifier_min = 0.5f;

		public float refresh_interval_duration_modifier_max = 1.5f;

		public override bool Load(Game game)
		{
			name = dt_def.path;
			DT.Field field = dt_def.field;
			refresh_period = field.GetFloat("refresh_period", null, refresh_period);
			periodic = field.GetBool("periodic", null, periodic);
			hidden = field.GetBool("hidden", null, hidden);
			spawn_base = field.GetFloat("spawn_base", null, spawn_base);
			max_chance = field.GetFloat("max_chance", null, max_chance);
			min_chance = field.GetFloat("min_chance", null, min_chance);
			spawn_chance = field.FindChild("spawn_chance");
			enable_periodic_spawn = field.GetBool("enable_periodic_spawn", null, enable_periodic_spawn);
			reason = field.GetString("reason");
			DT.Field f = field.FindChild("rebel");
			rebel_types = LoadRequirements(game, f);
			DT.Field f2 = field.FindChild("agenda");
			rebel_agendas = LoadAgenda(game, f2);
			if (rebel_types == null && !IsBase())
			{
				Game.Log(name + " is missing Rebels definitions", Game.LogType.Warning);
			}
			return true;
		}

		private List<Rebel.Def> LoadRequirements(Game game, DT.Field f)
		{
			if (f == null)
			{
				return null;
			}
			int num = f.NumValues();
			if (num <= 0)
			{
				return null;
			}
			List<Rebel.Def> list = new List<Rebel.Def>(num);
			for (int i = 0; i < num; i++)
			{
				DT.Field field = f.Value(i, null, calc_expression: false, as_value: false).Get<DT.Field>();
				if (field == null)
				{
					Game.Log(f.Path(include_file: true) + ": Invalid requirement #" + i, Game.LogType.Error);
					continue;
				}
				Rebel.Def def = game.defs.Get<Rebel.Def>(field.key);
				if (def == null || def.IsBase())
				{
					Game.Log(f.Path(include_file: true) + ": Invalid requirement '" + field.key + "'", Game.LogType.Error);
				}
				else
				{
					list.Add(def);
				}
			}
			return list;
		}

		private List<RebelAgenda.Def> LoadAgenda(Game game, DT.Field f)
		{
			if (f == null)
			{
				return null;
			}
			int num = f.NumValues();
			if (num <= 0)
			{
				return null;
			}
			List<RebelAgenda.Def> list = new List<RebelAgenda.Def>(num);
			for (int i = 0; i < num; i++)
			{
				DT.Field field = f.Value(i, null, calc_expression: false, as_value: false).Get<DT.Field>();
				if (field == null)
				{
					Game.Log(f.Path(include_file: true) + ": Invalid requirement #" + i, Game.LogType.Error);
					continue;
				}
				RebelAgenda.Def def = game.defs.Get<RebelAgenda.Def>(field.key);
				if (def == null || def.IsBase())
				{
					Game.Log(f.Path(include_file: true) + ": Invalid requirement '" + field.key + "'", Game.LogType.Error);
				}
				else
				{
					list.Add(def);
				}
			}
			return list;
		}

		public float GetChance(Realm realm)
		{
			float num = spawn_chance.Value(realm);
			if (!(num < min_chance))
			{
				if (!(num > max_chance))
				{
					return num;
				}
				return max_chance;
			}
			return min_chance;
		}
	}
}
