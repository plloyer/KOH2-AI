namespace Logic;

public struct CachedFormationData
{
	public PPos pos;

	public Point dir;

	public int count;

	public int rows;

	public int cols;

	public float troop_radius;

	public Point spacing;

	public float spacing_mul;

	public float ratio;

	public PathData.PassableArea.Type allowed_areas;

	public int battle_side;

	public bool is_inside_wall;

	public float last_line_size;

	public float dense_width;

	public float dense_height;

	public float dense_radius;

	public float cur_width;

	public float cur_height;

	public float cur_radius;

	public float min_spacing;

	public float default_spacing;

	public float max_spacing;

	public int DefCount;

	public int MinCols;

	public int MaxCols;

	public float MinWidth;

	public float MaxWidth;

	public bool owner_exists;

	public bool owner_game_exists;

	public bool owner_game_passability_exists;

	public bool owner_game_pathfinding_data_exists;

	public static CachedFormationData FromFormation(Formation formation)
	{
		return new CachedFormationData
		{
			pos = formation.pos,
			dir = formation.dir,
			count = formation.count,
			rows = formation.rows,
			cols = formation.cols,
			troop_radius = formation.troop_radius,
			spacing = formation.spacing,
			spacing_mul = formation.spacing_mul,
			ratio = formation.ratio,
			allowed_areas = formation.allowed_areas,
			battle_side = formation.battle_side,
			is_inside_wall = formation.is_inside_wall,
			last_line_size = formation.last_line_size,
			dense_height = formation.dense_height,
			dense_width = formation.dense_width,
			dense_radius = formation.dense_radius,
			cur_width = formation.cur_width,
			cur_height = formation.cur_height,
			cur_radius = formation.cur_radius,
			min_spacing = formation.min_spacing,
			default_spacing = formation.default_spacing,
			DefCount = formation.DefCount(),
			MinCols = formation.MinCols(),
			MaxCols = formation.MaxCols(),
			MinWidth = formation.MinWidth(),
			MaxWidth = formation.MaxWidth(),
			owner_exists = (formation.owner != null),
			owner_game_exists = (formation.owner?.game != null),
			owner_game_passability_exists = (formation.owner?.game?.passability != null),
			owner_game_pathfinding_data_exists = (formation.owner?.game?.path_finding?.data != null)
		};
	}
}
