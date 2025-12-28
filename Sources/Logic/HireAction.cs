namespace Logic;

public class HireAction : Action
{
	public HireAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new HireAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = base.own_character.GetKingdom();
		if (kingdom == null)
		{
			return "no_kingdom";
		}
		if (!def.secondary && base.own_character.cur_action != null && !base.own_character.cur_action.CanBeCancelled(this))
		{
			return "another_action_in_progress";
		}
		if (kingdom.court.Contains(base.own_character))
		{
			return "in_court";
		}
		if (kingdom.GetFreeCourtSlotIndex() == -1)
		{
			return "court_full";
		}
		if (base.own_character.IsRebel())
		{
			return "is_rebel";
		}
		return "ok";
	}

	public override bool NeedsTarget()
	{
		if (!base.own_character.IsMarshal())
		{
			return false;
		}
		if (!string.IsNullOrEmpty(def.target))
		{
			return def.target != "none";
		}
		return false;
	}

	public override bool ValidateTarget(Object target)
	{
		if (!NeedsTarget())
		{
			return true;
		}
		if (target == null)
		{
			return false;
		}
		if (def.target == "own_town")
		{
			if (!(target is Castle castle))
			{
				return false;
			}
			Kingdom kingdom = castle.GetKingdom();
			if (kingdom == null || kingdom != own_kingdom)
			{
				return false;
			}
			if (castle.battle != null)
			{
				return false;
			}
			if (castle.GetRealm().IsOccupied())
			{
				return false;
			}
			return true;
		}
		return base.ValidateTarget(target);
	}

	public override void Run()
	{
		Kingdom kingdom = base.own_character.GetKingdom();
		if (kingdom != null)
		{
			int index = -1;
			if (args != null && args.Count > 0)
			{
				index = (args[0].is_number ? args[0].int_val : (-1));
				if (kingdom.court[index] != null)
				{
					return;
				}
			}
			kingdom.AddCourtMember(base.own_character, index, is_hire: true);
			if (base.own_character.IsMarshal())
			{
				base.own_character.SpawnArmy(base.target as Castle);
			}
		}
		base.own_character.DelStatus<AvailableForAssignmentStatus>();
		base.own_character.DelStatus<ForHireStatus>();
		base.Run();
	}
}
