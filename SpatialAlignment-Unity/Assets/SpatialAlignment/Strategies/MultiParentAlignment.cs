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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Microsoft.SpatialAlignment
{
    /// <summary>
    /// Defines the various modes for multi-parent alignment.
    /// </summary>
    public enum MultiParentAlignmentMode
    {
        NearestNeighbor
    }

    /// <summary>
    /// An strategy that coordinates alignment based on the alignment of multiple "parent" objects.
    /// </summary>
    /// <remarks>
    /// This strategy directly updates the world transform of the attached object. Therefore,
    /// the transform of the Unity parent GameObject has no impact on alignment unless it is
    /// added to the <see cref="ParentTransforms"/> collection.
    /// </remarks>
    public class MultiParentAlignment : AlignmentStrategy
    {
        #region Member Variables
        private float lastUpdateTime;

        [SerializeField]
        [Tooltip("The method used to align with multiple parents.")]
        private MultiParentAlignmentMode mode;

        [SerializeField]
        [Tooltip("The list of transforms that will serve as parents.")]
        private List<Transform> parentTransforms = new List<Transform>();

        [SerializeField]
        [Tooltip("The transform that will serve as the frame of reference when calculating modes like NearestNeighbor. If blank, the main cameras transform will be used.")]
        private Transform referenceTransform;

        [SerializeField]
        [Tooltip("The time between updates (in seconds). If zero, alignment is updated every frame.")]
        private float updateFrequency = 0.02f;
        #endregion // Member Variables

        #region Internal Methods
        /// <summary>
        /// Attempts to align to the nearest neighbor.
        /// </summary>
        /// <returns>
        /// <c>true</c> if aligned; otherwise <c>false</c>.
        /// </returns>
        private bool AlignNearestNeighbor()
        {
            // Attempt to get the reference transform
            Transform reference = GetReferenceTransform();

            // If no transform, can't continue
            if (reference == null) { return false; }

            // Get position once
            Vector3 referencePos = reference.position;

            // Placeholder
            Transform parent;

            // If only one parent, always use that
            if (parentTransforms.Count == 1)
            {
                // Use only parent
                parent = parentTransforms[0];
            }
            else
            {
                // Find the parent closest to the reference point
                parent = parentTransforms.OrderBy(t => (t.position - referencePos).sqrMagnitude).First();
            }

            // Update the world transform
            transform.SetPositionAndRotation(parent.position, parent.rotation);

            // Success
            return true;
        }
        #endregion // Internal Methods

        #region Overridables / Event Triggers
        /// <summary>
        /// Gets the transform that serves as the frame of reference.
        /// </summary>
        /// <returns>
        /// The reference transform, if one can be determined;
        /// otherwise <see langword = "null" />.
        /// </returns>
        /// <remarks>
        /// The default implementation returns the value of
        /// <see cref="ReferenceTransform"/> if set; otherwise it
        /// attempts to return the main camera transform.
        /// </remarks>
        protected virtual Transform GetReferenceTransform()
        {
            if (referenceTransform != null)
            {
                return referenceTransform;
            }
            else
            {
                return Camera.main?.transform;
            }
        }

        /// <summary>
        /// Returns a value that indicates if <see cref="UpdateTransform"/>
        /// should be called, usually by the <see cref="Update"/> loop.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <see cref="UpdateTransform"/> should be called;
        /// otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The default implementation returns <c>true</c> when the time since
        /// last update exceeds <see cref="UpdateFrequency"/>.
        /// </remarks>
        protected virtual bool ShouldUpdateTransform()
        {
            return (Time.unscaledTime - lastUpdateTime) >= updateFrequency;
        }
        #endregion // Overridables / Event Triggers

        #region Unity Overrides
        /// <inheritdoc />
        protected override void Update()
        {
            // Call base first
            base.Update();

            // Should we update the transform?
            if (ShouldUpdateTransform())
            {
                // Yes, update
                UpdateTransform();

                // Store last update time
                lastUpdateTime = Time.unscaledTime;
            }
        }
        #endregion // Unity Overrides

        #region Public Methods
        /// <summary>
        /// Attempts to calculate and update the transform.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the transform was updated; otherwise <c>false</c>.
        /// </returns>
        public virtual void UpdateTransform()
        {
            // If there are no parents, nothing to do
            if (parentTransforms.Count == 0)
            {
                State = AlignmentState.Unresolved;
                return;
            }

            // Align based on mode
            switch (mode)
            {
                case MultiParentAlignmentMode.NearestNeighbor:
                    State = (AlignNearestNeighbor() ? AlignmentState.Tracking : AlignmentState.Inhibited);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected Branch");
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets the method used to align with multiple parents.
        /// </summary>
        public MultiParentAlignmentMode Mode
        {
            get
            {
                return mode;
            }
            set
            {
                // Ensure changing
                if (mode != value)
                {
                    // Store
                    mode = value;

                    // Attempt to update the transform
                    UpdateTransform();
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of transforms that will serve as parents.
        /// </summary>
        /// <remarks>
        /// Replacing this list will cause the transforms to be recalculated.
        /// </remarks>
        public List<Transform> ParentTransforms
        {
            get
            {
                return parentTransforms;
            }
            set
            {
                // Ensure changing
                if (parentTransforms != value)
                {
                    // Validate
                    if (value == null) throw new ArgumentNullException(nameof(value));

                    // Store
                    parentTransforms = value;

                    // Attempt to update the transform
                    UpdateTransform();
                }
            }
        }

        /// <summary>
        /// Gets or sets the transform that will serve as the frame of reference.
        /// </summary>
        /// <remarks>
        /// This transform is used when calculating modes like
        /// <see cref="MultiParentAlignmentMode.NearestNeighbor">NearestNeighbor</see>.
        /// By default, if this property is <see langword = "null" /> the main cameras
        /// transform will be used.
        /// </remarks>
        public Transform ReferenceTransform
        {
            get
            {
                return referenceTransform;
            }
            set
            {
                // Ensure changing
                if (referenceTransform != value)
                {
                    // Store
                    referenceTransform = value;

                    // Attempt to update the transform
                    UpdateTransform();
                }
            }
        }

        /// <summary>
        /// Gets or sets the time between updates (in seconds).
        /// </summary>
        /// <remarks>
        /// By default, if this value is zero alignment will be updated on every frame.
        /// </remarks>
        public float UpdateFrequency
        {
            get
            {
                return updateFrequency;
            }
            set
            {
                // Validate
                if (value < 0.0f) throw new ArgumentOutOfRangeException(nameof(value));

                // Store
                updateFrequency = value;
            }
        }
        #endregion // Public Properties
    }
}