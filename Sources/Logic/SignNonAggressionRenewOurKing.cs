namespace Logic;

public class SignNonAggressionRenewOurKing : SignNonAggression
{
	public SignNonAggressionRenewOurKing(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public SignNonAggressionRenewOurKing(Kingdom from, Kingdom to, params Value[] args)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new SignNonAggressionRenewOurKing(def, from, to);
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom k = GetTargetObj() as Kingdom;
		if (KingdomAndKingdomRelation.Get(kingdom, k).nap_broken_dead_king_kingdom != kingdom)
		{
			return "dead_king_is_not_ours";
		}
		return text;
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom k = GetTargetObj() as Kingdom;
		if (KingdomAndKingdomRelation.Get(kingdom, k).nap_broken_dead_king_kingdom != kingdom)
		{
			return "dead_king_is_not_ours";
		}
		return text;
	}
}
