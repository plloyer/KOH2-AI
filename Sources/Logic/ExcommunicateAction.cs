namespace Logic;

public class ExcommunicateAction : PopeInCourtAction
{
	public ExcommunicateAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ExcommunicateAction(owner as Character, def);
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
		if (base.game.session_time < base.game.religions.catholic.next_excommunicate)
		{
			return "_cooldown";
		}
		return "ok";
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Kingdom kingdom) || kingdom.IsDefeated())
		{
			return false;
		}
		if (!kingdom.is_catholic)
		{
			return false;
		}
		if (kingdom.excommunicated)
		{
			return false;
		}
		Catholic catholic = base.game.religions.catholic;
		if (kingdom == catholic.head_kingdom || kingdom == catholic.hq_kingdom)
		{
			return false;
		}
		return true;
	}

	public override void Run()
	{
		if (base.target is Kingdom { is_catholic: not false, excommunicated: false } kingdom)
		{
			base.game.religions.catholic.Excommunicate(kingdom);
			base.Run();
		}
	}
}
