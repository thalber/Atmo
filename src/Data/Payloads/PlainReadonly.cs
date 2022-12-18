namespace Atmo.Data.Payloads;
/// <summary>
/// A read only value based arg payload.
/// </summary>
public readonly record struct PlainReadonly : IArgPayload
{
	#region backing
	private readonly string _str;
	private readonly int _i32;
	private readonly float _f32;
	private readonly bool _bool;
	private readonly Vector4 _vec;
	/// <summary>
	/// Creates a filled instance.
	/// </summary>
	public PlainReadonly(
		string str,
		int i32,
		float f32,
		bool @bool,
		Vector4 vec)
	{
		_str = str;
		_i32 = i32;
		_f32 = f32;
		_bool = @bool;
		_vec = vec;
	}
	#endregion backing
	#region interface
	/// <inheritdoc/>
	public string Raw { get => string.Empty; set { } }
	/// <inheritdoc/>
	public float F32 { get => _f32; set { } }
	/// <inheritdoc/>
	public int I32 { get => _i32; set { } }
	/// <inheritdoc/>
	public string Str { get => _str; set { } }
	/// <inheritdoc/>
	public bool Bool { get => _bool; set { } }
	/// <inheritdoc/>
	public Vector4 Vec { get => _vec; set { } }
	/// <inheritdoc/>
	public ArgType DataType => ArgType.OTHER;
	/// <inheritdoc/>
	public void GetEnum<TE>(out TE? value) where TE : Enum
	{
		if (!TryParseEnum(Str, out value))
		{
			value = (TE)Convert.ChangeType(I32, typeof(TE));
		};
	}
	/// <inheritdoc/>
	public void SetEnum<TE>(in TE value) where TE : Enum { }
	#endregion
}
