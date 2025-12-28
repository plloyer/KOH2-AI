namespace Logic;

public class OfferCourtCleric : Offer
{
	public OfferCourtCleric(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public OfferCourtCleric(Kingdom from, Kingdom to, Character cleric)
		: base(from, to, cleric)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new OfferCourtCleric(def, from, to);
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
		if (kingdom != base.game.religions.catholic.hq_kingdom)
		{
			return "not_papacy";
		}
		if (!kingdom2.is_catholic)
		{
			return "not_catholic";
		}
		if (kingdom2.excommunicated)
		{
			return "excommunicated";
		}
		if (kingdom2.IsEnemy(kingdom))
		{
			return "in_war";
		}
		if (kingdom2.HasPope())
		{
			return "holds_pope";
		}
		bool flag = true;
		for (int i = 0; i < kingdom2.court.Count; i++)
		{
			Character courtOrSpecialCourtMember = kingdom2.GetCourtOrSpecialCourtMember(i);
			if (courtOrSpecialCourtMember == null)
			{
				flag = false;
			}
			else if (courtOrSpecialCourtMember.GetKingdom() == kingdom2 && courtOrSpecialCourtMember.IsCleric())
			{
				return "reciever_has_cleric";
			}
		}
		if (flag)
		{
			return "no_place_in_court";
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
		if (kingdom != base.game.religions.catholic.hq_kingdom)
		{
			return "not_papacy";
		}
		if (!kingdom2.is_catholic)
		{
			return "not_catholic";
		}
		if (kingdom2.excommunicated)
		{
			return "excommunicated";
		}
		if (kingdom2.IsEnemy(kingdom))
		{
			return "in_war";
		}
		if (kingdom2.HasPope())
		{
			return "holds_pope";
		}
		bool flag = true;
		for (int i = 0; i < kingdom2.court.Count; i++)
		{
			Character courtOrSpecialCourtMember = kingdom2.GetCourtOrSpecialCourtMember(i);
			if (courtOrSpecialCourtMember == null)
			{
				flag = false;
			}
			else if (courtOrSpecialCourtMember.GetKingdom() == kingdom2 && courtOrSpecialCourtMember.IsCleric())
			{
				return "reciever_has_cleric";
			}
		}
		if (flag)
		{
			return "no_place_in_court";
		}
		return text;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		GetSourceObj();
		Kingdom kingdom = GetTargetObj() as Kingdom;
		kingdom.AddCourtMember(CharacterFactory.CreateCharacter(base.game, kingdom.id, "Cleric"));
	}
}
