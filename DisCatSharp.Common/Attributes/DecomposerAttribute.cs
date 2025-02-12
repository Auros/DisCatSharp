// This file is part of the DisCatSharp project.
//
// Copyright (c) 2021 AITSYS
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;

namespace DisCatSharp.Common.Serialization
{
    /// <summary>
    /// Specifies a decomposer for a given type or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class DecomposerAttribute : SerializationAttribute
    {
        /// <summary>
        /// Gets the type of the decomposer.
        /// </summary>
        public Type DecomposerType { get; }

        /// <summary>
        /// Specifies a decomposer for given type or property.
        /// </summary>
        /// <param name="type">Type of decomposer to use.</param>
        public DecomposerAttribute(Type type)
        {
            if (!typeof(IDecomposer).IsAssignableFrom(type) || !type.IsClass || type.IsAbstract) // abstract covers static - static = abstract + sealed
                throw new ArgumentException("Invalid type specified. Must be a non-abstract class which implements DisCatSharp.Common.Serialization.IDecomposer interface.", nameof(type));

            this.DecomposerType = type;
        }
    }
}
