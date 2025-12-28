namespace Logic;

public class RestorePapacyAction : Action
{
	public RestorePapacyAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new RestorePapacyAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = own_kingdom;
		Realm realm = base.game.religions?.catholic?.hq_realm;
		if (kingdom == null)
		{
			return "not_a_kindom";
		}
		if (realm?.GetKingdom() != kingdom)
		{
			return "not_rome_owner";
		}
		if (kingdom.realms.Count <= 1)
		{
			return "we_have_one_province";
		}
		if (realm?.castle.battle != null)
		{
			return "rome_under_siege";
		}
		if (realm != null)
		{
			realm.IsOccupied();
			if (realm?.GetControllingKingdom() != kingdom)
			{
				return "rome_under_occupation";
			}
		}
		return "ok";
	}

	public override void Run()
	{
		base.game.religions.catholic.RestorePapacy("action");
		base.Run();
	}
}
