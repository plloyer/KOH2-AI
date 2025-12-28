namespace Logic;

public class HeightsGrid : RawGrid
{
	public new struct Data
	{
		public float terrain_size_x;

		public float terrain_size_y;

		public float tile_size_x;

		public float tile_size_y;

		public float min_height;

		public float max_height;

		public unsafe RawGrid.Data* raw_data;

		public unsafe float GetHeight(int x, int y)
		{
			if (x < 0 || x >= raw_data->width || y < 0 || y >= raw_data->height)
			{
				return 0f;
			}
			ushort val = *(ushort*)raw_data->Addr(x, y);
			return Height(val);
		}

		public unsafe void SetHeight(int x, int y, float height)
		{
			ushort num = Value(height);
			*(ushort*)raw_data->Addr(x, y) = num;
		}

		public float GetInterpolatedHeight(float wx, float wy)
		{
			wx /= tile_size_x;
			wy /= tile_size_y;
			int num = (int)wx;
			int num2 = (int)wy;
			float num3 = wx - (float)num;
			float num4 = wy - (float)num2;
			float height = GetHeight(num, num2);
			float height2 = GetHeight(num + 1, num2);
			float height3 = GetHeight(num, num2 + 1);
			float height4 = GetHeight(num + 1, num2 + 1);
			float num5 = height * (1f - num3) + height2 * num3;
			float num6 = height3 * (1f - num3) + height4 * num3;
			return num5 * (1f - num4) + num6 * num4;
		}

		public float Height(ushort val)
		{
			return min_height + (max_height - min_height) * (float)(int)val / 65535f;
		}

		public ushort Value(float height)
		{
			if (height < min_height)
			{
				height = min_height;
			}
			else if (height > max_height)
			{
				height = max_height;
			}
			return (ushort)(65535f * (height - min_height) / (max_height - min_height));
		}
	}

	public new unsafe Data* data = null;

	public unsafe void Alloc(int width, int height, float terrain_size_x, float terrain_size_y, float max_height, float min_height = 0f)
	{
		Alloc<ushort>(width, height);
		data->terrain_size_x = terrain_size_x;
		data->terrain_size_y = terrain_size_y;
		data->tile_size_x = terrain_size_x / (float)width;
		data->tile_size_y = terrain_size_y / (float)height;
		data->min_height = min_height;
		data->max_height = max_height;
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
