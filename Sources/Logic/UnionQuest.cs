using System.Collections.Generic;

namespace Logic;

public class UnionQuest : Quest
{
	public new static Quest Create(Def def, Object owner, Object source)
	{
		return new UnionQuest(def, owner, source);
	}

	public UnionQuest(Def def, Object owner, Object source)
		: base(def, owner, source)
	{
	}

	public new static bool CheckActivateConditions(Def def, Object obj)
	{
		if (!Quest.CheckActivateConditions(def, obj))
		{
			return false;
		}
		if (!(obj is Kingdom))
		{
			return false;
		}
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "union_kingdom")
		{
			string name = def.field.GetString("outcome.unlock_union");
			return base.game.GetKingdom(name);
		}
		return base.GetVar(key, vars, as_value);
	}

	protected override void OnComplete()
	{
		if (owner != null && owner is Kingdom)
		{
			Kingdom kingdom = owner as Kingdom;
			string text = def.field.GetString("outcome.unlock_union");
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			bool flag = def.field.GetBool("outcome.keep_old_culture");
			Value value = def.field.GetValue("outcome.rel_change_with_eligible");
			float num = value.Float();
			if (value.type == Value.Type.String)
			{
				num = KingdomAndKingdomRelation.GetValueOfModifier(base.game, value.String());
			}
			Value value2 = def.field.GetValue("outcome.crown_authority");
			int num2 = value2.Int();
			if (value2.type == Value.Type.String)
			{
				num2 = kingdom.GetCrownAuthority().def.field.GetInt(value2.String());
			}
			float num3 = def.field.GetFloat("outcome.fame");
			string nameKey = kingdom.GetNameKey();
			kingdom.ChangeNameAndCulture(text, flag ? kingdom.csv_field.key : null, kingdom.csv_field.key);
			kingdom.GetCrownAuthority().ChangeValue(num2);
			kingdom.AddFame(num3);
			List<Kingdom> list = null;
			for (int i = 0; i < base.game.kingdoms.Count; i++)
			{
				Kingdom kingdom2 = base.game.kingdoms[i];
				if (!kingdom2.IsDefeated() && kingdom2 != kingdom && kingdom2.quests != null && kingdom2.quests.Find(def) != null)
				{
					kingdom.AddRelationModifier(kingdom2, value, null);
					if (list == null)
					{
						list = new List<Kingdom>();
					}
					list.Add(kingdom2);
				}
			}
			Vars vars = new Vars();
			vars.SetVar("kingdom_a", kingdom);
			vars.SetVar("union_kingdom", kingdom);
			vars.SetVar("old_name", nameKey);
			vars.SetVar("rel_change_amount", num);
			vars.SetVar("crown_authority_change", num2);
			vars.SetVar("fame", num3);
			if (list != null)
			{
				vars.Set("eligible_kingdoms", list);
			}
			kingdom.FireEvent("unite_quest_complete", vars);
		}
		base.OnComplete();
	}

	public override void OnMessage(object obj, string message, object param)
	{
		base.OnMessage(obj, message, param);
	}
}
