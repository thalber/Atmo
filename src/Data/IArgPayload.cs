namespace Atmo.Data;
/// <summary>
/// Objects that can be used as payloads for <see cref="Arg"/>. Don't forget to add <see cref="object.ToString"/> override! Implement this, or use <see cref="Payloads.ByCallbackGetOnly"/> and <see cref="Payloads.ByCallback"/> if you want callback-driven shorthands.
/// </summary>
public interface IArgPayload
{
	/// <summary>
	/// Raw string used to create the instance.
	/// </summary>
	public string Raw { get; set; }
	/// <summary>
	/// Float value of the instance.
	/// </summary>
	public float F32 { get; set; }
	/// <summary>
	/// Int value of the instance.
	/// </summary>
	public int I32 { get; set; }
	/// <summary>
	/// String value of the instance.
	/// </summary>
	public string Str { get; set; }
	/// <summary>
	/// Boolean value of the instance.
	/// </summary>
	public bool Bool { get; set; }
	/// <summary>
	/// Vector value of the instance.
	/// </summary>
	public Vector4 Vec { get; set; }
	/// <summary>
	/// Tries getting an enum from the instance.
	/// </summary>
	public void GetEnum<T>(out T? value) where T : Enum;
	/// <summary>
	/// Tries setting instance to contain given enum.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="value"></param>
	public void SetEnum<T>(in T value) where T : Enum;

	public void GetExtEnum<T>(out T? value) where T : ExtEnumBase;

	public void SetExtEnum<T>(in T value) where T : ExtEnumBase;
	/// <summary>
	/// Returns instance's initial data type.
	/// </summary>
	public ArgType DataType { get; }
}
