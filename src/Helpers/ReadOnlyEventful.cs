namespace Atmo.Helpers;
/// <summary>
/// Callback-driven <see cref="IArgPayload"/>, for use in <see cref="Arg"/> wrapping.
/// </summary>
public struct ReadOnlyEventful : IArgPayload
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
