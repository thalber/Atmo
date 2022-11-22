using System.Text;

namespace Atmo.Data;
/// <summary>
/// Wraps a string argument for easy conversion into several other language primitives. Can be named (named arguments come in form of "name=value").
/// <para>
/// Args most frequently come in form of <seealso cref="ArgSet"/>s. Arg supports several primitive types: <see cref="int"/>, <see cref="float"/>, <see cref="string"/> and <see cref="bool"/>, and does its best to convert between them (for more details, see docstrings for property accessors). You can implicitly cast from supported primitive types to Arg:
/// <code>
///		Arg x = 1,
///		y = 2f,
///		z = "three",
///		w = false;
/// </code>
/// and do explicit conversions the other way around
/// (alternatively, use getters of <see cref="I32"/>/<see cref="F32"/>/<see cref="Bool"/>/<see cref="Str"/>/<see cref="Vec"/>):
/// <code>
///		Arg arg = new(12);
///		float fl = (float)arg; // 12f
///		bool bo = (bool)arg; // true
///		string st = (string)arg; // "12"
/// </code>
/// The reason conversions to primitives are explicit is because in Rain World modding 
/// you will often have tangled math blocks, where an incorrectly inferred int/float division 
/// can cause a hard to catch rounding logic error and be very annoying to debug.
/// </para>
/// <para>
/// When created from a string (<see cref="Arg(string, bool)"/> constructor, when <c>linkage</c> is <c>true</c>), an Arg can:
/// <list type="bullet">
///		<item>
///			Become named. This happens when the provided string contains at least one equal sign character (<c>=</c>).
///			Part before that becomes Arg's name, part after that is parsed into contents.
///		</item>
///		<item>
///			Become linked to a variable. This happens when value part of the source string begins with a
///			Dollar sign (<c>$</c>). An Arg that references a variable ignores 
///			its own inner state and accesses the variable object instead.
///			See <seealso cref="VarRegistry"/> for var storage details.
///		</item>
/// </list>
/// </para>
/// </summary>
public sealed class Arg : IEquatable<Arg>, IArgPayload, IConvertible
{
	private bool _skipparse = false;
	private bool _readonly = false;
	private ArgType _dt = ArgType.STRING;
	#region backing
	private IArgPayload? _payload;
	private string _raw;
	private string _str;
	private int _i32;
	private float _f32;
	private bool _bool;
	private Vector4 _vec;
	#endregion backing
	/// <summary>
	/// Parses contents of <see cref="_str"/> to fill other fields. Sets <see cref="DataType"/> to <see cref="ArgType.STRING"/>.
	/// </summary>
	private void _parseStr()
	{
		//check vec parsn
		string[] spl;
		_vec = default;
		Vector4 vecres = default;
		bool vecparsed = false;
		if ((spl = TXT.Regex.Split(_str, "\\s*;\\s*")).Length is 2 or 3 or 4)
		{
			vecparsed = true;
			for (int i = 0; i < spl.Length; i++)
			{
				if (!float.TryParse(spl[i], out float val)) vecparsed = false;
				vecres[i] = val;
			}
		}
		if (vecparsed)
		{
			_vec = vecres;
			_f32 = vecres.magnitude;
			_i32 = (int)_f32;
			_bool = _f32 != 0f;
		}
		else
		{
			if (trueStrings.Contains(_str.ToLower()))
			{
				_bool = true;
				_f32 = 1f;
				_i32 = 1;
			}
			else if (falseStrings.Contains(_str.ToLower()))
			{
				_bool = false;
				_f32 = 0f;
				_i32 = 0;
			}
			else
			{
				float.TryParse(_str, out _f32);
				if (!int.TryParse(_str, out _i32))
				{
					_i32 = (int)_f32;
				}
			}
		}
		DataType = ArgType.STRING;
	}
	#region convert
	/// <summary>
	/// Raw string previously used to create the argument. Using the setter sets <see cref="DataType"/> to <see cref="ArgType.STRING"/>.
	/// </summary>
	public string Raw
	{
		get => _payload?.Raw ?? _raw;
		set
		{
			if (_readonly) return;
			if (value is null) throw new ArgumentNullException(nameof(value));
			if (_payload is not null) { _payload.Raw = value; return; }
			_raw = value;
			Name = null;
			int splPoint = _raw.IndexOf('=');
			if (splPoint is not -1 && splPoint < _raw.Length - 1)
			{
				Name = _raw.Substring(0, splPoint);
				value = value.Substring(splPoint + 1);
			}
			if (value.StartsWith("$"))
			{
				int? ss = CurrentSaveslot;
				int? ch = CurrentCharacter;
				plog.DbgVerbose($"Linking variable {value}: {ss}, {ch}");
				if (ss is null)
				{
					plog.LogError($"Impossible to link variable! {value}: could not find RainWorldGame.");
					parseVal(value);
					return;
				}
				_payload = VarRegistry.GetVar(value.Substring(1), ss.Value, ch ?? -1);
				//DataType = ArgType.VAR;
			}
			else
			{
				parseVal(value);
			}

			void parseVal(string val)
			{
				_skipparse = false;
				Str = val;
			}

			//_parseStr();
		}
	}
	/// <summary>
	/// String value of the argument. If the argument is unnamed, this is equivalent to <see cref="Raw"/>; if the argument is named, returns everything after first "=" character. Using the setter sets <see cref="DataType"/> to <see cref="ArgType.STRING"/>.
	/// </summary>
	public string Str
	{
		get => _payload?.Str ?? _str;
		set
		{
			if (_readonly) return;
			if (_payload is not null) { _payload.Str = value; }
			else
			{
				_str = value;
				if (!_skipparse) _parseStr();
				_skipparse = false;
			}
		}
	}
	/// <summary>
	/// Int value of the argument. 0 if int value couldn't be parsed; rounded if <see cref="Arg"/> is created from a float; 1 or 0 if created from a bool; rounded magnitude of a vector if instance created from vector. Using the setter sets <see cref="DataType"/> to <see cref="ArgType.INTEGER"/>.
	/// </summary>
	public int I32
	{
		get => _payload?.I32 ?? _i32;
		set
		{

			if (_readonly) return;
			if (_payload is not null) { _payload.I32 = value; return; }
			_skipparse = true;
			_i32 = value;
			_f32 = value;
			_bool = value is not 0;
			_vec = default;
			Str = value.ToString();
			DataType = ArgType.INTEGER;
		}
	}
	/// <summary>
	/// Float value of the argument; 0f if float value couldn't be parsed; equal to <see cref="I32"/> if <see cref="Arg"/> is created from an int (may lose precision on large values!); 1f or 0f if created from a bool; magnitude of a vector if instance is created from vector. Using the setter sets <see cref="DataType"/> to <see cref="ArgType.DECIMAL"/>.
	/// </summary>
	public float F32
	{
		get => _payload?.F32 ?? _f32;
		set
		{
			if (_readonly) return;
			if (_payload is not null) { _payload.F32 = value; return; }
			_f32 = value;
			_i32 = value is float.PositiveInfinity or float.NegativeInfinity or float.NaN ? 0 : (int)value;
			_bool = value is not 0f;
			_vec = default;
			_skipparse = true;
			Str = value.ToString();
			DataType = ArgType.DECIMAL;
		}
	}
	/// <summary>
	/// Boolean value of the argument; false by default. False if original string is found in <see cref="falseStrings"/>, or if <see cref="Arg"/> is created from a zero int or float; True if original string is found in <see cref="trueStrings"/>, or of <see cref="Arg"/> is created from a non-zero int, float or vector. Using the setter sets <see cref="DataType"/> to <see cref="ArgType.BOOLEAN"/>.
	/// </summary>
	public bool Bool
	{
		get => _payload?.Bool ?? _bool;
		set
		{
			if (_readonly) return;
			if (_payload is not null) { _payload.Bool = value; return; }
			_bool = value;
			_i32 = value ? 1 : 0;
			_f32 = value ? 1 : 0;
			_skipparse = true;
			_vec = default;
			Str = value.ToString();
			DataType = ArgType.BOOLEAN;
		}
	}
	/// <summary>
	/// Vector value of the instance. Zeroed unless instance has been created from a vector or successfully parsed the vector from string.
	/// </summary>
	public Vector4 Vec
	{
		get
			=> _payload?.Vec ?? _vec;
		set
		{
			if (_payload is not null) { _payload.Vec = value; return; }
			_f32 = value.magnitude;
			_i32 = (int)_f32;
			_bool = _f32 != 0f;
			_vec = value;
			_skipparse = true;
			Str = $"{value.x};{value.y};{value.z};{value.w}";
			DataType = ArgType.VECTOR;
		}
	}

	/// <summary>
	/// Attempts to convert value of the current instance into a specified enum. Perf note: each call parses <see cref="Str"/> or invokes <see cref="Convert.ChangeType(object, Type)"/> to convert <see cref="I32"/>'s current value if parsing fails.
	/// </summary>
	/// <typeparam name="T">Type of the enum.</typeparam>
	/// <param name="value">Out param. Contains resulting enum value.</param>
	public void GetEnum<T>(out T? value)
		where T : Enum
	{
		if (_payload is not null)
		{
			_payload.GetEnum(out value);
			return;
		}
		if (TryParseEnum(Str, out value)) { return; }
		value = (T)Convert.ChangeType(I32, Enum.GetUnderlyingType(typeof(T)));
	}
	/// <summary>
	/// Sets value of current instance to specified enum. Perf note: each call invokes <see cref="Convert.ChangeType(object, Type)"/>. Will throw if your enum's underlying type can not be converted into an I32 (if value is out of range). Will set <see cref="I32"/> and <see cref="F32"/> to value of provided enum variant. Sets <see cref="DataType"/> to <see cref="ArgType.ENUM"/>.
	/// </summary>
	/// <typeparam name="T">Type of the enum.</typeparam>
	/// <param name="value">Value to be set.</param>
	public void SetEnum<T>(in T value)
		where T : Enum
	{
		if (_readonly) return;
		if (_payload is not null) { _payload.SetEnum(value); return; }
		_str = value.ToString();
		I32 = (int)Convert.ChangeType(value, typeof(int));
	}
	/// <summary>
	/// converts current float value into frames assuming it was seconds: (int)(F32*40f).
	/// </summary>
	public int SecAsFrames => (int)(F32 * 40f);
	#region iconv
	TypeCode IConvertible.GetTypeCode()
	{
		return TypeCode.Object;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return Bool;
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		return (char)I32;
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		return (sbyte)I32;
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		return (byte)I32;
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		return (short)I32;
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		return (ushort)I32;
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		return I32;
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		return (uint)I32;
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		return I32;
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		return (ulong)I32;
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		return (float)F32;
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		return (double)F32;
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		return (decimal)F32;
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	string IConvertible.ToString(IFormatProvider provider)
	{
		return ToString();
	}

	object IConvertible.ToType(Type conversionType, IFormatProvider provider)
	{
		throw new NotImplementedException();
	}
	#endregion iconv
	#endregion convert
	/// <summary>
	/// Name of the argument; null if unnamed.
	/// </summary>
	public string? Name { get; private set; } = null;
	/// <summary>
	/// Indicates whether this instance is linked to a variable. If yes, all property accessors will lead to associated variable, and the instance's internal state will be ignored.
	/// </summary>
	// internally (inside properties) replaced with individual checks to eliminate compiler warnings.
	public bool IsVar => _payload is not null;
	/// <summary>
	/// Indicates what data type was this instance's contents filled from.
	/// </summary>
	public ArgType DataType
	{
		get => _payload?.DataType ?? _dt;
		private set
		{
			if (IsVar) return;
			_dt = value;
		}
	}
	#region general
	/// <summary>
	/// Compares against another instance. Uses raw contents of <see cref="Str"/> for comparison.
	/// </summary>
	/// <param name="other"></param>
	/// <returns>whether instances are identical</returns>
	public bool Equals(Arg? other)
	{
		if (other is null) return false;
		//todo: add comparison inference?
		if (DataType == other.DataType)
		{
			return DataType switch
			{
				ArgType.DECIMAL => other.F32 == F32,
				ArgType.INTEGER => other.I32 == I32,
				ArgType.STRING => other.Str == Str,
				ArgType.ENUM => other.I32 == I32 || other.Str == Str,
				ArgType.BOOLEAN => other.Bool == Bool,
				ArgType.OTHER => false,
				_ => throw new InvalidOperationException("Impossible data type value!"),
			};
		}
		return _str == other._str;
	}
	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return _str.GetHashCode();
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		StringBuilder sb = new();
		sb.Append(Name is not null ? Name : "Arg");
		if (IsVar)
		{

			sb.Append($"(var){{{_payload}}}");
			return sb.ToString();
		}
		sb.Append(string.Format("{{ {0} : {1} }}", DataType, DataType switch
		{
			ArgType.DECIMAL => F32,
			ArgType.INTEGER => I32,
			ArgType.STRING => Str,
			ArgType.ENUM => $"{Str} / {I32}",
			ArgType.BOOLEAN => Bool,
			_ => Str
		}));

		return sb.ToString();
	}
	/// <summary>
	/// Returns a new read-only Arg, wrapping around the current one.
	/// </summary>
	public Arg Wrap => new Arg(this).MakeReadOnly();
	/// <summary>
	/// Makes current instance read-only. Not undoable.
	/// </summary>
	/// <returns></returns>
	internal Arg MakeReadOnly()
	{
		_readonly = true;
		return this;
	}
	#endregion
	#region ctors
	/// <summary>
	/// Creates the structure from a given string.
	/// </summary>
	/// <param name="orig">String to create argument from. Named arguments receive "name=value" type strings here. Can not be null.</param>
	/// <param name="linkage">Whether to check the provided string's structure, determining name and linking to a variable if needed. Off by default, for implicit casts</param>
	public Arg(string orig, bool linkage = false)
	{
		BangBang(orig, nameof(orig));
		_raw = orig;
		_f32 = default;
		_i32 = default;
		_bool = default;
		if (linkage) Raw = orig;
		else
		{
			_str = orig;
			_parseStr();
		}
		_str ??= string.Empty;
	}
	/// <summary>
	/// Creates the structure from a given int. Always unnamed. Mostly used for implicit casts.
	/// </summary>
	/// <param name="val"></param>
	public Arg(int val)
	{
		I32 = val;
		_raw ??= val.ToString();
		_str ??= val.ToString();
	}
	/// <summary>
	/// Creates the structure from a given float. Always unnamed. Mostly used for implicit casts.
	/// </summary>
	/// <param name="val"></param>
	public Arg(float val)
	{
		F32 = val;
		_raw ??= val.ToString();
		_str ??= val.ToString();
	}
	/// <summary>
	/// Creates the structure from a given bool. Always unnamed. Mostly used for implicit casts.
	/// </summary>
	/// <param name="val"></param>
	public Arg(bool val)
	{
		Bool = val;
		_raw ??= val.ToString();
		_str ??= val.ToString();
	}
	/// <summary>
	/// Creates an instance from a vector.
	/// </summary>
	/// <param name="val"></param>
	public Arg(Vector3 val)
	{
		Vec = val;
		_raw ??= val.ToString();
		_str ??= val.ToString();

	}
	/// <summary>
	/// Creates a new instance that wraps another as a variable, with an optional name.
	/// </summary>
	/// <param name="val">Another arg instance that serves as a variable. Must not be null.</param>
	/// <param name="name">Name of the new instance.</param>
	public Arg(IArgPayload val, string? name = null)
	{
		BangBang(val, nameof(val));
		_payload = val;
		Name = name;
		_raw = _str = string.Empty;
	}
	#endregion
	#region casts
	/// <summary>
	/// Converts an instance into a string.
	/// </summary>
	/// <param name="arg"></param>
	public static explicit operator string(Arg arg)
		=> arg.Str;

	/// <summary>
	/// Creates an instance from a string.
	/// </summary>
	/// <param name="src"></param>
	public static implicit operator Arg(string src)
		=> new(src, false);

	/// <summary>
	/// Converts an instance into an int.
	/// </summary>
	/// <param name="arg"></param>
	public static explicit operator int(Arg arg)
		=> arg.I32;
	/// <summary>
	/// Creates an unnamed instance from an int.
	/// </summary>
	/// <param name="src"></param>
	public static implicit operator Arg(int src)
		=> new(src);
	/// <summary>
	/// Converts an instance into a float.
	/// </summary>
	/// <param name="arg"></param>
	public static explicit operator float(Arg arg)
		=> arg.F32;
	/// <summary>
	/// Creates an unnamed instance from a float.
	/// </summary>
	/// <param name="src"></param>
	public static implicit operator Arg(float src)
		=> new(src);
	/// <summary>
	/// Converts an instance into a bool.
	/// </summary>
	/// <param name="arg"></param>
	public static explicit operator bool(Arg arg)
		=> arg.Bool;
	/// <summary>
	/// Creates an unnamed instance from a bool.
	/// </summary>
	/// <param name="src"></param>
	public static implicit operator Arg(bool src)
		=> new(src);
	/// <summary>
	/// Converts an instance into a vector.
	/// </summary>
	/// <param name="arg"></param>
	public static explicit operator Vector3(Arg arg)
		=> arg.Vec;
	/// <summary>
	/// Creates an unnamed instance from a vector.
	/// </summary>
	/// <param name="src"></param>
	public static implicit operator Arg(Vector3 src)
		=> new(src);

	#endregion;
}
