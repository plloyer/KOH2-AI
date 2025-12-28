namespace Logic;

public class DiplomacyPeaceTalksPreparationAction : Action
{
	public DiplomacyPeaceTalksPreparationAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacyPeaceTalksPreparationAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null)
		{
			return "not_a_character";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Kingdom k))
		{
			return false;
		}
		if (!War.CanStop(own_kingdom, k))
		{
			return false;
		}
		if (!base.ValidateTarget(target))
		{
			return false;
		}
		for (int i = 0; i < own_kingdom.court.Count; i++)
		{
			Character character = own_kingdom.court[i];
			if (character != null && character.IsDiplomat() && character != base.own_character)
			{
				if ((character.cur_action as DiplomacyPeaceTalksPreparationAction)?.target != null && target == (character.cur_action as DiplomacyPeaceTalksPreparationAction)?.target as Kingdom)
				{
					return false;
				}
				if ((character.cur_action as DiplomacyPeaceTalksAction)?.target != null && target == (character.cur_action as DiplomacyPeaceTalksAction)?.target as Kingdom)
				{
					return false;
				}
			}
		}
		return true;
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null && character.actions != null)
		{
			Kingdom kingdom = base.target as Kingdom;
			character.cur_action.Cancel(manual: false, notify: false);
			character.actions.Find("DiplomacyPeaceTalksAction").Execute(kingdom);
			base.Run();
		}
	}
}
