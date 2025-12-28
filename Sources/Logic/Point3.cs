using System;
using Unity.Mathematics;
using UnityEngine;

namespace Logic;

[Serializable]
public struct Point3
{
	public float x;

	public float y;

	public float z;

	public static readonly Point Invalid = default(Point);

	public static readonly Point Zero = new Point(0f, 0f);

	public static readonly Point UnitUp = new Point(0f, 1f);

	public static readonly Point UnitRight = new Point(1f);

	private static readonly System.Random rnd = new System.Random();

	public Point xy
	{
		get
		{
			return new Point(x, y);
		}
		set
		{
			x = value.x;
			y = value.y;
		}
	}

	public Point xz
	{
		get
		{
			return new Point(x, z);
		}
		set
		{
			x = value.x;
			z = value.y;
		}
	}

	public Point yz
	{
		get
		{
			return new Point(y, z);
		}
		set
		{
			y = value.x;
			z = value.y;
		}
	}

	public Point3 xyz
	{
		get
		{
			return new Point3(x, y, z);
		}
		set
		{
			x = value.x;
			y = value.y;
			z = value.z;
		}
	}

	public Point3(float x = 0f, float y = 0f, float z = 0f)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public override string ToString()
	{
		return DT.FloatToStr(x) + ", " + DT.FloatToStr(y);
	}

	public static bool TryParse(string s, out Point pt)
	{
		pt = Point.Invalid;
		if (s == null)
		{
			return false;
		}
		int num = s.IndexOf(',');
		if (num <= 0)
		{
			return false;
		}
		string s2 = s.Substring(0, num).Trim();
		string text = s.Substring(num + 1).Trim();
		if (text.IndexOf(',') >= 0)
		{
			return false;
		}
		if (!DT.ParseFloat(s2, out pt.x))
		{
			return false;
		}
		if (!DT.ParseFloat(text, out pt.y))
		{
			return false;
		}
		return true;
	}

	public static bool operator ==(Point3 pt1, Point3 pt2)
	{
		if (pt1.x == pt2.x && pt1.y == pt2.y)
		{
			return pt1.z == pt2.z;
		}
		return false;
	}

	public static bool operator !=(Point3 pt1, Point3 pt2)
	{
		if (pt1.x == pt2.x && pt1.y == pt2.y)
		{
			return pt1.z != pt2.z;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (obj is Point3)
		{
			return this == (Point3)obj;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return x.GetHashCode() ^ y.GetHashCode();
	}

	public static Point3 operator +(Point3 pt1, Point3 pt2)
	{
		return new Point3(pt1.x + pt2.x, pt1.y + pt2.y, pt1.z + pt2.z);
	}

	public static Point3 operator -(Point3 pt1, Point3 pt2)
	{
		return new Point3(pt1.x - pt2.x, pt1.y - pt2.y, pt1.z - pt2.z);
	}

	public static Point3 operator -(Point3 pt)
	{
		return new Point3(0f - pt.x, 0f - pt.y, 0f - pt.z);
	}

	public static Point3 operator *(Point3 pt, float f)
	{
		return new Point3(pt.x * f, pt.y * f, pt.z * f);
	}

	public static Point3 operator *(float f, Point3 pt)
	{
		return new Point3(pt.x * f, pt.y * f, pt.z * f);
	}

	public static Point3 operator /(Point3 pt, float f)
	{
		return new Point3(pt.x / f, pt.y / f, pt.z / f);
	}

	public float SqrLength()
	{
		return x * x + y * y + z * z;
	}

	public float Length()
	{
		return (float)Math.Sqrt(SqrLength());
	}

	public void SetLength(float len)
	{
		float num = Length();
		if (num != 0f)
		{
			float num2 = len / num;
			x *= num2;
			y *= num2;
			z *= num2;
		}
	}

	public float Normalize()
	{
		float num = SqrLength();
		if (num == 0f)
		{
			return 0f;
		}
		if (num == 1f)
		{
			return 1f;
		}
		float num2 = (float)Math.Sqrt(num);
		x /= num2;
		y /= num2;
		z /= num2;
		return num2;
	}

	public Point3 GetNormalized()
	{
		Point3 result = this;
		result.Normalize();
		return result;
	}

	public float SqrDist(Point3 pt)
	{
		return (pt - this).SqrLength();
	}

	public float Dist(Point3 pt)
	{
		return (pt - this).Length();
	}

	public bool InRange(Point3 pt, float range)
	{
		return SqrDist(pt) <= range * range;
	}

	public Point3 Cross(Point3 pt)
	{
		return new Point3(y * pt.z - z * pt.y, z * pt.x - x * pt.z, x * pt.y - y * pt.x);
	}

	public float Dot(Point3 pt)
	{
		return x * pt.x + y * pt.y + z * pt.z;
	}

	public float ProjLen(Point3 v)
	{
		v.Normalize();
		return Dot(v);
	}

	public Point3 Project(Point3 v)
	{
		v.Normalize();
		float num = Dot(v);
		return v * num;
	}

	public static implicit operator Point3(Point pt)
	{
		return new Point3(pt.x, 0f, pt.y);
	}

	public static implicit operator Point3(PPos pt)
	{
		return new Point3(pt.x, 0f, pt.y);
	}

	public static Point RandomOnUnitShpere()
	{
		Point result = new Point3((float)rnd.NextDouble() * 2f - 1f, (float)rnd.NextDouble() * 2f - 1f, (float)rnd.NextDouble() * 2f - 1f);
		result.Normalize();
		return result;
	}

	public static implicit operator Point3(Vector3 v)
	{
		return new Point3(v.x, v.y, v.z);
	}

	public static implicit operator Vector3(Point3 pt)
	{
		return new Vector3(pt.x, pt.y, pt.z);
	}

	public static implicit operator Point3(Vector2 v)
	{
		return new Point3(v.x, 0f, v.y);
	}

	public static implicit operator Vector2(Point3 pt)
	{
		return new Vector2(pt.x, pt.z);
	}

	public static implicit operator Point3(float3 v)
	{
		return new Point3(v.x, v.y, v.z);
	}

	public static implicit operator float3(Point3 pt)
	{
		return new float3(pt.x, pt.y, pt.z);
	}

	public static implicit operator Point3(float2 v)
	{
		return new Point3(v.x, 0f, v.y);
	}

	public static implicit operator float2(Point3 pt)
	{
		return new float2(pt.x, pt.z);
	}
}
