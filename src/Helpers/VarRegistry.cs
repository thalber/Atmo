using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atmo.Helpers;

public class VarRegistry
{
	internal static int? CSlot => inst?.rw?.options.saveSlot ?? 0;
	internal static Dictionary<int, VarRegistry> PerSaveSlot = new();
	public static Arg GetVar(string name)
	{
		throw new NotImplementedException("Registry not in yet!");
	}

}
