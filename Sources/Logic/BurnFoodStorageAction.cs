using System.Collections.Generic;

namespace Logic;

public class BurnFoodStorageAction : SpyPlot
{
	public BurnFoodStorageAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new BurnFoodStorageAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.mission_kingdom == null)
		{
			return "not_on_mission";
		}
		if (!base.own_character.mission_kingdom.IsEnemy(own_kingdom))
		{
			return "not_enemies";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		Realm realm = target as Realm;
		if (realm?.castle == null)
		{
			return false;
		}
		if (realm.castle.battle?.settlement_food_copy != null && realm.castle.battle.settlement_food_copy.Get() <= 0f)
		{
			return false;
		}
		bool flag = false;
		if (realm.castle.battle?.attackers != null)
		{
			for (int i = 0; i < realm.castle.battle.attackers.Count; i++)
			{
				if (own_kingdom.IsOwnStance(realm.castle.battle.attackers[i]))
				{
					flag = true;
					break;
				}
			}
		}
		if (!own_kingdom.externalBorderRealms.Contains(realm) && !flag)
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

	public override void Run()
	{
		Realm obj = base.target as Realm;
		obj.castle.SetFood(0f, clamp: false);
		obj.castle.battle?.SetFood(0f);
		base.Run();
	}
}
