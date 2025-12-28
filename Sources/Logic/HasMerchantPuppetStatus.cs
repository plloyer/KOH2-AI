namespace Logic;

public class HasMerchantPuppetStatus : HasPuppetStatus
{
	public HasMerchantPuppetStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new HasMerchantPuppetStatus(def);
	}

	public HasMerchantPuppetStatus(Character puppet)
		: base(puppet)
	{
	}
}
