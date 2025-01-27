using HarmonyLib;
using UnityEngine;

namespace BaldiTVAnnouncer.Patches
{
	[HarmonyPatch(typeof(PlayerMovement), "Update")]
	internal class PlayerMovementTestPatch
	{
		static void Prefix(PlayerMovement __instance)
		{
			if (Input.GetKeyDown(KeyCode.K))
			{
				var sds = Resources.FindObjectsOfTypeAll<SoundObject>();
				Singleton<CoreGameManager>.Instance.GetHud(__instance.pm.playerNumber).BaldiTv.Speak(sds[Random.Range(0, sds.Length)]);
			}
		}
	}
}
