using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo.Helpers;
/// <summary>
/// Wraps a string argument for easy conversion into several other language primitives. Can be named (named arguments come in form of "name=value").
/// </summary>
public struct Arg
{
    private string _raw;
    /// <summary>
	/// Raw string previously used to create the argument.
	/// </summary>
    public string Raw
    {
        get => _raw;
        private set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            _raw = value;
            Name = null;
            var splPoint = _raw.IndexOf('=');
            if (splPoint is not -1 && splPoint < _raw.Length - 1)
            {
                Name = _raw.Substring(0, splPoint);
                value = value.Substring(splPoint + 1);
            }
            float.TryParse(value, out _f32);
            //F32 = asfloat;
            int.TryParse(value, out _i32);
            bool.TryParse(value, out _bool);
            if (Utils.trueStrings.Contains(value.ToLower())) _bool = true;
            if (Utils.falseStrings.Contains(value.ToLower())) _bool = false;
        }
    }
    /// <summary>
	/// String value of the argument. If the argument is unnamed, this is equivalent to <see cref="Raw"/>; if the argument is named, returns everything after first "=" character.
	/// </summary>
    public string Str
    {
        get => Name is null ? Raw : Raw.Substring(Name.Length + 1);
        private set
        {
            _raw = Name is null ? value : Name + "=" + value;
        }
    }
    private int _i32;
    /// <summary>
	/// Int value of the argument. 0 if int value couldn't be parsed; rounded if <see cref="Arg"/> is created from a float; 1 or 0 if created from a bool.
	/// </summary>
    public int I32 { get => _i32; private set { Str = value.ToString(); } }
    private float _f32;
    /// <summary>
	/// Float value of the argument; 0f if float value couldn't be parsed; equal to <see cref="I32"/> if <see cref="Arg"/> is created from an int (may lose precision on large values!); 1f/0f if created from a bool.
	/// </summary>
    public float F32 { get => _f32; private set { Str = value.ToString(); } }
    private bool _bool;
    /// <summary>
	/// Boolean value of the argument; false by default. False if original string is found in <see cref="Utils.falseStrings"/>, or if <see cref="Arg"/> is created from a zero int or float; True if original string is found in <see cref="Utils.trueStrings"/>, or of <see cref="Arg"/> is created from a non-zero int or float.
	/// </summary>
    public bool Bool { get => _bool; private set { Str = value.ToString(); } }
    /// <summary>
	/// Name of the argument; null if unnamed.
	/// </summary>
    public string? Name { get; private set; } = null;
    //todo: allow changing names?
    /// <summary>
	/// Creates the structure from a given string.
	/// </summary>
	/// <param name="orig">String to create argument from. Named arguments receive "name=value" type strings here. Can not be null.</param>
    public Arg(string orig!!)
    {
        _raw = orig;
        _f32 = default;
        _i32 = default;
        _bool = default;
        Raw = orig;
    }
    /// <summary>
	/// Creates the structure from a given int. Always unnamed. Mostly used for implicit casts.
	/// </summary>
	/// <param name="val"></param>
    public Arg(int val)
    {
        _i32 = val;
        _f32 = val;
        _bool = val is not 0;
        _raw = val.ToString();
    }
    /// <summary>
	/// Creates the structure from a given float. Always unnamed. Mostly used for implicit casts.
	/// </summary>
	/// <param name="val"></param>
    public Arg(float val)
    {
        _f32 = val;
        _i32 = (int)val;
        _bool = val is not 0f;
        _raw = val.ToString();
    }
    /// <summary>
	/// Creates the structure from a given bool. Always unnamed. Mostly used for implicit casts.
	/// </summary>
	/// <param name="val"></param>
    public Arg(bool val)
    {
        _bool = val;
        _f32 = val ? 1f : 0f;
        _i32 = val ? 1 : 0;
        _raw = val.ToString();
    }
    #region casts
    public static explicit operator string(Arg arg) => arg.Str;
    public static implicit operator Arg(string src) => new(src);
    public static explicit operator int(Arg arg) => arg.I32;
    public static implicit operator Arg(int src) => new(src);
    public static explicit operator float(Arg arg) => arg.F32;
    public static implicit operator Arg(float src) => new(src);
    public static explicit operator bool(Arg arg) => arg.Bool;
    public static implicit operator Arg(bool src) => new(src);
    #endregion;
}
