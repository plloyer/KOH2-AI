using System;
using System.Collections.Generic;

namespace Logic;

public class Fortifications
{
	public class Def : Logic.Def
	{
		public string name;

		public int max_level = 3;

		public PerLevelValues cost;

		public PerLevelValues reqiured_citadel_level;

		public List<StatModifier.Def> realm_mods = new List<StatModifier.Def>();

		public List<StatModifier.Def> kingdom_mods = new List<StatModifier.Def>();

		public override bool Load(Game game)
		{
			name = dt_def.path;
			DT.Field field = base.field;
			if (field == null)
			{
				return false;
			}
			max_level = field.GetInt("max_level", null, max_level);
			DT.Field field2 = field.FindChild("cost");
			cost = PerLevelValues.Parse<Resource>(field2);
			DT.Field field3 = field.FindChild("reqiured_citadel_level");
			reqiured_citadel_level = PerLevelValues.Parse<int>(field3, null, no_null: true);
			LoadStats(game, field);
			return true;
		}

		private void LoadStats(Game game, DT.Field field)
		{
			if (field == null)
			{
				return;
			}
			List<string> list = field.Keys();
			if (realm_mods == null)
			{
				realm_mods = new List<StatModifier.Def>();
			}
			else
			{
				realm_mods.Clear();
			}
			if (kingdom_mods == null)
			{
				kingdom_mods = new List<StatModifier.Def>();
			}
			else
			{
				kingdom_mods.Clear();
			}
			for (int i = 0; i < list.Count; i++)
			{
				string path = list[i];
				DT.Field field2 = field.FindChild(path);
				if (field2 != null)
				{
					if (field2.type == "mod")
					{
						StatModifier.Def item = new StatModifier.Def(field2);
						realm_mods.Add(item);
					}
					else if (field2.type == "kingdom_mod")
					{
						StatModifier.Def item2 = new StatModifier.Def(field2);
						kingdom_mods.Add(item2);
					}
				}
			}
		}

		public override bool Validate(Game game)
		{
			return true;
		}
	}

	public class StatModifier : Stat.Modifier
	{
		public class Def
		{
			public DT.Field field;

			public string stat_name;

			public Type type;

			public Def(DT.Field mf)
			{
				field = mf;
				stat_name = mf.key;
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

			public float CalcValue(int level)
			{
				if (level < 0)
				{
					return 0f;
				}
				int val = field.NumValues();
				int idx = Math.Min(level, val);
				return field.Float(idx);
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

		public override bool IsConst()
		{
			return true;
		}
	}

	public enum UpgradeState
	{
		Started,
		Finished,
		Canceled
	}

	public Def def;

	public Castle castle;

	public int level;

	public List<StatModifier> realm_mods = new List<StatModifier>();

	public List<StatModifier> kingdom_mods = new List<StatModifier>();

	private UpgradeState state = UpgradeState.Finished;

	public float production_cost = -1f;

	public float current_production_amount;

	public float current_progress = -1f;

	public int max_level
	{
		get
		{
			if (def == null)
			{
				return 0;
			}
			return def.max_level;
		}
	}

	public Fortifications(Settlement s, Def def)
	{
		castle = s as Castle;
		this.def = def;
		CreateMods();
		SetLevel(0);
	}

	public void SetLevel(int lvl, bool send_state = true)
	{
		DelMods();
		level = lvl;
		AddMods();
		castle.NotifyListeners("fortification_changed");
		if (send_state && castle.IsAuthority())
		{
			castle.SendState<Castle.CastleFortificationsState>();
		}
	}

	public UpgradeState GetUpgradeState()
	{
		return state;
	}

	public void SetUpgradeState(UpgradeState new_state)
	{
		state = new_state;
	}

	private void CreateMods()
	{
		realm_mods.Clear();
		kingdom_mods.Clear();
		for (int i = 0; i < def.realm_mods.Count; i++)
		{
			StatModifier item = new StatModifier(def.realm_mods[i]);
			realm_mods.Add(item);
		}
		for (int j = 0; j < def.kingdom_mods.Count; j++)
		{
			StatModifier item2 = new StatModifier(def.kingdom_mods[j]);
			kingdom_mods.Add(item2);
		}
	}

	private void DelMods()
	{
		if (castle == null)
		{
			return;
		}
		for (int i = 0; i < realm_mods.Count; i++)
		{
			StatModifier statModifier = realm_mods[i];
			if (statModifier.stat != null)
			{
				statModifier.stat.DelModifier(statModifier);
			}
		}
		for (int j = 0; j < kingdom_mods.Count; j++)
		{
			StatModifier statModifier2 = kingdom_mods[j];
			if (statModifier2.stat != null)
			{
				statModifier2.stat.DelModifier(statModifier2);
			}
		}
	}

	private void AddMods()
	{
		if (castle != null)
		{
			Stats stats = castle.GetRealm().stats;
			Stats stats2 = castle.GetKingdom().stats;
			for (int i = 0; i < realm_mods.Count; i++)
			{
				StatModifier statModifier = realm_mods[i];
				statModifier.value = statModifier.def.CalcValue(level);
				stats.AddModifier(statModifier.def.stat_name, statModifier);
			}
			for (int j = 0; j < kingdom_mods.Count; j++)
			{
				StatModifier statModifier2 = kingdom_mods[j];
				statModifier2.value = statModifier2.def.CalcValue(level);
				stats2.AddModifier(statModifier2.def.stat_name, statModifier2);
			}
		}
	}

	public float GetOnUpgradeSiegeDefenseBonus()
	{
		if (level == max_level)
		{
			return 0f;
		}
		int num = level + 1;
		for (int i = 0; i < realm_mods.Count; i++)
		{
			StatModifier statModifier = realm_mods[i];
			if (statModifier.def.stat_name == "rs_siege_defense")
			{
				return statModifier.def.CalcValue(num) - statModifier.def.CalcValue(level);
			}
		}
		return 0f;
	}

	public string GetState()
	{
		if (castle.GetRealm().IsDisorder())
		{
			return "disorder";
		}
		if (IsUpgrading())
		{
			return "upgrading";
		}
		if (IsRepairing())
		{
			return "repairing";
		}
		return "normal";
	}

	public Resource GetUpgradeCost(int level)
	{
		if (level > max_level)
		{
			return null;
		}
		return def.cost.GetResources(level + 1);
	}

	public bool CheckReqierments(Castle c, int level)
	{
		if (c == null)
		{
			return false;
		}
		if (level < 0)
		{
			return false;
		}
		if (level > max_level)
		{
			return false;
		}
		int num = def.reqiured_citadel_level.GetInt(level);
		return c.GetCitadelLevel() >= num;
	}

	public bool IsRepairing()
	{
		if (castle == null)
		{
			return false;
		}
		return (castle.keep_effects.siege_defense_condition?.Get() ?? 100f) / 100f < 0.99f;
	}

	public bool BoostedRepairs()
	{
		return castle?.keep_effects?.active_defence_recovery_boost ?? false;
	}

	public bool CanBoostRepairs()
	{
		if (castle.battle != null)
		{
			return false;
		}
		if (castle.GetRealm().IsDisorder())
		{
			return false;
		}
		return !BoostedRepairs();
	}

	public void Update()
	{
		if (!castle.GetRealm().game.isInVideoMode && castle.IsAuthority() && IsUpgrading())
		{
			current_production_amount += castle.GetRealm().income[ResourceType.Hammers] * 1f;
			current_progress = current_production_amount / production_cost;
			if (current_progress >= 1f)
			{
				current_progress = 1f;
				CompleteUpgrade();
			}
			else
			{
				castle.SendState<Castle.ForitificationUpgradeState>();
			}
		}
	}

	public void BeginUpgrade(bool send_state = true)
	{
		if (!IsUpgrading())
		{
			Resource upgradeCost = GetUpgradeCost(level + 1);
			if (upgradeCost != null)
			{
				production_cost = upgradeCost[ResourceType.Hammers];
			}
			else
			{
				production_cost = 0f;
			}
			current_progress = 0f;
			current_production_amount = 0f;
			state = UpgradeState.Started;
			castle.NotifyListeners("fortification_upgarde_started");
			if (send_state)
			{
				castle.SendState<Castle.ForitificationUpgradeState>();
			}
		}
	}

	private void CompleteUpgrade()
	{
		if (castle.IsAuthority())
		{
			SetLevel(level + 1);
			Reset();
			state = UpgradeState.Finished;
			castle.NotifyListeners("fortification_upgrade_complete");
			castle.SendState<Castle.ForitificationUpgradeState>();
			castle.CacheTempDefenderStats();
		}
	}

	public void CancelUpgrade(bool send_state = true)
	{
		if (!(!castle.IsAuthority() && send_state) && IsUpgrading())
		{
			Reset();
			state = UpgradeState.Canceled;
			castle.NotifyListeners("fortification_changed");
			if (send_state)
			{
				castle.SendState<Castle.ForitificationUpgradeState>();
			}
		}
	}

	public bool IsUpgrading()
	{
		return state == UpgradeState.Started;
	}

	public float GetUpgradeProgress()
	{
		return current_progress;
	}

	public void Reset()
	{
		production_cost = -1f;
		current_production_amount = -1f;
		current_progress = -1f;
	}
}
