using Mirror;
using UnityEngine;

namespace Game
{
    public class Missile : NetworkBehaviour
    {
        [SerializeField] private float radius;
		[SerializeField] private float accel;
		[SerializeField] private SpriteRenderer blastSpr;

		private float speed = 5f;
		private float lifeTime = 3f;
		[SyncVar] private int owner;
		[SyncVar] private bool alive = true;

		public void SetOwner(int newOwner)
		{
			this.owner = newOwner;
			alive = true;
			Debug.Log("Owner: " +  newOwner);
		}

		private void Update()
		{
			lifeTime -= Time.deltaTime;

			if (alive)
			{
				transform.position += transform.up * speed * Time.deltaTime;
				speed += accel * Time.deltaTime;
			}

			if (!alive)
			{
				blastSpr.enabled = true;
				if (isServer)
				{
					if (lifeTime <= 0)
					{
						Explode();
					}
					return;
				}

				Collider2D col = Physics2D.OverlapCircle(transform.position, radius);
				if (col != null)
				{
					if(col.transform.parent.TryGetComponent<Player>(out Player player))
					{
						if ((int)player.GetComponent<NetworkIdentity>().netId != owner)
						{
							lifeTime = 0.2f;
							alive = false;
							return;
						}
					}
				}

				if (lifeTime < 0)
					Explode();
			}
		}

		[Server]
		private void Explode()
		{
			NetworkServer.Destroy(gameObject);
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position, radius);
		}
	}
}