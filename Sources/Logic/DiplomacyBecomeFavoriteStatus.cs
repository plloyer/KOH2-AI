namespace Logic;

public class DiplomacyBecomeFavoriteStatus : Status
{
	public DiplomacyBecomeFavoriteStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new DiplomacyBecomeFavoriteStatus(def);
	}
}
