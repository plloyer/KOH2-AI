namespace Logic;

public class RemoveRebelFromCourtAction : Action
{
	public RemoveRebelFromCourtAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new RemoveRebelFromCourtAction(owner as Character, def);
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
			return "dead";
		}
		if (!character.IsRebel())
		{
			return "not_rebel";
		}
		if (character.IsPrisoner())
		{
			return "is_prisoner";
		}
		return "ok";
	}

	public override void Run()
	{
		base.own_character.GetSpecialCourtKingdom()?.DelSpecialCourtMember(base.own_character);
		if (base.own_character.IsEcumenicalPatriarch())
		{
			base.game.religions.orthodox.OnCharacterDied(base.own_character);
		}
		else if (base.own_character.IsPatriarch())
		{
			base.own_character.GetSpecialCourtKingdom()?.religion.OnCharacterDied(base.own_character);
		}
	}
}
