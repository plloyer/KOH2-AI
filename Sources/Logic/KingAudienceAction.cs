namespace Logic;

public class KingAudienceAction : PrisonAction
{
	public KingAudienceAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new KingAudienceAction(owner as Kingdom, def);
	}

	public override string Validate(bool quick_out = false)
	{
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Character character) || !character.IsKing())
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override void Run()
	{
		own_kingdom.FireEvent("open_audience_with", base.target.GetKingdom(), own_kingdom.id);
		base.Run();
	}
}
