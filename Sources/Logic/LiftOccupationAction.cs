namespace Logic;

public class LiftOccupationAction : Action
{
	public LiftOccupationAction(Realm owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new LiftOccupationAction(owner as Realm, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (!(base.owner as Realm).IsOccupied())
		{
			return "realm_not_occupied";
		}
		return base.Validate(quick_out);
	}

	public override void Run()
	{
		Realm obj = base.owner as Realm;
		obj.SetOccupied(obj.GetKingdom());
		base.Run();
	}
}
