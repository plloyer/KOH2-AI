namespace Logic;

public class PopeLeaveAction : PopeInCourtAction
{
	public PopeLeaveAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PopeLeaveAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (!character.IsAlive())
		{
			return "not_alive";
		}
		if (character == null)
		{
			return "not_a_character";
		}
		if (!character.IsPope())
		{
			return "not_a_pope";
		}
		Kingdom specialCourtKingdom = character.GetSpecialCourtKingdom();
		if (specialCourtKingdom == null || specialCourtKingdom == base.game.religions.catholic.hq_kingdom)
		{
			return "from_papacy";
		}
		if (!specialCourtKingdom.special_court.Contains(character))
		{
			return "not_in_court";
		}
		return "ok";
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("own_kingdom", own_kingdom);
	}

	public override void Run()
	{
		base.game.religions.catholic.PopeLeave();
		base.Run();
	}
}
