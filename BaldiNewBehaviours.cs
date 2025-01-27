using UnityEngine;

namespace BaldiTVAnnouncer
{
	public class Baldi_Announcer(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement) : Baldi_SubState(npc, baldi, prevState) 
	{
		readonly protected bool EventAnnouncement = eventAnnouncement;
		public override void PlayerInSight(PlayerManager player)
		{
		}
	}
	public class Baldi_GoToRoom(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement) : Baldi_Announcer(npc, baldi, prevState, eventAnnouncement)
	{
		const float delayIncrement = 0.15f;
		float smallSlapDelay = 0f;
		Vector3 positionToGo;
		Cell cellToGo;

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

			baldi.ClearSoundLocations();
			positionToGo = BaldiTVObject.availableTVs[Random.Range(0, BaldiTVObject.availableTVs.Count)].FrontPosition;
			cellToGo = baldi.ec.CellFromPosition(positionToGo);
			baldi.Hear(positionToGo, 127, false);
		}

		public override void DestinationEmpty()
		{
			base.DestinationEmpty();
			if (cellToGo == baldi.ec.CellFromPosition(baldi.transform.position))
				baldi.behaviorStateMachine.ChangeState(new Baldi_Speaking(npc, baldi, previousState, EventAnnouncement));
		}

		public override void Update()
		{
			base.Update();
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

		internal static SoundObject[] audGoToEvent;
	}

	public class Baldi_Speaking(NPC npc, Baldi baldi, NpcState prevState, bool eventAnnouncement) : Baldi_Announcer(npc, baldi, prevState, eventAnnouncement)
	{
		public readonly SpriteVolumeAnimator animator = baldi.GetComponent<SpriteVolumeAnimator>();
		public readonly AnimatedSpriteRotator rotator = baldi.GetComponent<AnimatedSpriteRotator>();

		public override void Enter()
		{
			base.Enter();
			// animator.enabled = true;
			// rotator.enabled = true;
		}
	}
}
