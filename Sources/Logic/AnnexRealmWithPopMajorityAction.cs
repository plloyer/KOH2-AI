namespace Logic;

public class AnnexRealmWithPopMajorityAction : Action
{
	public AnnexRealmWithPopMajorityAction(Realm owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AnnexRealmWithPopMajorityAction(owner as Realm, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Realm realm = base.owner as Realm;
		if (!realm.IsOccupied())
		{
			return "realm_not_occupied";
		}
		if (realm.pop_majority.kingdom != realm.controller.GetKingdom())
		{
			return "no_population_majority";
		}
		return base.Validate(quick_out);
	}

	public override void Run()
	{
		Realm obj = base.owner as Realm;
		obj.SetKingdom(obj.controller.GetKingdom().id);
		base.Run();
	}
}
