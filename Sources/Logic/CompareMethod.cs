using System.Collections.Generic;

namespace Logic;

public class QuestConditionDef
{
	private enum CompareMethod
	{
		Equal,
		NotEqual,
		Less,
		LessOrEqual,
		More,
		MoreOrEqual
	}

	public DT.Field def;

	private List<DT.Field> conditions;

	public void Load(DT.Field field)
	{
		List<DT.Field> list = field.Children();
		if (list != null)
		{
			conditions = new List<DT.Field>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				AddCondition(list[i], conditions);
			}
		}
	}

	private void AddCondition(DT.Field field, List<DT.Field> conditions)
	{
		if (field != null && !string.IsNullOrEmpty(field.key))
		{
			if (conditions == null)
			{
				conditions = new List<DT.Field>();
			}
			conditions.Add(field);
		}
	}

	public bool Validate(Object source, Object target, IVars vars)
	{
		for (int i = 0; i < conditions.Count; i++)
		{
			if (!ValidateCondition(conditions[i], source, target, vars))
			{
				return false;
			}
		}
		return true;
	}

	public float GetProgress(Object source, Object target, IVars vars)
	{
		if (conditions == null || conditions.Count == 0)
		{
			return 0f;
		}
		int num = 0;
		for (int i = 0; i < conditions.Count; i++)
		{
			if (ValidateCondition(conditions[i], source, target, vars))
			{
				num++;
			}
		}
		return (float)num / (float)conditions.Count;
	}

	public bool CheckProgress(DT.Field field, Object source)
	{
		return ValidateCondition(field, source, null, null);
	}

	public bool CheckOwnRealmsProgress(DT.Field fieldChild, Object source)
	{
		if (!(source is Kingdom kingdom))
		{
			return false;
		}
		if (kingdom.realms.Find((Realm x) => x.def.key == fieldChild.key) == null)
		{
			return false;
		}
		return true;
	}

	private bool ValidateCondition(DT.Field field, Object source, Object target, IVars vars)
	{
		CompareMethod comapreMethod = GetComapreMethod(field);
		float arg = field.Float();
		string key = field.key;
		switch (key)
		{
		case "active_period":
		{
			if (field == null || field.NumValues() == 0)
			{
				return true;
			}
			for (int num4 = 0; num4 < field.NumValues(); num4++)
			{
				if (Game.MatchPeriod(field.Value(num4).String(), source?.game?.map_period))
				{
					return true;
				}
			}
			return false;
		}
		case "eligable_kingdoms":
		{
			if (!(source is Kingdom kingdom7))
			{
				return false;
			}
			List<DT.Field> list3 = field.Children();
			if (list3 == null)
			{
				return false;
			}
			for (int num5 = 0; num5 < list3.Count; num5++)
			{
				if (list3[num5].key == kingdom7.Name)
				{
					return true;
				}
			}
			return false;
		}
		case "excluded_kingdoms":
		{
			if (!(source is Kingdom kingdom9))
			{
				return true;
			}
			List<DT.Field> list4 = field.Children();
			if (list4 == null)
			{
				return true;
			}
			for (int num7 = 0; num7 < list4.Count; num7++)
			{
				if (list4[num7].key == kingdom9.Name)
				{
					return false;
				}
			}
			return true;
		}
		case "own_realms":
		{
			if (!(source is Kingdom kingdom4))
			{
				return false;
			}
			List<DT.Field> list2 = field.Children();
			if (list2 == null)
			{
				return false;
			}
			for (int num2 = 0; num2 < list2.Count; num2++)
			{
				DT.Field cf = list2[num2];
				if (kingdom4.realms.Find((Realm x) => x.def.key == cf.key) == null)
				{
					return false;
				}
			}
			return true;
		}
		case "eligable_realms":
		{
			Kingdom k = source as Kingdom;
			if (k == null)
			{
				return false;
			}
			List<DT.Field> list = field.Children();
			if (list == null)
			{
				return false;
			}
			if (k.realms.Count > 0)
			{
				for (int i = 0; i < k.realms.Count; i++)
				{
					Realm r = k.realms[i];
					if (list.Find((DT.Field x) => x.key == r.name) != null)
					{
						return true;
					}
				}
			}
			else if (list.Find((DT.Field x) => x.key == k.Name) != null)
			{
				return true;
			}
			return false;
		}
		case "no_own_kingdom":
		{
			Kingdom kingdom6 = source as Kingdom;
			string text4 = field?.Value().String();
			if (kingdom6 != null && text4 != null && kingdom6.Name == text4)
			{
				return false;
			}
			return true;
		}
		case "no_existing_kingdom":
		{
			string text5 = field?.Value().String();
			if (text5 == null || source?.game?.kingdoms == null)
			{
				return false;
			}
			for (int num6 = 0; num6 < source.game.kingdoms.Count; num6++)
			{
				Kingdom kingdom8 = source.game.kingdoms[num6];
				if (!kingdom8.IsDefeated() && kingdom8.ActiveName == text5)
				{
					return false;
				}
			}
			return true;
		}
		case "num_realms":
			if (!(source is Kingdom kingdom5))
			{
				return false;
			}
			return Compare(comapreMethod, kingdom5.realms.Count, arg);
		case "num_wars":
			if (!(source is Kingdom kingdom3))
			{
				return false;
			}
			return Compare(comapreMethod, kingdom3.wars.Count, arg);
		case "own_trade_center":
			if (!(source is Kingdom kingdom))
			{
				return false;
			}
			return Compare(comapreMethod, kingdom.wars.Count, arg);
		case "culture":
		{
			if (field == null || field.NumValues() == 0)
			{
				return true;
			}
			string text3 = (source as Kingdom)?.culture;
			for (int num3 = 0; num3 < field.NumValues(); num3++)
			{
				if (field.Value(num3).String() == text3)
				{
					return true;
				}
			}
			return false;
		}
		case "religion":
		{
			Kingdom kingdom2 = source as Kingdom;
			if (!(kingdom2?.GetVar("religion").obj_val is Religion))
			{
				return false;
			}
			string text = field.String();
			string text2 = "";
			int num = text.IndexOf('.');
			if (num >= 0)
			{
				text2 = text.Substring(num + 1);
				text = text.Substring(0, num);
			}
			if (kingdom2.religion.def.field.key != text)
			{
				return false;
			}
			if (!string.IsNullOrEmpty(text2))
			{
				switch (text2)
				{
				case "I":
					if (text != "Orthodox")
					{
						return false;
					}
					if (kingdom2.subordinated)
					{
						return false;
					}
					break;
				case "E":
					if (text != "Catholic")
					{
						return false;
					}
					if (!kingdom2.excommunicated)
					{
						return false;
					}
					break;
				case "C":
					if (!kingdom2.is_muslim || !kingdom2.IsCaliphate())
					{
						return false;
					}
					break;
				}
			}
			return true;
		}
		default:
		{
			Value var = source.GetVar(key);
			if (!var.is_valid)
			{
				return false;
			}
			return Compare(comapreMethod, var, arg);
		}
		case "active_quests_for_categroy":
			return false;
		}
	}

	private bool Compare(CompareMethod cm, float arg1, float arg2)
	{
		return cm switch
		{
			CompareMethod.Equal => arg1 == arg2, 
			CompareMethod.NotEqual => arg1 != arg2, 
			CompareMethod.Less => arg1 < arg2, 
			CompareMethod.LessOrEqual => arg1 <= arg2, 
			CompareMethod.More => arg1 > arg2, 
			CompareMethod.MoreOrEqual => arg1 >= arg2, 
			_ => false, 
		};
	}

	private CompareMethod GetComapreMethod(DT.Field field)
	{
		if (field == null)
		{
			return CompareMethod.Equal;
		}
		return field.type switch
		{
			"equal" => CompareMethod.Equal, 
			"not_equal" => CompareMethod.NotEqual, 
			"less" => CompareMethod.Less, 
			"less_or_euqal" => CompareMethod.LessOrEqual, 
			"more" => CompareMethod.More, 
			"more_or_equal" => CompareMethod.MoreOrEqual, 
			_ => CompareMethod.Equal, 
		};
	}
}
