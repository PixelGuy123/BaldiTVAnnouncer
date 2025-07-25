﻿using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BaldiTVAnnouncer.Patches
{
	[HarmonyPatch(typeof(BaldiTV))]
	internal static class BaldiTVPatches
	{
		// Unused since 1.0.4.1
		// [HarmonyPatch(typeof(EnvironmentController), "GetBaldi")]
		// [HarmonyReversePatch(HarmonyReversePatchType.Original)]
		// public static Baldi GetActualBaldi(EnvironmentController instance) =>
		// 	throw new System.NotImplementedException("stub");

		// this whole patch is included as well
		// [HarmonyPatch(typeof(BaseGameManager), "CollectNotebooks", [typeof(int)])]
		// [HarmonyPatch(typeof(TimeOut), "Update")]
		// [HarmonyTranspiler]
		// static IEnumerable<CodeInstruction> FixGetBaldiCall(IEnumerable<CodeInstruction> i) =>
		// 	new CodeMatcher(i)
		// 	.MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(EnvironmentController), "GetBaldi")))
		// 	.SetInstruction(CodeInstruction.Call(typeof(BaldiTVPatches), "GetActualBaldi", [typeof(EnvironmentController)])) // Should fix GetBaldi() not working
		// 	.InstructionEnumeration();

		// [HarmonyPatch(typeof(Baldi), "Praise")]
		// [HarmonyPatch(typeof(Baldi), "PraiseAnimation")]
		// [HarmonyPrefix]
		// static bool MakeSureBaldiIsntInterrupted(Baldi __instance) =>
		// 	__instance.behaviorStateMachine.CurrentState is not Baldi_Announcer;

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
				if (baldi && baldi.Character == Character.Baldi) // Very important character check to not mess up TeacherAPI
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
			if (!baldi || baldi.Character != Character.Baldi)
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

	[HarmonyPatch]
	internal static class FixPatches
	{
		[HarmonyPatch(typeof(VentController), "Update")]
		[HarmonyPostfix]
		static void FixVentWithBaldiIn(ref List<VentTravelStatus> ___ventTravelers, EnvironmentController ___ec, ref bool ___cameraInside) // To make sure the announcer is actually announcing!
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

		[HarmonyPatch(typeof(Baldi), "Slap")]
		[HarmonyPrefix]
		static bool ShouldBaldiReallySlap(Baldi __instance, Baldi.BaldiSlapDelegate ___OnBaldiSlap)
		{
			if (__instance.Character != Character.Baldi || __instance.behaviorStateMachine.CurrentState is not Baldi_Announcer)
				return true;

			// Copy paste of og Slap function
			___OnBaldiSlap.Invoke(__instance);
			__instance.slapTotal = 0f;
			__instance.slapDistance = __instance.nextSlapDistance;
			__instance.nextSlapDistance = 0f;
			__instance.navigator.SetSpeed(__instance.slapDistance / (__instance.Delay * __instance.MovementPortion));

			return false;
		}
	}


}
