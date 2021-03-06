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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.SpatialAlignment.Persistence.Json
{
    public class AlignmentStrategyConverter : CustomCreationConverter<IAlignmentStrategy>
    {
        public override IAlignmentStrategy Create(Type objectType)
        {
            throw new NotImplementedException();
        }

        //public override bool CanConvert(Type objectType)
        //{
        //    return typeof(IAlignmentStrategy).IsAssignableFrom(objectType);
        //}

        //public override bool CanWrite => false; // Only use for read operations, not write operations.

        //public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        //{
        //    // Existing object is a ???

        //    return null;
        //    //// Instantiate the component
        //    //GameObject go = new GameObject();

        //    //// Add a SpatialFrame to it
        //    //SpatialFrame frame = go.AddComponent<SpatialFrame>();

        //    //// Deserialize into the instance
        //    //serializer.Populate(reader, frame);

        //    //// Update the game object name to match
        //    //go.name = frame.Id;

        //    //// Return the frame
        //    //return frame;
        //}

        //public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
