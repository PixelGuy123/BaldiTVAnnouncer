﻿using BepInEx;
using EditorCustomRooms;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using PlusLevelLoader;
using System.IO;
using UnityEngine;
using MTM101BaldAPI;
using PixelInternalAPI.Extensions;
using HarmonyLib;
using System.Collections.Generic;
using BaldiTVAnnouncer.Patches;

namespace BaldiTVAnnouncer
{
    [BepInPlugin(guid, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)] // let's not forget this
	[BepInDependency("pixelguy.pixelmodding.baldiplus.pixelinternalapi", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("mtm101.rulerp.baldiplus.levelloader", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("pixelguy.pixelmodding.baldiplus.editorcustomrooms", BepInDependency.DependencyFlags.HardDependency)]
	public class Plugin : BaseUnityPlugin
    {
		internal const string guid = "pixelguy.pixelmodding.baldiplus.balditvannouncer", editorGuid = "TvAnnouncer_";

		readonly AssetManager man = new();

		internal static string modPath;

		RoomGroup officeGroup = null;

#pragma warning disable IDE0051 // Remover membros privados não utilizados
		private void Awake()
#pragma warning restore IDE0051 // Remover membros privados não utilizados
		{
			Harmony h = new(guid);
			h.PatchAll();

			modPath = AssetLoader.GetModPath(this);

			GeneratorManagement.Register(this, GenerationModType.Addend, (_, __, sco) =>
			{
				if (sco.levelObject == null)
					return;
				sco.levelObject.roomGroup = sco.levelObject.roomGroup.InsertAtStart(officeGroup); // Should have the highest priority regardless
			});

			LoadingEvents.RegisterOnAssetsLoaded(Info, () => PostSetup(man), true);

			LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
			{
				var tvCamSprs = TextureExtensions.LoadSpriteSheet(4, 2, 35f, modPath, "baldoTv.png");

				var tvCam = ObjectCreationExtensions.CreateSpriteBillboard(tvCamSprs[0]);
				tvCam.name = "BaldiTVCam";
				tvCam.CreateAnimatedSpriteRotator(GenericExtensions.CreateRotationMap(8, tvCamSprs));
				AddObjectToEditor(tvCam.gameObject);

				tvCam.gameObject.AddComponent<BaldiTVObject>();

				// ---- Room Creation ----
				RegisterRoom("BaldiTVAnnouncerOffice",
				new(0f, 0.75f, 0f),
				ObjectCreators.CreateDoorDataObject("BaldiTvAnnouncerOffice", AssetLoader.TextureFromFile(Path.Combine(modPath, "officeOpen.png")), AssetLoader.TextureFromFile(Path.Combine(modPath, "officeClosed.png"))));

				var room = RoomFactory.CreateAssetsFromPath(Path.Combine(modPath, "baldiOffice.cbld"), 0, false, mapBg: AssetLoader.TextureFromFile(Path.Combine(modPath, "MapBG_Baldi.png")))[0];
				var placeholderTex = new WeightedTexture2D[1] { new() { selection = null, weight = 100 } };
				var placeholderLight = new WeightedTransform[1] { new() { selection = null, weight = 100 } };
				officeGroup = new()
				{
					potentialRooms = [new() { selection = room, weight = 100 }],
					stickToHallChance = 1f,
					name = "BaldiTvAnnouncerOffice",
					minRooms = 1,
					maxRooms = 1,
					ceilingTexture = placeholderTex,
					floorTexture = placeholderTex,
					wallTexture = placeholderTex,
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
				// Override Baldis
				foreach (var baldi in GenericExtensions.FindResourceObjects<Baldi>())
				{
					var rot = baldi.gameObject.AddComponent<AnimatedSpriteRotator>();
					rot.spriteMap = maps;
					rot.renderer = baldi.spriteRenderer[0];
					rot.targetSprite = baldSprs[0];
					rot.enabled = false;

					var animator = baldi.gameObject.AddComponent<SpriteVolumeAnimator>();
					animator.renderer = rot;
					animator.sensitivity = volAnim.sensitivity;
					animator.enabled = false;
					animator.usesAnimationCurve = true;
					animator.sprites = targetSpriteSheet;
					animator.bufferTime = volAnim.bufferTime;
					animator.volumeMultipler = 1.35f;
				}

			}, false);
			
        }

		void PostSetup(AssetManager man) { }

		void AddObjectToEditor(GameObject obj)
		{
			PlusLevelLoaderPlugin.Instance.prefabAliases.Add(editorGuid + obj.name, obj);
			man.Add($"editorPrefab_{obj.name}", obj);
			obj.ConvertToPrefab(true);
		}

		RoomSettings RegisterRoom(string roomName, Color color, StandardDoorMats mat)
		{
			var settings = new RoomSettings(EnumExtensions.ExtendEnum<RoomCategory>(roomName), RoomType.Room, color, mat);
			PlusLevelLoaderPlugin.Instance.roomSettings.Add(roomName, settings);
			return settings;
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
