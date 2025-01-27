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
		static void TellIfItIsAnEvent()
		{
			if (BaldiTVObject.availableTVs.Count != 0)
				isAnEvent = true;
		}

		[HarmonyPatch("QueueEnumerator")]
		[HarmonyPrefix]
		static void PrepareBaldi(IEnumerator enumerator)
		{
			if (BaldiTVObject.availableTVs.Count == 0)
				return;

			if (enumerator.GetType() == baldiSpeakType)
			{
				var baldi = Singleton<BaseGameManager>.Instance.Ec.GetBaldi();
				if (baldi)
				{
					if (baldi.behaviorStateMachine.CurrentState is not Baldi_Announcer || baldi.behaviorStateMachine.CurrentState is Baldi_GoBackToTheSpot)
						baldi.behaviorStateMachine.ChangeState(new Baldi_GoToRoom(baldi, baldi, baldi.behaviorStateMachine.CurrentState, isAnEvent, baldi.transform.position));
				}
				isAnEvent = false;
			}
		}

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		static void CheckIfBaldiIsFree(BaldiTV __instance, List<IEnumerator> ___queuedEnumerators, bool ___busy)
		{
			if (!Singleton<BaseGameManager>.Instance || BaldiTVObject.availableTVs.Count == 0)
				return;

			var baldi = Singleton<BaseGameManager>.Instance.Ec.GetBaldi();
			if (!baldi)
				return;

			if (baldi.behaviorStateMachine.CurrentState is not Baldi_Announcer)
			{
				if (!___queuedEnumerators.Exists(x => x.GetType() == baldiSpeakType))
					return;
				baldi.behaviorStateMachine.ChangeState(new Baldi_GoToRoom(baldi, baldi, baldi.behaviorStateMachine.CurrentState, isAnEvent, baldi.transform.position));
			}
			

			if (___queuedEnumerators.Count != 0)
			{
				if (baldi.behaviorStateMachine.CurrentState is Baldi_EndSpeaking endSpeak && ___queuedEnumerators.Exists(x => x.GetType() == baldiSpeakType))
					baldi.behaviorStateMachine.ChangeState(new Baldi_Speaking(baldi, baldi, endSpeak.previousState, endSpeak.EventAnnouncement, endSpeak.tvObj, endSpeak.reachedInTime, endSpeak.ogPosition));
				else if (baldi.behaviorStateMachine.CurrentState is Baldi_Speaking speaker)
				{
					if (!___busy)
					{
						if (!___queuedEnumerators.Exists(x => x.GetType() == baldiSpeakType))
						{
							baldi.behaviorStateMachine.ChangeState(new Baldi_EndSpeaking(baldi, baldi, speaker.previousState, speaker.EventAnnouncement, speaker.tvObj, speaker.reachedInTime, speaker.ogOffset, speaker.ogPosition));
							return;
						}
						__instance.GetComponent<BaldiTVExtraData>().lastPlayedEnumerator = ___queuedEnumerators[0].GetType();
					}
					else
					{

						if (__instance.GetComponent<BaldiTVExtraData>().lastPlayedEnumerator == baldiSpeakType)
							speaker.animator.enabled = true;
						else
						{
							speaker.rotator.targetSprite = idleSprite;
							speaker.animator.enabled = false;
						}
						return;
					}
				}

				if (___busy)
				{
					if (baldi.behaviorStateMachine.CurrentState is Baldi_GoToRoom room) // To make sure he doesn't miss it out!
					{
						if (___queuedEnumerators[0].GetType() == baldiSpeakType)
						{
							baldi.Navigator.Entity.Teleport(room.PosToGo);
							room.reachedInTime = false;
							room.DestinationEmpty();
						}
					}
				}
			}
		}

		internal static Sprite idleSprite;
	}
}
