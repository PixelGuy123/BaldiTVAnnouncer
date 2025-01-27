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
			if (!Singleton<BaseGameManager>.Instance)
				return;

			var baldi = Singleton<BaseGameManager>.Instance.Ec.GetBaldi();
			if (!baldi || baldi.behaviorStateMachine.CurrentState is not Baldi_Announcer)
				return;

			if (___queuedEnumerators.Count != 0)
			{
				if (baldi.behaviorStateMachine.CurrentState is Baldi_Speaking speaker)
				{
					if (!___busy)
					{
						if (!___queuedEnumerators.Exists(x => x.GetType() == baldiSpeakType))
						{
							baldi.behaviorStateMachine.ChangeState(new Baldi_EndSpeaking(baldi, baldi, speaker.previousState, speaker.EventAnnouncement, speaker.reachedInTime, speaker.ogOffset));
							return;
						}
					}
					else
					{

						if (___queuedEnumerators[0].GetType() == baldiSpeakType)
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
						}
					}
				}
			}
		}

		internal static Sprite idleSprite;
	}
}
