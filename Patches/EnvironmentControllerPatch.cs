using HarmonyLib;

namespace BaldiTVAnnouncer.Patches
{
	[HarmonyPatch(typeof(EnvironmentController), "GetBaldi")]
	internal static class EnvironmentControllerPatch
	{
		static void Postfix(ref Baldi __result)
		{
			if (__result && __result.behaviorStateMachine.CurrentState is Baldi_Announcer)
				__result = null;
		}
	}
}
