namespace Logic;

public class AskForAbsolution : PopeInCourtAction
{
	public AskForAbsolution(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AskForAbsolution(owner as Character, def);
	}

	public virtual Kingdom GetHelpingKingdom()
	{
		return null;
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
		return "ok";
	}

	public override bool ValidateTarget(Object target)
	{
		if (!NeedsTarget())
		{
			return true;
		}
		if (target == null)
		{
			return false;
		}
		if (def.target == "kingdom")
		{
			if (!(target is Kingdom kingdom))
			{
				return false;
			}
			if (!kingdom.excommunicated)
			{
				return false;
			}
			return true;
		}
		return base.ValidateTarget(target);
	}

	public override void Run()
	{
		if (base.target is Kingdom k)
		{
			base.game?.religions?.catholic?.UnExcommunicate(k);
			base.Run();
		}
	}
}
