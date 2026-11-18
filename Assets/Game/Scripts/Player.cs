using Mirror;
using UnityEngine;

namespace Game
{
	public class Player : NetworkBehaviour
	{
		private readonly Color[] colors = new Color[] {Color.red, Color.orange, Color.yellow, Color.green, Color.blue, Color.indigo, Color.violet, Color.black, Color.white };

		[SerializeField] private GameObject[] avatar;

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
			NetManager.OnGameStarted += HandleGameStart;
		}

		private void HandleGameStart()
		{
			foreach (GameObject g in avatar)
			{
				g.SetActive(true);
				SpriteRenderer sprrend = g.GetComponentInChildren<SpriteRenderer>();
				if (sprrend != null) sprrend.color = colors[netId % colors.Length];
			}


			float angle = (netId / (float)NetworkServer.connections.Count) * Mathf.PI * 2f;

			float posX = Mathf.Cos(angle) * 10;
			float posY = Mathf.Sin(angle) * 10;
			transform.position = new Vector3(posX, posY, 0f);
		}
	}
}