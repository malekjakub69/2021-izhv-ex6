using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Script used for changing the current player character.
/// </summary>
public class CharacterSelector : MonoBehaviour
{
	[Header("Character")] 
	[Tooltip("List of available character to switch between")]
	public List<GameObject> characters;
	
	/// <summary>
	/// Currently used character prefab, starting with 1 for the first character.
	/// </summary>
	private int mCurrentCharacter = 0;
	
	private InputManager mInput;
	private GameObject mCharacter;
	private Animator mAnimator;
	
	/// <summary>
	/// Bounding box of the character.
	/// </summary>
	public struct CharacterBB
	{
		public CharacterBB(Vector3 s, Vector3 c)
		{ size = s; center = c; }
		
		public Vector3 size;
		public Vector3 center;
	} // struct CharacterBB
	
	private CharacterBB mCharacterBB;
	
	/// <summary>
	/// Current character animator.
	/// </summary>
	public Animator charAnimator
	{ get => mAnimator; }
	
	/// <summary>
	/// Current character collider.
	/// </summary>
	public CharacterBB characterBB
	{ get => mCharacterBB; }
	
	/// <summary>
	/// Current character GameObject.
	/// </summary>
	public GameObject character
	{ get => mCharacter; }

	/// <summary> Storage for the character enabler. </summary>
	private bool mCharacterEnabled = true;

	/// <summary> Is the character enabled? </summary>
	public bool characterEnabled
	{
		get
		{ return character != null ? character.activeSelf : false; }
		set
		{
			if (character != null)
			{ character.SetActive(value); }
			mCharacterEnabled = value;
		}
	}

	/// <summary>
    /// Search the children for the Model game object.
    /// </summary>
    /// <returns>The Model game object.</returns>
    [CanBeNull]
    GameObject FindCharacterGO()
    {
	    var modelGO = Util.Common.GetChildByName(gameObject, "Model");
	    if (modelGO != null) { return modelGO; }
	    
	    modelGO = Util.Common.GetChildByScript<Rigidbody>(gameObject);
	    if (modelGO != null) { return modelGO; }

	    modelGO = Util.Common.GetChildByScript<Rigidbody2D>(gameObject);

	    return modelGO;
    }

    /// <summary>
    /// Called when script is initialized.
    /// </summary>
    private void Awake()
    { mCharacter = FindCharacterGO(); }

    /// <summary>
	/// Called before the first frame update.
	/// </summary>
	void Start()
	{
		Debug.Assert(characters.Count > 0, "At least one character prefab must be specified!");
		
        mInput = GetComponent<InputManager>();
        mAnimator = null;
        mCharacterBB = default;
	}

    /// <summary>
    /// Update called once per frame.
    /// </summary>
    void Update()
    {
	    SelectCharacter(mInput.selectedCharacter);
    }

    /// <summary>
    /// Select given character as current.
    /// </summary>
    /// <param name="selection">Character selection starting at 1.</param>
    /// <returns>Returns true if the character changed.</returns>
    bool SelectCharacter(int selection)
    {
	    selection = Math.Clamp(selection, 1, characters.Count);
	    if (selection == mCurrentCharacter)
	    { return false; }
	    
	    // Change over to the new character.
	    if (mCharacter != null)
	    { Destroy(mCharacter); }
	    mCharacter = Instantiate(characters[selection - 1], transform);
	    mCurrentCharacter = selection;
	    
	    // Update references.
	    mAnimator = mCharacter.GetComponent<Animator>();
	    var boxCollider = mCharacter.GetComponent<BoxCollider>();
	    if (boxCollider)
	    { // Supported 3D collider is available.
		    mCharacterBB.size = boxCollider.size;
		    mCharacterBB.center = boxCollider.center;
	    }
		else
	    { // Supported 3D collider is not available, fall back to 2D.
		    var boxCollider2D = mCharacter.GetComponent<BoxCollider2D>();
		    Debug.Assert(boxCollider2D, "No supported collider found, add BoxCollider or BoxCollider2D!");

		    mCharacterBB.size = new Vector3(boxCollider2D.size.x, boxCollider2D.size.y, 0.1f);
		    mCharacterBB.center = new Vector3(boxCollider2D.offset.x, boxCollider2D.offset.y, 0.0f);
	    }
	    
	    mCharacter.SetActive(mCharacterEnabled);
	    
	    // Notify siblings that we changed character.
	    BroadcastMessage("OnCharacterChange", this);

	    return true;
    }
}
