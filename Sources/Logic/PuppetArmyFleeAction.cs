namespace Logic;

public class PuppetArmyFleeAction : Action
{
	public PuppetArmyFleeAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetArmyFleeAction(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		Character character = target as Character;
		Army army = character?.GetArmy();
		if (character == null)
		{
			return false;
		}
		if (!character.IsMarshal())
		{
			return false;
		}
		if (army == null)
		{
			return false;
		}
		if (army.battle == null)
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override void Run()
	{
		Army army = (base.target as Character).GetArmy();
		int num = -1;
		if (army.battle.attackers.Contains(army))
		{
			num = 0;
		}
		else if (army.battle.defenders.Contains(army))
		{
			num = 1;
		}
		if (num != -1 && !army.battle.is_siege)
		{
			army.NotifyListeners("retreat");
			army.battle.DoAction("retreat", num);
		}
		base.Run();
	}
}
