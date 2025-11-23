using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelFormat;
using UnityEngine;

namespace BaldiTVAnnouncer.Patches
{
	[HarmonyPatch]
	[ConditionalPatchMod(Plugin.studioGUID)]
	internal static class EditorPatch
	{
		internal static void Initialize(AssetManager man)
		{
			LoadEditorAssets();
			InitializeVisuals(man);
			InitializeDefaultTextures(LevelStudioPlugin.Instance.defaultRoomTextures);
			EditorInterfaceModes.AddModeCallback(InitializeTools);
		}

		static void LoadEditorAssets()
		{
			_editorAssetMan = new AssetManager();
			string editorUIPath = Path.Combine(Plugin.modPath, "EditorUI");

			// Load all general UI sprites
			string[] files = Directory.GetFiles(editorUIPath);
			foreach (string file in files)
			{
				string name = Path.GetFileNameWithoutExtension(file);
				_editorAssetMan.Add("UI/" + name, AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(file), 40f));
			}
		}

		static void InitializeVisuals(AssetManager man)
		{
			EditorInterface.AddObjectVisualWithCustomSphereCollider(Plugin.editorGuid + "BaldiTVCam", man.Get<GameObject>("editorPrefab_BaldiTVCam"), 1.5f, Vector3.zero)
				.gameObject.ReplaceAnimatedRotators();
		}

		static void InitializeDefaultTextures(Dictionary<string, TextureContainer> containers)
		{
			containers.Add("BaldiTVAnnouncerOffice", new TextureContainer("BlueCarpet", "Wall", "Ceiling"));
		}

		private static void InitializeTools(EditorMode mode, bool isVanillaCompliant)
		{
			EditorInterfaceModes.AddToolToCategory(mode, "objects", new ObjectTool(Plugin.editorGuid + "BaldiTVCam", GetSprite("UI/Object_TvAnnouncer_BaldiTVCam", "UI/object_TvAnnouncer_BaldiTVCam"), 5f));
			EditorInterfaceModes.AddToolToCategory(mode, "rooms", new RoomTool("BaldiTVAnnouncerOffice", GetSprite("UI/Floor_BaldiTVAnnouncerOffice", $"UI/floor_BaldiTVAnnouncerOffice")));
		}

		private static Sprite GetSprite(string key1, string key2)
		{
			var spr = _editorAssetMan.ContainsKey(key1) ? _editorAssetMan.Get<Sprite>(key1) : _editorAssetMan.Get<Sprite>(key2);
			return spr;
		}

		private static AssetManager _editorAssetMan;
	}
}
