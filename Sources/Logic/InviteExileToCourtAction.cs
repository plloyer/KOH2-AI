namespace Logic;

public class InviteExileToCourtAction : PrisonAction
{
	public InviteExileToCourtAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new InviteExileToCourtAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (own_kingdom.GetFreeCourtSlotIndex() == -1)
		{
			return "_no_free_court_slot";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Character character) || (!character.IsExile() && !character.IsMercenary()))
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override void Run()
	{
		if (base.target is Character character)
		{
			character.SetKingdom(own_kingdom.id);
			own_kingdom.AddCourtMember(character);
			character.Imprison(null, recall: true, send_state: true, null, destroy_if_free: false);
			character.OnPrisonActionAnalytics("invited_exile_back_to_court");
			if (character.sex == Character.Sex.Male)
			{
				character.SetTitle("Knight");
			}
			else
			{
				character.SetTitle("Lady");
			}
			character.GenerateNewSkills();
			base.Run();
		}
	}
}
