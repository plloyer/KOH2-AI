using System.Collections.Generic;

namespace Logic;

public class DiplomacySearchForSpouceAction : Action
{
	public DiplomacySearchForSpouceAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacySearchForSpouceAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null)
		{
			return "not_a_character";
		}
		return base.Validate(quick_out);
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		for (int i = 0; i < own_kingdom.royalFamily.Children.Count; i++)
		{
			AddTarget(ref targets, own_kingdom.royalFamily.Children[i]);
		}
		AddTarget(ref targets, own_kingdom.royalFamily.Sovereign);
		return targets;
	}

	public override bool ValidateTarget(Object target)
	{
		Character character = target as Character;
		if (character.IsDead())
		{
			return false;
		}
		if (!character.IsRoyalty())
		{
			return false;
		}
		if (character.IsQueen())
		{
			return false;
		}
		if (character.prison_kingdom != null)
		{
			return false;
		}
		if (!character.CanMarry())
		{
			return false;
		}
		if (character.age < Character.Age.Juvenile)
		{
			return false;
		}
		for (int i = 0; i < own_kingdom.court.Count; i++)
		{
			Character character2 = own_kingdom.court[i]?.FindStatus<SearchingForSpouseStatus>()?.character;
			if (character2 != null && character2 == character)
			{
				return false;
			}
		}
		return base.ValidateTarget(target);
	}

	public override void Cancel(bool manual = false, bool notify = true)
	{
		base.own_character.DelStatus<SearchingForSpouseStatus>();
		base.Cancel(manual, notify);
	}

	public override void Prepare()
	{
		SearchingForSpouseStatus status = new SearchingForSpouseStatus(base.target as Character);
		base.own_character.AddStatus(status);
		base.Prepare();
	}
}
