using System;

namespace Logic;

[Serializable]
public class HexGrid
{
	[Serializable]
	public struct Coord
	{
		public int x;

		public int y;

		public static Coord Invalid = new Coord(-1, 0);

		public static Coord Zero = new Coord(0, 0);

		public bool valid => (x + y) % 2 == 0;

		public bool odd => x % 2 != 0;

		public Coord(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public override string ToString()
		{
			return "(" + x + ", " + y + ")";
		}

		public static bool operator ==(Coord pt1, Coord pt2)
		{
			if (pt1.x == pt2.x)
			{
				return pt1.y == pt2.y;
			}
			return false;
		}

		public static bool operator !=(Coord pt1, Coord pt2)
		{
			if (pt1.x == pt2.x)
			{
				return pt1.y != pt2.y;
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj is Coord)
			{
				return this == (Coord)obj;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return x.GetHashCode() ^ y.GetHashCode();
		}

		public static Coord operator +(Coord pt1, Coord pt2)
		{
			return new Coord(pt1.x + pt2.x, pt1.y + pt2.y);
		}

		public static Coord operator -(Coord pt1, Coord pt2)
		{
			return new Coord(pt1.x - pt2.x, pt1.y - pt2.y);
		}

		public static Coord operator *(Coord c, int i)
		{
			return new Coord(c.x * i, c.y * i);
		}

		public int Dist(Coord c2)
		{
			int num = Math.Abs(c2.x - x);
			int num2 = Math.Abs(c2.y - y);
			if (num <= num2)
			{
				return num + (num2 - num) / 2;
			}
			return num;
		}

		public void ToIdx(out int x, out int y)
		{
			x = this.x;
			y = this.y / 2;
		}

		public CubeCoord ConvertToCubeCoord()
		{
			int num = (int)Math.Floor((float)y / 2f);
			int num2 = x & 1;
			int num3 = num - (x - num2) / 2;
			return new CubeCoord(x, num3, -x - num3);
		}

		public static Coord ConvertFromCubeCoord(CubeCoord c)
		{
			return new Coord(c.x, -2 * c.z - c.x);
		}

		public Coord Rotate(int rot)
		{
			return Rotate(this, rot);
		}

		public static Coord Rotate(Coord c, int rot)
		{
			rot %= 6;
			if (rot == 0)
			{
				return c;
			}
			CubeCoord cubeCoord = c.ConvertToCubeCoord();
			int num = Math.Abs(rot);
			CubeCoord c2 = cubeCoord;
			for (int i = 0; i < num; i++)
			{
				c2 = ((rot >= 0) ? new CubeCoord(-c2.y, -c2.z, -c2.x) : new CubeCoord(-c2.z, -c2.x, -c2.y));
			}
			return ConvertFromCubeCoord(c2);
		}

		public static Coord FromIdx(int x, int y)
		{
			y *= 2;
			if (x % 2 != 0)
			{
				y++;
			}
			return new Coord(x, y);
		}

		public void IsNeighbor(Coord c, out int dir)
		{
			dir = -1;
			for (int i = 0; i < neighbor_offsets.Length; i++)
			{
				if (c.x == x + neighbor_offsets[i].x && c.y == y + neighbor_offsets[i].y)
				{
					dir = i;
					break;
				}
			}
		}

		public Point[] GetHexRowsAndCols()
		{
			return GetHexRowsAndCols(this);
		}

		public static Point[] GetHexRowsAndCols(Coord c)
		{
			return new Point[8]
			{
				new Point(3 * c.x + 1, c.y),
				new Point(3 * c.x, c.y),
				new Point(3 * c.x - 1, c.y),
				new Point(3 * c.x - 2, c.y),
				new Point(3 * c.x - 2, c.y - 1),
				new Point(3 * c.x - 1, c.y - 1),
				new Point(3 * c.x, c.y - 1),
				new Point(3 * c.x + 1, c.y - 1)
			};
		}
	}

	public struct CubeCoord
	{
		public int x;

		public int y;

		public int z;

		public CubeCoord(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	public struct VertexNeighbors
	{
		public Coord n1;

		public int d1;

		public Coord n2;

		public int d2;

		public VertexNeighbors(int x1, int y1, int d1, int x2, int y2, int d2)
		{
			n1 = new Coord(x1, y1);
			this.d1 = d1;
			n2 = new Coord(x2, y2);
			this.d2 = d2;
		}
	}

	public float tile_radius;

	public float subdivision = 0.5f;

	public const float hofs = 0.5f;

	public const float vofs = 0.866f;

	public static Point odd_ofs = new Point(1.5f, 0.866f);

	public static readonly Point[] vertex_offsets = new Point[6]
	{
		new Point(1f),
		new Point(0.5f, 0.866f),
		new Point(-0.5f, 0.866f),
		new Point(-1f),
		new Point(-0.5f, -0.866f),
		new Point(0.5f, -0.866f)
	};

	public static readonly Point[] edge_vectors = new Point[6]
	{
		new Point(-0.5f, 0.866f),
		new Point(-1f),
		new Point(-0.5f, -0.866f),
		new Point(0.5f, -0.866f),
		new Point(1f),
		new Point(0.5f, 0.866f)
	};

	public static readonly Coord[] neighbor_offsets = new Coord[6]
	{
		new Coord(1, 1),
		new Coord(0, 2),
		new Coord(-1, 1),
		new Coord(-1, -1),
		new Coord(0, -2),
		new Coord(1, -1)
	};

	public static readonly VertexNeighbors[] vertex_neighbors = new VertexNeighbors[6]
	{
		new VertexNeighbors(1, -1, 2, 1, 1, 4),
		new VertexNeighbors(1, 1, 3, 0, 2, 5),
		new VertexNeighbors(0, 2, 4, -1, 1, 0),
		new VertexNeighbors(-1, 1, 5, -1, -1, 1),
		new VertexNeighbors(-1, -1, 0, 0, -2, 2),
		new VertexNeighbors(0, -2, 1, 1, -1, 3)
	};

	public HexGrid(float tile_radius = 24f)
	{
		this.tile_radius = tile_radius;
	}

	public Point Center(Coord c)
	{
		Point point = new Point((float)(c.x / 2) * 3f, (float)(c.y / 2 * 2) * 0.866f);
		if (c.odd)
		{
			point += odd_ofs;
		}
		return point * tile_radius;
	}

	public Coord WorldToGrid(Point pt)
	{
		pt.x /= 3f * tile_radius;
		pt.y /= 1.732f * tile_radius;
		Coord result = new Coord(2 * (int)pt.x, 2 * (int)pt.y);
		Coord result2 = new Coord(result.x + 1, result.y + 1);
		Point zero = Point.Zero;
		pt.x -= (int)pt.x;
		pt.y -= (int)pt.y;
		pt.x *= 3f;
		pt.y *= 1.732f;
		if (pt.y >= 0.866f)
		{
			result.y += 2;
			zero.y += 1.732f;
		}
		if (pt.x >= odd_ofs.x)
		{
			result.x += 2;
			zero.x += 3f;
		}
		float num = pt.SqrDist(zero);
		float num2 = pt.SqrDist(odd_ofs);
		if (!(num < num2))
		{
			return result2;
		}
		return result;
	}

	public Point SubCenter(Coord c, int dir)
	{
		Point point = Center(c);
		if (dir < 0)
		{
			return point;
		}
		return point + vertex_offsets[dir] * tile_radius * (1f - subdivision * 0.5f);
	}

	public Point Vertex(Coord c, int idx)
	{
		Point result = Center(c);
		if (idx >= 0)
		{
			result += vertex_offsets[idx] * tile_radius;
		}
		return result;
	}

	public Point EdgeCenter(Coord c, int dir)
	{
		Point result = Center(c);
		if (dir >= 0)
		{
			result += (vertex_offsets[dir] + vertex_offsets[(dir + 1) % 6]) * tile_radius / 2f;
		}
		return result;
	}

	public bool Contains(Coord c, Point pt)
	{
		return WorldToGrid(pt) == c;
	}

	public Point Snap(Point pt)
	{
		return Center(WorldToGrid(pt));
	}

	public void FindNearestEdge(Point pt, out Coord c, out int dir, out float dist, byte edge_mask = byte.MaxValue)
	{
		c = WorldToGrid(pt);
		pt -= Center(c);
		pt /= tile_radius;
		dir = -1;
		dist = float.MaxValue;
		for (int i = 0; i < 6; i++)
		{
			if (((1 << i) & edge_mask) != 0)
			{
				Point point = vertex_offsets[i];
				Point point2 = vertex_offsets[(i + 1) % 6];
				Point point3 = point2 - point;
				float num = point3.y * pt.x - point3.x * pt.y + point2.x * point.y - point2.y * point.x;
				if (num < 0f)
				{
					num = 0f - num;
				}
				if (num < dist)
				{
					dir = i;
					dist = num;
				}
			}
		}
		dist *= tile_radius;
	}

	public static Coord Neighbor(Coord c, int dir)
	{
		if (dir < 0)
		{
			return c;
		}
		return c + neighbor_offsets[dir];
	}

	public static int NeighborDir(Coord c, Coord n)
	{
		int num = n.x - c.x;
		int num2 = n.y - c.y;
		if (num < 0)
		{
			if (num2 >= 0)
			{
				return 2;
			}
			return 3;
		}
		if (num == 0)
		{
			if (num2 >= 0)
			{
				if (num2 != 0)
				{
					return 1;
				}
				return -1;
			}
			return 4;
		}
		if (num2 >= 0)
		{
			return 0;
		}
		return 5;
	}

	public static int OppositeDir(int dir)
	{
		if (dir < 0)
		{
			return dir;
		}
		return (dir + 3) % 6;
	}

	public static int DirDiff(int d, int d2)
	{
		if (d < 0 || d2 < 0)
		{
			return 1;
		}
		int num = d2 - d;
		if (num > 3)
		{
			num -= 6;
		}
		return num;
	}

	public static Coord Rotate(Coord c, int steps)
	{
		int num = (c.x + c.y) / 2;
		int num2 = c.x - num;
		return neighbor_offsets[steps % 6] * num + neighbor_offsets[(5 + steps) % 6] * num2;
	}

	public static Coord Rotate(Coord c0, Coord c, int steps)
	{
		return c0 + Rotate(c - c0, steps);
	}

	public static float DirToAngle(int dir)
	{
		return 30f + (float)dir * 60f;
	}

	public static int AngleToDir(float angle)
	{
		return (int)(angle / 60f);
	}

	public static int AngleToDir(float angle, out float frac)
	{
		int num = (int)(angle / 60f);
		float num2 = angle - (float)(num * 60);
		frac = num2 / 60f;
		return num;
	}

	public void CalcDirsTowards(Coord from, Coord to, out int dir1, out int dir2, out int dir3)
	{
		int num = from.Dist(to);
		if (num == 0)
		{
			dir1 = (dir2 = (dir3 = -1));
			return;
		}
		dir1 = AngleToDir(Center(from).Heading(Center(to)), out var frac);
		if (num == 1)
		{
			dir2 = (dir3 = -1);
			return;
		}
		int num2 = (dir1 + 5) % 6;
		int num3 = (dir1 + 1) % 6;
		if (frac < 0.5f)
		{
			dir2 = num2;
			dir3 = num3;
		}
		else
		{
			dir2 = num3;
			dir3 = num2;
		}
	}
}
