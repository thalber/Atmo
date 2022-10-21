using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo.Helpers;

public struct ArgWrap
{
	private string _orig;
	public string Orig
	{
		get => _orig;
		private set
		{
			_orig = value;
			Name = null;
			var splPoint = _orig.IndexOf('=');
			if (splPoint is not -1 && splPoint < _orig.Length - 1)
			{
				Name = _orig.Substring(0, splPoint);
				value = value.Substring(splPoint + 1);
			}
			float.TryParse(value, out _f32);
			//F32 = asfloat;
			int.TryParse(value, out _i32);
			//I32 = asInt;
		}
	}
	public string Str
	{
		get => Name is null ? Orig : Orig.Substring(Name.Length + 1);
		private set
		{
			Orig = Name is null ?  value : Name + "=" + value;
		}
	}
	private int _i32;
	public int I32 { get => _i32; private set { Str = value.ToString(); } }
	private float _f32;
	public float F32 { get => _f32; private set { Str = value.ToString(); } }
	public string? Name { get; private set; } = null;
	public ArgWrap(string orig)
	{
		_orig = null;
		_f32 = default;
		_i32 = default;
		Orig = orig;
	}
	#region casts
	public static explicit operator string(ArgWrap arg) => arg.Str;
	public static implicit operator ArgWrap(string src) => new(src);
	public static explicit operator int(ArgWrap arg) => arg.I32;
	public static implicit operator ArgWrap(int src) => new(src.ToString());
	public static explicit operator float(ArgWrap arg) => arg.F32;
	public static implicit operator ArgWrap(float src) => new(src.ToString());
	#endregion;
}
