using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BaldiTVAnnouncer.Patches
{
	[HarmonyPatch(typeof(BaldiTV))]
	internal static class BaldiTVPatches
	{
		internal static System.Type baldiSpeakType = AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(BaldiTV), "BaldiSpeaks", [typeof(SoundObject)])).DeclaringType; // Get the compiler generated class

		static bool isAnEvent = false;

		[HarmonyPatch("AnnounceEvent")]
		[HarmonyPrefix]
		static void TellIfItIsAnEvent() =>
			isAnEvent = true;

		[HarmonyPatch("QueueEnumerator")]
		[HarmonyPrefix]
		static void PrepareBaldi(IEnumerator enumerator)
		{
			if (enumerator.GetType() == baldiSpeakType)
			{
				var baldi = Singleton<BaseGameManager>.Instance.Ec.GetBaldi();
				if (baldi && baldi.behaviorStateMachine.CurrentState is not Baldi_Announcer)
					baldi.behaviorStateMachine.ChangeState(new Baldi_GoToRoom(baldi, baldi, baldi.behaviorStateMachine.CurrentState, isAnEvent));	
				isAnEvent = false;
			}
		}

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		static void CheckIfBaldiIsFree(List<IEnumerator> ___queuedEnumerators, bool ___busy)
		{
			var baldi = Singleton<BaseGameManager>.Instance.Ec.GetBaldi();
			if (true || !baldi || baldi.behaviorStateMachine.CurrentState is not Baldi_Announcer)
				return;

			if (___queuedEnumerators.Count != 0 && !___busy)
			{
				if (baldi.behaviorStateMachine.CurrentState is Baldi_Speaking speaker)
				{
					if (!___queuedEnumerators.Exists(x => x.GetType() == baldiSpeakType))
					{
						baldi.behaviorStateMachine.ChangeState(speaker.previousState);
						return;
					}

					if (___queuedEnumerators[0].GetType() == baldiSpeakType)
						speaker.animator.enabled = true;
					else
					{
						speaker.animator.enabled = false;
						speaker.rotator.targetSprite = idleSprite;
					}
				}
			}
		}

		internal static Sprite idleSprite;
	}
}
