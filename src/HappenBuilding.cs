using static Atmo.API;
using static Atmo.HappenTrigger;

namespace Atmo;
/// <summary>
/// Manages happens' initialization and builtin behaviours.
/// </summary>
public static partial class HappenBuilding
{
	/// <summary>
	/// Populates a happen with callbacks. Called automatically by the constructor.
	/// </summary>
	/// <param name="happen"></param>
	internal static void NewEvent(Happen happen)
	{
		if (MNH_invl is null) return;
		foreach (Create_RawHappenBuilder? cb in MNH_invl)
		{
			try
			{
				cb?.Invoke(happen);
			}
			catch (Exception ex)
			{
				inst.Plog.LogError(
					$"Happenbuild: NewEvent:" +
					$"Error invoking happen factory {cb}//{cb?.Method.Name} for {happen}:" +
					$"\n{ex}");
			}
		}
		//API_MakeNewHappen?.Invoke(ha);
	}
	/// <summary>
	/// Creates a new trigger with given ID, arguments using provided <see cref="RainWorldGame"/>.
	/// </summary>
	/// <param name="id">Name or ID</param>
	/// <param name="args">Optional arguments</param>
	/// <param name="rwg">game instance</param>
	/// <param name="owner">Happen that requests the trigger.</param>
	/// <returns>Resulting trigger; an <see cref="Always"/> if something went wrong.</returns>
	internal static HappenTrigger CreateTrigger(
		string id,
		string[] args,
		RainWorldGame rwg,
		Happen owner)
	{
		HappenTrigger? res = null;
		//res = DefaultTrigger(id, args, rwg, owner);

		if (MNT_invl is null) goto finish;
		foreach (Create_RawTriggerFactory? cb in MNT_invl)
		{
			if (res is not null) break;
			try
			{
				res ??= cb?.Invoke(id, args, rwg, owner);
			}
			catch (Exception ex)
			{
				inst.Plog.LogError(
					$"Happenbuild: CreateTrigger: Error invoking trigger factory " +
					$"{cb}//{cb?.Method.Name} for {id}({(args.Length == 0 ? string.Empty : args.Aggregate(Utils.JoinWithComma))}):" +
					$"\n{ex}");
			}
		}
	finish:
		if (res is null)
		{
			inst.Plog.LogWarning($"Failed to create a trigger! {id}, args: {(args.Length == 0 ? string.Empty : args.Aggregate(Utils.JoinWithComma))}. Replacing with a stub");
			res = new Always();
		}
		return res;
	}
}
