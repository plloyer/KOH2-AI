namespace Logic;

public struct CalcLineTransformData
{
	public PPos pos;

	public Point dir;

	public PathData.PassableArea.Type allowed_areas;

	public float troop_radius;

	public int battle_side;

	public bool is_inside_wall;

	public float MaxWidth;

	public bool owner_game_passability_exists;

	public PathData.DataPointers data;

	public static CalcLineTransformData FromFormation(Formation formation)
	{
		return new CalcLineTransformData
		{
			pos = formation.pos,
			dir = formation.dir,
			allowed_areas = formation.allowed_areas,
			battle_side = formation.battle_side,
			is_inside_wall = formation.is_inside_wall,
			troop_radius = formation.troop_radius,
			MaxWidth = formation.MaxWidth(),
			owner_game_passability_exists = (formation.owner?.game?.passability != null),
			data = formation.owner.game.path_finding.data.pointers
		};
	}
}
