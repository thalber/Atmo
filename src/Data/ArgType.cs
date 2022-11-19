namespace Atmo.Data;

/// <summary>
/// Displays what kind of data an <see cref="IArgPayload"/> instance was constructed from.
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
