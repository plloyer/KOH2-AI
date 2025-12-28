using System;
using System.Collections.Generic;

namespace Logic;

public class ObjGrid
{
	public struct Cell
	{
		public List<object> objects;
	}

	private Cell[,] cells;

	public Coord size;

	public bool init;

	public float tileSize;

	private List<Coord> toAdd = new List<Coord>();

	public ObjGrid(float realX, float realY, float tileSize)
	{
		size = new Coord((int)(realX / tileSize), (int)(realY / tileSize));
		this.tileSize = tileSize;
		Init();
	}

	public ObjGrid(Coord size, float tileSize)
	{
		this.size = size;
		this.tileSize = tileSize;
		Init();
	}

	public void Init()
	{
		cells = new Cell[size.x, size.y];
		for (int i = 0; i < size.y; i++)
		{
			for (int j = 0; j < size.x; j++)
			{
				cells[j, i].objects = new List<object>();
			}
		}
		init = true;
	}

	public bool inBounds(Coord coord)
	{
		if (coord.x >= 0 && coord.x < size.x && coord.y >= 0)
		{
			return coord.y < size.y;
		}
		return false;
	}

	public static Point LocalizedPoint(Point p)
	{
		return new Point((p.x >= 0f) ? (p.x % 1f) : (1f + p.x % 1f), (p.y >= 0f) ? (p.y % 1f) : (1f + p.y % 1f));
	}

	public static Point LocalizedPoint(Point p, float tileSize)
	{
		return LocalizedPoint(p / tileSize);
	}

	public Cell Get(Coord coord)
	{
		return cells[coord.x, coord.y];
	}

	public static Coord WorldToGrid(float tileSize, Point point, out Point local)
	{
		local = LocalizedPoint(point, tileSize);
		return WorldToGrid(tileSize, point);
	}

	public static Coord WorldToGrid(float tileSize, Point point)
	{
		return new Coord((int)Math.Floor(point.x / tileSize), (int)Math.Floor(point.y / tileSize));
	}

	public static Point GridToWorld(float tileSize, Coord coord)
	{
		return new Point((float)coord.x * tileSize, (float)coord.y * tileSize);
	}

	public Coord WorldToGrid(Point point)
	{
		return new Coord((int)(point.x / tileSize), (int)(point.y / tileSize));
	}

	public Point GridToWorld(Coord coord)
	{
		return new Point((float)coord.x * tileSize, (float)coord.y * tileSize);
	}

	public void Add(object obj, Coord coord)
	{
		Get(coord).objects.Add(obj);
	}

	public void Add(object o, List<Coord> coords)
	{
		for (int i = 0; i < coords.Count; i++)
		{
			Add(o, coords[i]);
		}
	}

	public void Add(object obj, Point pt)
	{
		Add(obj, WorldToGrid(pt));
	}

	public void Add(object obj, Point pt, float r)
	{
		toAdd.Clear();
		ListCells(pt, r, toAdd);
		Add(obj, toAdd);
	}

	public void Add(object obj, Coord min, Coord max)
	{
		for (int i = min.y; i < max.y; i++)
		{
			for (int j = min.x; j < max.x; j++)
			{
				Add(obj, new Coord(j, i));
			}
		}
	}

	public void Add(object obj, Point min, Point max)
	{
		Add(obj, WorldToGrid(min), WorldToGrid(max));
	}

	public void Add(object obj, Point pt1, Point pt2, float r)
	{
		toAdd.Clear();
		RayTrace(pt1, pt2, r, toAdd);
		Add(obj, toAdd);
	}

	public void Delete(object obj, Coord coord)
	{
		Get(coord).objects.Remove(obj);
	}

	public void Delete(object obj, List<Coord> coords)
	{
		for (int i = 0; i < coords.Count; i++)
		{
			Delete(obj, coords[i]);
		}
	}

	public void Delete(object obj, Point pt)
	{
		Delete(obj, WorldToGrid(pt));
	}

	public void Delete(object obj, Point pt, float r)
	{
		toAdd.Clear();
		ListCells(pt, r, toAdd);
		Delete(obj, toAdd);
	}

	public void Delete(object obj, Coord min, Coord max)
	{
		for (int i = min.y; i < max.y; i++)
		{
			for (int j = min.x; j < max.x; j++)
			{
				Delete(obj, new Coord(j, i));
			}
		}
	}

	public void Delete(object obj, Point min, Point max)
	{
		Delete(obj, WorldToGrid(min), WorldToGrid(max));
	}

	public void Delete(object obj, Point pt1, Point pt2, float r)
	{
		toAdd.Clear();
		RayTrace(pt1, pt2, r, toAdd);
		Delete(obj, toAdd);
	}

	public static bool AddUnique(List<Coord> coords, Coord coord)
	{
		for (int i = 0; i < coords.Count; i++)
		{
			if (coords[i] == coord)
			{
				return false;
			}
		}
		coords.Add(coord);
		return true;
	}

	public void ListCells(Point pt, float r, List<Coord> coords)
	{
		ListCells(tileSize, pt, r, coords);
	}

	public static void ListCells(float tileSize, Point pt, float r, List<Coord> coords)
	{
		for (int i = (int)Math.Floor((pt.x - r) / tileSize); i <= (int)((pt.x + r) / tileSize); i++)
		{
			for (int j = (int)Math.Floor((pt.y - r) / tileSize); j <= (int)((pt.y + r) / tileSize); j++)
			{
				AddUnique(coords, new Coord(i, j));
			}
		}
	}

	public static bool RayStep(ref Coord tile, ref Point ptLocal, ref Point destLocal, float r, List<Coord> coords)
	{
		if (destLocal.x >= 0f && destLocal.x <= 1f && destLocal.y >= 0f && destLocal.y <= 1f)
		{
			return false;
		}
		Point point = destLocal - ptLocal;
		float num = 0f;
		float num2 = 0f;
		if (point.x > 0f)
		{
			num = 1f - ptLocal.x;
			num2 = ptLocal.y + point.y * num / point.x;
			if (num2 > 1f)
			{
				num = 1f - ptLocal.y;
				num2 = ptLocal.x + point.x * num / point.y;
				ptLocal = new Point(num2);
				destLocal = new Point(destLocal.x, destLocal.y - 1f);
				if (ptLocal.x + r > 1f)
				{
					AddUnique(coords, new Coord(tile.x + 1, tile.y + 1));
					AddUnique(coords, new Coord(tile.x + 1, tile.y));
				}
				if (ptLocal.x - r < 0f)
				{
					AddUnique(coords, new Coord(tile.x - 1, tile.y + 1));
					AddUnique(coords, new Coord(tile.x - 1, tile.y));
				}
				tile = new Coord(tile.x, tile.y + 1);
			}
			else if (num2 < 0f)
			{
				num = 0f - ptLocal.y;
				num2 = ptLocal.x + point.x * num / point.y;
				ptLocal = new Point(num2, 1f);
				destLocal = new Point(destLocal.x, destLocal.y + 1f);
				if (ptLocal.x + r > 1f)
				{
					AddUnique(coords, new Coord(tile.x + 1, tile.y - 1));
					AddUnique(coords, new Coord(tile.x + 1, tile.y));
				}
				if (ptLocal.x - r < 0f)
				{
					AddUnique(coords, new Coord(tile.x - 1, tile.y - 1));
					AddUnique(coords, new Coord(tile.x - 1, tile.y));
				}
				tile = new Coord(tile.x, tile.y - 1);
			}
			else
			{
				ptLocal = new Point(0f, num2);
				destLocal = new Point(destLocal.x - 1f, destLocal.y);
				if (ptLocal.y + r > 1f)
				{
					AddUnique(coords, new Coord(tile.x, tile.y + 1));
					AddUnique(coords, new Coord(tile.x + 1, tile.y + 1));
				}
				if (ptLocal.y - r < 0f)
				{
					AddUnique(coords, new Coord(tile.x, tile.y - 1));
					AddUnique(coords, new Coord(tile.x + 1, tile.y - 1));
				}
				tile = new Coord(tile.x + 1, tile.y);
			}
		}
		else if (point.x < 0f)
		{
			num = 0f - ptLocal.x;
			num2 = ptLocal.y + point.y * num / point.x;
			if (num2 > 1f)
			{
				num = 1f - ptLocal.y;
				num2 = ptLocal.x + point.x * num / point.y;
				ptLocal = new Point(num2);
				destLocal = new Point(destLocal.x, destLocal.y - 1f);
				if (ptLocal.x + r > 1f)
				{
					AddUnique(coords, new Coord(tile.x + 1, tile.y + 1));
					AddUnique(coords, new Coord(tile.x + 1, tile.y));
				}
				if (ptLocal.x - r < 0f)
				{
					AddUnique(coords, new Coord(tile.x - 1, tile.y + 1));
					AddUnique(coords, new Coord(tile.x - 1, tile.y));
				}
				tile = new Coord(tile.x, tile.y + 1);
			}
			else if (num2 < 0f)
			{
				num = 0f - ptLocal.y;
				num2 = ptLocal.x + point.x * num / point.y;
				ptLocal = new Point(num2, 1f);
				destLocal = new Point(destLocal.x, destLocal.y + 1f);
				if (ptLocal.x + r > 1f)
				{
					AddUnique(coords, new Coord(tile.x + 1, tile.y - 1));
					AddUnique(coords, new Coord(tile.x + 1, tile.y));
				}
				if (ptLocal.x - r < 0f)
				{
					AddUnique(coords, new Coord(tile.x - 1, tile.y - 1));
					AddUnique(coords, new Coord(tile.x - 1, tile.y));
				}
				tile = new Coord(tile.x, tile.y - 1);
			}
			else
			{
				ptLocal = new Point(1f, num2);
				destLocal = new Point(destLocal.x + 1f, destLocal.y);
				if (ptLocal.y + r > 1f)
				{
					AddUnique(coords, new Coord(tile.x, tile.y + 1));
					AddUnique(coords, new Coord(tile.x - 1, tile.y + 1));
				}
				if (ptLocal.y - r < 0f)
				{
					AddUnique(coords, new Coord(tile.x, tile.y - 1));
					AddUnique(coords, new Coord(tile.x - 1, tile.y - 1));
				}
				tile = new Coord(tile.x - 1, tile.y);
			}
		}
		else if (point.y > 0f)
		{
			ptLocal = new Point(ptLocal.x);
			if (ptLocal.x - r < 0f)
			{
				AddUnique(coords, new Coord(tile.x - 1, tile.y + 1));
				AddUnique(coords, new Coord(tile.x - 1, tile.y));
			}
			if (ptLocal.x + r > 1f)
			{
				AddUnique(coords, new Coord(tile.x + 1, tile.y + 1));
				AddUnique(coords, new Coord(tile.x + 1, tile.y));
			}
			tile = new Coord(tile.x, tile.y + 1);
			destLocal = new Point(destLocal.x, destLocal.y - 1f);
		}
		else
		{
			if (!(point.y < 0f))
			{
				return false;
			}
			ptLocal = new Point(ptLocal.x, 1f);
			if (ptLocal.x - r < 0f)
			{
				AddUnique(coords, new Coord(tile.x - 1, tile.y - 1));
				AddUnique(coords, new Coord(tile.x - 1, tile.y));
			}
			if (ptLocal.x + r > 1f)
			{
				AddUnique(coords, new Coord(tile.x + 1, tile.y - 1));
				AddUnique(coords, new Coord(tile.x + 1, tile.y));
			}
			tile = new Coord(tile.x, tile.y - 1);
			destLocal = new Point(destLocal.x, destLocal.y + 1f);
		}
		AddUnique(coords, tile);
		return true;
	}

	public static void RayTrace(float tileSize, Point pt, Point dest, float r, List<Coord> coords)
	{
		ListCells(tileSize, pt, r, coords);
		Point local;
		Coord tile = WorldToGrid(tileSize, pt, out local);
		Point destLocal = (dest - GridToWorld(tileSize, tile)) / tileSize;
		bool flag = false;
		while (RayStep(ref tile, ref local, ref destLocal, r / tileSize, coords))
		{
		}
	}

	public void RayTrace(Point pt, Point dest, float r, List<Coord> coords)
	{
		RayTrace(tileSize, pt, dest, r, coords);
	}

	public void Move(object obj, Coord oldCoord, Coord newCoord)
	{
		if (!(oldCoord == newCoord))
		{
			Delete(obj, oldCoord);
			Add(obj, newCoord);
		}
	}
}
