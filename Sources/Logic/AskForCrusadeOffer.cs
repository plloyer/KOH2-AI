using System.Collections.Generic;

namespace Logic;

public class AskForCrusadeOffer : Offer
{
	public AskForCrusadeOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public AskForCrusadeOffer(Kingdom from, Kingdom to, Kingdom against)
		: base(from, to, against)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new AskForCrusadeOffer(def, from, to);
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
		if (kingdom2 != kingdom2.game.religions.catholic.hq_kingdom)
		{
			return "not_papacy";
		}
		string text2 = Crusade.ValidateNew(kingdom.game, kingdom);
		if (ShouldReturn(text2))
		{
			return text2;
		}
		if (text2 != "ok" && !text2.Contains("cooldown"))
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
			text = "_excommunicated";
		}
		if (kingdom2 != kingdom2.game.religions.catholic.hq_kingdom)
		{
			return "not_papacy";
		}
		if (kingdom2.GetRelationship(kingdom) < GetRelationshipTreshold())
		{
			text = "_not_enough_relationship";
		}
		string text2 = Crusade.ValidateNew(kingdom.game, kingdom);
		if (ShouldReturn(text2))
		{
			return text2;
		}
		if (text2 != "ok" && !text2.Contains("cooldown"))
		{
			text = text2;
		}
		string text3 = Crusade.ValidateTarget(GetArg(0).obj_val as Kingdom, kingdom);
		if (ShouldReturn(text3))
		{
			return text3;
		}
		if (text3 != "ok" && text == "ok")
		{
			return text = text3;
		}
		return text;
	}

	public override bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		base.GetPossibleArgValues(idx, lst);
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Game game = kingdom.game;
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = game.kingdoms[i];
			if (!kingdom2.IsDefeated() && !(Crusade.ValidateTarget(kingdom2, kingdom) != "ok") && kingdom2.IsEnemy(kingdom))
			{
				lst.Add(kingdom2);
			}
		}
		return lst.Count > 0;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom helping_kingdom = GetSourceObj() as Kingdom;
		Crusade.Start(GetArg(0).obj_val as Kingdom, helping_kingdom);
	}

	public override bool IsValidForAI()
	{
		return true;
	}
}
