using System.Collections.Generic;

namespace Logic;

public class KingdomAdvantage : Updateable, IVars, IListener
{
	public class StatModifier : Stat.Modifier
	{
		public class Def
		{
			public DT.Field field;

			public string stat_name;

			public Type type;

			public bool Load(Game game, DT.Field field)
			{
				this.field = field;
				stat_name = field.key;
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

			public float CalcValue(Kingdom kingdom)
			{
				return field?.Float(kingdom) ?? 0f;
			}
		}

		public Def def;

		public StatModifier(Def def)
		{
			this.def = def;
			type = def.type;
		}

		public override DT.Field GetField()
		{
			return def.field;
		}

		public override DT.Field GetNameField()
		{
			return def.field.parent;
		}

		public override float CalcValue(Stats stats, Stat stat)
		{
			return def.field.Value(stats.owner as IVars);
		}
	}

	public class RequirementInfo
	{
		public DT.Field field;

		public int amount;

		public string type;

		public DT.Field def;

		public string key => field.key;

		public RequirementInfo(DT.Field field)
		{
			this.field = field;
			amount = field.Int(null, 1);
		}

		public override string ToString()
		{
			return $"{type} {key}: {amount}";
		}
	}

	public class Def : Logic.Def
	{
		public string Name = "";

		public List<RequirementInfo> soft_requires;

		public List<RequirementInfo> hard_requires;

		public List<RequirementInfo> hard_requires_or;

		public List<StatModifier.Def> mods = new List<StatModifier.Def>();

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			Name = field.GetString("name");
			LoadMods(game, field);
			soft_requires = LoadRequirements(game, field.FindChild("soft_requires"));
			hard_requires = LoadRequirements(game, field.FindChild("hard_requires"));
			hard_requires_or = LoadRequirements(game, field.FindChild("hard_requires_or"));
			return true;
		}

		private void LoadMods(Game game, DT.Field f)
		{
			mods.Clear();
			List<string> list = f.Keys();
			for (int i = 0; i < list.Count; i++)
			{
				string path = list[i];
				DT.Field field = f.FindChild(path);
				if (!(field.type != "mod"))
				{
					StatModifier.Def def = new StatModifier.Def();
					if (def.Load(game, field))
					{
						mods.Add(def);
					}
				}
			}
		}

		private void ValidateMods(Game game)
		{
			Stats.Def def = game.defs.Get<Stats.Def>("KingdomStats");
			for (int i = 0; i < mods.Count; i++)
			{
				StatModifier.Def def2 = mods[i];
				if (!def.HasStat(def2.stat_name))
				{
					Game.Log(def2.field.Path(include_file: true) + ": Unknown Kingdom stat: " + base.id + "." + def2.stat_name, Game.LogType.Error);
				}
			}
		}

		public override bool Validate(Game game)
		{
			if (!IsBase())
			{
				ValidateMods(game);
				ValidateRequirements(game, soft_requires, base.id);
				ValidateRequirements(game, hard_requires);
				ValidateRequirements(game, hard_requires_or);
			}
			return true;
		}

		public static void ValidateRequirements(Game game, List<RequirementInfo> requirements, string advantage_id = null)
		{
			if (requirements == null)
			{
				return;
			}
			for (int i = 0; i < requirements.Count; i++)
			{
				RequirementInfo requirementInfo = requirements[i];
				if (!ResolveRequirement(requirementInfo, game.dt))
				{
					Game.Log(requirementInfo.field.Path(include_file: true) + ": invalid requirement '" + requirementInfo.key + "'", Game.LogType.Warning);
					requirements.RemoveAt(i);
					i--;
				}
				else if (requirementInfo.type == "Resource" && !string.IsNullOrEmpty(advantage_id))
				{
					Resource.Def def = Logic.Def.Get<Resource.Def>(requirementInfo.def);
					if (def != null)
					{
						Container.AddUnique_Class(ref def.advantages, advantage_id);
					}
				}
			}
		}

		public static List<RequirementInfo> LoadRequirements(Game game, DT.Field f)
		{
			List<DT.Field> list = f?.Children();
			if (list == null)
			{
				return null;
			}
			int count = list.Count;
			if (count <= 0)
			{
				return null;
			}
			List<RequirementInfo> list2 = new List<RequirementInfo>(count);
			for (int i = 0; i < count; i++)
			{
				DT.Field field = list[i];
				if (!string.IsNullOrEmpty(field.key))
				{
					RequirementInfo item = new RequirementInfo(field);
					list2.Add(item);
				}
			}
			return list2;
		}
	}

	private bool is_active;

	public Def def;

	public Kingdom kingdom;

	public int num_met_soft_requirements = -1;

	public List<StatModifier> mods = new List<StatModifier>();

	public Game game => kingdom.game;

	public KingdomAdvantage(Def def, Kingdom kingdom)
	{
		this.def = def;
		this.kingdom?.DelListener(this);
		this.kingdom = kingdom;
		this.kingdom?.AddListener(this);
		if (CheckRequirements())
		{
			ActivateWithoutNotification();
		}
	}

	public int NumMetSoftRequirements(bool force_recalc = true)
	{
		if (!force_recalc && num_met_soft_requirements >= 0)
		{
			return num_met_soft_requirements;
		}
		num_met_soft_requirements = 0;
		int count = def.soft_requires.Count;
		for (int i = 0; i < count; i++)
		{
			RequirementInfo req = def.soft_requires[i];
			if (CheckRequirement(req))
			{
				num_met_soft_requirements++;
			}
		}
		int num = count / 2 + count % 2;
		if (num_met_soft_requirements == num && CheckHardRequirements())
		{
			Vars vars = new Vars();
			vars.Set("def_id", def.id);
			kingdom.NotifyListeners("advantage_halfway", vars);
		}
		return num_met_soft_requirements;
	}

	public bool CheckResourceRequirement(RequirementInfo req)
	{
		if (req.type != "Resource")
		{
			return true;
		}
		if (kingdom == null)
		{
			return false;
		}
		return kingdom.GetRealmTag(req.key) >= req.amount;
	}

	public bool CheckRequirement(RequirementInfo req)
	{
		if (req.type == "Resource")
		{
			return CheckResourceRequirement(req);
		}
		if (req.type == "Religion")
		{
			if (kingdom?.religion == null)
			{
				return false;
			}
			return kingdom.religion.HasTag(req.key);
		}
		return kingdom.GetRealmTag(req.key) >= req.amount;
	}

	public static bool ResolveRequirement(RequirementInfo req, DT dt)
	{
		if (string.IsNullOrEmpty(req.key))
		{
			return false;
		}
		if (req.key == "Citadel")
		{
			req.type = "Citadel";
			return true;
		}
		req.def = dt.Find(req.key);
		if (req.def == null)
		{
			return false;
		}
		if (req.key == "Coastal")
		{
			req.type = "ProvinceFeature";
			return true;
		}
		DT.Field field = req.def.BaseRoot();
		if (field == null || field.def == null)
		{
			return false;
		}
		req.type = field.key;
		return true;
	}

	public bool CheckSoftRequirements()
	{
		return NumMetSoftRequirements() == def.soft_requires.Count;
	}

	public bool CheckHardRequirements()
	{
		if (def.hard_requires != null && def.hard_requires.Count > 0)
		{
			for (int i = 0; i < def.hard_requires.Count; i++)
			{
				RequirementInfo requirementInfo = def.hard_requires[i];
				if (requirementInfo != null && !CheckRequirement(requirementInfo))
				{
					return false;
				}
			}
		}
		if (def.hard_requires_or != null && def.hard_requires_or.Count > 0)
		{
			bool flag = false;
			for (int j = 0; j < def.hard_requires_or.Count; j++)
			{
				RequirementInfo requirementInfo2 = def.hard_requires_or[j];
				if (requirementInfo2 != null && CheckRequirement(requirementInfo2))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public bool IsActive()
	{
		return is_active;
	}

	public bool CheckRequirements()
	{
		if (CheckSoftRequirements())
		{
			return CheckHardRequirements();
		}
		return false;
	}

	public void SetActive(bool active)
	{
		if (active && !IsActive())
		{
			Activate();
		}
		if (!active && IsActive())
		{
			Deactivate();
		}
	}

	private void ActivateWithoutNotification()
	{
		is_active = true;
		AddModifiers();
	}

	public void Activate()
	{
		ActivateWithoutNotification();
		int num = kingdom.advantages.NumActiveAdvantages();
		int num2 = kingdom.advantages.MaxActiveAdvantages();
		Vars vars = new Vars();
		vars.Set("can_claim_victory", num >= num2);
		vars.Set("def_id", def.id);
		kingdom.NotifyListeners("gained_advantage", vars);
	}

	public void Deactivate()
	{
		int num = kingdom.advantages.NumActiveAdvantages();
		int num2 = kingdom.advantages.MaxActiveAdvantages();
		is_active = false;
		DelModifiers();
		Vars vars = new Vars();
		vars.Set("claim_victory_lost", num == num2);
		vars.Set("def_id", def.id);
		kingdom.NotifyListeners("lost_advantage", vars);
	}

	public void Refresh()
	{
		if (CheckRequirements())
		{
			SetActive(active: true);
		}
		else
		{
			SetActive(active: false);
		}
	}

	public void DelayedRefresh()
	{
		if (Game.isLoadingSaveGame)
		{
			Refresh();
		}
		else if (!IsRegisteredForUpdate())
		{
			game?.scheduler.RegisterForNextFrame(this);
		}
	}

	public override void OnUpdate()
	{
		Refresh();
	}

	public void AddModifiers()
	{
		for (int i = 0; i < this.def.mods.Count; i++)
		{
			StatModifier.Def def = this.def.mods[i];
			StatModifier statModifier = new StatModifier(def);
			kingdom.stats.AddModifier(def.stat_name, statModifier);
			mods.Add(statModifier);
		}
	}

	public void DelModifiers()
	{
		for (int i = 0; i < mods.Count; i++)
		{
			StatModifier statModifier = mods[i];
			statModifier.stat?.DelModifier(statModifier);
		}
		mods.Clear();
	}

	public void OnDestroy()
	{
		kingdom?.DelListener(this);
		is_active = false;
		DelModifiers();
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "req_met")
		{
			return NumMetSoftRequirements();
		}
		return Value.Unknown;
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (!(message == "religion_changed"))
		{
			if (message == "resources_changed")
			{
				DelayedRefresh();
			}
		}
		else
		{
			DelayedRefresh();
		}
	}
}
