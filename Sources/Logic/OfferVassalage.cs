namespace Logic;

public class OfferVassalage : Offer
{
	public OfferVassalage(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferVassalage(Kingdom from, Kingdom to)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferVassalage(def, from, to);
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
		if (CountSimilarOffersInParent() > 0)
		{
			return false;
		}
		return true;
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		if (!(from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "not_in_war";
		}
		if ((GetSourceObj() as Kingdom).sovereignState != null)
		{
			return "_new_vassal_is_already_vassal";
		}
		if ((GetTargetObj() as Kingdom).sovereignState != null)
		{
			return "_new_sovereign_is_a_vassal";
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
		if (!(from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "not_in_war";
		}
		if ((GetSourceObj() as Kingdom).sovereignState != null)
		{
			return "_new_vassal_is_already_vassal";
		}
		if ((GetTargetObj() as Kingdom).sovereignState != null)
		{
			return "_new_sovereign_is_a_vassal";
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		kingdom2.AddVassalState(kingdom);
		kingdom2.GetCrownAuthority().AddModifier("vassalGet");
		kingdom.GetCrownAuthority().AddModifier("vassalBecome");
		War war = kingdom.FindWarWith(kingdom2);
		while (kingdom.vassalStates.Count > 0)
		{
			Kingdom kingdom3 = kingdom.vassalStates[0];
			kingdom.DelVassalState(kingdom3);
			if (!kingdom3.IsEnemy(kingdom2) || war?.GetLeader(kingdom3) == kingdom)
			{
				kingdom2.AddVassalState(kingdom3);
				kingdom3.NotifyListeners("became_vassal");
				Vars vars = new Vars();
				vars.Set("new_sovereign", kingdom2);
				vars.Set("old_sovereign", kingdom);
				kingdom3.FireEvent("changed_sovereign", vars, kingdom3.id);
			}
		}
		if (parent is PeaceOfferTribute peaceOfferTribute)
		{
			peaceOfferTribute.stance |= RelationUtils.Stance.AnyVassalage;
		}
		Vars vars2 = new Vars();
		vars2.SetVar("kingdom_a", kingdom);
		vars2.SetVar("kingdom_b", kingdom2);
		kingdom.game.BroadcastRadioEvent("KingdomVassalizedMessage", vars2);
	}
}
