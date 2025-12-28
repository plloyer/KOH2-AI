namespace Logic;

public class CloseTradeAgreementAction : Action
{
	public enum Type
	{
		Manual,
		Auto
	}

	public Type type;

	public CloseTradeAgreementAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CloseTradeAgreementAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		return "ok";
	}

	public override void Run()
	{
		own_kingdom.UnsetStance(base.target as Kingdom, RelationUtils.Stance.Trade);
	}
}
