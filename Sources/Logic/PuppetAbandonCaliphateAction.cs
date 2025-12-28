namespace Logic;

public class PuppetAbandonCaliphateAction : PuppetPlot
{
	public PuppetAbandonCaliphateAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetAbandonCaliphateAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (!puppet.IsCaliph())
		{
			return "not_a_caliph";
		}
		return base.ValidatePuppet(puppet);
	}

	public override void Run()
	{
		Character character = base.target as Character;
		Kingdom kingdom = character.GetKingdom();
		kingdom.actions.Find("AbandonCaliphateAction")?.Execute(kingdom);
		base.own_character.DelPuppet(character);
		base.Run();
	}
}
