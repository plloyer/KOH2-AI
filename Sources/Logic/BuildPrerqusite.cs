using System.Collections.Generic;

namespace Logic;

public class BuildPrerqusite
{
	private class PrerqusiteItem
	{
		public DT.Field field;

		public float quantity;
	}

	private List<PrerqusiteItem> items = new List<PrerqusiteItem>();

	public void Add(DT.Field field, float quantity)
	{
		items.Add(new PrerqusiteItem
		{
			field = field,
			quantity = quantity
		});
	}

	public bool Validate(Castle castle, Character character = null, bool is_rebel = false)
	{
		Realm realm = castle?.GetRealm();
		if (castle != null && castle.battle == null && !is_rebel && (realm.IsOccupied() || realm.IsDisorder()))
		{
			return false;
		}
		if (items == null || items.Count == 0)
		{
			return true;
		}
		if (character != null && !character.IsAlive())
		{
			return false;
		}
		for (int i = 0; i < items.Count; i++)
		{
			if (!ValidateItem(items[i], castle, character))
			{
				return false;
			}
		}
		return true;
	}

	public bool Validate(Character character = null, bool is_rebel = false)
	{
		if (items == null || items.Count == 0)
		{
			return true;
		}
		if (character != null && !character.IsAlive())
		{
			return false;
		}
		if (character.stats == null)
		{
			return false;
		}
		for (int i = 0; i < items.Count; i++)
		{
			if (character.stats.Find(items[i].field.key, must_exist: false) != null && !ValidateItem(items[i], character))
			{
				return false;
			}
		}
		return true;
	}

	private bool ValidateItem(PrerqusiteItem item, Castle castle, Character character = null)
	{
		if (castle == null)
		{
			return true;
		}
		if (item.field == null)
		{
			return true;
		}
		Building.Def def = Def.Get<Building.Def>(item.field);
		if (def != null)
		{
			return castle.HasWorkingBuilding(def);
		}
		if (castle.GetRealm().HasTag(item.field.key))
		{
			return true;
		}
		if (ValidateItem(item, character))
		{
			return true;
		}
		Realm realm = castle.GetRealm();
		if (realm != null && realm.GetVar(item.field.key).Bool())
		{
			return true;
		}
		return castle.GetKingdom().GetVar(item.field.key).Bool();
	}

	private bool ValidateItem(PrerqusiteItem item, Character character = null)
	{
		return character?.GetVar(item.field.key).Bool() ?? false;
	}

	private string GetLocalized(PrerqusiteItem item, Game game, Castle castle, Character character = null)
	{
		if (item.field == null)
		{
			return null;
		}
		if (character != null && !character.IsAlive())
		{
			return null;
		}
		DT.Field field = item.field.FindChild("tooltip");
		if (field != null)
		{
			return "{" + field.Path() + "}";
		}
		Kingdom kingdom = castle?.GetKingdom() ?? character?.GetKingdom();
		if (kingdom == null)
		{
			return "{" + item.field.key + "}";
		}
		if (character != null && game.dt.Find("CharacterStats." + item.field.key) != null)
		{
			string text = "{CharacterStats." + item.field.key + ".name}";
			DT.Field field2 = item.field.FindChild("tooltip");
			if (field2 != null)
			{
				text = "{" + field2.Path() + "}";
			}
			Stat stat = character.stats.Find(item.field.key, must_exist: false);
			if (stat != null && stat.CalcValue() > 0f)
			{
				return "{clr:sp_stat_color}" + text + "{/clr}";
			}
			return "{requirement_not_met}" + text + "{/requirement_not_met}";
		}
		ResourceInfo.Resolve(game, item.field.key, out var type, out var def);
		if (def != null)
		{
			ResourceInfo resourceInfo = ((!(type != "Resource")) ? kingdom.GetResourceInfo(item.field.key) : castle?.GetResourceInfo(item.field.key));
			if (resourceInfo != null)
			{
				string open_tag = null;
				string close_tag = null;
				resourceInfo.GetColorTags(out open_tag, out close_tag);
				string text2 = "{" + item.field.key + ":link}";
				if (open_tag != null)
				{
					text2 = text2 + "{" + open_tag + "}";
				}
				text2 = text2 + "{" + item.field.key + ".name}";
				if (close_tag != null)
				{
					text2 = text2 + "{" + close_tag + "}";
				}
				return text2 + "{/link}";
			}
		}
		bool flag = false;
		flag |= kingdom.GetVar(item.field.key).Bool();
		if (castle != null)
		{
			flag |= castle.GetRealm().GetVar(item.field.key).Bool();
		}
		string text3 = "{" + item.field.key + ".name}";
		if (!flag)
		{
			text3 = "{requirement_not_met}" + text3 + "{/requirement_not_met}";
		}
		return text3;
	}

	public string GetLoclaized(Game game, Castle castle, Character character)
	{
		if (items == null || items.Count == 0)
		{
			return null;
		}
		string text = "";
		for (int i = 0; i < items.Count; i++)
		{
			PrerqusiteItem item = items[i];
			string localized = GetLocalized(item, game, castle, character);
			if (!string.IsNullOrEmpty(localized))
			{
				if (text != "")
				{
					text += "{list_separator}";
				}
				text += localized;
			}
		}
		if (text == "")
		{
			return null;
		}
		return "@" + text;
	}

	public static BuildPrerqusite BuildUnitPrerqusite(Def unit, Game game)
	{
		if (game == null)
		{
			return null;
		}
		BuildPrerqusite buildPrerqusite = new BuildPrerqusite();
		DT.Field field = unit.field.FindChild("require");
		if (field != null && field.children != null && field.children.Count > 0)
		{
			for (int i = 0; i < field.children.Count; i++)
			{
				if (!string.IsNullOrEmpty(field.children[i].key))
				{
					DT.Field field2 = game.dt.Find(field.children[i].key);
					if (field2 != null)
					{
						buildPrerqusite.Add(field2, 0f);
					}
					else
					{
						buildPrerqusite.Add(field.children[i], 0f);
					}
				}
			}
		}
		return buildPrerqusite;
	}
}
