namespace Logic;

public class OpenChoosePatriarchAction : Action
{
	public OpenChoosePatriarchAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new OpenChoosePatriarchAction(owner as Character, def);
	}

	public override void Run()
	{
		base.game.religions.FireEvent("open_choose_patriarch_message", own_kingdom);
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
			return "is_alive";
		}
		if (character.FindStatus<DeadPatriarchStatus>() == null)
		{
			return "not_a_patriarch";
		}
		if (own_kingdom == null)
		{
			return "no_kingdom";
		}
		if (own_kingdom.religion != base.game.religions.orthodox || own_kingdom.subordinated)
		{
			return "no_patriarchate";
		}
		return "ok";
	}
}
