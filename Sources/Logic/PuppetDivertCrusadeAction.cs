namespace Logic;

public class PuppetDivertCrusadeAction : PuppetPlot
{
	public PuppetDivertCrusadeAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetDivertCrusadeAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.GetArmy() == null)
		{
			return "no_army";
		}
		if (!puppet.IsCrusader())
		{
			return "not_crusader";
		}
		return base.ValidatePuppet(puppet);
	}

	public override void Run()
	{
		Character leader = base.game.religions.catholic.crusade.leader;
		base.game.religions.catholic.crusade.end_reason = "crusade_diverted";
		base.own_character.DelPuppet(leader);
		leader.TurnIntoRebel("LeaderRebels");
		base.Run();
	}
}
