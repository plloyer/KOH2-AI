namespace Logic;

public class DiplomacyBecomeFavoriteAction : DiplomatAction
{
	public DiplomacyBecomeFavoriteAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacyBecomeFavoriteAction(owner as Character, def);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			character.mission_kingdom.favoriteDiplomat = character;
			base.Run();
		}
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.mission_kingdom == null)
		{
			return "not_in_a_kingdom";
		}
		if (character.mission_kingdom.favoriteDiplomat == character)
		{
			return "already_favorite";
		}
		return base.Validate(quick_out);
	}
}
