using System.Collections.Generic;

namespace Logic;

public class PrincessClaimInheritanceOffer : Offer
{
	public PrincessClaimInheritanceOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public PrincessClaimInheritanceOffer(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new PrincessClaimInheritanceOffer(def, from, to);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		GetTargetObj();
		if (kingdom.GetComponent<Inheritance>().currentPrincess != GetArg<Character>(0))
		{
			return "current_princess_is_not_the_inheritor";
		}
		List<Realm> arg = GetArg<List<Realm>>(1);
		if (arg == null)
		{
			return "no_realms";
		}
		for (int i = 0; i < arg.Count; i++)
		{
			if (!kingdom.IsOwnStance(arg[i]) || arg[i].IsOccupied())
			{
				return "_invalid_realm";
			}
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		List<Realm> arg = GetArg<List<Realm>>(1);
		if (arg != null)
		{
			using (new Kingdom.CacheRBS("ClaimInheritanceOffer"))
			{
				for (int i = 0; i < arg.Count; i++)
				{
					if (kingdom.IsOwnStance(arg[i]))
					{
						arg[i].SetKingdom(kingdom2.id, ignore_victory: false, check_cancel_battle: true, !arg[i].IsDisorder());
					}
				}
			}
		}
		kingdom.GetComponent<Inheritance>().HandleNextPrincess();
	}

	public override void OnDecline()
	{
		base.OnDecline();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		kingdom2.StartWarWith(kingdom, War.InvolvementReason.InheritanceClaimDeclined, "WarDeclaredMessage")?.SetType("TeritorialClaimsWar");
		kingdom.GetComponent<Inheritance>().HandleNextPrincess();
		kingdom.FireEvent("inheritance_war_declared", kingdom2);
		kingdom2.FireEvent("declared_inheritance_war", kingdom);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "can_declare_war")
		{
			return War.CanStart(GetSourceObj() as Kingdom, GetTargetObj() as Kingdom);
		}
		return base.GetVar(key, vars, as_value);
	}
}
