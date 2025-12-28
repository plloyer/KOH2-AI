namespace Logic;

public class ForgetCharacterAction : Action
{
	public ForgetCharacterAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ForgetCharacterAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.IsAlive())
		{
			return "alive";
		}
		if (character.FindStatus<DeadPatriarchStatus>() != null && own_kingdom?.religion == base.game.religions.orthodox && !own_kingdom.subordinated)
		{
			return "is_patriarch";
		}
		return "ok";
	}

	public override string GetConfirmationMessageKey()
	{
		if (WasLeadingArmy(base.own_character))
		{
			return "confirmation_message";
		}
		return string.Empty;
	}

	private bool WasLeadingArmy(Character c)
	{
		if (base.own_character.IsAlive())
		{
			return false;
		}
		Character character = base.own_character?.GetKingdom()?.GetKing();
		if (character == null || character.class_title != "Marshal")
		{
			return false;
		}
		if (!(base.own_character.status is DeadStatus deadStatus))
		{
			return false;
		}
		if (deadStatus.vars == null)
		{
			return false;
		}
		Army army = deadStatus.vars.Get<Army>("army");
		if (army == null)
		{
			return false;
		}
		if (!army.IsValid())
		{
			return false;
		}
		if (army.mercenary == null)
		{
			return false;
		}
		return true;
	}

	public override void Run()
	{
		base.own_character?.Destroy();
	}
}
