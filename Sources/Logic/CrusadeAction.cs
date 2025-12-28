namespace Logic;

public class CrusadeAction : PopeInCourtAction
{
	public CrusadeAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CrusadeAction(owner as Character, def);
	}

	public virtual Kingdom GetHelpingKingdom()
	{
		return null;
	}

	public virtual Character GetLeader()
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
		string text = Crusade.ValidateNew(base.game, GetHelpingKingdom());
		if (text != "ok")
		{
			return text;
		}
		return "ok";
	}

	public override bool ValidateTarget(Object target)
	{
		if (Crusade.ValidateTarget(target as Kingdom, GetHelpingKingdom()) != "ok")
		{
			return false;
		}
		return true;
	}

	public override void Run()
	{
		Crusade.Start(base.target as Kingdom, GetHelpingKingdom(), GetLeader());
		base.Run();
	}
}
