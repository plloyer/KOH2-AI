namespace Logic;

public class DemandSovereignDefendOffer : Offer
{
	public DemandSovereignDefendOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public DemandSovereignDefendOffer(Kingdom from, Kingdom to, Kingdom target, War war)
		: base(from, to)
	{
		SetArg(0, target);
		SetArg(1, war);
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new DemandSovereignDefendOffer(def, from, to);
	}

	public static DemandSovereignDefendOffer Get(Game game, Object from, Object to, Kingdom target, War war)
	{
		Offers offers = Offers.Get(to, create: false);
		if (offers == null)
		{
			return null;
		}
		for (int i = 0; i < offers.incoming.Count; i++)
		{
			if (offers.incoming[i] is DemandSovereignDefendOffer demandSovereignDefendOffer)
			{
				Kingdom arg = demandSovereignDefendOffer.GetArg<Kingdom>(0);
				War arg2 = demandSovereignDefendOffer.GetArg<War>(1);
				if (demandSovereignDefendOffer.from == from && demandSovereignDefendOffer.to == to && arg == target && arg2 == war)
				{
					return demandSovereignDefendOffer;
				}
			}
		}
		return null;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "target":
			return args[0];
		case "our_kingdom":
		case "own_kingdom":
		case "src_kingdom":
			return from?.GetKingdom();
		case "their_kingdom":
		case "target_kingdom":
		case "tgt_kingdom":
			return to?.GetKingdom();
		default:
			return base.GetVar(key, vars, as_value);
		}
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
		return true;
	}

	public override string ValidateWithoutArgs()
	{
		string text = Crusade.ValidateNew((to as Kingdom).game, null);
		if (text != "ok")
		{
			return text;
		}
		return base.ValidateWithoutArgs();
	}

	public override string Validate()
	{
		Kingdom kingdom = from as Kingdom;
		Kingdom kingdom2 = to as Kingdom;
		Kingdom arg = GetArg<Kingdom>(0);
		War arg2 = GetArg<War>(1);
		if (arg == null || arg2 == null)
		{
			return "fail";
		}
		if (arg2.GetSide(kingdom) != 1)
		{
			return "fail";
		}
		if (!arg.IsEnemy(from as Kingdom))
		{
			return "fail";
		}
		if (kingdom.sovereignState != kingdom2)
		{
			return "fail";
		}
		return base.Validate();
	}

	public override void OnAnswer(string answer)
	{
		base.OnAnswer(answer);
		Kingdom kingdom = from as Kingdom;
		Kingdom kingdom2 = to as Kingdom;
		War arg = GetArg<War>(1);
		if (!(answer == "decline"))
		{
			return;
		}
		kingdom2.GetCrownAuthority().AddModifier("DeclinedVassalDefend");
		kingdom2.NotifyListeners("reject_vassal_defence_opinion");
		kingdom.AddRelationModifier(kingdom2, "rel_refused_vassal_defence", null);
		for (int i = 0; i < kingdom2.vassalStates.Count; i++)
		{
			Kingdom kingdom3 = kingdom2.vassalStates[i];
			if (kingdom3 != kingdom)
			{
				kingdom3.AddRelationModifier(kingdom2, "rel_refused_other_vassal_defence", null);
			}
		}
		arg?.refusedToParticipate.Add(kingdom2);
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = from as Kingdom;
		Kingdom kingdom2 = to as Kingdom;
		War arg = GetArg<War>(1);
		int side = 1;
		if (arg.CanJoin(kingdom2, side))
		{
			arg.Join(kingdom2, side, War.InvolvementReason.OfferedSupport, apply_consequences: false);
		}
		for (int i = 0; i < kingdom2.vassalStates.Count; i++)
		{
			Kingdom kingdom3 = kingdom2.vassalStates[i];
			if (kingdom3 != kingdom && kingdom3.vassalage != null && kingdom3.vassalage.def.type == Vassalage.Type.March && arg.CanJoin(kingdom3, side))
			{
				arg.Join(kingdom3, side, War.InvolvementReason.VassalSummoned, apply_consequences: false);
			}
		}
	}
}
