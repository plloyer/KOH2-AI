using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Logic;

public class RawGrid
{
	public struct Data
	{
		public short width;

		public short height;

		public short blocks_x;

		public short blocks_y;

		public short block_width;

		public short block_height;

		public short sizeof_tile;

		public short sizeof_block;

		public unsafe byte* data;

		public unsafe void* Addr(int x, int y)
		{
			Clamp(ref x, ref y);
			byte* block = ToBlock(ref x, ref y);
			return Addr(block, x, y);
		}

		public bool Contains(int x, int y)
		{
			if (x >= 0 && x < width && y >= 0)
			{
				return y < height;
			}
			return false;
		}

		public void Clamp(ref int x, ref int y)
		{
			if (x < 0)
			{
				x = 0;
			}
			else if (x >= width)
			{
				x = width - 1;
			}
			if (y < 0)
			{
				y = 0;
			}
			else if (y >= height)
			{
				y = height - 1;
			}
		}

		private unsafe byte* ToBlock(ref int x, ref int y)
		{
			int num = x / block_width;
			int num2 = y / block_height;
			x %= block_width;
			y %= block_height;
			return data + (num2 * blocks_x + num) * sizeof_block;
		}

		private unsafe byte* Addr(byte* block, int x, int y)
		{
			return block + (y * block_width + x) * sizeof_tile;
		}
	}

	public const int cache_line_size = 64;

	private byte[] bytes;

	private GCHandle gc_handle;

	private int offset;

	public unsafe Data* data = null;

	public unsafe Data* RawData()
	{
		return data;
	}

	private void AllocAligned(int size)
	{
		bytes = new byte[size + 64];
		offset = 0;
		gc_handle = AllocationManager.AllocPinned(bytes);
		int num = (int)((long)gc_handle.AddrOfPinnedObject() % 64);
		if (num != 0)
		{
			offset = 64 - num;
		}
	}

	public unsafe void Alloc<T>(int width, int height, int block_width = 0, int block_height = 0) where T : struct
	{
		Dispose();
		int num = Marshal.SizeOf<T>();
		if (block_height == 0)
		{
			if (block_width == 0)
			{
				int num2 = (int)Math.Sqrt(64 / num);
				block_width = num2;
				int num3 = 64 % (num2 * num);
				if (num3 != 0)
				{
					while (num2 > 1)
					{
						num2--;
						int num4 = 64 % (num2 * num);
						if (num4 < num3)
						{
							num3 = num4;
							block_width = num2;
							if (num3 == 0)
							{
								break;
							}
						}
					}
				}
			}
			block_height = 64 / (block_width * num);
		}
		int num5 = block_width * block_height * num;
		int num6 = width / block_width;
		if (width % block_width != 0)
		{
			num6++;
		}
		int num7 = height / block_height;
		if (height % block_height != 0)
		{
			num7++;
		}
		AllocAligned(64 + num6 * num7 * num5);
		SetData();
		data->width = (short)width;
		data->height = (short)height;
		data->sizeof_tile = (short)num;
		data->block_width = (short)block_width;
		data->block_height = (short)block_height;
		data->blocks_x = (short)num6;
		data->blocks_y = (short)num7;
		data->sizeof_block = (short)num5;
	}

	protected unsafe virtual void SetData()
	{
		byte* ptr = (byte*)(void*)gc_handle.AddrOfPinnedObject();
		ptr = (byte*)(data = (Data*)(ptr + offset));
		data->data = ptr + 64;
	}

	public void Load(string file_name)
	{
		Dispose();
		using FileStream fileStream = File.OpenRead(file_name);
		int num = (int)fileStream.Length;
		AllocAligned(num);
		fileStream.Read(bytes, offset, num);
		SetData();
	}

	public void Save(string file_name)
	{
		using FileStream fileStream = File.OpenWrite(file_name);
		fileStream.Write(bytes, offset, bytes.Length - 64);
	}

	public unsafe virtual void Dispose()
	{
		if (data != null)
		{
			AllocationManager.Free(ref gc_handle);
			bytes = null;
			data = null;
		}
	}
}
