namespace Logic;

public class InspireRiotAction : InspireRiotBaseAction
{
	public InspireRiotAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new InspireRiotAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.prison_kingdom == own_kingdom)
		{
			return "imprisoned_in_own_kingdom";
		}
		return base.Validate(quick_out);
	}
}
