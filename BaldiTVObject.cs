using System.Collections.Generic;
using UnityEngine;

namespace BaldiTVAnnouncer
{
	public class BaldiTVObject : MonoBehaviour
	{
		public readonly static List<BaldiTVObject> availableTVs = [];

		public Vector3 FrontPosition => transform.position + transform.forward * 5f;

		public Vector3 DirToLookAt => -transform.forward;

		void Start() =>
			availableTVs.Add(this);
		void OnDestroy() =>
			availableTVs.Remove(this);
	}
}
