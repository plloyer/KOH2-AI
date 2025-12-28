using System.Collections.Generic;

namespace Logic;

public class InciteLoyalistRebellionAction : SpyPlot
{
	private Rebel rebel;

	public InciteLoyalistRebellionAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new InciteLoyalistRebellionAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.mission_kingdom == null)
		{
			return "not_on_mission";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		Realm realm = target as Realm;
		float num = def.field.GetFloat("max_distance");
		if (realm?.pop_majority.kingdom != own_kingdom)
		{
			return false;
		}
		if ((float)base.game.KingdomAndRealmDistance(own_kingdom.id, realm.id) > num)
		{
			return false;
		}
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		if (mission_kingdom == null)
		{
			return null;
		}
		for (int i = 0; i < mission_kingdom.realms.Count; i++)
		{
			AddTarget(ref targets, mission_kingdom.realms[i]);
		}
		return targets;
	}

	public override bool ApplyEarlyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "success")
		{
			Realm realm = base.target as Realm;
			Rebel.Def rDef = base.game.defs.Get<Rebel.Def>("Loyalists");
			RebelSpawnCondition.Def cDef = base.game.defs.Get<RebelSpawnCondition.Def>("LoyalistsSpawnCondition");
			rebel = realm?.rebellionRisk.ForceRebel(rDef, cDef);
		}
		return base.ApplyEarlyOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("rebel", rebel);
	}
}
