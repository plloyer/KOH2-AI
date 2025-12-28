namespace Logic;

public class PuppetPlotAbolishAutocephaly : PuppetPlot
{
	public PuppetPlotAbolishAutocephaly(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetPlotAbolishAutocephaly(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (!puppet.IsPatriarch())
		{
			return "not_a_caliph";
		}
		if (puppet.IsEcumenicalPatriarch())
		{
			return "is_ecumenical_patriarch";
		}
		return base.ValidatePuppet(puppet);
	}

	public override void Run()
	{
		Kingdom kingdom = (base.target as Character).GetKingdom();
		base.game.religions.orthodox.SetSubordinated(kingdom, subordinated: true);
		base.Run();
	}
}
