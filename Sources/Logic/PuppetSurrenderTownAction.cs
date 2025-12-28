namespace Logic;

public class PuppetSurrenderTownAction : PuppetPlot
{
	public PuppetSurrenderTownAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetSurrenderTownAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.IsKing())
		{
			return "is_king";
		}
		if (puppet.governed_castle == null)
		{
			return "not_governing";
		}
		if (puppet.governed_castle.battle == null)
		{
			return "castle_not_in_battle";
		}
		return base.ValidatePuppet(puppet);
	}

	public override Kingdom CalcTargetKingdom(Object target)
	{
		Character character = target as Character;
		if (character?.governed_castle?.battle?.attacker == null)
		{
			return null;
		}
		return character.governed_castle.battle.attacker.GetKingdom();
	}

	public override void Run()
	{
		(base.target as Character).governed_castle.battle.Victory(attacker_won: true, Battle.VictoryReason.Surrender);
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "realm")
		{
			return (base.target as Character)?.governed_castle.GetRealm();
		}
		return base.GetVar(key, vars, as_value);
	}
}
