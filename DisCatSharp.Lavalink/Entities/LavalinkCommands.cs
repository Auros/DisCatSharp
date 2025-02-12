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
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DisCatSharp.Lavalink.Entities
{
    /// <summary>
    /// The lavalink configure resume.
    /// </summary>
    internal sealed class LavalinkConfigureResume : LavalinkPayload
    {
        /// <summary>
        /// Gets the key.
        /// </summary>
        [JsonProperty("key")]
        public string Key { get; }

        /// <summary>
        /// Gets the timeout.
        /// </summary>
        [JsonProperty("timeout")]
        public int Timeout { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LavalinkConfigureResume"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="timeout">The timeout.</param>
        public LavalinkConfigureResume(string key, int timeout)
            : base("configureResuming")
        {
            this.Key = key;
            this.Timeout = timeout;
        }
    }

    /// <summary>
    /// The lavalink destroy.
    /// </summary>
    internal sealed class LavalinkDestroy : LavalinkPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LavalinkDestroy"/> class.
        /// </summary>
        /// <param name="lvl">The lvl.</param>
        public LavalinkDestroy(LavalinkGuildConnection lvl)
            : base("destroy", lvl.GuildIdString)
        { }
    }

    /// <summary>
    /// The lavalink play.
    /// </summary>
    internal sealed class LavalinkPlay : LavalinkPayload
    {
        /// <summary>
        /// Gets the track.
        /// </summary>
        [JsonProperty("track")]
        public string Track { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LavalinkPlay"/> class.
        /// </summary>
        /// <param name="lvl">The lvl.</param>
        /// <param name="track">The track.</param>
        public LavalinkPlay(LavalinkGuildConnection lvl, LavalinkTrack track)
            : base("play", lvl.GuildIdString)
        {
            this.Track = track.TrackString;
        }
    }

    /// <summary>
    /// The lavalink play partial.
    /// </summary>
    internal sealed class LavalinkPlayPartial : LavalinkPayload
    {
        /// <summary>
        /// Gets the track.
        /// </summary>
        [JsonProperty("track")]
        public string Track { get; }

        /// <summary>
        /// Gets the start time.
        /// </summary>
        [JsonProperty("startTime")]
        public long StartTime { get; }

        /// <summary>
        /// Gets the stop time.
        /// </summary>
        [JsonProperty("endTime")]
        public long StopTime { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LavalinkPlayPartial"/> class.
        /// </summary>
        /// <param name="lvl">The lvl.</param>
        /// <param name="track">The track.</param>
        /// <param name="start">The start.</param>
        /// <param name="stop">The stop.</param>
        public LavalinkPlayPartial(LavalinkGuildConnection lvl, LavalinkTrack track, TimeSpan start, TimeSpan stop)
            : base("play", lvl.GuildIdString)
        {
            this.Track = track.TrackString;
            this.StartTime = (long)start.TotalMilliseconds;
            this.StopTime = (long)stop.TotalMilliseconds;
        }
    }

    /// <summary>
    /// The lavalink pause.
    /// </summary>
    internal sealed class LavalinkPause : LavalinkPayload
    {
        /// <summary>
        /// Gets a value indicating whether pause.
        /// </summary>
        [JsonProperty("pause")]
        public bool Pause { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LavalinkPause"/> class.
        /// </summary>
        /// <param name="lvl">The lvl.</param>
        /// <param name="pause">If true, pause.</param>
        public LavalinkPause(LavalinkGuildConnection lvl, bool pause)
            : base("pause", lvl.GuildIdString)
        {
            this.Pause = pause;
        }
    }

    /// <summary>
    /// The lavalink stop.
    /// </summary>
    internal sealed class LavalinkStop : LavalinkPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LavalinkStop"/> class.
        /// </summary>
        /// <param name="lvl">The lvl.</param>
        public LavalinkStop(LavalinkGuildConnection lvl)
            : base("stop", lvl.GuildIdString)
        { }
    }

    /// <summary>
    /// The lavalink seek.
    /// </summary>
    internal sealed class LavalinkSeek : LavalinkPayload
    {
        /// <summary>
        /// Gets the position.
        /// </summary>
        [JsonProperty("position")]
        public long Position { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LavalinkSeek"/> class.
        /// </summary>
        /// <param name="lvl">The lvl.</param>
        /// <param name="position">The position.</param>
        public LavalinkSeek(LavalinkGuildConnection lvl, TimeSpan position)
            : base("seek", lvl.GuildIdString)
        {
            this.Position = (long)position.TotalMilliseconds;
        }
    }

    /// <summary>
    /// The lavalink volume.
    /// </summary>
    internal sealed class LavalinkVolume : LavalinkPayload
    {
        /// <summary>
        /// Gets the volume.
        /// </summary>
        [JsonProperty("volume")]
        public int Volume { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LavalinkVolume"/> class.
        /// </summary>
        /// <param name="lvl">The lvl.</param>
        /// <param name="volume">The volume.</param>
        public LavalinkVolume(LavalinkGuildConnection lvl, int volume)
            : base("volume", lvl.GuildIdString)
        {
            this.Volume = volume;
        }
    }

    /// <summary>
    /// The lavalink equalizer.
    /// </summary>
    internal sealed class LavalinkEqualizer : LavalinkPayload
    {
        /// <summary>
        /// Gets the bands.
        /// </summary>
        [JsonProperty("bands")]
        public IEnumerable<LavalinkBandAdjustment> Bands { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LavalinkEqualizer"/> class.
        /// </summary>
        /// <param name="lvl">The lvl.</param>
        /// <param name="bands">The bands.</param>
        public LavalinkEqualizer(LavalinkGuildConnection lvl, IEnumerable<LavalinkBandAdjustment> bands)
            : base("equalizer", lvl.GuildIdString)
        {
            this.Bands = bands;
        }
    }
}
