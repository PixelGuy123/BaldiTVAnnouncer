using HarmonyLib;
using UnityEngine;

namespace BaldiTVAnnouncer.Patches
{
	[HarmonyPatch(typeof(PlayerMovement), "Update")]
	internal class PlayerMovementTestPatch
	{
		static void Prefix(PlayerMovement __instance)
		{
			if (Input.GetKeyDown(KeyCode.F))
			{
				var sds = Resources.FindObjectsOfTypeAll<RandomEvent>();
				Singleton<CoreGameManager>.Instance.GetHud(__instance.pm.playerNumber).BaldiTv.Speak(sds[Random.Range(0, sds.Length)].EventIntro);
			}

			if (Input.GetKeyDown(KeyCode.G))
			{
				var sds = Resources.FindObjectsOfTypeAll<RandomEvent>();
				var ev = sds[Random.Range(0, sds.Length)];
				if (ev.EventJingleOverride != null)
				{
					__instance.pm.ec.audMan.PlaySingle(ev.EventJingleOverride);
				}
				else
				{
					__instance.pm.ec.audMan.PlaySingle(__instance.pm.ec.audEventNotification);
				}
				Singleton<CoreGameManager>.Instance.GetHud(__instance.pm.playerNumber).BaldiTv.AnnounceEvent(ev.EventIntro);
			}
		}
	}
}
