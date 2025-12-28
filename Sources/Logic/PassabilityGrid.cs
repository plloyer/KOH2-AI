namespace Logic;

public class PassabilityGrid : RawGrid
{
	public new struct Data
	{
		public float tile_size;

		public unsafe RawGrid.Data* raw_data;

		public unsafe bool IsPassable(Coord tile)
		{
			int mask;
			return (*Resolve(tile.x, tile.y, out mask) & mask) != 0;
		}

		public bool IsPassable(Point ptw)
		{
			Coord tile = Coord.WorldToGrid(ptw, tile_size);
			return IsPassable(tile);
		}

		public Point Trace(Point from, Point to, float r = 0f)
		{
			Coord coord = Coord.WorldToGrid(from, tile_size);
			if (!IsPassable(coord))
			{
				return from;
			}
			Point ptLocal = Coord.WorldToLocal(coord, from, tile_size);
			Point destLocal = Coord.WorldToLocal(coord, to, tile_size);
			r /= tile_size;
			Coord tile;
			Coord coord2 = (tile = coord);
			int num = 0;
			Coord t;
			Coord t2;
			while (Coord.RayStep(ref tile, ref ptLocal, ref destLocal, 0.1f, out t, out t2))
			{
				if (++num > 100)
				{
					return to;
				}
				bool flag = IsPassable(tile);
				if (r > 0f)
				{
					if (flag && t.valid && !IsPassable(t))
					{
						flag = false;
					}
					if (flag && t2.valid && !IsPassable(t2))
					{
						flag = false;
					}
				}
				if (flag)
				{
					coord2 = tile;
					continue;
				}
				if (coord2 == coord)
				{
					return from;
				}
				return Coord.GridCenterToWorld(coord2, tile_size);
			}
			return to;
		}

		public unsafe void SetPassable(int x, int y, bool passable)
		{
			int mask;
			int* ptr = Resolve(x, y, out mask);
			if (passable)
			{
				*ptr |= mask;
			}
			else
			{
				*ptr &= ~mask;
			}
		}

		private unsafe int* Resolve(int x, int y, out int mask)
		{
			int x2 = x / 32;
			mask = 1 << x % 32;
			return (int*)raw_data->Addr(x2, y);
		}
	}

	public new unsafe Data* data = null;

	public unsafe void Alloc(int width, int height, float tile_size)
	{
		if (width % 32 != 0)
		{
			width += 32 - width % 32;
		}
		Alloc<int>(width / 32, height, 1);
		data->tile_size = tile_size;
		data->raw_data = RawData();
	}

	public unsafe override void Dispose()
	{
		base.Dispose();
		data = null;
	}

	protected unsafe override void SetData()
	{
		base.SetData();
		data = (Data*)(RawData() + 1);
		data->raw_data = RawData();
	}
}
