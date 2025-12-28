using System.Collections.Generic;

namespace Logic;

public class TurnRebelIntoLoyalist : SpyPlot
{
	public TurnRebelIntoLoyalist(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new TurnRebelIntoLoyalist(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Character character))
		{
			return false;
		}
		Army army = character.GetArmy();
		Rebel rebel = army?.rebel;
		if (rebel == null)
		{
			return false;
		}
		if (rebel.rebellion?.leader != rebel)
		{
			return false;
		}
		if (rebel.IsLoyalist())
		{
			return false;
		}
		int num = 4;
		DT.Field field = base.own_character?.actions?.Find("InfiltrateKingdomAction")?.def?.field;
		if (field != null)
		{
			num = field.GetInt("max_rebel_puppet_distance");
		}
		int num2 = base.game.KingdomAndRealmDistance(own_kingdom.id, army.realm_in.id);
		if (num2 == 0 || num2 > num)
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override List<Object> GetPossibleTargets()
	{
		Kingdom kingdom = base.own_character?.mission_kingdom;
		if (kingdom == null)
		{
			return null;
		}
		List<Object> targets = null;
		foreach (Rebellion rebellion in kingdom.rebellions)
		{
			AddTarget(ref targets, rebellion.leader?.character);
		}
		return targets;
	}

	public override void Run()
	{
		((((base.target as Character)?.GetArmy())?.rebel)?.rebellion)?.ChangeLoyalTo(own_kingdom.id);
		base.Run();
	}
}
