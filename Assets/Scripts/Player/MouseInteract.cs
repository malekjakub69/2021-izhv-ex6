using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Behavior allowing the player to interact with the scene.
/// </summary>
public class MouseInteract : MonoBehaviour
{
    [Header("References")] 
    [Tooltip("Primary camera used by the player to interact with the objects.")]
    public Camera primaryCamera;
    [Header("Settings")] 
    [Tooltip("Which layers are interactable?")]
	public LayerMask interactLayers;
    [Tooltip("Maximum distance to cast the selection rays.")]
	public float selectionDistance = 100.0f;

    [Space]
    [Header("Tool Options")] 
    [Tooltip("Interaction mode for the primary tool.")]
    public PrimaryInteractMode primaryInteractMode = PrimaryInteractMode.Move;
    [Header("Move Tool")] 
    [Header("Spring Tool")] 
    [Tooltip("Strength of the spring for the ")]
    public float springStrength = 20.0f;
    public float springDamper = 5.0f;
    [Space]
    
    [Header("Outline Options")] 
    [Tooltip("Outline selected objects?")] 
    public bool outlineSelect = true;
    [Tooltip("Color of the selection outline.")] 
    public Color outlineSelectColor = Color.red;
    [Tooltip("Width of the outline.")] 
    [Range(0f, 10f)]
    public float outlineWidth = 2.0f;
    [Tooltip("Mode of the outline.")] 
    public Outline.Mode outlineMode = Outline.Mode.OutlineAll;

    /// <summary>
    /// Interaction modes for the primary interaction tool
    /// </summary>
    public enum PrimaryInteractMode
    {
        None, 
        Move, 
        Spring, 
        //Impulse
    } // enum PrimaryInteractMode

    /// <summary>
    /// Common interface for all operations.
    /// </summary>
    internal interface IInteractOperation
    {
        public void RunOperation(MouseInteract mi);
        public void OnSelect(GameObject go, MouseInteract mi);
        public void OnDeselect(GameObject go, MouseInteract mi);
    }
    
    /// <summary> Available primary operations</summary>
    private readonly Dictionary<PrimaryInteractMode, Func<MouseInteract, IInteractOperation>> PRIMARY_OPS = new()
    {
        [ PrimaryInteractMode.Move ] = (mi) => new PrimaryInteractMove(mi), 
        [ PrimaryInteractMode.Spring ] = (mi) => new PrimaryInteractSpring(mi)
    };
    
    /// <summary> Currently used input manager containing player inputs. </summary>
	private InputManager mInput;
    /// <summary> Primary mouse used for getting specific player inputs. </summary>
	private Mouse mMouse;

    /// <summary> Currently used primary operation. </summary>
    private IInteractOperation mPrimaryOp = null;
    /// <summary> Identifier of the currently used primary operation. </summary>
    private PrimaryInteractMode mCurrentPrimaryOp = PrimaryInteractMode.None;
    
    /// <summary>
    /// Called before the first frame update.
    /// </summary>
    void Start()
    {
        mInput = GetComponent<InputManager>();
        mMouse = Mouse.current;
        
        UpdatePrimaryMode();
    }

    /// <summary>
    /// Update called once per frame.
    /// </summary>
    void Update()
    {
        UpdatePrimaryMode();
        
        if (mInput.interact)
        { PrimaryInteract(); }
    }

    /// <summary> Update the primary interaction operation. </summary>
    void UpdatePrimaryMode()
    {
        if (mCurrentPrimaryOp != primaryInteractMode)
        {
            mPrimaryOp = PRIMARY_OPS[primaryInteractMode](this);
            mCurrentPrimaryOp = primaryInteractMode;
        }
    }

    /// <summary>
    /// Perform primary interaction.
    /// </summary>
    void PrimaryInteract()
    {
        mPrimaryOp?.RunOperation(this);
    }

    /// <summary>
    /// Perform Collider picking from a given camera.
    /// </summary>
    /// <param name="ssPosition">Picking coordinate in screen-space.</param>
    /// <returns>Returns the Collider corresponding to the given position or null.</returns>
    [CanBeNull]
    internal Collider PickSceneObject(Camera camera, Vector2 ssPosition)
    {
        var ray = camera.ScreenPointToRay(ssPosition);
        var hitValid = Physics.Raycast(ray, out var hit, selectionDistance, interactLayers);

        return hitValid ? hit.collider : null;
    }

    /// <summary> Helper tagging component used for selected GameObjects. </summary>
    class SelectedTag : MonoBehaviour
    {
        /// <summary> Reference to the parent MouseInteract script. </summary>
        internal MouseInteract mi;

        /// <summary> Reference to the added Outline script. </summary>
        internal Outline mOutline;

        /// <summary> Make the parent GameObject selected. </summary>
        private void Awake()
        {
            if (mi.outlineSelect)
            { // Add outline if enabled.
                var outline = gameObject.GetComponent<Outline>();
                if (!outline)
                { outline = gameObject.AddComponent<Outline>(); }

                outline.enabled = true;
                outline.OutlineColor = mi.outlineSelectColor;
                outline.OutlineWidth = mi.outlineWidth;
                outline.OutlineMode = mi.outlineMode;
                
                mOutline = outline;
            }
        }

        /// <summary> Make the parent GameObject not selected. </summary>
        private void OnDestroy()
        {
            if (mOutline)
            { Destroy(mOutline); }
        }
    } // class SelectedTag

    /// <summary>
    /// Select GameObject represented by its collider.
    /// </summary>
    /// <param name="collider">Collider of the target GameObject.</param>
    /// <param name="interaction">Optional interaction which triggered this operation.</param>
    /// <returns>Returns the selected GameObject.</returns>
    [CanBeNull]
    internal GameObject SelectGameObject([CanBeNull] Collider collider, 
        [CanBeNull] IInteractOperation interaction = null)
    {
        if (!collider)
        { return null; }
        
        var go = collider.gameObject;
        
        // First make sure we don't have any previous selection on the target.
        var oldTag = go.GetComponent<SelectedTag>();
        if (oldTag)
        { DestroyImmediate(oldTag); }
        
        // Hack to prevent triggering Awake method before initializing.
        go.SetActive(false);
        // Then add the selection tag to the target GameObject.
        go.AddComponent<SelectedTag>().mi = this;
        // Make the GameObject active again, triggering Awake.
        go.SetActive(true);
        
        // Trigger OnSelect if possible.
        interaction?.OnSelect(go, this);

        return go;
    }
    
    /// <summary>
    /// Deselect GameObject, reversing any changes made in the SelectGameObject method.
    /// </summary>
    /// <param name="go">GameObject to deselect</param>
    /// <param name="interaction">Optional interaction which triggered this operation.</param>
    internal void DeselectGameObject([CanBeNull] GameObject go, 
        [CanBeNull] IInteractOperation interaction = null)
    {
        if (!go)
        { return; }
        
        // Trigger OnDeSelect if possible.
        interaction?.OnDeselect(go, this);
        
        // Remove changes we made during the selection.
        var selectedTag = go.GetComponent<SelectedTag>();
        if (selectedTag)
        { DestroyImmediate(selectedTag); }
    }

    /// <summary> Create an empty GameObject parented to this one. Optionally provide a name for it. </summary>
    internal GameObject CreateEmptyChildGO(string name = "")
    {
        // Create the GameObject.
        var go = name.Length == 0 ? new GameObject() : new GameObject(name);
        // Use this GameObject as its parent.
        go.transform.parent = transform;

        return go;
    }

    /// <summary>
    /// Class implementing the Move mode for the primary interaction mode.
    /// </summary>
    private class PrimaryInteractMove : IInteractOperation
    {
        /// <summary> Currently selected GameObject</summary>
        [CanBeNull] private GameObject mSelected = null;

        /// <summary> Was kinematic mode originally enabled? </summary>
        private bool mKinematicEnabled = false;
        
        /// <summary> Prepare the interaction mode. </summary>
        public PrimaryInteractMove(MouseInteract mi)
        { }
        
        /// <summary> De-initialize and destroy. </summary>
        ~PrimaryInteractMove()
        { }

        /// <summary> Operations triggered when selecting a new GameObject. </summary>
        public void OnSelect(GameObject go, MouseInteract mi)
        {
            // Switch to kinematic mode if the GameObject has Rigidbody associated.
            var rb = go.GetComponent<Rigidbody>();
            if (rb)
            { mKinematicEnabled = rb.isKinematic; rb.isKinematic = true; }
        }
        
        /// <summary> Operations triggered when deselecting a GameObject. </summary>
        public void OnDeselect(GameObject go, MouseInteract mi)
        {
            // Return the kinematic mode to original setting.
            var rb = go.GetComponent<Rigidbody>();
            if (rb)
            { rb.isKinematic = mKinematicEnabled; }
        }
    
        /// <summary> Run the Operation. </summary>
        public void RunOperation(MouseInteract mi)
        {
            // Get current mouse state.
            var mousePosition = mi.mMouse.position.ReadValue();
            var mouseDelta = mi.mMouse.delta.ReadValue();
            
            if (mi.mMouse.leftButton.wasPressedThisFrame)
            { // Pressing left mouse button -> Select object.
                mSelected = mi.SelectGameObject(mi.PickSceneObject(mi.primaryCamera, mousePosition), this);
            }
            if (mi.mMouse.leftButton.wasReleasedThisFrame)
            { // Releasing left mouse button -> Deselect object.
                mi.DeselectGameObject(mSelected, this);
                mSelected = null;
            }

            if (mSelected)
            { // Move the selected object to the mouse cursor position, which is projected onto a parallel plane.
                var ssSelectedPosition = mi.primaryCamera.WorldToScreenPoint(mSelected.transform.position);
                var ssPlanarMousePosition = new Vector3(
                    mousePosition.x, mousePosition.y, 
                    ssSelectedPosition.z
                );
                var wsMousePosition = mi.primaryCamera.ScreenToWorldPoint(ssPlanarMousePosition);
                mSelected.transform.position = wsMousePosition;
            }
        }
    } // class PrimaryInteractMove
    
    /// <summary>
    /// Class implementing the Spring mode for the primary interaction mode.
    /// </summary>
    private class PrimaryInteractSpring : IInteractOperation
    {
        /// <summary> Currently selected GameObject</summary>
        [CanBeNull] private GameObject mSelected = null;

        /// <summary> Original screen-space position of the selected GameObject. </summary>
        private Vector3 mSsSelectedPosition;
        /// <summary> Anchor for the spring. </summary>
        private GameObject mAnchor = null;
        /// <summary> Rigidbody component of our anchor. </summary>
        private Rigidbody mAnchorRigidbody = null;
        /// <summary> Currently used spring connecting the selected GameObject to our anchor. </summary>
        private SpringJoint mSpring = null;

        /// <summary> Prepare the interaction mode. </summary>
        public PrimaryInteractSpring(MouseInteract mi)
        {
            // Create the anchor and add Rigidbody to connect to.
            mAnchor = mi.CreateEmptyChildGO("Anchor");
            mAnchorRigidbody = mAnchor.AddComponent<Rigidbody>();
            mAnchorRigidbody.isKinematic = true;
        }

        /// <summary> De-initialize and destroy. </summary>
        ~PrimaryInteractSpring()
        { if (mAnchor) { GameObject.DestroyImmediate(mAnchor); } }

        /// <summary> Operations triggered when selecting a new GameObject. </summary>
        public void OnSelect(GameObject go, MouseInteract mi)
        {
            // Move the anchor to neutral position within the target GameObject.
            mAnchor.transform.position = go.transform.position;
            
            // Create the spring.
            mSpring = go.AddComponent<SpringJoint>();
            mSpring.autoConfigureConnectedAnchor = false;
            
            // Configure the spring.
            mSpring.spring = mi.springStrength;
            mSpring.damper = mi.springDamper;
            
            // Link the target GameObject to our anchor.
            mSpring.connectedBody = mAnchorRigidbody;
            mSpring.anchor = Vector3.zero;
            mSpring.connectedAnchor = Vector3.zero;
            mSpring.autoConfigureConnectedAnchor = true;
        }
        
        /// <summary> Operations triggered when deselecting a GameObject. </summary>
        public void OnDeselect(GameObject go, MouseInteract mi)
        {
            // Disconnect the spring by destroying it.
            if (mSpring)
            { GameObject.Destroy(mSpring); mSpring = null; }
        }
    
        /// <summary> Run the Operation. </summary>
        public void RunOperation(MouseInteract mi)
        {
            // Get current mouse state.
            var mousePosition = mi.mMouse.position.ReadValue();
            var mouseDelta = mi.mMouse.delta.ReadValue();
            
            if (mi.mMouse.leftButton.wasPressedThisFrame)
            { // Pressing left mouse button -> Select object.
                mSelected = mi.SelectGameObject(mi.PickSceneObject(mi.primaryCamera, mousePosition), this);
                if (mSelected)
                { mSsSelectedPosition = mi.primaryCamera.WorldToScreenPoint(mSelected.transform.position); }
            }
            if (mi.mMouse.leftButton.wasReleasedThisFrame)
            { // Releasing left mouse button -> Deselect object.
                mi.DeselectGameObject(mSelected, this);
                mSelected = null;
            }

            if (mSelected)
            { // Move the anchor to the mouse cursor position, which is projected onto a parallel plane.
                var ssPlanarMousePosition = new Vector3(
                    mousePosition.x, mousePosition.y, 
                    mSsSelectedPosition.z
                );
                var wsMousePosition = mi.primaryCamera.ScreenToWorldPoint(ssPlanarMousePosition);
                mAnchor.transform.position = wsMousePosition;
            }
        }
    } // class PrimaryInteractSpring
}
