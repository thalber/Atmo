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
