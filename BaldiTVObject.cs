using System.Collections.Generic;
using UnityEngine;

namespace BaldiTVAnnouncer
{
	public class BaldiTVObject : MonoBehaviour
	{
		public readonly static List<BaldiTVObject> availableTVs = [];

		public Vector3 FrontPosition => transform.position + transform.forward * 2.5f;

		void Start() =>
			availableTVs.Add(this);
		void OnDestroy() =>
			availableTVs.Remove(this);
	}
}
