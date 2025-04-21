using UnityEngine;

namespace BaldiTVAnnouncer
{
	public class Baldi_Announcer(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement, Vector3 originalPosition) : Baldi_SubState(npc, baldi, prevState) 
	{
		readonly public bool EventAnnouncement = eventAnnouncement;
		public SpriteVolumeAnimator animator;
		public AnimatedSpriteRotator rotator;
		readonly public Vector3 ogPosition = originalPosition;

		public override void Enter()
		{
			base.Enter();
			animator = baldi.GetComponent<SpriteVolumeAnimator>();
			rotator = baldi.GetComponent<AnimatedSpriteRotator>();
			if (!animator || !rotator)
			{
				baldi.behaviorStateMachine.ChangeState(previousState);
				return;
			}
		}
		public override void PlayerInSight(PlayerManager player)
		{
		}
	}
	public class Baldi_GoToRoom(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement, Vector3 originalPosition) : Baldi_Announcer(npc, baldi, prevState, eventAnnouncement, originalPosition)
	{
		const float delayIncrement = 0.05f;
		float smallSlapDelay = 0f, delayBeforeGoing = 0f;
		Vector3 positionToGo;
		public Vector3 PosToGo => positionToGo;
		Cell cellToGo;
		BaldiTVObject tv;
		public bool reachedInTime = true;
		float angerUsed = constantAnger;

		public override void Enter()
		{
			base.Enter();


			if (BaldiTVObject.availableTVs.Count == 0)
			{
				baldi.behaviorStateMachine.ChangeState(previousState);
				return;
			}

			baldi.ResetSlapDistance();

			if (EventAnnouncement)
			{
				baldi.AudMan.FlushQueue(true);
				var sdToPlay = audGoToEvent[Random.Range(0, audGoToEvent.Length)];
				baldi.AudMan.QueueAudio(sdToPlay);
				delayBeforeGoing = 0.8f;
			}

			angerUsed *= Vector3.Distance(baldi.transform.position, positionToGo) * 0.75f;
			baldi.GetAngry(angerUsed);
			baldi.ClearSoundLocations();
			tv = BaldiTVObject.availableTVs[Random.Range(0, BaldiTVObject.availableTVs.Count)];
			positionToGo = tv.FrontPosition;
			cellToGo = baldi.ec.CellFromPosition(positionToGo);
			baldi.Hear(null, positionToGo, 127, false);
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (cellToGo == baldi.ec.CellFromPosition(baldi.transform.position))
				baldi.behaviorStateMachine.ChangeState(new Baldi_Speaking(npc, baldi, previousState, EventAnnouncement, tv, reachedInTime, ogPosition));
		}

		public override void Update()
		{
			base.Update();

			if (delayBeforeGoing > 0f)
			{
				delayBeforeGoing -= baldi.TimeScale * Time.deltaTime;
				return;
			}

			baldi.UpdateSlapDistance();
			baldi.UpdateSlapDistance(); // To make sure optimal path!
			baldi.UpdateSlapDistance();
			baldi.ClearSoundLocations();
			baldi.Hear(null, positionToGo, 127, false);

			smallSlapDelay -= Time.deltaTime * npc.TimeScale;
			if (smallSlapDelay  <= 0f)
			{
				baldi.Slap();
				ActivateSlapAnimation();
				smallSlapDelay += delayIncrement;
			}
		}

		public override void Exit()
		{
			base.Exit();
			baldi.GetAngry(-angerUsed);
			baldi.ResetSlapDistance();
			baldi.EndSlap();
			baldi.Navigator.Entity.Teleport(positionToGo);
		}

		const float constantAnger = 90f;

		internal static SoundObject[] audGoToEvent;
	}

	public class Baldi_Speaking(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement, BaldiTVObject tvObj, bool reachedInTime, Vector3 originalPosition) : Baldi_Announcer(npc, baldi, prevState, eventAnnouncement, originalPosition)
	{
		readonly public BaldiTVObject tvObj = tvObj;
		readonly public bool reachedInTime = reachedInTime;
		public float ogOffset;
		public override void Enter()
		{
			base.Enter();
			ogOffset = baldi.spriteRenderer[0].transform.localPosition.y;
			baldi.Navigator.Entity.SetFrozen(true);
			baldi.Navigator.Entity.SetInteractionState(false);
			baldi.Navigator.Entity.SetBlinded(true);
			baldi.spriteRenderer[0].transform.localPosition = Vector3.down * 0.35f;

			baldi.transform.forward = tvObj.DirToLookAt;
			animator.enabled = true;
			animator.volumeMultipler = 1.35f;
			rotator.enabled = true;
			ChangeNavigationState(new NavigationState_DoNothing(baldi, 0));

			baldi.AudMan.FlushQueue(true);

			animator.AudMan = Singleton<CoreGameManager>.Instance.GetHud(0).BaldiTv.baldiTvAudioManager;
		}

		public override void Update()
		{
			base.Update();
			baldi.transform.forward = tvObj.DirToLookAt;
			baldi.Navigator.Entity.Teleport(tvObj.FrontPosition);
		}

		public override void Exit()
		{
			base.Exit();
			
			baldi.Navigator.Entity.SetFrozen(false);
			baldi.Navigator.Entity.SetInteractionState(true);
			baldi.Navigator.Entity.SetBlinded(false);
		}
	}

	public class Baldi_EndSpeaking(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement, BaldiTVObject tvObj, bool reachedInTime, float ogoffset, Vector3 originalPosition) : Baldi_Announcer(npc, baldi, prevState, eventAnnouncement, originalPosition)
	{
		public readonly bool reachedInTime = reachedInTime;
		readonly float ogoffset = ogoffset;
		readonly public BaldiTVObject tvObj = tvObj;
		public override void Enter()
		{
			base.Enter();
			baldi.transform.forward = tvObj.DirToLookAt;
			baldi.Navigator.Entity.Teleport(tvObj.FrontPosition);
			animator.volumeMultipler = 1.75f;
			animator.AudMan = baldi.AudMan;

			if (EventAnnouncement)
				baldi.AudMan.QueueRandomAudio(reachedInTime ? audEndEvent : audEndEvent_NoTime);
		}

		public override void Update()
		{
			base.Update();
			baldi.transform.forward = tvObj.DirToLookAt;
			baldi.Navigator.Entity.Teleport(tvObj.FrontPosition);
			if (!baldi.AudMan.QueuedAudioIsPlaying)
				baldi.behaviorStateMachine.ChangeState(new Baldi_GoBackToTheSpot(npc, baldi, previousState, ogPosition));
		}

		public override void Exit()
		{
			base.Exit();
			animator.enabled = false;
			rotator.enabled = false;
			baldi.spriteRenderer[0].transform.localPosition = Vector3.up * ogoffset;
		}

		internal static SoundObject[] audEndEvent, audEndEvent_NoTime;
	}

	public class Baldi_GoBackToTheSpot(NPC npc, Baldi baldi, NpcState prevState, Vector3 originalPosition) : Baldi_Announcer(npc, baldi, prevState, false, originalPosition)
	{
		const float delayIncrement = 0.05f;
		float smallSlapDelay = 0f;
		readonly Cell cellToGo = npc.ec.CellFromPosition(originalPosition);
		public override void Enter()
		{
			base.Enter();
			baldi.ResetSlapDistance();

			baldi.GetAngry(constantAnger);
			baldi.ClearSoundLocations();
			baldi.Hear(null, ogPosition, 127, false);
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			var myCell = npc.ec.CellFromPosition(baldi.transform.position);
			if (cellToGo == myCell)
			{
				baldi.behaviorStateMachine.ChangeState(previousState);
				return;
			}
			npc.ec.FindPath(myCell, cellToGo, PathType.Nav, out _, out bool success);

			if (!success)
				baldi.behaviorStateMachine.ChangeState(previousState);
		}

		public override void Update()
		{
			base.Update();
			baldi.UpdateSlapDistance();
			baldi.UpdateSlapDistance(); // To make sure optimal path!
			baldi.UpdateSlapDistance();
			baldi.ClearSoundLocations();
			baldi.Hear(null, ogPosition, 127, false);

			smallSlapDelay -= Time.deltaTime * npc.TimeScale;
			if (smallSlapDelay <= 0f)
			{
				baldi.Slap();
				ActivateSlapAnimation();
				smallSlapDelay += delayIncrement;
			}
		}

		public override void Exit()
		{
			base.Exit();
			baldi.GetAngry(-constantAnger);
			baldi.ResetSlapDistance();
			baldi.EndSlap();
		}

		const float constantAnger = 125f;
	}
}
