using UnityEngine;

namespace BaldiTVAnnouncer
{
	public class SpriteVolumeAnimator : MonoBehaviour // yoinked this from Times, which yoinked from mtm101BaldiDevAPI, so it works specifically with AnimtedSpriteRotator, not the Animator from the api
	{
		[SerializeField]
		public Sprite[] sprites;

		private bool wasPlayingLastFrame;

		[SerializeField]
		public float volumeMultipler = 3f;

		[SerializeField]
		public bool usesAnimationCurve = false;

		[SerializeField]
		public AnimationCurve sensitivity;

		[SerializeField]
		public AnimatedSpriteRotator renderer;

		[SerializeField]
		internal AudioManager audMan;

		public AudioManager AudMan
		{
			get => audMan; set
			{
				audMan = value;
				audioSource = audMan.audioDevice;
			}
		}

		public float bufferTime = 0.1f;

		private float[] clipData;

		private float volume;

		private float potentialVolume;

		private int lastSample;

		private int sampleBuffer;

		private AudioSource audioSource;

		private AudioClip currentClip;

		private void Update()
		{
			if (!audioSource)
			{
				audioSource = audMan.audioDevice;
				return;
			}

			if (sprites == null || sprites.Length == 0)
				return;

			if (sprites.Length != 1 && audioSource.clip && audioSource.isPlaying) // != because it shouldn't even have 0 sprites in the first place lol
			{
				if (audioSource.clip != currentClip)
				{
					currentClip = audioSource.clip;
					clipData = new float[currentClip.samples * currentClip.channels];
					currentClip.GetData(clipData, 0);
					lastSample = 0;
					sampleBuffer = Mathf.RoundToInt(currentClip.samples / currentClip.length * bufferTime);
				}

				if (!currentClip)
					return;

				volume = 0f;
				for (int i = Mathf.Max(lastSample - sampleBuffer, 0); i < audioSource.timeSamples * currentClip.channels && i < clipData.Length; i++)
				{
					if (!usesAnimationCurve)
						potentialVolume = Mathf.Abs(clipData[i]);

					else
						potentialVolume = sensitivity.Evaluate(Mathf.Abs(clipData[i]));


					if (potentialVolume > volume)
						volume = potentialVolume;
				}

				lastSample = audioSource.timeSamples * currentClip.channels;
				renderer.targetSprite = sprites[Mathf.RoundToInt(Mathf.Clamp(volume * volumeMultipler, 0f, 1f) * (sprites.Length - 1))];
				wasPlayingLastFrame = true;
			}
			else if (wasPlayingLastFrame)
			{
				wasPlayingLastFrame = false;
				renderer.targetSprite = sprites[0];
			}
		}
	}
}
