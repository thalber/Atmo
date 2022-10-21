using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo.Helpers;

public struct Arg
{
    private string _orig;
    public string Orig
    {
        get => _orig;
        private set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
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
            bool.TryParse(value, out _bool);
            if (Utils.trueStrings.Contains(value.ToLower())) _bool = true;
            if (Utils.falseStrings.Contains(value.ToLower())) _bool = false;
        }
    }
    public string Str
    {
        get => Name is null ? Orig : Orig.Substring(Name.Length + 1);
        private set
        {
            _orig = Name is null ? value : Name + "=" + value;
        }
    }
    private int _i32;
    public int I32 { get => _i32; private set { Str = value.ToString(); } }
    private float _f32;
    public float F32 { get => _f32; private set { Str = value.ToString(); } }
    private bool _bool;
    public bool Bool { get => _bool; private set { Str = value.ToString(); } }
    public string? Name { get; private set; } = null;
    public Arg(string orig!!)
    {
        _orig = orig;
        _f32 = default;
        _i32 = default;
        _bool = default;
        Orig = orig;
    }
    public Arg(int val)
    {
        _i32 = val;
        _f32 = val;
        _bool = val is not 0;
        _orig = val.ToString();
    }
    public Arg(float val)
    {
        _f32 = val;
        _i32 = (int)val;
        _bool = val is not 0f;
        _orig = val.ToString();
    }
    public Arg(bool val)
    {
        _bool = val;
        _f32 = val ? 1f : 0f;
        _i32 = val ? 1 : 0;
        _orig = val.ToString();
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
