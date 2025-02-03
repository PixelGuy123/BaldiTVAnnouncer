using BaldiLevelEditor;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using PlusLevelFormat;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BaldiTVAnnouncer.Patches
{
	[HarmonyPatch]
	[ConditionalPatchMod("mtm101.rulerp.baldiplus.leveleditor")]
	internal class EditorPatch
	{
		[HarmonyPatch(typeof(Plugin), "PostSetup")]
		[HarmonyPostfix]
		private static void MakeEditorSeeAssets(AssetManager man)
		{
			MarkObject(man.Get<GameObject>("editorPrefab_BaldiTVCam"), Vector3.up * 5f, false);

			string[] files = Directory.GetFiles(Path.Combine(Plugin.modPath, "EditorUI"));
			for (int i = 0; i < files.Length; i++)
				BaldiLevelEditorPlugin.Instance.assetMan.Add("UI/" + Path.GetFileNameWithoutExtension(files[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(files[i]), 40f));
		}

		static void MarkObject(GameObject obj, Vector3 offset, bool useActual = false)
		{
			markersToAdd.Add(Plugin.editorGuid + obj.name);
			BaldiLevelEditorPlugin.editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>(Plugin.editorGuid + obj.name, obj, offset, useActual));
		}

		static readonly List<string> markersToAdd = [];


		[HarmonyPatch(typeof(PlusLevelEditor), "Initialize")]
		[HarmonyPostfix]
		static void InitializeStuff(PlusLevelEditor __instance)
		{
			var objectCats = __instance.toolCats.Find(x => x.name == "objects").tools;

			foreach (var objMark in markersToAdd)
				objectCats.Add(new RotateAndPlacePrefab(objMark));

			__instance.toolCats.Find(x => x.name == "halls").tools.Add(new FloorTool("BaldiTVAnnouncerOffice"));
		}

		[HarmonyPatch(typeof(EditorLevel), "InitializeDefaultTextures")]
		[HarmonyPostfix]
		private static void AddRoomTexs(EditorLevel __instance) =>
			__instance.defaultTextures.Add("BaldiTVAnnouncerOffice", new TextureContainer("BlueCarpet", "Wall", "Ceiling"));


		
	}
}
