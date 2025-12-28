using System.Collections.Generic;

namespace Logic;

public class OfferSupportInWar : Offer
{
	public OfferSupportInWar(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferSupportInWar(Kingdom from, Kingdom to, War war)
		: base(from, to, war)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferSupportInWar(def, from, to);
	}

	public override bool DoesOfferHasSameArgs(Offer offer)
	{
		if (args == null || args.Count == 0 || offer.args == null || args.Count != offer.args.Count)
		{
			return false;
		}
		if (offer is DemandAttackKingdom)
		{
			War obj = args[0].obj_val as War;
			Kingdom item = offer.args[0].obj_val as Kingdom;
			Kingdom k = GetTargetObj() as Kingdom;
			List<Kingdom> enemies = obj.GetEnemies(k);
			if (enemies == null)
			{
				return false;
			}
			if (enemies.Contains(item))
			{
				return true;
			}
			return false;
		}
		if (offer is OfferSupportInWar)
		{
			War war = args[0].obj_val as War;
			War obj2 = offer.args[0].obj_val as War;
			Kingdom k2 = GetTargetObj() as Kingdom;
			List<Kingdom> enemies2 = war.GetEnemies(k2);
			List<Kingdom> enemies3 = obj2.GetEnemies(k2);
			if (enemies2 == null || enemies3 == null)
			{
				return false;
			}
			for (int i = 0; i < enemies2.Count; i++)
			{
				if (enemies3.Contains(enemies2[i]))
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public override bool HasValidParent()
	{
		if (!base.HasValidParent())
		{
			return false;
		}
		if (CountSimilarOffersInParent(sameType: false, sameSourceTarget: false, sameArgs: true) > 0)
		{
			return false;
		}
		return true;
	}

	public virtual bool ValidateWar(War war, Kingdom helper, Kingdom helped)
	{
		if (war == null)
		{
			return false;
		}
		int side;
		if (helped == war.attacker)
		{
			side = 0;
		}
		else
		{
			if (helped != war.defender)
			{
				return false;
			}
			side = 1;
		}
		if (!war.CanJoin(helper, side))
		{
			return false;
		}
		if (war.IsConcluded())
		{
			return false;
		}
		return true;
	}

	public War FindValidWar(Kingdom helper, Kingdom helped)
	{
		if (helped?.wars == null)
		{
			return null;
		}
		for (int i = 0; i < helped.wars.Count; i++)
		{
			War war = helped.wars[i];
			if (ValidateWar(war, helper, helped))
			{
				return war;
			}
		}
		return null;
	}

	public virtual bool ValidateVassalage(Kingdom helper, Kingdom helped)
	{
		return true;
	}

	public override bool IsWar(bool sender)
	{
		return sender;
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom == null || kingdom2 == null)
		{
			return "invalid_from_to";
		}
		if (!ValidateVassalage(kingdom, kingdom2))
		{
			return "invalid_vassalage";
		}
		if (parent == null && from.IsEnemy(to))
		{
			return "are_enemies";
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
		if (kingdom == null || kingdom2 == null)
		{
			return "invalid_from_to";
		}
		if (!ValidateVassalage(kingdom, kingdom2))
		{
			return "invalid_vassalage";
		}
		if (args == null || args.Count < 1)
		{
			return "_invalid_args";
		}
		if (!(args[0].obj_val is War war))
		{
			return "_invalid_args";
		}
		if (!ValidateWar(war, kingdom, kingdom2))
		{
			return "_invalid_args";
		}
		if (parent == null && from.IsEnemy(to))
		{
			return "are_enemies";
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
		for (int i = 0; i < kingdom2.wars.Count; i++)
		{
			War war = kingdom2.wars[i];
			if (ValidateWar(war, kingdom, kingdom2))
			{
				lst.Add(war);
			}
		}
		ClearDuplicatesWithParent(idx, lst);
		return lst.Count > 0;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom k = GetSourceObj() as Kingdom;
		Kingdom kingdom = GetTargetObj() as Kingdom;
		War war = args[0].obj_val as War;
		if (war.CanJoin(k, war.GetSide(kingdom)))
		{
			war.Join(k, kingdom, War.InvolvementReason.OfferedSupport);
		}
	}

	public override bool IsOfferOfSimilarType(Offer offer)
	{
		if (offer.IsOfType(typeof(OfferSupportInWar)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandSupportInWar)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(AskForProtection)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(DemandAttackKingdom)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(OfferAttackKingdom)))
		{
			return true;
		}
		if (offer.IsOfType(typeof(SummonVassal)))
		{
			return true;
		}
		return base.IsOfferOfSimilarType(offer);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		War war = ((args == null || args.Count == 0) ? null : (args[0].obj_val as War));
		if (war != null)
		{
			GetSourceObj();
			Kingdom k = GetTargetObj() as Kingdom;
			switch (key)
			{
			case "target":
				return war.GetEnemyLeader(war.GetSide(k));
			case "source_supporters":
				return new Value(war.GetDirectSupporters(k));
			case "target_supporters":
				return new Value(war.GetDirectSupporters(war.GetEnemyLeader(war.GetSide(k))));
			}
		}
		return base.GetVar(key, vars, as_value);
	}
}
