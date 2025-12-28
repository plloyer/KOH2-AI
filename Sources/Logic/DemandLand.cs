using System.Collections.Generic;

namespace Logic;

public class DemandLand : Offer
{
	public DemandLand(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandLand(Kingdom from, Kingdom to, Realm realm)
		: base(from, to, realm)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandLand(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}

	public override bool HasValidParent()
	{
		if (!base.HasValidParent())
		{
			return false;
		}
		if (parent == null && (from as Kingdom).IsEnemy(to as Kingdom))
		{
			return false;
		}
		if ((GetSourceObj() as Kingdom).realms.Count - CountSimilarOffersInParent(sameType: false, sameSourceTarget: true) < 2)
		{
			return false;
		}
		return true;
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		Realm arg = GetArg<Realm>(0);
		if (arg == null || arg.castle == null)
		{
			return "realm_has_no_castle";
		}
		if (arg.kingdom_id != kingdom.id)
		{
			return "realm_now_owned_by_giving";
		}
		if (arg.IsOccupied())
		{
			return "realm_occupied";
		}
		if (arg == kingdom.religion.hq_realm && (kingdom.IsPapacy() || kingdom.is_ecumenical_patriarchate))
		{
			return "cant_give_up_hq_realm";
		}
		if (arg.castle.battle != null && arg.castle.battle.attacker_kingdom != kingdom2 && arg.castle.battle.defender_kingdom != kingdom2)
		{
			return "realm_is_in_battle";
		}
		if (kingdom.IsEnemy(kingdom2))
		{
			if (arg.pop_majority.kingdom != kingdom2 && !kingdom2.externalBorderRealms.Contains(arg) && (arg.castle.battle == null || arg.castle.battle.attacker_kingdom != kingdom2))
			{
				return "no_pop_majority_or_border_or_battle";
			}
		}
		else if (kingdom2.sovereignState != kingdom && arg.culture != kingdom2.culture)
		{
			return "pop_majoirt_not_from_recieving";
		}
		return text;
	}

	public override bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		base.GetPossibleArgValues(idx, lst);
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom == null)
		{
			return false;
		}
		if (kingdom.realms.Count < 2)
		{
			return false;
		}
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			Realm realm = kingdom.realms[i];
			if (realm.castle == null || realm.IsOccupied() || (realm.castle.battle != null && realm.castle.battle.attacker_kingdom != kingdom2 && realm.castle.battle.defender_kingdom != kingdom2) || (realm == kingdom.religion.hq_realm && (kingdom.IsPapacy() || kingdom.is_ecumenical_patriarchate)))
			{
				continue;
			}
			if (kingdom.IsEnemy(kingdom2))
			{
				if (realm.pop_majority.kingdom != kingdom2 && !kingdom2.externalBorderRealms.Contains(realm) && (realm.castle.battle == null || realm.castle.battle.attacker_kingdom != kingdom2))
				{
					continue;
				}
			}
			else if (kingdom2.sovereignState != kingdom && realm.culture != kingdom2.culture)
			{
				continue;
			}
			lst.Add(realm);
		}
		ClearDuplicatesWithParent(idx, lst);
		return lst.Count > 0;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetTargetObj() as Kingdom;
		GetArg<Realm>(0).SetKingdom(kingdom.id, ignore_victory: false, check_cancel_battle: true, via_diplomacy: true);
	}

	public override bool IsValidForAI()
	{
		if (AI && parent == null)
		{
			return false;
		}
		return base.IsValidForAI();
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(OfferLand)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandLand)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}

	public override float Eval(string threshold_name, bool reverse_kingdoms = false)
	{
		Realm arg = GetArg<Realm>(0);
		Kingdom forKingdom = GetSourceObj() as Kingdom;
		if (reverse_kingdoms)
		{
			if (threshold_name == "Accept")
			{
				forKingdom = GetTargetObj() as Kingdom;
			}
		}
		else if (threshold_name == "Propose")
		{
			forKingdom = GetTargetObj() as Kingdom;
		}
		ProsAndCons prosAndCons = ProsAndCons.Get(this, threshold_name, reverse_kingdoms);
		if (prosAndCons == null)
		{
			return 0f;
		}
		return arg.CalcCost(forKingdom) * prosAndCons.def.cost.Float(prosAndCons, 100000f);
	}
}
