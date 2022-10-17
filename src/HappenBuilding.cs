using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Atmo.Atmod;
using static Atmo.HappenTrigger;
using static Atmo.HappenBuilding;
using static Atmo.API;

using URand = UnityEngine.Random;

namespace Atmo;
/// <summary>
/// Manages happens' initialization and builtin behaviours.
/// </summary>
internal static partial class HappenBuilding
{
    #region fields
    /// <summary>
    /// Strings that evaluate to bool.true
    /// </summary>
    public static readonly string[] trueStrings = new[] { "true", "1", "yes", };
    /// <summary>
    /// Strings that evaluate to bool.false
    /// </summary>
    public static readonly string[] falseStrings = new[] { "false", "0", "no", };
    #endregion

    internal static void NewEvent(Happen ha)
    {
        //try
        //{
        //    AddDefaultCallbacks(ha);
        //}
        //catch (Exception ex)
        //{
        //    inst.Plog.LogError($"HappenBuild: NewEvent:" +
        //        $"Error adding default callbacks to {ha}:" +
        //        $"\n{ex}");
        //}
        if (MNH_invl is null) return;
        foreach (var cb in MNH_invl)
        {
            try
            {
                cb?.Invoke(ha);
            }
            catch (Exception ex)
            {
                inst.Plog.LogError(
                    $"Happenbuild: NewEvent:" +
                    $"Error invoking event factory {cb}//{cb?.Method.Name} for {ha}:" +
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
    /// <returns>Resulting trigger; an <see cref="Always"/> if something went wrong.</returns>
    internal static HappenTrigger? CreateTrigger(
        string id,
        string[] args,
        RainWorldGame rwg,
        Happen owner)
    {
#warning untested
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
