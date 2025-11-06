using System.Collections.Generic;
using System.IO;
using BaldiTVAnnouncer.Patches;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using PixelInternalAPI.Extensions;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;
using UnityEngine;

namespace BaldiTVAnnouncer
{
	[BepInPlugin(guid, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)] // let's not forget this
	[BepInDependency("pixelguy.pixelmodding.baldiplus.pixelinternalapi", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.HardDependency)]

	[BepInDependency(studioGUID, BepInDependency.DependencyFlags.SoftDependency)]
	public class Plugin : BaseUnityPlugin
	{
		internal const string guid = "pixelguy.pixelmodding.baldiplus.balditvannouncer", editorGuid = "TvAnnouncer_", studioGUID = "mtm101.rulerp.baldiplus.levelstudio";

		readonly AssetManager man = new();

		internal static string modPath;

		RoomGroup officeGroup = null;

#pragma warning disable IDE0051 // Remover membros privados não utilizados
		private void Awake()
#pragma warning restore IDE0051 // Remover membros privados não utilizados
		{
			Harmony h = new(guid);
			h.PatchAllConditionals();

			modPath = AssetLoader.GetModPath(this);
			AssetLoader.LoadLocalizationFolder(Path.Combine(modPath, "Language", "English"), Language.English);

			GeneratorManagement.Register(this, GenerationModType.Addend, (_, __, sco) =>
			{
				foreach (var levelObject in sco.GetCustomLevelObjects())
				{
					if (levelObject.IsModifiedByMod(Info)) continue;
					levelObject.roomGroup = levelObject.roomGroup.InsertAtStart(officeGroup); // Should have the highest priority regardless
					levelObject.MarkAsModifiedByMod(Info);
				}
			});

			LoadingEvents.RegisterOnAssetsLoaded(Info, () => PostSetup(man), LoadingEventOrder.Post);

			LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
			{
				var tvCamSprs = TextureExtensions.LoadSpriteSheet(4, 2, 55f, modPath, "baldoTv.png");

				var tvCam = ObjectCreationExtensions.CreateSpriteBillboard(tvCamSprs[0]);
				tvCam.name = "BaldiTVCam";
				tvCam.CreateAnimatedSpriteRotator(GenericExtensions.CreateRotationMap(8, tvCamSprs));
				AddObjectToEditor(tvCam.gameObject);

				tvCam.gameObject.AddComponent<BaldiTVObject>();

				// ---- Room Creation ----
				RegisterRoom("BaldiTVAnnouncerOffice",
				new(0f, 0.75f, 0f),
				ObjectCreators.CreateDoorDataObject("BaldiTvAnnouncerOffice", AssetLoader.TextureFromFile(Path.Combine(modPath, "officeOpen.png")), AssetLoader.TextureFromFile(Path.Combine(modPath, "officeClosed.png"))));

				var mapIcon = AssetLoader.TextureFromFile(Path.Combine(modPath, "MapBG_Baldi.png"));
				List<RoomAsset> rooms = [LoadRoom("baldiOffice.rbpl", mapIcon), LoadRoom("baldiOffice_2.rbpl", mapIcon)];

				var placeholderTex = new WeightedTexture2D[1] { new() { selection = null, weight = 100 } };
				var placeholderLight = new WeightedTransform[1] { new() { selection = null, weight = 100 } };

				officeGroup = new()
				{
					potentialRooms = [.. rooms.ConvertAll(x => new WeightedRoomAsset() { selection = x, weight = 100 })],
					stickToHallChance = 1f,
					name = "BaldiTvAnnouncerOffice",
					minRooms = 1,
					maxRooms = 1,
					// new TextureContainer("BlueCarpet", "Wall", "Ceiling")
					ceilingTexture = [new() { selection = LevelLoaderPlugin.Instance.roomTextureAliases["Ceiling"], weight = 100 }],
					floorTexture = [new() { selection = LevelLoaderPlugin.Instance.roomTextureAliases["BlueCarpet"], weight = 100 }],
					wallTexture = [new() { selection = LevelLoaderPlugin.Instance.roomTextureAliases["Wall"], weight = 100 }],
					light = placeholderLight
				};


				// Get Baldi sprites
				const int rows = 24, columns = 4, sprsPerArray = 8;

				var baldSprs = TextureExtensions.LoadSpriteSheet(columns, rows, 27f, modPath, "baldiTvRepresentationSheet_4x24.png");
				BaldiTVPatches.idleSprite = baldSprs[0];
				Sprite[] mapsList = new Sprite[sprsPerArray];
				var maps = new SpriteRotationMap[rows / 2 - 1];
				int z = 0, y = 0;
				for (int i = 0; i < baldSprs.Length; i++) // Basically get every rotation map from the huge array
				{
					if (mapsList[0] != null && i % sprsPerArray == 0)
					{
						maps[z++] = GenericExtensions.CreateRotationMap(sprsPerArray, mapsList);
						mapsList = new Sprite[sprsPerArray]; // Makes a new reference
						y = 0;
					}
					mapsList[y++] = baldSprs[i];
				}

				Sprite[] targetSpriteSheet = new Sprite[maps.Length];
				for (int i = 0; i < maps.Length; i++)
					targetSpriteSheet[i] = maps[i].spriteSheet[0];

				var volAnim = GenericExtensions.FindResourceObject<CoreGameManager>().hudPref.BaldiTv.GetComponentInChildren<VolumeAnimator>();

				Baldi_GoToRoom.audGoToEvent = [
					ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(modPath, "BAL_Hurry_1.wav")), "BAL_Announcer_HurryUp_1", SoundType.Voice, Color.green),
					ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(modPath, "BAL_Hurry_2.wav")), "BAL_Announcer_HurryUp_2", SoundType.Voice, Color.green)
					];

				Baldi_EndSpeaking.audEndEvent = [
					ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(modPath, "BAL_Done_1.wav")), "BAL_Announcer_Done_1", SoundType.Voice, Color.green),
					ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(modPath, "BAL_Done_2.wav")), "BAL_Announcer_Done_2", SoundType.Voice, Color.green),
					ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(modPath, "BAL_Done_3.wav")), "BAL_Announcer_Done_3", SoundType.Voice, Color.green)
					];

				Baldi_EndSpeaking.audEndEvent_NoTime = [
					ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(modPath, "BAL_HurryDone_1.wav")), "BAL_Announcer_HurryDone_1", SoundType.Voice, Color.green),
					ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(Path.Combine(modPath, "BAL_HurryDone_2.wav")), "BAL_Announcer_HurryDone_2", SoundType.Voice, Color.green)
					];

				var baldiPre = ObjectCreationExtensions.CreateAnimatedSpriteRotator(
					ObjectCreationExtensions.CreateSpriteBillboard(baldSprs[0]),
					maps
				);
				baldiPre.name = "SpeakerBaldi";
				baldiPre.targetSprite = baldSprs[0];
				baldiPre.renderer.name = "BaldiSpeakerSprite";
				baldiPre.gameObject.ConvertToPrefab(true);

				Baldi_Announcer.talkingBaldiPre = baldiPre.gameObject.AddComponent<SpriteVolumeAnimator>();
				Baldi_Announcer.talkingBaldiPre.renderer = baldiPre;
				Baldi_Announcer.talkingBaldiPre.sensitivity = volAnim.sensitivity;
				Baldi_Announcer.talkingBaldiPre.usesAnimationCurve = true;
				Baldi_Announcer.talkingBaldiPre.sprites = targetSpriteSheet;
				Baldi_Announcer.talkingBaldiPre.bufferTime = volAnim.bufferTime;
			}, LoadingEventOrder.Pre);

		}

		void PostSetup(AssetManager man)
		{
			if (Chainloader.PluginInfos.ContainsKey(studioGUID))
				EditorPatch.Initialize(man);
		}

		void AddObjectToEditor(GameObject obj)
		{
			LevelLoaderPlugin.Instance.basicObjects.Add(editorGuid + obj.name, obj);
			man.Add($"editorPrefab_{obj.name}", obj);
			obj.ConvertToPrefab(true);
		}

		RoomSettings RegisterRoom(string roomName, Color color, StandardDoorMats mat)
		{
			var settings = new RoomSettings(EnumExtensions.ExtendEnum<RoomCategory>(roomName), RoomType.Room, color, mat);
			LevelLoaderPlugin.Instance.roomSettings.Add(roomName, settings);
			return settings;
		}

		RoomAsset LoadRoom(string fileName, Texture2D mapBg)
		{
			using BinaryReader reader = new(File.Open(Path.Combine(modPath, fileName), FileMode.Open));
			var asset = LevelImporter.CreateRoomAsset(BaldiRoomAsset.Read(reader));
			if (mapBg != null)
			{
				asset.mapMaterial = new(asset.mapMaterial);
				asset.mapMaterial.SetTexture("_MapBackground", mapBg);
				asset.mapMaterial.SetShaderKeywords(["_KEYMAPSHOWBACKGROUND_ON"]);
				asset.mapMaterial.name = asset.name;
			}
			return asset;
		}
	}

	internal static class ArrayExtensions
	{
		public static T[] InsertAtStart<T>(this T[] ar, T element)
		{
			var newAr = new T[ar.Length + 1];
			newAr[0] = element;
			for (int i = 0; i < ar.Length; i++)
				newAr[i + 1] = ar[i];
			return newAr;
		}
	}
}
