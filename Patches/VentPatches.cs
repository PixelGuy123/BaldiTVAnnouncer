using HarmonyLib;
using System.Collections.Generic;

namespace BaldiTVAnnouncer.Patches
{
	[HarmonyPatch(typeof(VentController), "Update")]
	internal static class VentPatches
	{
		static void Postfix(ref List<VentTravelStatus> ___ventTravelers, EnvironmentController ___ec, ref bool ___cameraInside) // To make sure the announcer is actually announcing!
		{
			var baldo = ___ec.GetBaldi();
			if (!baldo || baldo.behaviorStateMachine.CurrentState is not Baldi_Speaking) return;

			for (int i = 0; i < ___ventTravelers.Count; i++)
			{
				if (___ventTravelers[i].overrider.entity == baldo.Navigator.Entity)
				{
					if (___ventTravelers[i].camera)
						___cameraInside = false;
					
					___ventTravelers[i].overrider.Release();
					___ventTravelers.RemoveAt(i--);
				}
			}
		}
	}
}
