namespace Logic;

public class RecallFromTradeExpeditionAction : Action
{
	public RecallFromTradeExpeditionAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new RecallFromTradeExpeditionAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.FindStatus<TradeExpeditionStatus>() == null)
		{
			return "no_trade_expedition";
		}
		return "ok";
	}

	public override void Run()
	{
		Status status = base.target as Status;
		base.own_character.DelStatus(status);
		base.Run();
	}
}
