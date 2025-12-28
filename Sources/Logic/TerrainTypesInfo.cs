using System;
using System.IO;

namespace Logic;

public class TerrainTypesInfo
{
	public float tile_size = 10f;

	public Point world_size = Point.Zero;

	public byte[,] data;

	public void SetWorldSize(Point size)
	{
		world_size = size;
	}

	public TerrainType GetTerrainType(int x, int y)
	{
		if (data == null || x < 0 || y < 0 || x >= data.GetLength(0) || y >= data.GetLength(1))
		{
			return TerrainType.COUNT;
		}
		return (TerrainType)data[x, y];
	}

	public void WorldToGrid(Point pt, out int x, out int y)
	{
		x = (int)(pt.x / tile_size);
		y = (int)(pt.y / tile_size);
	}

	public TerrainType GetTerrainType(Point pt)
	{
		WorldToGrid(pt, out var x, out var y);
		return GetTerrainType(x, y);
	}

	public void Load(DT dt, string map_name)
	{
		try
		{
			tile_size = dt.GetFloat("TerrainTypes.tile_size");
			using FileStream input = new FileStream(ModManager.GetModdedAssetPath(Game.maps_path + map_name + "/terrain_types.bin", allow_unmodded_path: true), FileMode.Open, FileAccess.Read);
			using BinaryReader reader = new BinaryReader(input);
			Load(reader);
		}
		catch (Exception ex)
		{
			Game.Log("Error loading terrain types data: " + ex.Message, Game.LogType.Error);
		}
	}

	public void Load(BinaryReader reader)
	{
		int num = reader.ReadInt32();
		int num2 = reader.ReadInt32();
		data = new byte[num, num2];
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				byte b = reader.ReadByte();
				data[j, i] = b;
			}
		}
	}
}
