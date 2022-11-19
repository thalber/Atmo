namespace Atmo.Data;

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
		get => prop_Str.a?.Invoke() ?? string.Empty;
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
