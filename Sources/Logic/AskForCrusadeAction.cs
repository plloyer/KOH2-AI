using System.Collections.Generic;

namespace Logic;

public class AskForCrusadeAction : CrusadeAction
{
	private Character tmp_leader;

	public AskForCrusadeAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AskForCrusadeAction(owner as Character, def);
	}

	public override Kingdom GetHelpingKingdom()
	{
		return base.own_character.GetSpecialCourtKingdom();
	}

	public override Character GetLeader()
	{
		return tmp_leader;
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.GetSpecialCourtKingdom() == base.game.religions.catholic.hq_kingdom)
		{
			return "not_a_foreign_pope";
		}
		string text = base.Validate(quick_out);
		_ = text != "ok";
		return text;
	}

	public override List<OutcomeDef> DecideOutcomes()
	{
		tmp_leader = null;
		List<OutcomeDef> result = new List<OutcomeDef>();
		OutcomeDef outcomeDef = def.outcomes?.DecideOption(base.game, this, forced_outcomes, AlterOutcomeChance);
		if (outcomeDef == null)
		{
			return result;
		}
		if (outcomeDef.key != "success")
		{
			outcomeDef.AddOutcomes(result, base.game, this, forced_outcomes, AlterOutcomeChance);
			return result;
		}
		tmp_leader = Crusade.PickLeader(base.game, target_kingdom, GetHelpingKingdom());
		Kingdom kingdom = tmp_leader?.GetKingdom();
		string name = ((kingdom == null || !kingdom.is_player) ? "AI_leader" : ((kingdom != base.own_character.GetSpecialCourtKingdom()) ? "another_player_leader" : "own_leader"));
		OutcomeDef outcomeDef2 = outcomeDef.FindOption(name);
		if (outcomeDef2 == null)
		{
			outcomeDef2 = outcomeDef;
		}
		outcomeDef2.AddOutcomes(result, base.game, this);
		return result;
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("leader", tmp_leader);
	}

	public override void Run()
	{
		base.Run();
		tmp_leader = null;
	}
}
