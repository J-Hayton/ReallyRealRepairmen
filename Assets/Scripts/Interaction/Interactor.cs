﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactor : MonoBehaviour {
	//Change the enabled variable to disable and re-enable this

	[Header("Components")]
	public Transform CamTrans;

	public UIManager uiManager;

	[Header("Settings")]
	public float castRadius = 0.5f;

	/// <summary>
	/// The range of the interactable physics cast
	/// </summary>
	public float interactRange = 5f;

	//--------------- PRIVATE VARIABLES ---------------

	/// <summary>
	/// This holds the layer numnber of the interactable objects
	/// </summary>
	private int interactLayer;

	/// <summary>
	/// This is the physics mask that is used to search for a interactable object
	/// </summary>
	private int interactMask;

	/// <summary>
	/// This is holding the interactable reference from the previous frame
	/// </summary>
	private Interactable _lastInteract;

	/// <summary>
	/// Used to count how long the mouse has been hold down for while over a interactable object
	/// </summary>
	private float _holdCounter = 0f;

	/// <summary>
	/// Tracks if an interaction has been attempted and only allows another interaction until the mouse button has been released
	/// </summary>
	private bool _interactAttempted = false;

	/// <summary>
	/// The interaction state from the last frame
	/// </summary>
	private bool _previousInteract = false;
	
	// Use this for initialization
	void Start () 
	{
		interactLayer = LayerMask.NameToLayer("Interact");
		interactMask = 1 << interactLayer | 1 << LayerMask.NameToLayer("Floor") | 1 << LayerMask.NameToLayer("Default");
	}
	
	// Update is called once per frame
	void Update () 
	{
		Interactable newInteract = null;

		//Attempt to find a interactable
		Ray ray = new Ray(CamTrans.position, CamTrans.forward);
		RaycastHit hitInfo;
		
		if(Physics.SphereCast(ray, castRadius, out hitInfo, interactRange, interactMask, QueryTriggerInteraction.Collide))
		{
			if(hitInfo.collider.gameObject.layer == interactLayer)
			{
				//Attempt to get the component
				newInteract = hitInfo.collider.gameObject.GetComponent<Interactable>();
			}
		}

		//If this new interact is valid and different from the last one 
		bool newInteractValid = newInteract != null;
		bool oldInteractValid = _lastInteract != null;

		//Check if we need to tell the old interact that it is no longer highlighted
		if(oldInteractValid && (!newInteract || (newInteract != _lastInteract)))
		{
			//If old is valid, and new is null or new isn't the same as old then we are telling the old that it is no longer highlighted
			_lastInteract.RemoveHighlight();
		}

		//Check if we need to highlight the current interact
		if(newInteractValid && (newInteract != _lastInteract))
		{
			newInteract.Highlight();
		}

		//Has to be looking at a valid object, mouse button must be held down, and the button has been released since the last interaction
		if(newInteractValid && Input.GetMouseButton(0) && newInteract.AllowInteraction())
		{
			//Enable the UI
			var gameplayScreen = uiManager.gameplayScreen;
			if(!_previousInteract)
			{
				gameplayScreen.ProgressBG.enabled = true;
				gameplayScreen.ProgressCircle.enabled = true;
			}
			
			_holdCounter += Time.deltaTime;

			if(_holdCounter > newInteract.InteractTime && !_interactAttempted)
			{
				newInteract.AttemptInteraction();

				_interactAttempted = true;

				//Hide the progress circles now
				gameplayScreen.ProgressBG.enabled = false;
				gameplayScreen.ProgressCircle.enabled = false;
			}

			gameplayScreen.ProgressCircle.fillAmount = _holdCounter / newInteract.InteractTime;

			_previousInteract = true;
		}
		else
		{
			if(_previousInteract)
			{
				//Need to turn off the images
				var gameplayScreen = uiManager.gameplayScreen;
				gameplayScreen.ProgressBG.enabled = false;
				gameplayScreen.ProgressCircle.enabled = false;
			}

			_holdCounter = 0f;
			_interactAttempted = false;
			_previousInteract = false;
		}

		_lastInteract = newInteract;
	}

    private void OnDrawGizmos() 
	{
		if(Application.isPlaying)
		{
			Gizmos.color = Color.red;
			Vector3 camPos = CamTrans.position;
			Vector3 endPos = camPos + (CamTrans.forward * interactRange);
			Gizmos.DrawLine(camPos, endPos);
			Gizmos.DrawWireSphere(camPos, castRadius);
			Gizmos.DrawWireSphere(endPos, castRadius);
		}
    }
}
