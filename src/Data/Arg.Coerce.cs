using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo.Data;

public sealed partial class Arg
{
	/// <summary>
	/// Parses contents of <see cref="_str"/> to fill other fields. Sets <see cref="DataType"/> to <see cref="ArgType.STRING"/>.
	/// </summary>
	private void _parseStr()
	{
		//check vec parsn
		//_vec = default;
		//if (TryParseVec4(_str, out _vec))
		//{
		//	//_vec = vecres;
		//	_f32 = _vec.magnitude;
		//	_i32 = (int)_f32;
		//	_bool = _f32 != 0f;
		//}
		//else
		//{
		//	if (trueStrings.Contains(_str.ToLower()))
		//	{
		//		_bool = true;
		//		_f32 = 1f;
		//		_i32 = 1;
		//	}
		//	else if (falseStrings.Contains(_str.ToLower()))
		//	{
		//		_bool = false;
		//		_f32 = 0f;
		//		_i32 = 0;
		//	}
		//	else
		//	{
		//		float.TryParse(_str, out _f32);
		//		if (!int.TryParse(_str, out _i32))
		//		{
		//			_i32 = (int)_f32;
		//		}
		//	}
		//}
		Coerce_Str(_str, out _i32, out _f32, out _bool, out _vec, out bool asv);
		DataType = asv ? ArgType.VECTOR : ArgType.STRING;
	}
	internal static void Coerce_Str(
		in string s,
		out int i,
		out float f,
		out bool b,
		out Vector4 v,
		out bool parsedAsVec)
	{
		if (parsedAsVec = TryParseVec4(s, out var _v))
		{
			Coerce_Vec(_v, out _, out i, out f, out b);
			v = _v;
			
		}
		else
		{
			if (trueStrings.Contains(s.ToLower()))
			{
				b = true;
				f = 1f;
				i = 1;
			}
			else if (falseStrings.Contains(s.ToLower()))
			{
				b = false;
				f = 0f;
				i = 0;
			}
			else
			{
				float.TryParse(s, out f);
				if (!int.TryParse(s, out i))
				{
					i = (int)f;
				}
				b = i != 0;
			}
			v = default;
		}
	}
	internal static void Coerce_I32(
		in int i,
		out string s,
		out float f,
		out bool b,
		out Vector4 v)
	{
		s = i.ToString();
		f = i;
		b = i != 0;
		v = default;
	}
	internal static void Coerce_F32(
		in float f,
		out string s,
		out int i,
		out bool b,
		out Vector4 v)
	{
		i = (int)f;
		s = f.ToString();
		b = f != 0;
		v = default;
	}
	internal static void Coerce_Bool(
		in bool b,
		out string s,
		out int i,
		out float f,
		out Vector4 v)
	{
		s = b.ToString();
		i = b ? 1 : 0;
		f = b ? 1 : 0;
		v = default;
	}
	internal static void Coerce_Vec(
		in Vector4 v,
		out string s,
		out int i,
		out float f,
		out bool b
		)
	{
		f = v.magnitude;
		i = (int)f;
		b = i != 0f;
		s = $"{v.x};{v.y};{v.z};{v.w}";
	}

}
