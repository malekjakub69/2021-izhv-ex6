using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Simple behavior which automatically rotates any object it is attached to. </summary>
public class Rotator : MonoBehaviour
{
    [Header("Rotation Options")]
    [Tooltip("Speed of rotation.")]
    public float speed = 1.0f;
    [Tooltip("Euler angles representing the direction of rotation.")]
    public Vector3 direction = Vector3.right;
    
    /// <summary> Called when the script instance is first loaded. </summary>
    private void Awake()
    { }
    
    /// <summary> Called before the first frame update. </summary>
    void Start()
    { }

    /// <summary> Update called once per frame. </summary>
    void Update()
    { }

    /// <summary> Update called at time points with fixed delta. </summary>
    void FixedUpdate()
    { transform.Rotate(direction * speed * Time.fixedDeltaTime); }
}
