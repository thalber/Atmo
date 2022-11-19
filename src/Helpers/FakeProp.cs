namespace Atmo.Helpers;

/// <summary>
/// Carries getter and setter callbacks for a pseudo-property (both optional).
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed record FakeProp<T> : VT<Func<T>?, Action<T>?>
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
