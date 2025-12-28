namespace Logic;

public class OnSiegeConvertReligion : Component
{
	public Realm realm;

	public OnSiegeConvertReligion(Realm realm)
		: base(realm)
	{
		this.realm = realm;
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
	}

	public bool TryConvertBy(Character c)
	{
		if (c == null)
		{
			return false;
		}
		if (IsRegisteredForUpdate())
		{
			return false;
		}
		if (c.GetKingdom().religion == realm.religion || !c.GetKingdom().IsRegular())
		{
			return false;
		}
		float num = c.GetStat(Stats.cs_convert_chance_after_siege_perc) + War.GetBonus(c.GetKingdom(), realm.GetLastOwner(), "conversion_chance");
		if (num == 0f)
		{
			return false;
		}
		if ((float)base.game.Random(0, 100) < num)
		{
			realm.SetReligion(c.GetKingdom().religion);
			realm.FireEvent("province_converted", c.GetKingdom());
			return true;
		}
		UpdateAfter(realm.on_siege_convert_timeout);
		return false;
	}
}
