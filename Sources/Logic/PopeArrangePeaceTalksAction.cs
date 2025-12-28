using System.Collections.Generic;

namespace Logic;

public class PopeArrangePeaceTalksAction : PopeInCourtAction
{
	public PopeArrangePeaceTalksAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PopeArrangePeaceTalksAction(owner as Character, def);
	}

	public virtual Kingdom GetHelpingKingdom()
	{
		return null;
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (!character.IsAlive())
		{
			return "not_alive";
		}
		if (!character.IsPope())
		{
			return "not_a_pope";
		}
		return "ok";
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is War war))
		{
			return false;
		}
		if (CalcKingdomFriendliness(war.defender) >= CalcKingdomFriendliness(war.attacker))
		{
			if (!war.attacker.is_catholic)
			{
				return false;
			}
			if (war.attacker.is_player)
			{
				return false;
			}
			if (war.attackers.Contains(own_kingdom))
			{
				return false;
			}
		}
		else
		{
			if (!war.defender.is_catholic)
			{
				return false;
			}
			if (war.defender.is_player)
			{
				return false;
			}
			if (war.defenders.Contains(own_kingdom))
			{
				return false;
			}
		}
		return true;
	}

	public int CalcKingdomFriendliness(Kingdom kingdom)
	{
		if (kingdom == own_kingdom)
		{
			return 10;
		}
		if (own_kingdom.vassalStates.Contains(kingdom))
		{
			return 9;
		}
		if (kingdom == own_kingdom.sovereignState)
		{
			return 8;
		}
		Game.Team team = base.game.teams.Get(own_kingdom);
		if (team != null)
		{
			for (int i = 0; i < team.players.Count; i++)
			{
				if (team.players[i] != null && team.players[i].GetKingdom() == kingdom)
				{
					return 7;
				}
			}
		}
		if (own_kingdom.allies.Contains(kingdom))
		{
			return 1;
		}
		return 0;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = null;
		if (own_kingdom == null)
		{
			return null;
		}
		for (int i = 0; i < own_kingdom.wars.Count; i++)
		{
			AddKingdomWarsAsTargets(ref targets, own_kingdom);
		}
		for (int j = 0; j < own_kingdom.vassalStates.Count; j++)
		{
			if (own_kingdom.vassalStates[j] != null)
			{
				AddKingdomWarsAsTargets(ref targets, own_kingdom.vassalStates[j]);
			}
		}
		if (own_kingdom.sovereignState != null)
		{
			AddKingdomWarsAsTargets(ref targets, own_kingdom.sovereignState);
		}
		for (int k = 0; k < own_kingdom.allies.Count; k++)
		{
			if (own_kingdom.allies[k] != null)
			{
				AddKingdomWarsAsTargets(ref targets, own_kingdom.allies[k]);
			}
		}
		Game.Team team = base.game.teams.Get(own_kingdom);
		if (team != null)
		{
			for (int l = 0; l < team.players.Count; l++)
			{
				if (team.players[l] != null)
				{
					Kingdom kingdom = team.players[l].GetKingdom();
					if (kingdom != null && kingdom != own_kingdom)
					{
						AddKingdomWarsAsTargets(ref targets, kingdom);
					}
				}
			}
		}
		return targets;
	}

	public void AddKingdomWarsAsTargets(ref List<Object> targets, Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return;
		}
		for (int i = 0; i < kingdom.wars.Count; i++)
		{
			War item = kingdom.wars[i];
			if (targets == null || !targets.Contains(item))
			{
				AddTarget(ref targets, item);
			}
		}
	}

	public override Kingdom CalcTargetKingdom(Object target)
	{
		if (target is War war)
		{
			if (CalcKingdomFriendliness(war.defender) >= CalcKingdomFriendliness(war.attacker))
			{
				return war.attacker;
			}
			return war.defender;
		}
		return null;
	}

	public override void FillPossibleTargetVars(Vars vars)
	{
		War war = base.target as War;
		if (CalcKingdomFriendliness(war.defender) >= CalcKingdomFriendliness(war.attacker))
		{
			vars.SetVar("target_kingdom", war.attacker);
			vars.SetVar("helped_kingdom", war.defender);
			vars.SetVar("helped_kingdom_2", war.defender);
		}
		else
		{
			vars.SetVar("target_kingdom", war.defender);
			vars.SetVar("helped_kingdom", war.attacker);
			vars.SetVar("helped_kingdom_2", war.attacker);
		}
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("src_kingdom", own_kingdom);
		War war = base.target as War;
		if (CalcKingdomFriendliness(war.defender) >= CalcKingdomFriendliness(war.attacker))
		{
			outcome_vars.SetVar("target_kingdom", war.attacker);
			outcome_vars.SetVar("helped_kingdom", war.defender);
			outcome_vars.SetVar("helped_kingdom_2", war.defender);
		}
		else
		{
			outcome_vars.SetVar("target_kingdom", war.defender);
			outcome_vars.SetVar("helped_kingdom", war.attacker);
			outcome_vars.SetVar("helped_kingdom_2", war.attacker);
		}
	}

	public override void Run()
	{
		if (!(base.target is War war))
		{
			return;
		}
		if (CalcKingdomFriendliness(war.defender) >= CalcKingdomFriendliness(war.attacker))
		{
			WhitePeaceOffer whitePeaceOffer = new WhitePeaceOffer(war.attacker, war.defender);
			if (whitePeaceOffer.Validate() == "ok")
			{
				whitePeaceOffer.Send();
			}
		}
		else
		{
			WhitePeaceOffer whitePeaceOffer2 = new WhitePeaceOffer(war.defender, war.attacker);
			if (whitePeaceOffer2.Validate() == "ok")
			{
				whitePeaceOffer2.Send();
			}
		}
		base.Run();
	}
}
