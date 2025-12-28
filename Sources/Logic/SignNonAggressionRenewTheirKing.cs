namespace Logic;

public class SignNonAggressionRenewTheirKing : SignNonAggression
{
	public SignNonAggressionRenewTheirKing(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public SignNonAggressionRenewTheirKing(Kingdom from, Kingdom to, params Value[] args)
		: base(from, to)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new SignNonAggressionRenewTheirKing(def, from, to);
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom k = GetSourceObj() as Kingdom;
		Kingdom kingdom = GetTargetObj() as Kingdom;
		if (KingdomAndKingdomRelation.Get(k, kingdom).nap_broken_dead_king_kingdom != kingdom)
		{
			return "dead_king_is_not_theirs";
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
		Kingdom k = GetSourceObj() as Kingdom;
		Kingdom kingdom = GetTargetObj() as Kingdom;
		if (KingdomAndKingdomRelation.Get(k, kingdom).nap_broken_dead_king_kingdom != kingdom)
		{
			return "dead_king_is_not_theirs";
		}
		return text;
	}
}
