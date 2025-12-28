namespace Logic;

public class ChangeHeirAction : Action
{
	public ChangeHeirAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ChangeHeirAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null)
		{
			return "no_heir";
		}
		if (base.own_character.IsHeir())
		{
			return "is_heir";
		}
		if (own_kingdom == null)
		{
			return "no_kingdom";
		}
		if (!base.own_character.CanBeHeir())
		{
			return "cant_be_heir";
		}
		return "ok";
	}

	public override Resource GetCost(Object target, IVars vars = null)
	{
		if (base.own_character.IsHeir())
		{
			return null;
		}
		if (!base.own_character.CanBeHeir())
		{
			return null;
		}
		return base.GetCost(target, vars);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("old_heir", own_kingdom.GetHeir());
	}

	public override void Run()
	{
		own_kingdom.royalFamily.SetHeir(base.own_character);
	}
}
