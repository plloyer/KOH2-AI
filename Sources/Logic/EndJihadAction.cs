namespace Logic;

public class EndJihadAction : Action
{
	public EndJihadAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new EndJihadAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return "not_a_kindom";
		}
		if (kingdom.jihad_target == null)
		{
			return "no_jihad";
		}
		return "ok";
	}

	public override void Run()
	{
		War.EndJihad(own_kingdom, "action");
		base.Run();
	}
}
