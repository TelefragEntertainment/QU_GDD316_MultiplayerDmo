using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
	[SerializeField] private float speed;
	private Vector2 moveInput;

	private void Update()
	{
		if (!isLocalPlayer) return;

		transform.position += (Vector3)moveInput * speed * Time.deltaTime;
		if (moveInput.sqrMagnitude > 0.01f)
		{
			// Calculate angle in degrees (assumes sprite faces Right by default)
			// Subtract 90f if your sprite faces Up by default
			float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;

			Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle + 270f);
			transform.rotation = Quaternion.RotateTowards(
				transform.rotation,
				targetRotation,
				1000f * Time.deltaTime
			);
		}
	}

	private void OnMove(InputValue value)
	{
		moveInput = value.Get<Vector2>();
		
	}
}
