using System;
using Unity.Mathematics;
using UnityEngine;

namespace Logic;

[Serializable]
public struct Point
{
	public float x;

	public float y;

	public static readonly Point Invalid = default(Point);

	public static readonly Point Zero = new Point(0f, 0f);

	public static readonly Point UnitUp = new Point(0f, 1f);

	public static readonly Point UnitRight = new Point(1f);

	public Point(float x = 0f, float y = 0f)
	{
		this.x = x;
		this.y = y;
	}

	public override string ToString()
	{
		return DT.FloatToStr(x) + ", " + DT.FloatToStr(y);
	}

	public static bool TryParse(string s, out Point pt)
	{
		pt = Invalid;
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

	public static bool operator ==(Point pt1, Point pt2)
	{
		if (pt1.x == pt2.x)
		{
			return pt1.y == pt2.y;
		}
		return false;
	}

	public static bool operator !=(Point pt1, Point pt2)
	{
		if (pt1.x == pt2.x)
		{
			return pt1.y != pt2.y;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (obj is Point)
		{
			return this == (Point)obj;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return x.GetHashCode() ^ y.GetHashCode();
	}

	public static Point operator +(Point pt1, Point pt2)
	{
		return new Point(pt1.x + pt2.x, pt1.y + pt2.y);
	}

	public static Point operator -(Point pt1, Point pt2)
	{
		return new Point(pt1.x - pt2.x, pt1.y - pt2.y);
	}

	public static Point operator -(Point pt)
	{
		return new Point(0f - pt.x, 0f - pt.y);
	}

	public static Point operator *(Point pt, float f)
	{
		return new Point(pt.x * f, pt.y * f);
	}

	public static Point operator *(float f, Point pt)
	{
		return new Point(pt.x * f, pt.y * f);
	}

	public static Point operator /(Point pt, float f)
	{
		return new Point(pt.x / f, pt.y / f);
	}

	public float SqrLength()
	{
		return x * x + y * y;
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
		return num2;
	}

	public Point GetNormalized()
	{
		Point result = this;
		result.Normalize();
		return result;
	}

	public float SqrDist(Point pt)
	{
		return (pt - this).SqrLength();
	}

	public float Dist(Point pt)
	{
		return (pt - this).Length();
	}

	public bool InRange(Point pt, float range)
	{
		return SqrDist(pt) <= range * range;
	}

	public Point Right(float len = 0f)
	{
		Point result = new Point(y, 0f - x);
		if (len != 0f)
		{
			result.SetLength(len);
		}
		return result;
	}

	public float Dot(Point pt)
	{
		return x * pt.x + y * pt.y;
	}

	public float ProjLen(Point v)
	{
		v.Normalize();
		return Dot(v);
	}

	public Point Project(Point v)
	{
		v.Normalize();
		float num = Dot(v);
		return v * num;
	}

	public float Heading()
	{
		double num = Math.Atan2(y, x);
		num *= 180.0 / Math.PI;
		if (num < 0.0)
		{
			num += 360.0;
		}
		return (float)num;
	}

	public float Heading(Point to)
	{
		return (to - this).Heading();
	}

	public Point GetRotated(float sin, float cos)
	{
		return new Point(x * cos - y * sin, x * sin + y * cos);
	}

	public Point GetRotated(float angle)
	{
		double num = (double)(0f - angle) * Math.PI / 180.0;
		double num2 = Math.Sin(num);
		double num3 = Math.Cos(num);
		double num4 = (double)x * num3 - (double)y * num2;
		double num5 = (double)x * num2 + (double)y * num3;
		return new Point((float)num4, (float)num5);
	}

	public static implicit operator Point(PPos pt)
	{
		return new Point(pt.x, pt.y);
	}

	public static implicit operator Point(Point3 pt)
	{
		return new Point(pt.x, pt.z);
	}

	public static Point RandomOnUnitCircle(Game game)
	{
		Point result = new Point(game.Random(-1f, 1f), game.Random(-1f, 1f));
		result.Normalize();
		return result;
	}

	public static float AngleBetween(Point ab, Point cb)
	{
		float num = ab.x * cb.x + ab.y * cb.y;
		float num2 = ab.x * ab.x + ab.y * ab.y;
		float num3 = cb.x * cb.x + cb.y * cb.y;
		float num4 = num * num / num2 / num3;
		float num5 = 2f * num4 - 1f;
		float num6 = ((num5 <= -1f) ? 3.141592f : ((num5 >= 1f) ? 0f : ((float)Math.Acos(num5)))) / 2f * 180f / 3.141592f;
		if (num < 0f)
		{
			num6 = 180f - num6;
		}
		if (ab.x * cb.y - ab.y * cb.y < 0f)
		{
			num6 = 0f - num6;
		}
		return num6;
	}

	public static float AngleBetween(Point a, Point b, Point c)
	{
		Point ab = b - a;
		Point cb = b - c;
		return AngleBetween(ab, cb);
	}

	public static implicit operator Point(Vector3 v)
	{
		return new Point(v.x, v.z);
	}

	public static implicit operator Vector3(Point pt)
	{
		return new Vector3(pt.x, 0f, pt.y);
	}

	public static implicit operator Point(Vector2 v)
	{
		return new Point(v.x, v.y);
	}

	public static implicit operator Vector2(Point pt)
	{
		return new Vector2(pt.x, pt.y);
	}

	public static implicit operator Point(float3 v)
	{
		return new Point(v.x, v.z);
	}

	public static implicit operator float3(Point pt)
	{
		return new float3(pt.x, 0f, pt.y);
	}

	public static implicit operator Point(float2 v)
	{
		return new Point(v.x, v.y);
	}

	public static implicit operator float2(Point pt)
	{
		return new float2(pt.x, pt.y);
	}
}
