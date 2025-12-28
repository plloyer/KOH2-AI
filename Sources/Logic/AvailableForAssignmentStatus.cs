namespace Logic;

public class AvailableForAssignmentStatus : Status
{
	public AvailableForAssignmentStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new AvailableForAssignmentStatus(def);
	}

	public override bool IsIdle()
	{
		return true;
	}

	public override bool IsAutomatic()
	{
		return true;
	}

	public static bool Validate(Character c)
	{
		if (c == null || (!c.IsKingOrPrince() && !c.IsRoyalRelative()))
		{
			return false;
		}
		if (c.age < Character.Age.Young)
		{
			return false;
		}
		if (c.IsInCourt())
		{
			return false;
		}
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "cannot_hire_marshal")
		{
			return CannotHireMarshal();
		}
		return base.GetVar(key, vars, as_value);
	}

	public bool CannotHireMarshal()
	{
		Character character = base.own_character;
		if (character == null || !character.IsMarshal())
		{
			return false;
		}
		Kingdom kingdom = character.GetKingdom();
		if (kingdom == null)
		{
			return true;
		}
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			Realm realm = kingdom.realms[i];
			if (realm.castle.battle == null && !realm.IsOccupied() && !realm.IsDisorder())
			{
				return false;
			}
		}
		return true;
	}

	public bool Assign(int index = -1)
	{
		if (owner == null)
		{
			return false;
		}
		Kingdom kingdom = owner.GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		Character character = base.own_character;
		int freeCourtSlotIndex = kingdom.GetFreeCourtSlotIndex();
		if (freeCourtSlotIndex == -1)
		{
			return false;
		}
		kingdom.AddCourtMember(character, freeCourtSlotIndex);
		character.DelStatus<AvailableForAssignmentStatus>();
		return true;
	}

	public override void OnButton(string btn_id)
	{
		base.OnButton(btn_id);
		if (btn_id == "assign")
		{
			Assign();
		}
	}
}
