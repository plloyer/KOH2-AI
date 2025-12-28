namespace Logic;

public class ForcedCrusadeAction : Action
{
	public ForcedCrusadeAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public override Character GetVoicingCharacter()
	{
		return base.game?.religions?.catholic?.head;
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ForcedCrusadeAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return "no_kingdom";
		}
		if (base.game.religions.catholic.hq_kingdom.IsDefeated())
		{
			return "no_papacy";
		}
		if (!kingdom.HasPope())
		{
			return "no_pope";
		}
		if (!kingdom.is_catholic)
		{
			return "no_catholic";
		}
		if (!Crusade.IsValidLeader(base.own_character, null, kingdom))
		{
			return "invalid_leader";
		}
		string text = Crusade.ValidateNew(kingdom.game, kingdom, forced: true);
		if (text != "ok")
		{
			return text;
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		Kingdom kingdom = own_kingdom;
		Kingdom kingdom2 = target as Kingdom;
		if (Crusade.ValidateTarget(kingdom2, kingdom, new_crusade: true, forced: true) != "ok")
		{
			return false;
		}
		if (kingdom.IsEnemy(kingdom2))
		{
			return true;
		}
		if (kingdom.GetRelationship(kingdom2) < 0f && kingdom.neighbors.Contains(kingdom2))
		{
			return true;
		}
		return false;
	}

	public override void Run()
	{
		Crusade.Start(base.target as Kingdom, own_kingdom, "forced", base.own_character);
	}
}
