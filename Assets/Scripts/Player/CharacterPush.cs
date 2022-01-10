using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior allowing the player character to push elements of the scene.
/// </summary>
public class CharacterPush : MonoBehaviour
{
	[Header("Push Options")]
    [Tooltip("Mask specifying which objects can be pushed.")]
	public LayerMask pushLayers;
    [Tooltip("Enable pushing in the x-direction?")]
	public bool enablePushX;
    [Tooltip("Enable pushing in the y-direction?")]
	public bool enablePushY;
    [Tooltip("Enable pushing in the z-direction?")]
	public bool enablePushZ;
    [Tooltip("Disable pushing in highly vertical directions? Helps with standing stability.")]
	public bool limitStandingPush;
    [Tooltip("How strongly should the push affect the object.")]
	public float pushStrength = 1.1f;

	public bool anyPushEnabled => enablePushX || enablePushY || enablePushZ;

    /// <summary>
    /// Called before the first frame update.
    /// </summary>
	private void Start()
    {
	    // Register callback for hit detection.
	    var characterController = GetComponent<Character3DController>();
	    if (characterController)
	    { characterController.onColliderHit += OnColliderHit; }
    }

	private void OnColliderHit(Character3DController.ColliderHit hit)
	{
		if (anyPushEnabled)
		{ PushRigidBodies(hit); }
	}

	private void PushRigidBodies(Character3DController.ColliderHit hit)
	{
		// Inspired by https://docs.unity3d.com/ScriptReference/CharacterController.OnControllerColliderHit.html

		// Push only rigid bodies which are not kinematic.
		var body = hit.collider.attachedRigidbody;
		if (body == null || body.isKinematic) 
		{ return; }

		// Limit elements to the selected layers.
		if ((1 << body.gameObject.layer & pushLayers.value) == 0)
		{ return; }

		// Do not push elements we stand on.
		if (limitStandingPush && hit.moveDirection.y < -0.5f)
		{ return; }

		// Calculate direction of the push, keeping only the horizontal movement.
		var pushDirection = new Vector3(
			enablePushX ? hit.moveDirection.x : 0.0f, 
			enablePushY ? hit.moveDirection.y : 0.0f, 
			enablePushZ ? hit.moveDirection.z : 0.0f
		);

		// Apply the push force.
		body.AddForce(pushDirection * pushStrength, ForceMode.Impulse);
	}
}
