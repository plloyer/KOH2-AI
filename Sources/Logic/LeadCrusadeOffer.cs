namespace Logic;

public class LeadCrusadeOffer : Offer
{
	public LeadCrusadeOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public LeadCrusadeOffer(Kingdom to, Kingdom target, Kingdom helping_kingdom, string reason, Character leader)
		: base(to.game.religions.catholic.hq_kingdom, to)
	{
		SetArg(0, target);
		SetArg(1, helping_kingdom);
		SetArg(2, reason);
		SetArg(3, leader);
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new LeadCrusadeOffer(def, from, to);
	}

	public static LeadCrusadeOffer Get(Game game)
	{
		Offers offers = Offers.Get(game?.religions?.catholic?.hq_kingdom, create: false);
		if (offers?.outgoing == null)
		{
			return null;
		}
		for (int i = 0; i < offers.outgoing.Count; i++)
		{
			if (offers.outgoing[i] is LeadCrusadeOffer result)
			{
				return result;
			}
		}
		return null;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Catholic catholic = base.game.religions.catholic;
		switch (key)
		{
		case "target":
			return args[0];
		case "helping_kingdom":
			return args[1];
		case "reason":
			return args[2];
		case "leader":
			return args[3];
		case "pope":
			return catholic.head;
		case "our_kingdom":
		case "own_kingdom":
		case "src_kingdom":
			return catholic.hq_kingdom;
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
		Kingdom kingdom = to as Kingdom;
		Kingdom arg = GetArg<Kingdom>(0);
		Kingdom arg2 = GetArg<Kingdom>(1);
		Character arg3 = GetArg<Character>(3);
		if (!Crusade.IsValidLeaderKingdom(kingdom, arg, arg2))
		{
			return "leader_kingdom_invalid";
		}
		if (!Crusade.IsValidLeader(arg3, arg, arg2))
		{
			return "leader_kingdom_invalid";
		}
		string text = Crusade.ValidateTarget(arg, arg2, new_crusade: false);
		if (text != "ok")
		{
			return text;
		}
		string text2 = Crusade.ValidateNew(kingdom.game, arg2);
		if (text2 != "ok")
		{
			if (text2 != "_active_lead_offer")
			{
				return text2;
			}
			if (this != Get(base.game))
			{
				return "exisitng_offer";
			}
		}
		return base.Validate();
	}

	public override void OnAnswer(string answer)
	{
		Catholic catholic = base.game.religions.catholic;
		base.OnAnswer(answer);
		if (answer == "decline")
		{
			catholic.last_crusade_end = base.game.time;
			catholic.ConsiderExcommunicate(catholic.just_refused_crusade = to as Kingdom, ignore_cooldown: true);
			catholic.just_refused_crusade = null;
		}
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom arg = GetArg<Kingdom>(0);
		Kingdom arg2 = GetArg<Kingdom>(1);
		string arg3 = GetArg<string>(2);
		Character arg4 = GetArg<Character>(3);
		Crusade.Start(arg, arg2, arg3, arg4);
	}
}
