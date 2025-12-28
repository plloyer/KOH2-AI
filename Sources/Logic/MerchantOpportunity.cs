namespace Logic;

public class MerchantOpportunity : CharacterOpportunity
{
	private float validation_min_chance;

	public MerchantOpportunity(Character owner, Def def)
		: base(owner, def)
	{
		validation_min_chance = def.field.GetFloat("validation_min_chance", null, validation_min_chance);
	}

	public new static Action Create(Object owner, Def def)
	{
		return new MerchantOpportunity(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (validation_min_chance > 0f && SuccessChanceValue(non_trivial_only: false).Float() < validation_min_chance)
		{
			return "chance_too_low";
		}
		return base.Validate(quick_out);
	}
}
