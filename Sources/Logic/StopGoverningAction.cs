namespace Logic;

public class StopGoverningAction : Action
{
	public StopGoverningAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new StopGoverningAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.GetGovernedCastle() == null)
		{
			return "not_governing";
		}
		if (character.IsPope())
		{
			return "pope";
		}
		if (base.game.religions?.catholic?.crusade?.army?.leader == character)
		{
			return "leading_crusade";
		}
		return "ok";
	}

	public override void Run()
	{
		Castle governed_castle = base.own_character.governed_castle;
		base.own_character.StopGoverning();
		if (governed_castle != null)
		{
			base.own_character.OnGovernAnalytics(own_kingdom, governed_castle.GetRealm(), GetType().Name);
		}
		base.Run();
	}
}
