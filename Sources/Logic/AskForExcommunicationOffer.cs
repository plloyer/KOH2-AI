using System.Collections.Generic;

namespace Logic;

public class AskForExcommunicationOffer : Offer
{
	public AskForExcommunicationOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public AskForExcommunicationOffer(Kingdom from, Kingdom to, Kingdom against)
		: base(from, to, against)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new AskForExcommunicationOffer(def, from, to);
	}

	public float GetRelationshipTreshold()
	{
		if ((GetSourceObj() as Kingdom).HasCardinal())
		{
			return def.field.GetFloat("relationship_treshold_cardinal");
		}
		return def.field.GetFloat("relationship_treshold_without_cardinal");
	}

	public override string ValidateWithoutArgs()
	{
		string text = base.ValidateWithoutArgs();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom == null || kingdom2 == null)
		{
			return "broken_fromto";
		}
		if ((from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "in_war";
		}
		if (!kingdom.is_christian)
		{
			return "not_christian";
		}
		if (kingdom.excommunicated)
		{
			text = "_excommunicated";
		}
		if (!kingdom.HasPope() && kingdom2.GetRelationship(kingdom) < GetRelationshipTreshold())
		{
			text = "_not_enough_relationship";
		}
		if (kingdom2 != kingdom2?.game?.religions?.catholic?.hq_kingdom)
		{
			return "not_papacy";
		}
		Action action = kingdom2.GetKing()?.actions?.Find("ExcommunicateAction");
		if (action == null)
		{
			return "no_action";
		}
		string text2 = action.Validate();
		if (ShouldReturn(text2))
		{
			return text2;
		}
		if (text2 != "ok" && text == "ok")
		{
			text = text2;
		}
		return text;
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if ((from as Kingdom).IsEnemy(to as Kingdom))
		{
			return "in_war";
		}
		if (!kingdom.is_christian)
		{
			return "not_christian";
		}
		if (kingdom.excommunicated)
		{
			return "_excommunicated";
		}
		if (kingdom2 != kingdom2.game.religions.catholic.hq_kingdom)
		{
			return "not_papacy";
		}
		if (kingdom2.GetRelationship(kingdom) < GetRelationshipTreshold())
		{
			return "_not_enough_relationship";
		}
		Kingdom arg = GetArg<Kingdom>(0);
		Action action = kingdom2.GetKing().actions.Find("ExcommunicateAction");
		if (action == null)
		{
			return "no_action";
		}
		string text2 = action.Validate();
		if (ShouldReturn(text2))
		{
			return text2;
		}
		if (text2 != "ok" && text == "ok")
		{
			text = text2;
		}
		if (!action.ValidateTarget(arg))
		{
			return "_invalid_args";
		}
		return text;
	}

	public override bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		Kingdom kingdom = GetSourceObj() as Kingdom;
		if (!(GetTargetObj() is Kingdom kingdom2))
		{
			return false;
		}
		Character king = kingdom2.GetKing();
		if (king == null)
		{
			return false;
		}
		Action action = king.actions.Find("ExcommunicateAction");
		if (action == null)
		{
			return false;
		}
		base.GetPossibleArgValues(idx, lst);
		List<Object> possibleTargets = action.GetPossibleTargets();
		if (possibleTargets == null)
		{
			return false;
		}
		foreach (Object item in possibleTargets)
		{
			if (item != kingdom)
			{
				lst.Add(item);
			}
		}
		return lst.Count > 0;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		if (GetTargetObj() is Kingdom kingdom)
		{
			kingdom.GetKing()?.actions.Find("ExcommunicateAction")?.Execute(GetArg<Kingdom>(0));
		}
	}

	public override bool IsValidForAI()
	{
		return true;
	}
}
