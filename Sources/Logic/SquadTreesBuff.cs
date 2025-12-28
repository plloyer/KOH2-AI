namespace Logic;

public class SquadTreesBuff : SquadBuff
{
	public class Def : Logic.Def
	{
		public int min_trees_count = 2;

		public override bool Load(Game game)
		{
			min_trees_count = base.field.GetInt("min_trees_count", null, min_trees_count);
			return base.Load(game);
		}
	}

	public Def tree_buff_def;

	public SquadTreesBuff(Squad squad, Logic.Def def, DT.Field field = null)
		: base(squad, def, field)
	{
	}

	public new static SquadBuff Create(Squad owner, Logic.Def def, DT.Field field = null)
	{
		return new SquadTreesBuff(owner, def, field);
	}

	public override void Init(Squad squad, Logic.Def def, DT.Field field = null)
	{
		tree_buff_def = def as Def;
		base.Init(squad, def, field);
	}

	public override bool Validate()
	{
		if (!base.Validate())
		{
			return false;
		}
		PPos position = squad.position;
		if (position.paID != 0)
		{
			return false;
		}
		if (squad.climbing_buff != null && squad.climbing_buff.enabled)
		{
			return false;
		}
		return squad.battle.GetTreeCount(position) >= tree_buff_def.min_trees_count;
	}

	public override string DebugUIText()
	{
		return "F";
	}
}
