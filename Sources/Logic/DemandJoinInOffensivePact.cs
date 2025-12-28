using System.Collections.Generic;

namespace Logic;

public class DemandJoinInOffensivePact : Offer
{
	protected Pact.Type type;

	public DemandJoinInOffensivePact(Def def, Object from, Object to)
		: base(def, from, to)
	{
		InitType();
	}

	public DemandJoinInOffensivePact(Kingdom from, Kingdom to, Pact pact)
		: base(from, to, pact)
	{
		InitType();
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandJoinInOffensivePact(def, from, to);
	}

	protected virtual void InitType()
	{
		type = Pact.Type.Offensive;
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}

	public override bool IsWar(bool sender)
	{
		return !sender;
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		if (text == "_no_possible_args")
		{
			return "no_possible_args";
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom.IsEnemy(kingdom2))
		{
			return "in_war";
		}
		bool flag = false;
		for (int i = 0; i < kingdom2.pacts.Count; i++)
		{
			Pact pact = kingdom2.pacts[i];
			if (pact.type == type && pact.leader == kingdom2 && kingdom.HasNeighbor(pact.target) && pact.type == type && pact.CanJoin(kingdom))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return "no_possible_args";
		}
		return text;
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
		if (kingdom.IsEnemy(kingdom2))
		{
			return "in_war";
		}
		Pact arg = GetArg<Pact>(0);
		if (arg == null)
		{
			return "no_arg";
		}
		if (arg.type != type)
		{
			return "wrong_pact_type";
		}
		if (arg.leader != kingdom2)
		{
			return "pact_leader_is_not_helped_kingdom";
		}
		if (!kingdom.HasNeighbor(arg.target))
		{
			return "kelper_not_neighbor_with_pacts_target";
		}
		if (!arg.CanJoin(kingdom))
		{
			return "cant_join";
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
		for (int i = 0; i < kingdom2.pacts.Count; i++)
		{
			Pact pact = kingdom2.pacts[i];
			if (pact.type == type && pact.leader == kingdom2 && pact.CanJoin(kingdom))
			{
				lst.Add(pact);
			}
		}
		ClearDuplicatesWithParent(idx, lst);
		return lst.Count > 0;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Pact arg = GetArg<Pact>(0);
		Kingdom k = GetSourceObj() as Kingdom;
		arg.Join(k);
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(DemandJoinInOffensivePact)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(OfferJoinInOffensivePact)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}
}
