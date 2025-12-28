namespace Logic;

public class AbandonAction : Action
{
	public AbandonAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AbandonAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.IsKingOrPrince())
		{
			return "family_member";
		}
		Kingdom kingdom = character.GetKingdom();
		if (kingdom == null)
		{
			return "no_kingdom";
		}
		if (!kingdom.court.Contains(character))
		{
			return "not_in_court";
		}
		if (character.IsRebel())
		{
			return "rebel";
		}
		if (character.IsPope())
		{
			return "pope";
		}
		if (character.prison_kingdom == null)
		{
			return "not_imprisoned";
		}
		if (character.prison_kingdom == kingdom)
		{
			return "imprisoned_in_own_kingdom";
		}
		return "ok";
	}

	public override void CreateOutcomeVars()
	{
		target_kingdom = base.own_character.prison_kingdom;
		base.CreateOutcomeVars();
		outcome_vars.Set("old_kingdom", base.own_character.GetKingdom());
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			Kingdom kingdom = own_kingdom;
			character.OnPrisonActionAnalytics("abandoned");
			character.Exile();
			kingdom.NotifyListeners("character_abandoned", character);
			base.Run();
		}
	}
}
