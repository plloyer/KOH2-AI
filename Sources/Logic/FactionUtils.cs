using System;
using System.Collections.Generic;

namespace Logic;

public class FactionUtils
{
	public static Kingdom GetFactionKingdom(Game game, string kingdom_key)
	{
		if (game == null)
		{
			return null;
		}
		if (string.IsNullOrEmpty(kingdom_key))
		{
			return null;
		}
		return game.GetKingdom(kingdom_key);
	}

	private static Kingdom.Type GetType(Rebel.Def def)
	{
		Kingdom.Type result = Kingdom.Type.Regular;
		if (Enum.TryParse<Kingdom.Type>(def.fraction_type, out result))
		{
			return result;
		}
		return Kingdom.Type.RebelFaction;
	}

	private static Kingdom.Type GetType(DT.Def def)
	{
		Kingdom.Type result = Kingdom.Type.Regular;
		if (Enum.TryParse<Kingdom.Type>(def.field.GetString("fraction_type"), out result))
		{
			return result;
		}
		return Kingdom.Type.Faction;
	}

	public static void BuildFactionKingdoms(Game game)
	{
		List<DT.Def> defs = game.dt.FindDef("FactionKingdom").defs;
		for (int i = 0; i < defs.Count; i++)
		{
			DT.Def def = defs[i];
			Kingdom k = BuildFactionKingdom(game, def);
			game.AddKingdom(k);
		}
	}

	private static Kingdom BuildFactionKingdom(Game game, DT.Def def)
	{
		Kingdom kingdom = new Kingdom(game);
		kingdom.ClearAllComponents();
		kingdom.type = GetType(def);
		kingdom.Name = (kingdom.ActiveName = def.field.key);
		DT.Field field = new DT.Field(game.dt);
		field.type = "def";
		field.key = def.field.key;
		field.base_path = "Kingdom";
		kingdom.def = field;
		kingdom.Load();
		kingdom.nobility_key = def.field.GetString("nobility_key", null, "OutlawNobility");
		kingdom.nobility_level = def.field.GetString("nobility_level", null, "Duchy");
		kingdom.names_key = def.field.GetString("names_key", null, "EnglishNames");
		kingdom.map_color = def.field.GetInt("map_color");
		kingdom.primary_army_color = def.field.GetInt("primary_color");
		kingdom.secondary_army_color = def.field.GetInt("secondary_color");
		string text = def.field.GetString("available_units_set");
		if (!string.IsNullOrEmpty(text))
		{
			kingdom.units_set = text;
		}
		return kingdom;
	}

	public static DT.Field BuildKingdomDef(DT dt, DT.Def def)
	{
		return new DT.Field(dt)
		{
			type = "def",
			key = def.field.key,
			base_path = "Kingdom"
		};
	}

	public static Point MapToWorldPoint(Game g, Point pt)
	{
		if (g == null)
		{
			return Point.Zero;
		}
		int length = g.realm_id_map.GetLength(0);
		int length2 = g.realm_id_map.GetLength(1);
		float x = pt.x / (float)length * g.world_size.x;
		float y = pt.y / (float)length2 * g.world_size.y;
		return new Point(x, y);
	}

	public static Point WorldToMapPoint(Game g, Point pt)
	{
		if (g.realm_id_map == null)
		{
			return Point.Zero;
		}
		int length = g.realm_id_map.GetLength(0);
		int length2 = g.realm_id_map.GetLength(1);
		int num = (int)(pt.x * (float)length / g.world_size.x);
		int num2 = (int)(pt.y * (float)length2 / g.world_size.y);
		if (num < 0)
		{
			num = 0;
		}
		else if (num >= length)
		{
			num = length - 1;
		}
		if (num2 < 0)
		{
			num2 = 0;
		}
		else if (num2 >= length2)
		{
			num2 = length2 - 1;
		}
		return new Point(num, num2);
	}

	public static void Trace(int x0, int y0, int x1, int y1, short rid, short[,] grid, List<Point> points)
	{
		int num = grid.GetLength(0) - 1;
		int num2 = grid.GetLength(1) - 1;
		if (x0 < 1)
		{
			x0 = 1;
		}
		if (y0 < 1)
		{
			y0 = 1;
		}
		if (x0 > num - 2)
		{
			x0 = num - 2;
		}
		if (y0 > num - 2)
		{
			y0 = num - 2;
		}
		if (x1 < 1)
		{
			x1 = 1;
		}
		if (y1 < 1)
		{
			y1 = 1;
		}
		if (x1 > num2 - 2)
		{
			x1 = num2 - 2;
		}
		if (y1 > num2 - 2)
		{
			y1 = num2 - 2;
		}
		int num3 = Math.Abs(x1 - x0);
		int num4 = Math.Abs(y1 - y0);
		int num5 = x0;
		int num6 = y0;
		int num7 = 1 + num3 + num4;
		int num8 = ((x1 > x0) ? 1 : (-1));
		int num9 = ((y1 > y0) ? 1 : (-1));
		int num10 = num3 - num4;
		num3 *= 2;
		num4 *= 2;
		while (num7 > 0)
		{
			points.Add(new Point(num5, num6));
			if (grid[num5, num6] != rid)
			{
				break;
			}
			if (num10 > 0)
			{
				num5 += num8;
				num10 -= num4;
			}
			else
			{
				num6 += num9;
				num10 += num3;
			}
			num7--;
		}
	}

	internal static bool CheckStance(Kingdom k1, Kingdom k2, out RelationUtils.Stance stance)
	{
		stance = RelationUtils.Stance.Peace;
		if (k1.def.key == "Mercenary" || k2.def.key == "Mercenary")
		{
			return true;
		}
		if (k1.type == Kingdom.Type.RebelFaction || k2.type == Kingdom.Type.RebelFaction)
		{
			if (k1.type == k2.type)
			{
				stance = RelationUtils.Stance.NonAggression;
			}
			else
			{
				stance = RelationUtils.Stance.War;
			}
			return true;
		}
		if (k1.type == Kingdom.Type.ReligiousFaction || k2.type == Kingdom.Type.ReligiousFaction)
		{
			if (k1.type == k2.type)
			{
				stance = RelationUtils.Stance.NonAggression;
			}
			else
			{
				stance = RelationUtils.Stance.War;
			}
			return true;
		}
		if (k1.type == Kingdom.Type.LoyalistsFaction || k2.type == Kingdom.Type.LoyalistsFaction)
		{
			if (k1.type == k2.type)
			{
				stance = RelationUtils.Stance.NonAggression;
			}
			else
			{
				stance = RelationUtils.Stance.War;
			}
			return true;
		}
		return false;
	}
}
