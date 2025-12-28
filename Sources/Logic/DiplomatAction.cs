namespace Logic;

public class DiplomatAction : Action
{
	public DiplomatAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomatAction(owner as Character, def);
	}

	public override bool ApplyCost(bool check_first = true)
	{
		if (!base.ApplyCost(check_first))
		{
			return false;
		}
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "influence")
		{
			return own_kingdom.GetInfluenceIn(base.own_character.mission_kingdom);
		}
		return base.GetVar(key, vars, as_value);
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
		return base.Validate(quick_out);
	}
}
