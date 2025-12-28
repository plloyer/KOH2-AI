using System.Collections.Generic;

namespace Logic;

public class SupplyArmyAction : MerchantOpportunity
{
	public SupplyArmyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new SupplyArmyAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.mission_kingdom == null)
		{
			return "not_in_a_mission_kingdom";
		}
		return base.Validate(quick_out);
	}

	public override bool NeedsTarget()
	{
		return true;
	}

	public override bool ValidateTarget(Object target)
	{
		Army army = (target as Character)?.GetArmy();
		if (army == null)
		{
			return false;
		}
		if (!base.own_character.IsOwnStance(army))
		{
			return false;
		}
		if (army.supplies.Get() > army.supplies.GetMax() * def.field.GetFloat("food_treshold") / 100f)
		{
			return false;
		}
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		for (int i = 0; i < mission_kingdom.realms.Count; i++)
		{
			Realm realm = mission_kingdom.realms[i];
			for (int j = 0; j < realm.armies.Count; j++)
			{
				Army army = realm.armies[j];
				AddTarget(ref targets, army.leader);
			}
		}
		for (int k = 0; k < mission_kingdom.externalBorderRealms.Count; k++)
		{
			Realm realm2 = mission_kingdom.externalBorderRealms[k];
			for (int l = 0; l < realm2.armies.Count; l++)
			{
				Army army2 = realm2.armies[l];
				AddTarget(ref targets, army2.leader);
			}
		}
		return targets;
	}

	public override void Run()
	{
		Army army = (base.target as Character).GetArmy();
		army?.SetSupplies(army.supplies.GetMax());
		base.Run();
	}

	public override Resource GetCost(Object target, IVars vars = null)
	{
		if (def.cost == null)
		{
			return null;
		}
		if (vars == null)
		{
			if (target == null)
			{
				vars = this;
			}
			else
			{
				Vars vars2 = new Vars(this);
				vars2.Set("target", target);
				vars = vars2;
			}
		}
		return Resource.Parse(def.cost, vars);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Army army = (base.target as Character)?.GetArmy();
		if (army != null && key == "missing_food")
		{
			return army.supplies.GetMax() - army.supplies.Get();
		}
		return base.GetVar(key, vars, as_value);
	}
}
