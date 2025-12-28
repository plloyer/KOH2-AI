using System;
using System.Text;

namespace Logic;

public struct MemStream
{
	private byte[] buf;

	private int pos;

	private int len;

	public byte[] Buffer => buf;

	public int Position
	{
		get
		{
			return pos;
		}
		set
		{
			pos = value;
			if (pos < 0 || pos > len)
			{
				Error("Invalid stream position set");
			}
		}
	}

	public int Length
	{
		get
		{
			return len;
		}
		set
		{
			len = value;
			if (len < 0 || len > buf.Length)
			{
				Error("Invalid stream length");
			}
		}
	}

	public bool AtEnd => pos >= len;

	public int Remaining => len - pos;

	public MemStream(byte[] buf, int pos = 0, int len = -1)
	{
		if (buf == null)
		{
			Error("MemStream initialized with null");
			this.buf = null;
			this.pos = 0;
			this.len = -1;
			return;
		}
		if (len < 0)
		{
			len = buf.Length;
		}
		if (pos < 0)
		{
			pos = 0;
		}
		this.buf = buf;
		this.pos = pos;
		this.len = len;
	}

	public void Close()
	{
		buf = null;
		pos = 0;
		len = -1;
	}

	public void WriteByte(byte bt)
	{
		if (AtEnd)
		{
			Error("Write stream overflow");
		}
		else
		{
			buf[pos++] = bt;
		}
	}

	public byte ReadByte()
	{
		if (AtEnd)
		{
			Error("Read stream overflow");
			return 0;
		}
		return buf[pos++];
	}

	public void Write7BitUInt(int i)
	{
		if (i < 0)
		{
			Error("Serializing " + i + " as 7 bit unsigned");
		}
		Write7BitUInt((uint)i);
	}

	public void Write7BitUInt(uint v)
	{
		byte b;
		while (true)
		{
			b = (byte)(v & 0x7F);
			v >>= 7;
			if (v == 0)
			{
				break;
			}
			b |= 0x80;
			WriteByte(b);
		}
		WriteByte(b);
	}

	public void Write7BitSInt(int i)
	{
		int num;
		int num2;
		if (i >= 0)
		{
			num = 0;
			num2 = i;
		}
		else
		{
			num2 = -i;
			num = 1;
		}
		if ((num2 & 0x80000000u) != 0L)
		{
			Game.Log("Serializing " + i + " as 7 bit integer", Game.LogType.Warning);
		}
		num2 = (num2 << 1) | num;
		Write7BitUInt(num2);
	}

	public int Read7BitUInt_Safe()
	{
		int num = 0;
		int num2 = 0;
		while (true)
		{
			if (AtEnd)
			{
				return -1;
			}
			int num3 = ReadByte();
			num |= (num3 & 0x7F) << num2;
			if ((num3 & 0x80) == 0)
			{
				break;
			}
			num2 += 7;
		}
		return num;
	}

	public int Read7BitUInt()
	{
		int num = Read7BitUInt_Safe();
		if (num < 0)
		{
			Error("Read stream overflow");
		}
		return num;
	}

	public int Read7BitSInt_Safe()
	{
		int num = Read7BitUInt_Safe();
		if (num < 0)
		{
			Error("Read stream overflow");
		}
		int num2 = num >> 1;
		if ((num & 1) != 0)
		{
			num2 = -num2;
		}
		return num2;
	}

	public int Read7BitSInt()
	{
		return Read7BitSInt_Safe();
	}

	public void WriteString(string s)
	{
		if (s == null)
		{
			Write7BitUInt(0);
			return;
		}
		int length = s.Length;
		Write7BitUInt(length + 1);
		for (int i = 0; i < length; i++)
		{
			int i2 = s[i];
			Write7BitUInt(i2);
		}
	}

	public string ReadString()
	{
		int num = Read7BitUInt() - 1;
		if (num < 0)
		{
			return null;
		}
		if (num == 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(num);
		for (int i = 0; i < num; i++)
		{
			char value = (char)Read7BitUInt();
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	public unsafe void WriteFloat(float f)
	{
		byte* ptr = (byte*)(&f);
		for (int i = 0; i < 4; i++)
		{
			byte bt = *(ptr++);
			WriteByte(bt);
		}
	}

	public unsafe float ReadFloat()
	{
		float result = default(float);
		byte* ptr = (byte*)(&result);
		for (int i = 0; i < 4; i++)
		{
			byte b = ReadByte();
			*(ptr++) = b;
		}
		return result;
	}

	public void WriteBytes(byte[] bytes, int ofs = 0, int len = -1)
	{
		if (len < 0)
		{
			len = bytes.Length;
		}
		int num = len - ofs;
		if (num > 0)
		{
			if (num > Remaining)
			{
				Error("Write stream overflow");
				num = Remaining;
			}
			System.Buffer.BlockCopy(bytes, ofs, buf, pos, num);
			pos += num;
		}
	}

	public void CopyBytesFrom(ref MemStream stream, int cnt)
	{
		if (cnt > stream.Remaining)
		{
			Error("Read stream overflow");
			cnt = stream.Remaining;
		}
		if (cnt > Remaining)
		{
			Error("Write stream overflow");
			cnt = Remaining;
		}
		System.Buffer.BlockCopy(stream.buf, stream.pos, buf, pos, cnt);
		stream.pos += cnt;
		pos += cnt;
	}

	public override string ToString()
	{
		return "MemStream(" + pos + "/" + len + ")";
	}

	private static void Error(string msg)
	{
		Game.Log(msg, Game.LogType.Error);
	}
}
