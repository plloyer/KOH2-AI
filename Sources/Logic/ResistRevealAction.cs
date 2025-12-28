namespace Logic;

public class ResistRevealAction : SpyPlot
{
	public ResistRevealAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ResistRevealAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null || !character.IsSpy())
		{
			return "not_spy";
		}
		if (character.mission_kingdom == null)
		{
			return "not_on_mission";
		}
		return "ok";
	}
}
