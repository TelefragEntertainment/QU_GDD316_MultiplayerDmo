using Mirror;
using System;
using UnityEngine;

namespace Game
{
    public class NetManager : NetworkManager
    {
		public static Action OnGameStarted;

		public override void OnDestroy()
		{
			base.OnDestroy();
			OnGameStarted = null;
		}

		public override void OnStartHost()
		{
			base.OnStartHost();
			Debug.Log("Host Started");
		}

		public override void OnClientSceneChanged()
		{
			base.OnClientSceneChanged();
			OnGameStarted?.Invoke();
		}
    }
}