// This file is part of the DSharpPlusNextGen project.
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
using System.Collections.Concurrent;
using System.Threading;
using DSharpPlusNextGen.EventArgs;

namespace DSharpPlusNextGen.Interactivity.EventHandling
{
    /// <summary>
    /// Represents a component event that is being waited for.
    /// </summary>
    internal sealed class ComponentCollectRequest : ComponentMatchRequest
    {
        /// <summary>
        /// Gets the collected.
        /// </summary>
        public ConcurrentBag<ComponentInteractionCreateEventArgs> Collected { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentCollectRequest"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="cancellation">The cancellation token.</param>
        public ComponentCollectRequest(ulong id, Func<ComponentInteractionCreateEventArgs, bool> predicate, CancellationToken cancellation) : base(id, predicate, cancellation) { }
    }
}