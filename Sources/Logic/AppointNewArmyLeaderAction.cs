namespace Logic;

public class AppointNewArmyLeaderAction : Action
{
	public AppointNewArmyLeaderAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AppointNewArmyLeaderAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = base.own_character.GetKingdom();
		if (kingdom == null)
		{
			return null;
		}
		if (base.own_character == null)
		{
			return "not_character";
		}
		if (base.own_character.IsAlive())
		{
			return "alive";
		}
		if (!(base.own_character.status is DeadStatus deadStatus))
		{
			return "not_dead";
		}
		if (deadStatus.vars == null)
		{
			return "missing_data";
		}
		Army army = deadStatus.vars.Get<Army>("army");
		if (army == null)
		{
			return "no_army";
		}
		if (!army.IsValid())
		{
			return "army_destoryed";
		}
		Character character = kingdom?.royalFamily?.Sovereign;
		if (character == null || character.class_title != "Marshal")
		{
			return "non_marshal_king";
		}
		return "ok";
	}

	public override Resource GetCost(Object target, IVars vars = null)
	{
		Kingdom kingdom = base.own_character.GetKingdom();
		return ForHireStatus.GetCost(base.game, kingdom, "Marshal");
	}

	public override bool NeedsTarget()
	{
		return false;
	}

	public override void Run()
	{
		Kingdom kingdom = base.own_character.GetKingdom();
		if (kingdom != null)
		{
			Army leaderlessArmy = GetLeaderlessArmy(base.own_character);
			if (leaderlessArmy != null && leaderlessArmy.mercenary != null)
			{
				int index = -1;
				if (base.own_character != null && base.own_character.IsInCourt())
				{
					index = kingdom.court.IndexOf(base.own_character);
				}
				kingdom.DelCourtMember(base.own_character);
				Character leader = kingdom.HireCharacter("Marshal", index);
				leaderlessArmy.mercenary.BecomeRegular(kingdom, leader);
			}
		}
		base.Run();
	}

	private Army GetLeaderlessArmy(Character previus_leader)
	{
		if (previus_leader.IsAlive())
		{
			return null;
		}
		if (!(base.own_character.status is DeadStatus deadStatus))
		{
			return null;
		}
		if (deadStatus.vars == null)
		{
			return null;
		}
		Army army = deadStatus.vars.Get<Army>("army");
		if (army == null)
		{
			return null;
		}
		if (!army.IsValid())
		{
			return null;
		}
		return army;
	}
}
