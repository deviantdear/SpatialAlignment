﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// #define UNITY_IOS
// #define WINDOWS_UWP

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_IOS
using UnityEngine.EventSystems;
using UnityEngine.XR.iOS;
#elif WINDOWS_UWP
using UnityEngine.XR.WSA.Input;
#endif // WINDOWS_UWP

namespace Microsoft.SpatialAlignment
{
    public class ASAExampleManager : MonoBehaviour
    {
        #region Nested Types
        /// <summary>
        /// Different modes of this demo controller.
        /// </summary>
        private enum ControllerMode
        {
            /// <summary>
            /// No active session
            /// </summary>
            SessionStopped,
            /// <summary>
            /// Ready to perform an action
            /// </summary>
            ActionReady,
            /// <summary>
            /// Adding an anchor
            /// </summary>
            AnchorAdd,
            /// <summary>
            /// Deleting an anchor
            /// </summary>
            AnchorDelete,
            /// <summary>
            /// Locating anchors
            /// </summary>
            AnchorLocate
        }
        #endregion // Nested Types

        #region Constants
        static private readonly Color COLOR_FADED = new Color(255, 255, 255, 40); // Used to fade UI elements
        static private readonly Color COLOR_FULL = new Color(255, 255, 255, 255);
        #endregion // Constants

        #region Member Variables
        private List<string> anchorIds = new List<string>();                // List of all known anchor IDs
        private ControllerMode currentMode;                                 // The current mode of the controller
        private GameObject currentObject;                                   // The current object that may be being added or removed
        private bool isPlaced = false;                                      // When adding a new object, has the object been placed?
        private List<GameObject> localObjects = new List<GameObject>();     // List of locally created anchor objects
        private int locateDataNeeded;                                       // The number of locate objects that were deleted
        private int locateDeleted;                                          // The number of locate objects that were deleted
        private int locateFound;                                            // The number of locate objects that were found
        private int locateOrphaned;                                         // The number of locate objects that were orphaned
        private int spatialMeshLayer;                                       // The layer where spatial meshes appear (usually named "SpatialMesh")
        #endregion // Member Variables

        #region Unity Inspector Variables
        [Tooltip("Shows the progress toward minimum data needed for creating anchors.")]
        public Image MinCreateProgressImage;

        [Tooltip("Shows if minimum data is available for anchors to be created.")]
        public Image MinCreateReadyImage;

        [Tooltip("Shows the progress toward minimum data needed for locating anchors.")]
        public Image MinLocateProgressImage;

        [Tooltip("Shows if minimum data is available for anchors to be located.")]
        public Image MinLocateReadyImage;

        [Tooltip("The transform where new objects will be parented. If one is not specified, this GameObject will be used as the parent.")]
        public Transform ObjectContainer;

        [Tooltip("The prefab to use when creating new objects to represent anchors. If one is not specified, a cube will be used.")]
        public GameObject ObjectPrefab;

        [Tooltip("Shows the progress toward recommended data for creating anchors.")]
        public Image RecCreateProgressImage;

        [Tooltip("Shows if recommended data is available for anchors to be created.")]
        public Image RecCreateReadyImage;

        [Tooltip("Shows the progress toward recommended data for locating anchors.")]
        public Image RecLocateProgressImage;

        [Tooltip("Shows if recommended data is available for anchors to be located.")]
        public Image RecLocateReadyImage;

        [Tooltip("The spatial services manager that will be used to create and track anchors.")]
        public SpatialServiceManager SpatialManager;

        [Tooltip("An optional Text control that can be used to display status messages.")]
        public Text StatusText;

        [Tooltip("UI panel for the 'Action Ready' mode.")]
        public GameObject UIActionReady;

        [Tooltip("UI panel for the 'Anchor Add' mode.")]
        public GameObject UIAnchorAdd;

        [Tooltip("UI panel for the 'Anchor Delete' mode.")]
        public GameObject UIAnchorDelete;

        [Tooltip("UI panel for the 'Anchor Locate' mode.")]
        public GameObject UIAnchorLocate;

        [Tooltip("UI panel for the 'Session Stopped' mode.")]
        public GameObject UISessionStopped;
        #endregion // Unity Inspector Variables

        #region Internal Methods
        /// <summary>
        /// Instantiates the specified prefab or creates a default object if no prefab is specified.
        /// </summary>
        /// <returns>
        /// The newly created object.
        /// </returns>
        private GameObject CreateNewObject()
        {
            // Placeholder
            GameObject go;

            // Use prefab or default object?
            if (ObjectPrefab != null)
            {
                // Instantiate the prefab
                go = Instantiate(ObjectPrefab);
            }
            else
            {
                // Create default
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            }

            // Parent it to the object container
            go.transform.SetParent(ObjectContainer, worldPositionStays: true);

            // Done
            return go;
        }

        /// <summary>
        /// If a local game object exists for the specified cloud anchor it will be updated.
        /// Otherwise a new game object will be created.
        /// </summary>
        /// <param name="anchor">
        /// The <see cref="CloudSpatialAnchor"/>.
        /// </param>
        private void CreateOrUpdateObjectForAnchor(CloudSpatialAnchor anchor)
        {
            // Validate
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));

            // Try to find an existing game object that already uses this anchor
            GameObject go = null;
            lock (localObjects)
            {
                go = (from g in localObjects
                      let cn = g.GetComponent<CloudNativeAnchor>()
                      where cn?.CloudAnchor?.Identifier == anchor.Identifier
                      select g).FirstOrDefault();
            }

            // If an existing object wasn't found, create a new one
            if (go == null)
            {
                // Create a new object
                go = CreateNewObject();

                // Add it to the list of known objects
                lock (localObjects)
                {
                    localObjects.Add(go);
                }
            }

            // Apply the cloud anchor to the object
            SpatialManager.ApplyAnchor(anchor, go);
        }

        /// <summary>
        /// Processes any touch input for pressed or released events.
        /// </summary>
        private void ProcessTouchInput()
        {
            // Only one touch point?
            if (Input.touchCount == 1)
            {
                // Being released?
                if (Input.touches[0].phase == TouchPhase.Began)
                {
                    OnInputPressed();
                }
            }
        }

        /// <summary>
        /// Sets the current mode of the controller.
        /// </summary>
        /// <param name="mode">
        /// The mode to set the controller in.
        /// </param>
        /// <remarks>
        /// This method mainly changes which UI buttons are available.
        /// </remarks>
        private void SetMode(ControllerMode mode)
        {
            // Already in this mode?
            if (currentMode == mode) { return; }

            // Store the new mode
            currentMode = mode;

            // Show and hide UI panels to match the current mode
            switch (currentMode)
            {
                case ControllerMode.ActionReady:
                    UISessionStopped.SetActive(false);
                    UIAnchorAdd.SetActive(false);
                    UIAnchorDelete.SetActive(false);
                    UIAnchorLocate.SetActive(false);
                    UIActionReady.SetActive(true);
                    break;

                case ControllerMode.AnchorAdd:
                    UIActionReady.SetActive(false);
                    UIAnchorAdd.SetActive(true);
                    break;

                case ControllerMode.AnchorDelete:
                    UIActionReady.SetActive(false);
                    UIAnchorDelete.SetActive(true);
                    break;

                case ControllerMode.AnchorLocate:
                    UIActionReady.SetActive(false);
                    UIAnchorLocate.SetActive(true);
                    break;

                case ControllerMode.SessionStopped:
                    UIAnchorAdd.SetActive(false);
                    UIAnchorDelete.SetActive(false);
                    UIAnchorLocate.SetActive(false);
                    UIActionReady.SetActive(false);
                    UISessionStopped.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// Attempts to get a location and angle suitable to place a new object.
        /// </summary>
        /// <param name="pos">
        /// The output placement position.
        /// </param>
        /// <param name="rot">
        /// The output placement rotation.
        /// </param>
        /// <returns>
        /// <c>true</c> if a valid placement could be determined; otherwise <c>false</c>.
        /// </returns>
        private bool TryGetPlacement(out Vector3 pos, out Quaternion rot)
        {
            // Defaults
            pos = Vector3.zero;
            rot = Quaternion.identity;

            // Shortcut to camera
            Camera mainCamera = Camera.main;

            #if UNITY_IOS
            // iOS uses ARKit Surfaces
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);

                if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    var screenPosition = mainCamera.ScreenToViewportPoint(touch.position);
                    ARPoint point = new ARPoint
                    {
                        x = screenPosition.x,
                        y = screenPosition.y
                    };

                    List<ARHitTestResult> hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point,
                                                       ARHitTestResultType.ARHitTestResultTypeEstimatedHorizontalPlane | ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent);
                    if (hitResults.Count > 0)
                    {
                        pos = UnityARMatrixOps.GetPosition(hitResults[0].worldTransform);
                        rot = Quaternion.AngleAxis(0, Vector3.up);
                        return true;
                    }
                }
            }
            #endif // UNITY_IOS

            #if WINDOWS_UWP
            // WMR uses the spatial mapping mesh
            // Look for an intersection point on the spatial mesh
            int layerMask = 1 << spatialMeshLayer;
            RaycastHit hitInfo;
            if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hitInfo, 90f, layerMask))
            {
                pos = hitInfo.point;
                rot = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
                return true;
            }
            #endif // WINDOWS_UWP

            // No solution
            return false;
        }

        /// <summary>
        /// Attempts to move the current object to a new valid placement location.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the object was moved; otherwise <c>false</c>.
        /// </returns>
        private bool TryMoveObject()
        {
            // If there's no object to move, nothing to do.
            if (currentObject == null) { return false; }

            // If we can't get a valid placement position, nowhere to move to.
            Vector3 pos;
            Quaternion rot;
            if (!TryGetPlacement(out pos, out rot)) { return false; }

            // Actually move the object
            currentObject.transform.position = pos;
            currentObject.transform.rotation = rot;

            // Success!
            return true;
        }

        /// <summary>
        /// Updates the UI to reflect the specified session state.
        /// </summary>
        /// <param name="status">
        /// The session state to reflect.
        /// </param>
        /// <remarks>
        /// This is a large method but really it's just for demonstration purposes.
        /// Most applications do not need to display these statistics. In fact,
        /// <see cref="SpatialServiceManager"/> will even automatically wait to
        /// save an anchor until enough data is available.
        /// </remarks>
        private void UpdateUI(SessionStatus status)
        {
            /* Minimum */

            // Progress minimum create
            if (MinCreateProgressImage != null)
            {
                float createProgress = Mathf.Min(status.ReadyForCreateProgress, 1.0f); // Can go beyond 1.0
                MinCreateProgressImage.rectTransform.localScale = new Vector3(createProgress, 1, 1);
            }

            // Ready minimum create
            if (MinCreateReadyImage != null)
            {
                MinCreateReadyImage.enabled = SpatialManager.IsReadyForCreate;
            }

            // Progress minimum locate
            if (MinLocateProgressImage != null)
            {
                float locateProgress = Mathf.Min(status.ReadyForLocateProgress, 1.0f); // Can go beyond 1.0
                MinLocateProgressImage.rectTransform.localScale = new Vector3(locateProgress, 1, 1);
            }

            /* Recommended */

            // Ready recommended locate
            if (RecLocateReadyImage != null)
            {
                RecLocateReadyImage.enabled = SpatialManager.IsReadyForLocate;
            }

            // Progress recommended create
            if (RecCreateProgressImage != null)
            {
                float createProgress = Mathf.Min(status.RecommendedForCreateProgress, 1.0f); // Can go beyond 1.0
                RecCreateProgressImage.rectTransform.localScale = new Vector3(createProgress, 1, 1);
            }

            // Ready recommended create
            if (RecCreateReadyImage != null)
            {
                RecCreateReadyImage.enabled = SpatialManager.IsRecommendedForCreate;
            }

            // Progress recommended locate
            if (RecLocateProgressImage != null)
            {
                float locateProgress = Mathf.Min(status.RecommendedForLocateProgress, 1.0f); // Can go beyond 1.0
                RecLocateProgressImage.rectTransform.localScale = new Vector3(locateProgress, 1, 1);
            }

            // Ready recommended locate
            if (RecLocateReadyImage != null)
            {
                RecLocateReadyImage.enabled = SpatialManager.IsRecommendedForLocate;
            }
        }

        /// <summary>
        /// Validates the configuration of this manager behavior.
        /// </summary>
        /// <param name="disable">
        /// <c>true</c> if the behavior should be disabled when validation fails; otherwise <c>false</c>.
        /// </param>
        /// <param name="except">
        /// <c>true</c> if an exception should be thrown when validation fails; otherwise <c>false</c>.
        /// </param>
        private void ValidateConfig(bool disable = false, bool except = false)
        {
            string errMsg = "";
            bool valid = true;

            if (SpatialManager == null)
            {
                errMsg += $"{nameof(SpatialManager)} cannot be null. ";
                valid = false;
            }

            if (!valid)
            {
                Logger.LogError(errMsg, ui: StatusText);
                if (disable)
                {
                    this.enabled = false;
                }
                if (except)
                {
                    throw new InvalidOperationException(errMsg);
                }
            }
        }

        /// <summary>
        /// Validates that there is an active session and logs a warning if not.
        /// </summary>
        /// <returns>
        /// <c>true</c> if there's a valid session; otherwise <c>false</c>.
        /// </returns>
        private bool ValidateSession()
        {
            if (SpatialManager.SessionState != SpatialSessionState.Started)
            {
                Logger.LogWarn("The session must be started first.", ui: StatusText);
                return false;
            }
            return true;
        }
        #endregion // Internal Methods

        #region Overridables / Event Triggers
        /// <summary>
        /// Called when the input device is pressed (finger pressed on screen, tap gesture down, etc.)
        /// </summary>
        protected virtual void OnInputPressed()
        {
            // If we're currently adding an anchor, end the process (but on the main thread)
            if (currentMode == ControllerMode.AnchorAdd)
            {
                UnityDispatcher.InvokeOnAppThread(() => EndAnchorAdd());
            }
        }
        #endregion // Overridables / Event Triggers

        #region Unity Overrides
        protected virtual void Awake()
        {
            // Validate the configuration of this manager on Awake and
            // disable the manger if not configured correctly.
            ValidateConfig(disable: true);
        }

        protected virtual void Start()
        {
            // If no parent container is specified for objects to be created in,
            // use the same game object we're attached to.
            if (ObjectContainer == null)
            {
                ObjectContainer = this.transform;
            }

            // Subscribe to spatial manager events
            SpatialManager.AnchorLocated += SpatialManager_AnchorLocated;
            SpatialManager.LocateAnchorsCompleted += SpatialManager_LocateAnchorsCompleted;
            SpatialManager.SessionStateChanged += SpatialManager_SessionStateChanged;
            SpatialManager.SessionUpdated += SpatialManager_SessionUpdated;

            #if WINDOWS_UWP
            // Get the spatial mesh layer
            spatialMeshLayer = LayerMask.NameToLayer("SpatialMesh");

            // Subscribe to interaction manager for Windows Mixed Reality
            InteractionManager.InteractionSourcePressed += InteractionManager_InteractionSourcePressed;
            #endif // WINDOWS_UWP
        }

        protected virtual void Update()
        {
            // If we're adding a new anchor, try to move the object to a valid location
            if (currentMode == ControllerMode.AnchorAdd)
            {
                // If we're successful in moving the object to a
                // valid location, consider it "placed"
                if (TryMoveObject()) { isPlaced = true; }
            }

            // Check for any touch input events
            ProcessTouchInput();
        }
        #endregion // Unity Overrides

        #region Overrides / Event Handlers
        private void SpatialManager_AnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            switch (args.Status)
            {
                case LocateAnchorStatus.Located:
                case LocateAnchorStatus.AlreadyTracked:
                    locateFound++;
                    // A cloud anchor has been located. We need to create or update
                    // a local game object to match this cloud anchor. However, we
                    // must do this on Unity's main thread.
                    UnityDispatcher.InvokeOnAppThread(() => CreateOrUpdateObjectForAnchor(args.Anchor));
                    break;

                case LocateAnchorStatus.NotLocatedAnchorDoesNotExist:
                    locateDeleted++;
                    break;

                case LocateAnchorStatus.NotLocatedAnchorOrphaned:
                    locateOrphaned++;
                    break;

                case LocateAnchorStatus.NotLocatedNeedsMoreData:
                    locateDataNeeded++;
                    break;
            }
        }

        /// <summary>
        /// The Spatial Managers session has changed.
        /// </summary>
        /// <remarks>
        /// This event handler is called as sessions are started and stopped. We really
        /// only use it to show or hide Unity UI elements that only make sense in or
        /// out of a session.
        /// </remarks>
        private void SpatialManager_SessionStateChanged(object sender, EventArgs e)
        {
            // Update our demo mode to match the state of the session
            switch (SpatialManager.SessionState)
            {
                case SpatialSessionState.Started:
                    SetMode(ControllerMode.ActionReady);
                    break;
                default:
                    SetMode(ControllerMode.SessionStopped);
                    break;
            }
        }
        private void SpatialManager_LocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args)
        {
            Logger.LogInfo($"Locate Complete. {locateFound} found, {locateDeleted} deleted, {locateOrphaned} orphaned, {locateDataNeeded} need more data.", ui: StatusText);
        }

        private void SpatialManager_SessionUpdated(object sender, SessionUpdatedEventArgs args)
        {
            // Update the UI, but we must do it on Unity's main thread
            UnityDispatcher.InvokeOnAppThread(() => UpdateUI(args.Status));
        }

        #if WINDOWS_UWP
        private void InteractionManager_InteractionSourcePressed(InteractionSourcePressedEventArgs obj)
        {
            OnInputPressed();
        }
        #endif // WINDOWS_UWP

        #endregion // Overrides / Event Handlers

        #region Public Methods
        /// <summary>
        /// Begins the process of adding a new anchor to the scene.
        /// </summary>
        public void BeginAnchorAdd()
        {
            // If already adding, ignore
            if (currentMode == ControllerMode.AnchorAdd) { return; }

            // Make sure there's an active session
            if (!ValidateSession()) { return; }

            // Create a new object
            currentObject = CreateNewObject();

            // Parent it
            currentObject.transform.SetParent(ObjectContainer, worldPositionStays: true);

            // Reset the "placed" flag since this object hasn't been placed yet
            isPlaced = false;

            // Now in adding mode
            SetMode(ControllerMode.AnchorAdd);

            Logger.LogInfo("Adding an anchor. Look around and tap to place the object.", ui: StatusText);
        }

        /// <summary>
        /// Begins the process of deleting an anchor from the scene.
        /// </summary>
        public void BeginAnchorDelete()
        {
            Logger.LogInfo("Not implemented yet.", ui: StatusText);
        }

        /// <summary>
        /// Instructs the controller to start locating all known cloud anchors.
        /// </summary>
        public void BeginAnchorLocate()
        {
            if (anchorIds.Count < 1)
            {
                Logger.LogInfo("No cloud anchors to locate. Create some first.", ui: StatusText);
            }
            else if (SpatialManager.SessionState != SpatialSessionState.Started)
            {
                Logger.LogWarn("The session must be started first.", ui: StatusText);
            }
            else
            {
                Logger.LogInfo("Locating cloud anchors.", ui: StatusText);

                // Reset counters
                locateDataNeeded = 0;
                locateDeleted = 0;
                locateFound = 0;
                locateOrphaned = 0;

                // Create the search criteria
                AnchorLocateCriteria locateCriteria = new AnchorLocateCriteria();
                locateCriteria.Identifiers = anchorIds.ToArray(); // QUESTION: Why is this an array?

                // Remove any existing criteria
                SpatialManager.Session.LocateCriteria.Clear();

                // Add new criteria
                SpatialManager.Session.LocateCriteria.Add(locateCriteria);

                // Start locating
                SpatialManager.Session.BeginLocateAnchors(); // QUESTION: What if it's already locating? How do we stop and start over?
            }
        }

        /// <summary>
        /// Ends the process of adding a new anchor to the scene.
        /// </summary>
        public async void EndAnchorAdd()
        {
            // If not adding, ignore
            if (currentMode != ControllerMode.AnchorAdd) { return; }

            // Was it placed?
            if (isPlaced)
            {
                // Make sure there's an active session
                if (!ValidateSession()) { return; }

                // Create an anchor for the object
                CloudNativeAnchor cnAnchor = await SpatialManager.CreateAnchorAsync(currentObject);

                // Add the cloud anchor ID to the list of known anchors
                anchorIds.Add(cnAnchor.CloudAnchor.Identifier);

                // Add the object to the list of created objects
                localObjects.Add(currentObject);

                Logger.LogInfo("Anchor created.", ui: StatusText);
            }
            else
            {
                // Nope, never placed. Just delete it.
                DestroyImmediate(currentObject);

                Logger.LogInfo("New anchor canceled.", ui: StatusText);
            }

            // Reset the current object placeholder for the next add
            currentObject = null;

            // No longer in adding mode
            SetMode(ControllerMode.ActionReady);
        }

        /// <summary>
        /// Ends the process of deleting an anchor from the scene.
        /// </summary>
        public void EndAnchorDelete()
        {
            Logger.LogInfo("Not implemented yet.", ui: StatusText);
        }

        /// <summary>
        /// Ends the process of locating anchors in the scene.
        /// </summary>
        public void EndAnchorLocate()
        {
            // If not locating, ignore
            if (currentMode != ControllerMode.AnchorLocate) { return; }

            // Make sure there's an active session
            if (!ValidateSession()) { return; }

            // QUESTION: How to stop locating
            // SpatialManager.Session.?

            // No longer in locating mode
            SetMode(ControllerMode.ActionReady);
        }

        /// <summary>
        /// Instructs the controller to remove all local objects but NOT delete their corresponding cloud anchors.
        /// </summary>
        public void RemoveLocalObjects()
        {
            if (localObjects.Count < 1)
            {
                Logger.LogInfo("No objects to remove.", ui: StatusText);
            }
            else
            {
                lock (localObjects)
                {
                    while (localObjects.Count > 0)
                    {
                        DestroyImmediate(localObjects[0]);
                        localObjects.RemoveAt(0);
                    }
                }
                Logger.LogInfo("Local objects removed.", ui: StatusText);
            }
        }
        #endregion // Public Methods
    }
}