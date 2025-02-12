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
using System.Reflection;
using System.Threading.Tasks;
using DisCatSharp.Entities;

namespace DisCatSharp.CommandsNext.Converters
{
    /// <summary>
    /// Represents a enum converter.
    /// </summary>
    public class EnumConverter<T> : IArgumentConverter<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        /// <summary>
        /// Converts a string.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <param name="ctx">The command context.</param>
        Task<Optional<T>> IArgumentConverter<T>.ConvertAsync(string value, CommandContext ctx)
        {
            var t = typeof(T);
            var ti = t.GetTypeInfo();
            return !ti.IsEnum
                ? throw new InvalidOperationException("Cannot convert non-enum value to an enum.")
                : Enum.TryParse(value, !ctx.Config.CaseSensitive, out T ev)
                ? Task.FromResult(Optional.FromValue(ev))
                : Task.FromResult(Optional.FromNoValue<T>());
        }
    }
}
