namespace Logic;

public class OpenAudienceAction : DiplomatAction
{
	public OpenAudienceAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new OpenAudienceAction(owner as Character, def);
	}

	public override void Run()
	{
		own_kingdom.FireEvent("open_audience_with", base.own_character.mission_kingdom, own_kingdom.id);
	}

	public override string ValidateMissionKingdom()
	{
		return "ok";
	}

	public override string ValidateIdle()
	{
		return "ok";
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (!character.IsDiplomat())
		{
			return "not_a_diplomat";
		}
		if (character.IsRebel())
		{
			return "rebel";
		}
		return base.Validate(quick_out);
	}
}
