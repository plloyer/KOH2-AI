namespace Logic;

public class ImproveOpinionsAction : Action
{
	public ImproveOpinionsAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ImproveOpinionsAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null)
		{
			return "not_a_character";
		}
		return base.Validate(quick_out);
	}

	public override void Run()
	{
		own_kingdom.SetImproveOpinionsDiplomat(base.own_character);
		base.Run();
	}
}
