using System.Collections.Generic;

namespace Logic;

public class KingdomRankingCategory : BaseObject, IVars
{
	public class Def : Logic.Def
	{
		public string Name = "";

		public List<string> rankings = new List<string>();

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			Name = field.GetString("name");
			return true;
		}

		private void LoadRankings(Game game)
		{
			rankings.Clear();
			DT.Field field = base.field.FindChild("rankings");
			if (field == null || field.children == null)
			{
				Game.Log(base.field.Path(include_file: true) + ": Category '" + Name + "' has no rankings", Game.LogType.Warning);
				return;
			}
			for (int i = 0; i < field.children.Count; i++)
			{
				DT.Field field2 = field.children[i];
				string key = field2.key;
				if (game.defs.Find<KingdomRanking.Def>(key) == null)
				{
					Game.Log(field2.Path(include_file: true) + ": Category '" + Name + "' has invalid ranking '" + key + "'", Game.LogType.Warning);
				}
				else
				{
					rankings.Add(key);
				}
			}
		}

		public override bool Validate(Game game)
		{
			if (!IsBase())
			{
				LoadRankings(game);
			}
			return true;
		}
	}

	public Def def;

	public Kingdom kingdom;

	private int score = -1;

	public Game game => kingdom.game;

	public KingdomRankingCategory(Def def, Kingdom kingdom)
	{
		this.def = def;
		this.kingdom = kingdom;
		for (int i = 0; i < def.rankings.Count; i++)
		{
			string name = def.rankings[i];
			(game.rankings?.Find(name))?.categories.Add(this);
		}
	}

	public int GetScore()
	{
		if (score < 0)
		{
			score = CalcScore();
		}
		return score;
	}

	public float GetRating()
	{
		return CalcRating();
	}

	public void ResetScore()
	{
		score = -1;
	}

	public bool LeadsAllRankings(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		List<string> rankings = def.rankings;
		if (rankings == null || rankings.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < rankings.Count; i++)
		{
			string name = rankings[i];
			KingdomRanking kingdomRanking = game.rankings?.Find(name);
			if (kingdomRanking == null)
			{
				return false;
			}
			if (kingdomRanking.GetRank(k) != 1 || kingdomRanking.GetFame(k) < kingdomRanking.def.max_fame)
			{
				return false;
			}
		}
		return true;
	}

	private int CalcScore()
	{
		List<string> rankings = def.rankings;
		if (rankings == null || rankings.Count == 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < rankings.Count; i++)
		{
			string name = rankings[i];
			KingdomRanking kingdomRanking = game.rankings?.Find(name);
			num += kingdomRanking.GetFame(kingdom);
		}
		return num;
	}

	private float CalcRating()
	{
		List<string> rankings = def.rankings;
		if (rankings == null || rankings.Count == 0)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < rankings.Count; i++)
		{
			string name = rankings[i];
			KingdomRanking kingdomRanking = game.rankings?.Find(name);
			num += kingdomRanking.GetScore(kingdom);
		}
		return num;
	}

	public void OnDestroy()
	{
		for (int i = 0; i < def.rankings.Count; i++)
		{
			string name = def.rankings[i];
			(game.rankings?.Find(name))?.categories.Remove(this);
		}
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		string text = "rankings#";
		if (key.Contains(text) && int.TryParse(key.Substring(text.Length), out var result) && result < def.rankings.Count)
		{
			return game.rankings?.Find(def.rankings[result]);
		}
		return Value.Unknown;
	}
}
