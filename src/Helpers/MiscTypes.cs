namespace Atmo.Helpers;
/// <summary>
/// Displays what kind of data was originally provided to the Arg object.
/// </summary>
public enum ArgType
{
	/// <summary>
	/// Value was originally assigned as float.
	/// </summary>
	F32,
	/// <summary>
	/// Value was originally assigned as int.
	/// </summary>
	I32,
	/// <summary>
	/// Value was originally assigned as string.
	/// </summary>
	STR,
	/// <summary>
	/// Value was originally assigned as an enum.
	/// </summary>
	ENUM,
	/// <summary>
	/// Value was originally assigned as boolean.
	/// </summary>
	BOOL,
	/// <summary>
	/// The data type is unspecified.
	/// </summary>
	OTHER
}
/// <summary>
/// Simple read-only callback-driven <see cref="IArgPayload"/>
/// </summary>
public struct GetOnlyCallbackPayload : IArgPayload
{
	/// <summary>
	/// Callback to get int value.
	/// </summary>
	public Func<int>? getI32;
	/// <summary>
	/// Callback to get float value.
	/// </summary>
	public Func<float>? getF32;
	/// <summary>
	/// Callback to get bool value.
	/// </summary>
	public Func<bool>? getBool;
	/// <summary>
	/// Callback to get string value.
	/// </summary>
	public Func<string>? getStr;
	/// <summary>
	/// Operation is not supported.
	/// </summary>
	public string Raw
	{
		get => throw new NotSupportedException("Operation not supported");
		set => throw new NotSupportedException("Operation not supported");
	}
	/// <summary>
	/// Float value of the instance. Read-only.
	/// </summary>
	public float F32
	{
		get => getF32?.Invoke() ?? 0f;
		set { }
	}
	/// <summary>
	/// Int value of the instance. Read-only.
	/// </summary>
	public int I32
	{
		get => getI32?.Invoke() ?? 0;
		set { }
	}
	/// <summary>
	/// String value of the instance. Read-only.
	/// </summary>
	public string Str
	{
		get => getStr?.Invoke() ?? string.Empty;
		set { }
	}
	/// <summary>
	/// Bool value of the instance. Read-only.
	/// </summary>
	public bool Bool
	{
		get => getBool?.Invoke() ?? false;
		set { }
	}
	/// <summary>
	/// Type of the instance. Read-only.
	/// </summary>
	public ArgType DataType
		=> ArgType.OTHER;
	void IArgPayload.GetEnum<T>(out T? value) where T : default
		=> throw new NotSupportedException("Operation not supported");
	void IArgPayload.SetEnum<T>(in T value)
		=> throw new NotSupportedException("Operation not supported");
	/// <inheritdoc/>
	public override string ToString()
		=> Str;
}
/// <summary>
/// A callback driven payload, with support for both getters and setters. Uses <see cref="FakeProp{T}"/> to group functions together.
/// </summary>
public struct CallbackPayload : IArgPayload
{
	/// <summary>
	/// <see cref="I32"/> property backing.
	/// </summary>
	public FakeProp<int> prop_I32;
	/// <summary>
	/// <see cref="F32"/> property backing.
	/// </summary>
	public FakeProp<float> prop_F32;
	/// <summary>
	/// <see cref="Bool"/> property backing.
	/// </summary>
	public FakeProp<bool> prop_Bool;
	/// <summary>
	/// <see cref="Str"/> property backing.
	/// </summary>
	public FakeProp<string> prop_Str;
	/// <summary>
	/// Creates a new instance with given prop backings.
	/// </summary>
	/// <param name="prop_I32"></param>
	/// <param name="prop_F32"></param>
	/// <param name="prop_Bool"></param>
	/// <param name="prop_Str"></param>
	public CallbackPayload(
		FakeProp<int>? prop_I32 = null,
		FakeProp<float>? prop_F32 = null,
		FakeProp<bool>? prop_Bool = null,
		FakeProp<string>? prop_Str = null)
	{
		this.prop_I32 = prop_I32 ?? new(null, null);
		this.prop_F32 = prop_F32 ?? new(null, null);
		this.prop_Bool = prop_Bool ?? new(null, null);
		this.prop_Str = prop_Str ?? new(null, null);
	}

	/// <inheritdoc/>
	public string Raw
	{
		get => string.Empty;
		set { }
	}
	/// <inheritdoc/>
	public float F32
	{
		get => prop_F32.a?.Invoke() ?? 0f;
		set => prop_F32.b?.Invoke(value);
	}
	/// <inheritdoc/>
	public int I32
	{
		get => prop_I32.a?.Invoke() ?? 0;
		set => prop_I32.b?.Invoke(value);
	}
	/// <inheritdoc/>
	public string Str
	{
		get => prop_Str.a?.Invoke() ?? String.Empty;
		set => prop_Str.b?.Invoke(value);
	}
	/// <inheritdoc/>
	public bool Bool
	{
		get => prop_Bool.a?.Invoke() ?? false;
		set => prop_Bool.b?.Invoke(value);
	}
	/// <inheritdoc/>
	public ArgType DataType => ArgType.OTHER;
	void IArgPayload.GetEnum<T>(out T? value) where T : default
	{
		throw new NotImplementedException();
	}

	void IArgPayload.SetEnum<T>(in T value)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Carries getter and setter callbacks for a pseudo-property (both optional).
/// </summary>
/// <typeparam name="T"></typeparam>
public record FakeProp<T> : VT<Func<T>?, Action<T>?>
{
	/// <summary>
	/// Creates a new instance from given getter and setter. Instance, left and right names are preset.
	/// </summary>
	/// <param name="_a">Property getter function ( "T get ()" header )</param>
	/// <param name="_b">Property setter function ( "void set (T value)" header )</param>
	public FakeProp(Func<T>? _a, Action<T>? _b) 
		: base(_a, _b, "PropBacking", "getter", "setter")
	{
	}
}
