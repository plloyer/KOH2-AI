namespace Logic;

public class DiplomacyCallForPeaceAction : DiplomatAction
{
	public DiplomacyCallForPeaceAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacyCallForPeaceAction(owner as Character, def);
	}

	public override void Run()
	{
		if (base.own_character == null)
		{
			return;
		}
		foreach (Kingdom neighbor in own_kingdom.neighbors)
		{
			if (own_kingdom.IsEnemy(neighbor))
			{
				WhitePeaceOffer whitePeaceOffer = new WhitePeaceOffer(own_kingdom, neighbor);
				if (whitePeaceOffer.Validate() == "ok")
				{
					whitePeaceOffer.Send();
				}
			}
		}
		base.Run();
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (own_kingdom.royalFamily.Sovereign != character)
		{
			return "diplomat_is_not_a_king";
		}
		return base.Validate(quick_out);
	}
}
