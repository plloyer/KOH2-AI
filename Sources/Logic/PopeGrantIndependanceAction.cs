namespace Logic;

public class PopeGrantIndependanceAction : PopeInCourtAction
{
	public PopeGrantIndependanceAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PopeGrantIndependanceAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (!character.IsAlive())
		{
			return "not_alive";
		}
		if (!character.IsPope())
		{
			return "not_a_pope";
		}
		Kingdom specialCourtKingdom = character.GetSpecialCourtKingdom();
		if (specialCourtKingdom == base.game.religions.catholic.hq_kingdom)
		{
			return "not_a_foreign_pope";
		}
		if (specialCourtKingdom?.sovereignState == null)
		{
			return "not_vassal";
		}
		if (specialCourtKingdom?.sovereignState == base.game.religions.catholic.hq_kingdom)
		{
			return "vassal_of_papacy";
		}
		if (!specialCourtKingdom.sovereignState.is_catholic)
		{
			return "not_catholic";
		}
		return "ok";
	}

	public override void Run()
	{
		base.Run();
		Kingdom specialCourtKingdom = base.own_character.GetSpecialCourtKingdom();
		if (specialCourtKingdom.sovereignState != null)
		{
			specialCourtKingdom.sovereignState.DelVassalState(specialCourtKingdom);
		}
	}
}
