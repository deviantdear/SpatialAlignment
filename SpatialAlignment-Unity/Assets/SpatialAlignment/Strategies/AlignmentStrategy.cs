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
using UnityEngine;

namespace Microsoft.SpatialAlignment
{
    /// <summary>
    /// An abstract base class for a <see cref="MonoBehaviour"/> that implements
    /// <see cref="IAlignmentStrategy"/>.
    /// </summary>
    public abstract class AlignmentStrategy : MonoBehaviour, IAlignmentStrategy
    {
        #region Member Variables
        private Vector3 accuracy = Vector3.positiveInfinity;
        private AlignmentState state;
        #endregion // Member Variables

        #region Unity Overrides
        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        protected virtual void Awake() {}

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        protected virtual void OnDestroy() { }

        /// <summary>
        /// Called each time the script is disabled.
        /// </summary>
        protected virtual void OnDisable() { }

        /// <summary>
        /// Called each time the script is enabled.
        /// </summary>
        protected virtual void OnEnable() { }

        /// <summary>
        /// Start is called before the first frame update.
        /// </summary>
        protected virtual void Start() {}

        /// <summary>
        /// Update is called once per frame
        /// </summary>
        protected virtual void Update() {}
        #endregion // Unity Overrides

        #region Overridables / Event Triggers
        /// <summary>
        /// Called when the value of the <see cref="Accuracy"/> property has changed.
        /// </summary>
        protected virtual void OnAccuracyChanged()
        {
            AccuracyChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the value of the <see cref="State"/> property has changed.
        /// </summary>
        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion // Overridables / Event Triggers

        #region Public Properties
        /// <inheritdoc />
        public virtual Vector3 Accuracy
        {
            get
            {
                return accuracy;
            }
            protected set
            {
                if (accuracy != value)
                {
                    accuracy = value;
                    OnAccuracyChanged();
                }
            }
        }

        /// <inheritdoc />
        public virtual AlignmentState State
        {
            get
            {
                return state;
            }
            protected set
            {
                if (state != value)
                {
                    state = value;
                    OnStateChanged();
                }
            }
        }

        #endregion // Public Properties

        #region Public Events
        /// <inheritdoc />
        public event EventHandler AccuracyChanged;

        /// <inheritdoc />
        public event EventHandler StateChanged;
        #endregion // Public Events
    }
}