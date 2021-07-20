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
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlusNextGen.Entities;
using DSharpPlusNextGen.EventArgs;
using DSharpPlusNextGen.Interactivity.Enums;
using Microsoft.Extensions.Logging;

namespace DSharpPlusNextGen.Interactivity.EventHandling
{
    /// <summary>
    /// The component paginator.
    /// </summary>
    internal class ComponentPaginator : IPaginator
    {
        private readonly DiscordClient _client;
        private readonly InteractivityConfiguration _config;
        private readonly DiscordMessageBuilder _builder = new();
        private readonly Dictionary<ulong, IPaginationRequest> _requests = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentPaginator"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="config">The config.</param>
        public ComponentPaginator(DiscordClient client, InteractivityConfiguration config)
        {
            this._client = client;
            this._client.ComponentInteractionCreated += this.Handle;
            this._config = config;
        }

        /// <summary>
        /// Does the pagination async.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task DoPaginationAsync(IPaginationRequest request)
        {
            var id = (await request.GetMessageAsync().ConfigureAwait(false)).Id;
            this._requests.Add(id, request);

            try
            {
                var tcs = await request.GetTaskCompletionSourceAsync().ConfigureAwait(false);
                await tcs.Task;
            }
            catch (Exception ex)
            {
                this._client.Logger.LogError(InteractivityEvents.InteractivityPaginationError, ex, "There was an exception while paginating.");
            }
            finally
            {
                this._requests.Remove(id);
                try
                {
                    await request.DoCleanupAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._client.Logger.LogError(InteractivityEvents.InteractivityPaginationError, ex, "There was an exception while cleaning up pagination.");
                }
            }
        }

        /// <summary>
        /// Disposes the paginator.
        /// </summary>
        public void Dispose() => this._client.ComponentInteractionCreated -= this.Handle;


        /// <summary>
        /// Handles the pagination event.
        /// </summary>
        /// <param name="_">The client.</param>
        /// <param name="e">The event arguments.</param>
        private async Task Handle(DiscordClient _, ComponentInteractionCreateEventArgs e)
        {
            if (!this._requests.TryGetValue(e.Message.Id, out var req))
                return;

            if (this._config.AckPaginationButtons)
            {
                e.Handled = true;
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            }

            if (await req.GetUserAsync().ConfigureAwait(false) != e.User)
            {
                if (this._config.ResponseBehavior is InteractionResponseBehavior.Respond)
                    await e.Interaction.CreateFollowupMessageAsync(new() { Content = this._config.ResponseMessage, IsEphemeral = true });

                return;
            }


            await this.HandlePaginationAsync(req, e).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the pagination async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="args">The arguments.</param>
        private async Task HandlePaginationAsync(IPaginationRequest request, ComponentInteractionCreateEventArgs args)
        {
            var buttons = this._config.PaginationButtons;
            var msg = await request.GetMessageAsync().ConfigureAwait(false);
            var id = args.Id;
            var tcs = await request.GetTaskCompletionSourceAsync().ConfigureAwait(false);

#pragma warning disable CS8846 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            var paginationTask = id switch
#pragma warning restore CS8846 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
                _ when id == buttons.SkipLeft.CustomId => request.SkipLeftAsync(),
                _ when id == buttons.SkipRight.CustomId => request.SkipRightAsync(),
                _ when id == buttons.Stop.CustomId => Task.FromResult(tcs.TrySetResult(true)),
                _ when id == buttons.Left.CustomId => request.PreviousPageAsync(),
                _ when id == buttons.Right.CustomId => request.NextPageAsync(),
            };

            await paginationTask.ConfigureAwait(false);

            if (id == buttons.Stop.CustomId)
                return;

            var page = await request.GetPageAsync().ConfigureAwait(false);

            this._builder.Clear();

            this._builder
                .WithContent(page.Content)
                .AddEmbed(page.Embed)
                .AddComponents(await request.GetButtonsAsync().ConfigureAwait(false));

            await this._builder.ModifyAsync(msg);

        }
    }
}