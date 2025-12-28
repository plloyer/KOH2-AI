using System.Collections.Generic;

namespace Logic;

public class RebellionRiskCategory
{
	public class Def : Logic.Def
	{
		public int total_index = -1;

		public int index = -1;

		public float min;

		public float max;

		public bool isGlobal;

		public string stat_name;

		public override bool Load(Game game)
		{
			min = base.field.GetFloat("min");
			max = base.field.GetFloat("max");
			isGlobal = base.field.GetBool("is_global");
			stat_name = base.field.GetString("stat_name");
			m_InvalidateDef = true;
			return true;
		}

		public override void Unload(Game game)
		{
			m_InvalidateDef = true;
			base.Unload(game);
		}
	}

	private static List<Def> cashed_defs = new List<Def>();

	private static bool m_InvalidateDef = false;

	public static List<Def> GetCategories(Game game)
	{
		if (m_InvalidateDef)
		{
			cashed_defs.Clear();
			DT.Def def = game.dt.FindDef("RebellionRiskCategory");
			for (int i = 0; i < def.defs.Count; i++)
			{
				Def def2 = game.defs.Find<Def>(def.defs[i].field.key);
				def2.total_index = i;
				cashed_defs.Add(def2);
			}
			m_InvalidateDef = false;
		}
		return cashed_defs;
	}
}
