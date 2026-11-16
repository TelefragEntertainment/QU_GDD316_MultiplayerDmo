using Mirror;
using UnityEngine;

namespace Game
{
    public class NetManager : NetworkManager
    {
		public override void OnStartHost()
		{
			base.OnStartHost();
			Debug.Log("Host Started");
		}
    }
}