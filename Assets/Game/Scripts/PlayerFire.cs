using Game;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFire : NetworkBehaviour
{
	[SerializeField] private float delay = 1f;
	[SerializeField] private Missile missilePrefab;

	private float fireTime;

	private void Start()
	{
		fireTime = delay;
	}

	private void Update()
	{
		fireTime -= Time.deltaTime;
	}

	private void OnAttack()
	{
		if (fireTime < 0)
		{
			fireTime = delay;
			CmdFire(transform.position, transform.up); // tells server to spawn missile
		}
	}

	/// <summary>
	/// Called on host/server
	/// </summary>
	/// <param name="spawnPos"></param>
	/// <param name="up"></param>
	[Command]
	private void CmdFire(Vector3 spawnPos, Vector3 up)
	{
		// Instantiate on the server
		GameObject missile = Instantiate(missilePrefab, spawnPos, Quaternion.identity).gameObject;
		missile.transform.up = up;
		missile.GetComponent<Missile>().SetOwner((int)GetComponentInParent<NetworkIdentity>().netId);


		// Spawn on clients
		NetworkServer.Spawn(missile);
	}
}
