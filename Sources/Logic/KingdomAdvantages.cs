using System.Collections.Generic;

namespace Logic;

public class KingdomAdvantages : Component, IVars
{
	public List<KingdomAdvantage> advantages = new List<KingdomAdvantage>();

	public Kingdom kingdom => obj as Kingdom;

	public KingdomAdvantages(Kingdom kingdom)
		: base(kingdom)
	{
		Defs.Registry registry = base.game.defs.Get(typeof(KingdomAdvantage.Def));
		if (registry == null)
		{
			return;
		}
		foreach (KeyValuePair<string, Def> def in registry.defs)
		{
			KingdomAdvantage item = new KingdomAdvantage(def.Value as KingdomAdvantage.Def, kingdom);
			advantages.Add(item);
		}
	}

	public KingdomAdvantage Find(KingdomAdvantage.Def def)
	{
		for (int i = 0; i < advantages.Count; i++)
		{
			KingdomAdvantage kingdomAdvantage = advantages[i];
			if (kingdomAdvantage.def == def)
			{
				return kingdomAdvantage;
			}
		}
		return null;
	}

	public KingdomAdvantage FindByName(string name)
	{
		for (int i = 0; i < advantages.Count; i++)
		{
			KingdomAdvantage kingdomAdvantage = advantages[i];
			if (kingdomAdvantage.def.Name == name)
			{
				return kingdomAdvantage;
			}
		}
		return null;
	}

	public KingdomAdvantage FindById(string def_id)
	{
		for (int i = 0; i < advantages.Count; i++)
		{
			KingdomAdvantage kingdomAdvantage = advantages[i];
			if (kingdomAdvantage.def.id == def_id)
			{
				return kingdomAdvantage;
			}
		}
		return null;
	}

	public void Refresh()
	{
		for (int i = 0; i < advantages.Count; i++)
		{
			advantages[i].DelayedRefresh();
		}
	}

	public override void OnDestroy()
	{
		for (int i = 0; i < advantages.Count; i++)
		{
			advantages[i].OnDestroy();
		}
		base.OnDestroy();
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		KingdomAdvantage kingdomAdvantage = FindByName(key);
		if (kingdomAdvantage != null)
		{
			return kingdomAdvantage;
		}
		return Value.Unknown;
	}

	public bool CanClaimVictory()
	{
		return ValidateClaimVictory() == "ok";
	}

	public int MaxActiveAdvantages()
	{
		int num = 0;
		for (int i = 0; i < advantages.Count; i++)
		{
			KingdomAdvantage kingdomAdvantage = advantages[i];
			if (kingdomAdvantage != null && kingdomAdvantage.CheckHardRequirements())
			{
				num++;
			}
		}
		return num;
	}

	public int NumActiveAdvantages()
	{
		int num = 0;
		for (int i = 0; i < advantages.Count; i++)
		{
			KingdomAdvantage kingdomAdvantage = advantages[i];
			if (kingdomAdvantage != null && kingdomAdvantage.IsActive())
			{
				num++;
			}
		}
		return num;
	}

	public string ValidateClaimVictory()
	{
		if (advantages == null)
		{
			return "ok";
		}
		for (int i = 0; i < advantages.Count; i++)
		{
			KingdomAdvantage kingdomAdvantage = advantages[i];
			if (kingdomAdvantage != null && kingdomAdvantage.CheckHardRequirements() && !kingdomAdvantage.CheckSoftRequirements())
			{
				return "_not_all_advantages_met";
			}
		}
		return "ok";
	}

	public void ClaimVictory()
	{
		base.game.ForceEndGame(kingdom, "KingdomAdvantages");
	}
}
