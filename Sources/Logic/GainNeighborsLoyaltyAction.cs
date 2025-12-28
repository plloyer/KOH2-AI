using System.Collections.Generic;

namespace Logic;

public class GainNeighborsLoyaltyAction : CharacterOpportunity
{
	public GainNeighborsLoyaltyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new GainNeighborsLoyaltyAction(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Realm realm))
		{
			return false;
		}
		if (realm.pop_majority.kingdom == own_kingdom)
		{
			return false;
		}
		if (realm.pop_majority.strength >= def.field.GetFloat("pop_majority_treshold"))
		{
			return false;
		}
		if (realm.IsOccupied())
		{
			return false;
		}
		if (!own_kingdom.externalBorderRealms.Contains(realm))
		{
			return false;
		}
		int num = def.field.GetInt("min_our_neighboring_provinces");
		if (num > 0)
		{
			int num2 = 0;
			for (int i = 0; i < realm.logicNeighborsRestricted.Count; i++)
			{
				Realm realm2 = realm.logicNeighborsRestricted[i];
				if (realm2.kingdom_id == own_kingdom.id && realm2.pop_majority.kingdom == own_kingdom && !realm2.IsOccupied() && !realm2.IsDisorder() && !IsTargetUsedByAnotherCourtMember(realm2))
				{
					num2++;
					if (num2 >= num)
					{
						break;
					}
				}
			}
			if (num2 < num)
			{
				return false;
			}
		}
		return base.ValidateTarget(target);
	}

	private bool IsTargetUsedByAnotherCourtMember(Realm realm)
	{
		if (realm == null)
		{
			return false;
		}
		if (own_kingdom?.court == null)
		{
			return false;
		}
		for (int i = 0; i < own_kingdom.court.Count; i++)
		{
			Character character = own_kingdom.court[i];
			if (character != null && character.IsCleric())
			{
				Action action = character.actions.FindActive(def);
				if (action != null && action.is_active && action.target == realm)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		for (int i = 0; i < own_kingdom.externalBorderRealms.Count; i++)
		{
			Realm realm = own_kingdom.externalBorderRealms[i];
			if (!IsTargetUsedByAnotherCourtMember(realm))
			{
				AddTarget(ref targets, realm);
			}
		}
		return targets;
	}

	public override void Run()
	{
		Realm realm = base.target as Realm;
		float num = def.field.GetFloat("majority_after_convertion");
		realm.AdjustPopMajority(100f - num - realm.pop_majority.strength, own_kingdom);
		base.Run();
	}
}
