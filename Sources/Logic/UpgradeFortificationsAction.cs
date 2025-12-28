namespace Logic;

public class UpgradeFortificationsAction : Action
{
	public UpgradeFortificationsAction(Realm owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new UpgradeFortificationsAction(owner as Realm, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Realm realm = (Realm)base.owner;
		if (realm == null)
		{
			return "owner_not_a_realm";
		}
		Castle castle = realm.castle;
		if (castle == null)
		{
			return "no_castle";
		}
		if (castle.fortifications == null)
		{
			return "no_fortifications";
		}
		if (castle.FortificationsNeedRepair())
		{
			if (!castle.CanAffordBoostSiegeDefenceRepairs())
			{
				return "cant_afford_repair";
			}
		}
		else if (castle.CanUpgradeFortification() && !castle.CanAffordFortificationsUpgrade())
		{
			return "cant_afford_upgrade";
		}
		return base.Validate(quick_out);
	}

	public override bool CheckCost(Object target)
	{
		Castle castle = (base.owner as Realm).castle;
		if (castle == null)
		{
			return false;
		}
		if (castle.FortificationsNeedRepair() && castle.CanAffordBoostSiegeDefenceRepairs())
		{
			return true;
		}
		if (castle.CanUpgradeFortification() && castle.CanAffordFortificationsUpgrade())
		{
			return true;
		}
		return false;
	}

	public override bool ApplyCost(bool check_first = true)
	{
		Castle castle = (base.owner as Realm).castle;
		if (castle == null)
		{
			return false;
		}
		castle.GetKingdom().SubResources(KingdomAI.Expense.Category.Military, castle.FortificationsNeedRepair() ? castle.GetSiegeDefenceRepairCost() : castle.GetFortificationsUpgradeCost());
		return true;
	}

	public override void Run()
	{
		Castle castle = (base.owner as Realm).castle;
		if (castle.FortificationsNeedRepair())
		{
			castle.BoostSiegeDefenceRepairs();
		}
		else
		{
			castle.UpgradeFortification();
		}
		base.Run();
	}
}
