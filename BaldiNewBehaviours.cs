using UnityEngine;

namespace BaldiTVAnnouncer
{
	public class Baldi_Announcer(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement) : Baldi_SubState(npc, baldi, prevState) 
	{
		readonly public bool EventAnnouncement = eventAnnouncement;
		public SpriteVolumeAnimator animator;
		public AnimatedSpriteRotator rotator;

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
	public class Baldi_GoToRoom(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement) : Baldi_Announcer(npc, baldi, prevState, eventAnnouncement)
	{
		const float delayIncrement = 0.15f;
		float smallSlapDelay = 0f;
		Vector3 positionToGo;
		public Vector3 PosToGo => positionToGo;
		Cell cellToGo;
		BaldiTVObject tv;
		public bool reachedInTime = true;

		public override void Enter()
		{
			base.Enter();


			if (BaldiTVObject.availableTVs.Count == 0)
			{
				baldi.behaviorStateMachine.ChangeState(previousState);
				return;
			}

			baldi.ResetSlapDistance();
			baldi.ResetSprite();

			if (EventAnnouncement)
			{
				baldi.AudMan.FlushQueue(true);
				//baldi.AudMan.QueueRandomAudio(audGoToEvent);
			}

			baldi.GetAngry(constantAnger);
			baldi.ClearSoundLocations();
			tv = BaldiTVObject.availableTVs[Random.Range(0, BaldiTVObject.availableTVs.Count)];
			positionToGo = tv.FrontPosition;
			cellToGo = baldi.ec.CellFromPosition(positionToGo);
			baldi.Hear(positionToGo, 127, false);
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (cellToGo == baldi.ec.CellFromPosition(baldi.transform.position))
				baldi.behaviorStateMachine.ChangeState(new Baldi_Speaking(npc, baldi, previousState, EventAnnouncement, tv, reachedInTime));
		}

		public override void Update()
		{
			base.Update();
			baldi.UpdateSlapDistance();
			baldi.UpdateSlapDistance(); // To make sure optimal path!
			baldi.UpdateSlapDistance();
			baldi.ClearSoundLocations();
			baldi.Hear(positionToGo, 127, false);

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
			baldi.GetAngry(-constantAnger);
			baldi.Navigator.Entity.Teleport(positionToGo);
		}

		const float constantAnger = 99f;

		internal static SoundObject[] audGoToEvent;
	}

	public class Baldi_Speaking(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement, BaldiTVObject tvObj, bool reachedInTime) : Baldi_Announcer(npc, baldi, prevState, eventAnnouncement)
	{
		readonly BaldiTVObject tvObj = tvObj;
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
			rotator.enabled = true;
			ChangeNavigationState(new NavigationState_DoNothing(baldi, 0));

			animator.audMan = Singleton<CoreGameManager>.Instance.GetHud(0).BaldiTv.baldiTvAudioManager;
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
			animator.audMan = baldi.AudMan;
			baldi.Navigator.Entity.SetFrozen(false);
			baldi.Navigator.Entity.SetInteractionState(true);
			baldi.Navigator.Entity.SetBlinded(false);
		}
	}

	public class Baldi_EndSpeaking(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement, bool reachedInTime, float ogoffset) : Baldi_Announcer(npc, baldi, prevState, eventAnnouncement)
	{
		readonly bool reachedInTime = reachedInTime;
		readonly float ogoffset = ogoffset;
		public override void Enter()
		{
			base.Enter();
			// talk something
		}

		public override void Update()
		{
			base.Update();
			if (!baldi.AudMan.QueuedAudioIsPlaying)
				baldi.behaviorStateMachine.ChangeState(previousState);
		}

		public override void Exit()
		{
			base.Exit();
			animator.enabled = false;
			rotator.enabled = false;
			baldi.spriteRenderer[0].transform.localPosition = Vector3.up * ogoffset;
			baldi.ResetSlapDistance();
			baldi.EndSlap();
		}

		internal static SoundObject[] audEndEvent;
		internal static SoundObject audEndEvent_NoTime;
	}
}
