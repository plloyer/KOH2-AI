using System;

namespace Logic;

public class AutocephalyAction : Action
{
	public AutocephalyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AutocephalyAction(owner as Character, def);
	}

	public override void Run()
	{
		Kingdom k = own_kingdom;
		base.own_character.EnableAging(can_die: true);
		base.game.religions.orthodox.SetSubordinated(k, subordinated: false, base.own_character);
		base.Run();
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (!(key == "rel_change_with_orthodox_constantinople"))
		{
			if (key == "rel_change_with_independent_orthodox")
			{
				Value value = outcome.field.value;
				foreach (Kingdom kingdom in own_kingdom.game.kingdoms)
				{
					if (!kingdom.IsDefeated() && kingdom.is_orthodox && !kingdom.subordinated && kingdom != own_kingdom)
					{
						own_kingdom.AddRelationModifier(kingdom, "rel_change_with_independent_orthodox", null, value);
					}
				}
				return true;
			}
			return base.ApplyOutcome(outcome);
		}
		Value value2 = outcome.field.value;
		own_kingdom.AddRelationModifier(own_kingdom.game.religions.orthodox.head_kingdom, "rel_change_with_orthodox_constantinople", null, value2);
		return true;
	}

	public static int sf_non_orthodox_provinces(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int num = factor.field.Int(sf.vars);
		if (num == 0)
		{
			return 0;
		}
		int num2 = 0;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			if (kingdom.realms[i].is_orthodox)
			{
				num2++;
			}
		}
		return -(int)((float)(num2 - kingdom.realms.Count) / (float)kingdom.realms.Count * (float)num);
	}

	public static int sf_independent_kingdoms(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int num = factor.field.Int(sf.vars);
		if (num == 0)
		{
			return 0;
		}
		int num2 = 0;
		for (int i = 0; i < kingdom.game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = kingdom.game.kingdoms[i];
			if (!kingdom2.IsDefeated() && kingdom2.is_orthodox && !kingdom2.subordinated)
			{
				num2++;
			}
		}
		return num2 * num;
	}

	public static int sf_constantinople_is_our_vassal(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		int result = 0;
		if (kingdom.game.religions.orthodox.head_kingdom?.sovereignState == kingdom)
		{
			result = factor.field.Int(sf.vars);
		}
		return result;
	}

	public static int sf_relationship_with_constantinople(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		Kingdom head_kingdom = kingdom.game.religions.orthodox.head_kingdom;
		float relationship = kingdom.GetRelationship(head_kingdom);
		int num = Math.Abs(factor.field.GetInt("max"));
		int num2 = Math.Abs(factor.field.GetInt("min"));
		float num3 = relationship / RelationUtils.Def.maxRelationship;
		num3 = ((!(relationship < 0f)) ? (num3 * (float)num) : (num3 * (float)num2));
		return (int)num3;
	}

	public static int sf_positive_relationship_with_orthodox_kingdoms(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < kingdom.game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = kingdom.game.kingdoms[i];
			if (!kingdom2.IsDefeated() && kingdom2.is_orthodox && kingdom2 != kingdom)
			{
				float relationship = kingdom2.GetRelationship(kingdom);
				if (relationship > 0f)
				{
					num2 += relationship;
				}
				num += 1f;
			}
		}
		if (num == 0f)
		{
			return 0;
		}
		float value = num2 / num;
		int num3 = factor.field.Int(sf.vars);
		return (int)(Math.Abs(value) / Math.Abs(RelationUtils.Def.minRelationship) * (float)num3);
	}

	public static int sf_negative_relationship_with_orthodox_kingdoms(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < kingdom.game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = kingdom.game.kingdoms[i];
			if (!kingdom2.IsDefeated() && kingdom2.is_orthodox && kingdom2 != kingdom)
			{
				float relationship = kingdom2.GetRelationship(kingdom);
				if (relationship < 0f)
				{
					num2 += relationship;
				}
				num += 1f;
			}
		}
		if (num == 0f)
		{
			return 0;
		}
		float value = num2 / num;
		int num3 = factor.field.Int(sf.vars);
		return (int)(Math.Abs(value) / Math.Abs(RelationUtils.Def.minRelationship) * (float)num3);
	}

	public static int sf_our_princess_in_constantinople(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		Kingdom head_kingdom = kingdom.game.religions.orthodox.head_kingdom;
		if (kingdom?.marriages == null || head_kingdom?.marriages == null || kingdom == head_kingdom)
		{
			return 0;
		}
		for (int i = 0; i < head_kingdom.marriages.Count; i++)
		{
			Marriage marriage = head_kingdom.marriages[i];
			if (!marriage.wife.IsQueen() && marriage.wife.original_kingdom_id == kingdom.id)
			{
				return factor.field.Int(sf.vars);
			}
		}
		return 0;
	}

	public static int sf_our_queen_in_constantinople(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		Kingdom head_kingdom = kingdom.game.religions.orthodox.head_kingdom;
		if (kingdom?.marriages == null || head_kingdom?.marriages == null || kingdom == head_kingdom)
		{
			return 0;
		}
		for (int i = 0; i < head_kingdom.marriages.Count; i++)
		{
			Marriage marriage = head_kingdom.marriages[i];
			if (marriage.wife.IsQueen() && marriage.wife.original_kingdom_id == kingdom.id)
			{
				return factor.field.Int(sf.vars);
			}
		}
		return 0;
	}

	public static int sf_influence_in_constantinople(SuccessAndFail sf, SuccessAndFail.Factor.Def factor)
	{
		Kingdom kingdom = sf.vars?.GetVar("src_kingdom").Get<Kingdom>();
		if (kingdom == null)
		{
			return 0;
		}
		Kingdom head_kingdom = kingdom.game.religions.orthodox.head_kingdom;
		float influenceIn = kingdom.GetInfluenceIn(head_kingdom);
		float num = factor.field.GetFloat("divider");
		return (int)(influenceIn / num);
	}
}
