using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Simple input manager for storing current inputs..
/// </summary>
public class InputManager : MonoBehaviour
{
	[Header("Movement Control")]
	public Vector2 move;
	public bool jump;
	public bool sprint;
	public bool crouch;
	
	[Header("Switches")]
	public bool interact;
	public int selectedCharacter = 1;
	
	[Header("Mouse Control")]
	public Vector2 mouse;
	public bool primary;
	public bool primaryPressed;
	public bool primaryReleased;
	public bool secondary;
	public bool secondaryPressed;
	public bool secondaryReleased;
	public float scroll;

	[Header("Movement Settings")]
	public bool analogMovement;

#if !UNITY_IOS || !UNITY_ANDROID
	[Header("Mouse Settings")]
	public bool cursorLocked = true;
	public bool cursorInputForLook = true;
#endif

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    /// <summary>
    /// Update called once per frame.
    /// </summary>
    void Update()
    {
	    primaryPressed = false; primaryReleased = false;
	    secondaryPressed = false; secondaryReleased = false;
    }
    
	public void OnMove(InputValue value)
	{ MoveInput(value.Get<Vector2>()); }

	public void OnMouse(InputValue value)
	{
		if(cursorInputForLook)
		{ MouseInput(value.Get<Vector2>()); }
	}
	
	public void OnInteract(InputValue value)
	{ InteractInput(value.isPressed); }

	public void OnPrimary(InputValue value)
	{ PrimaryInput(value.isPressed); }
	
	public void OnSecondary(InputValue value)
	{ SecondaryInput(value.isPressed); }
	
	public void OnScroll(InputValue value)
	{ ScrollInput(value.Get<Vector2>()); }

	public void OnJump(InputValue value)
	{ JumpInput(value.isPressed); }

	public void OnSprint(InputValue value)
	{ SprintInput(value.isPressed); }
	
	public void OnCrouch(InputValue value)
	{ CrouchInput(value.isPressed); }

	public void OnCharacterSelect(InputValue value)
	{ CharacterSelectInput(value.Get<float>()); }
#else
    #error Old Unity input system is not supported!
#endif

	public void MoveInput(Vector2 newMoveDirection)
	{ move = newMoveDirection; } 

	public void MouseInput(Vector2 newLookDirection)
	{ mouse = newLookDirection; }
	
	public void InteractInput(bool newInteractState)
	{ interact = newInteractState; }

	public void PrimaryInput(bool newPrimary)
	{
		primary = newPrimary;
		primaryPressed = newPrimary;
		primaryReleased = !newPrimary;
	}
	
	public void SecondaryInput(bool newSecondary)
	{
		secondary = newSecondary;
		secondaryPressed = newSecondary;
		secondaryReleased = !newSecondary;
	}
	
	public void ScrollInput(Vector2 newScroll)
	{ scroll = newScroll.y; }

	public void JumpInput(bool newJumpState)
	{ jump = newJumpState; }

	public void SprintInput(bool newSprintState)
	{ sprint = newSprintState; }
	
	public void CrouchInput(bool newCrouchState)
	{ crouch = newCrouchState; }
	
	public void CharacterSelectInput(float newCharacterSelect)
	{ selectedCharacter = (int) newCharacterSelect; }
	
#if !UNITY_IOS || !UNITY_ANDROID

	private void OnApplicationFocus(bool hasFocus)
	{ SetCursorState(cursorLocked); }

	private void SetCursorState(bool newState)
	{ Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None; }

#endif

}