namespace Logic;

public class AskForProtection : DemandSupportInWar
{
	public AskForProtection(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public AskForProtection(Kingdom from, Kingdom to, War war)
		: base(from, to, war)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new AskForProtection(def, from, to);
	}

	public override bool IsWar(bool sender)
	{
		return !sender;
	}

	public override bool ValidateVassalage(Kingdom helper, Kingdom helped)
	{
		return !base.ValidateVassalage(helper, helped);
	}

	public override bool ValidateWar(War war, Kingdom helper, Kingdom helped)
	{
		if (war?.defender != helped)
		{
			return false;
		}
		if (!base.ValidateWar(war, helper, helped))
		{
			return false;
		}
		return true;
	}

	public override void OnDecline()
	{
		base.OnDecline();
		_ = from;
		_ = to;
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		if (outcome.key == "rel_change_other_vassals")
		{
			Kingdom kingdom = GetTargetObj() as Kingdom;
			Kingdom kingdom2 = GetSourceObj() as Kingdom;
			for (int i = 0; i < kingdom2.vassalStates.Count; i++)
			{
				Kingdom kingdom3 = kingdom2.vassalStates[i];
				if (kingdom3 != kingdom)
				{
					Value val = outcome.CalcValue(base.game, null);
					kingdom2.AddRelationModifier(kingdom3, val, null);
				}
			}
			return true;
		}
		return base.ApplyOutcome(outcome);
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetTargetObj() as Kingdom;
		Kingdom kingdom2 = GetSourceObj() as Kingdom;
		Kingdom enemyLeader = GetArg<War>(0).GetEnemyLeader(kingdom);
		Vars vars = new Vars();
		vars.SetVar("kingdom_a", kingdom);
		vars.SetVar("kingdom_b", kingdom2);
		vars.SetVar("kingdom_c", enemyLeader);
		kingdom.game.BroadcastRadioEvent("AskForProtectionMessage", vars);
	}

	public override Object GetSourceObj()
	{
		return to;
	}

	public override Object GetTargetObj()
	{
		return from;
	}
}
