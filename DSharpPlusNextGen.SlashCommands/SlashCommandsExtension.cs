﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlusNextGen.Entities;
using System.Linq;
using DSharpPlusNextGen.EventArgs;
using Microsoft.Extensions.Logging;
using Emzi0767.Utilities;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlusNextGen.SlashCommands.EventArgs;

namespace DSharpPlusNextGen.SlashCommands
{
    /// <summary>
    /// A class that handles slash commands for a client
    /// </summary>
    public class SlashCommandsExtension : BaseExtension
    {
        private List<CommandMethod> CommandMethods { get; set; } = new List<CommandMethod>();
        private List<GroupCommand> GroupCommands { get; set; } = new List<GroupCommand>();
        private List<SubGroupCommand> SubGroupCommands { get; set; } = new List<SubGroupCommand>();

        private List<KeyValuePair<ulong?, Type>> UpdateList { get; set; } = new List<KeyValuePair<ulong?, Type>>();

        private readonly SlashCommandsConfiguration _configuration;
        private bool Errored { get; set; } = false;

        internal SlashCommandsExtension(SlashCommandsConfiguration configuration)
        {
            this._configuration = configuration;
        }

        protected override void Setup(DiscordClient client)
        {
            if (this.Client != null)
                throw new InvalidOperationException("What did I tell you?");

            this.Client = client;

            this._error = new AsyncEvent<SlashCommandsExtension, SlashCommandErrorEventArgs>("SLASHCOMMAND_ERRORED", TimeSpan.Zero, null);
            this._executed = new AsyncEvent<SlashCommandsExtension, SlashCommandExecutedEventArgs>("SLASHCOMMAND_EXECUTED", TimeSpan.Zero, null);

            this.Client.Ready += this.Update;
            this.Client.InteractionCreated += this.InteractionHandler;
        }

        //Register

        /// <summary>
        /// Registers a command class
        /// </summary>
        /// <typeparam name="T">The command class to register</typeparam>
        public void RegisterCommands<T>(ulong? guildid = null) where T : SlashCommandModule => this.UpdateList.Add(new KeyValuePair<ulong?, Type>(guildid, typeof(T)));

        internal Task Update(DiscordClient client, ReadyEventArgs e)
        {
            foreach(var key in this.UpdateList.Select(x => x.Key).Distinct())
            {
                this.RegisterCommands(this.UpdateList.Where(x => x.Key == key).Select(x => x.Value), key);
            }
            return Task.CompletedTask;
        }

        private void RegisterCommands(IEnumerable<Type> types, ulong? guildid)
        {
            var InternalCommandMethods = new List<CommandMethod>();
            var InternalGroupCommands = new List<GroupCommand>();
            var InternalSubGroupCommands = new List<SubGroupCommand>();
            var ToUpdate = new List<DiscordApplicationCommand>();

            _ = Task.Run(async () =>
            {
                foreach (var t in types)
                {
                    try
                    {
                        var ti = t.GetTypeInfo();

                        var classes = ti.DeclaredNestedTypes.Where(x => x.GetCustomAttribute<SlashCommandGroupAttribute>() != null);
                        foreach (var tti in classes)
                        {
                            var groupatt = tti.GetCustomAttribute<SlashCommandGroupAttribute>();
                            var submethods = tti.DeclaredMethods.Where(x => x.GetCustomAttribute<SlashCommandAttribute>() != null).ToList();
                            var subclasses = tti.DeclaredNestedTypes.Where(x => x.GetCustomAttribute<SlashCommandGroupAttribute>() != null).ToList();
                            if (subclasses.Any() && submethods.Any())
                            {
                                throw new ArgumentException("Slash command groups cannot have both subcommands and subgroups!");
                            }
                            var payload = new DiscordApplicationCommand(groupatt.Name, groupatt.Description);


                            var commandmethods = new Dictionary<string, MethodInfo>();
                            foreach (var submethod in submethods)
                            {
                                var commandattribute = submethod.GetCustomAttribute<SlashCommandAttribute>();


                                var parameters = submethod.GetParameters();
                                if (!ReferenceEquals(parameters.First().ParameterType, typeof(InteractionContext)))
                                    throw new ArgumentException($"The first argument must be an InteractionContext!");
                                parameters = parameters.Skip(1).ToArray();

                                var options = await this.ParseParameters(parameters);

                                var subpayload = new DiscordApplicationCommandOption(commandattribute.Name, commandattribute.Description, ApplicationCommandOptionType.SubCommand, null, null, options, commandattribute.DefaultPermission);

                                commandmethods.Add(commandattribute.Name, submethod);

                                payload = new DiscordApplicationCommand(payload.Name, payload.Description, payload.Options?.Append(subpayload) ?? new[] { subpayload }, payload.DefaultPermission);

                                InternalGroupCommands.Add(new GroupCommand { Name = groupatt.Name, ParentClass = tti, Methods = commandmethods });
                            }
                            foreach (var subclass in subclasses)
                            {
                                var subgroupatt = subclass.GetCustomAttribute<SlashCommandGroupAttribute>();
                                var subsubmethods = subclass.DeclaredMethods.Where(x => x.GetCustomAttribute<SlashCommandAttribute>() != null);

                                var command = new SubGroupCommand { Name = groupatt.Name };

                                var options = new List<DiscordApplicationCommandOption>();

                                foreach (var subsubmethod in subsubmethods)
                                {
                                    var suboptions = new List<DiscordApplicationCommandOption>();
                                    var commatt = subsubmethod.GetCustomAttribute<SlashCommandAttribute>();
                                    var parameters = subsubmethod.GetParameters();
                                    if (!ReferenceEquals(parameters.First().ParameterType, typeof(InteractionContext)))
                                        throw new ArgumentException($"The first argument must be an InteractionContext!");
                                    parameters = parameters.Skip(1).ToArray();
                                    suboptions = suboptions.Concat(await this.ParseParameters(parameters)).ToList();

                                    var subsubpayload = new DiscordApplicationCommandOption(commatt.Name, commatt.Description, ApplicationCommandOptionType.SubCommand, null, null, suboptions, commatt.DefaultPermission);
                                    options.Add(subsubpayload);
                                    commandmethods.Add(commatt.Name, subsubmethod);
                                }

                                var subpayload = new DiscordApplicationCommandOption(subgroupatt.Name, subgroupatt.Description, ApplicationCommandOptionType.SubCommandGroup, null, null, options, subgroupatt.DefaultPermission);
                                command.SubCommands.Add(new GroupCommand { Name = subgroupatt.Name, ParentClass = subclass, Methods = commandmethods });
                                InternalSubGroupCommands.Add(command);
                                payload = new DiscordApplicationCommand(payload.Name, payload.Description, payload.Options?.Append(subpayload) ?? new[] { subpayload }, payload.DefaultPermission);
                            }
                            ToUpdate.Add(payload);
                        }

                        var methods = ti.DeclaredMethods.Where(x => x.GetCustomAttribute<SlashCommandAttribute>() != null);
                        foreach (var method in methods)
                        {
                            var commandattribute = method.GetCustomAttribute<SlashCommandAttribute>();

                            var options = new List<DiscordApplicationCommandOption>();

                            var parameters = method.GetParameters();
                            if (parameters.Length == 0 || parameters == null || !ReferenceEquals(parameters.FirstOrDefault()?.ParameterType, typeof(InteractionContext)))
                                throw new ArgumentException($"The first argument must be an InteractionContext!");
                            parameters = parameters.Skip(1).ToArray();
                            options = options.Concat(await this.ParseParameters(parameters)).ToList();

                            InternalCommandMethods.Add(new CommandMethod { Method = method, Name = commandattribute.Name, ParentClass = t });

                            var payload = new DiscordApplicationCommand(commandattribute.Name, commandattribute.Description, options, commandattribute.DefaultPermission);
                            ToUpdate.Add(payload);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Client.Logger.LogCritical(ex, $"There was an error registering slash commands");
                        this.Errored = true;
                    }
                }
                if (!this.Errored)
                {
                    try
                    {
                        IEnumerable<DiscordApplicationCommand> commands;
                        if (guildid == null)
                        {
                            commands = await this.Client.BulkOverwriteGlobalApplicationCommandsAsync(ToUpdate);
                        }
                        else
                        {
                            commands = await this.Client.BulkOverwriteGuildApplicationCommandsAsync(guildid.Value, ToUpdate);
                        }
                        foreach (var command in commands)
                        {
                            if (InternalCommandMethods.Any(x => x.Name == command.Name))
                                InternalCommandMethods.First(x => x.Name == command.Name).Id = command.Id;

                            else if (InternalGroupCommands.Any(x => x.Name == command.Name))
                                InternalGroupCommands.First(x => x.Name == command.Name).Id = command.Id;

                            else if (InternalSubGroupCommands.Any(x => x.Name == command.Name))
                                InternalSubGroupCommands.First(x => x.Name == command.Name).Id = command.Id;
                        }
                        this.CommandMethods.AddRange(InternalCommandMethods);
                        this.GroupCommands.AddRange(InternalGroupCommands);
                        this.SubGroupCommands.AddRange(InternalSubGroupCommands);
                    }
                    catch (Exception ex)
                    {
                        this.Client.Logger.LogCritical(ex, $"There was an error registering slash commands");
                        this.Errored = true;
                    }
                }
            });
        }

        private async Task<List<DiscordApplicationCommandOptionChoice>> GetChoiceAttributesFromProvider(IEnumerable<ChoiceProviderAttribute> customAttributes)
        {
            var choices = new List<DiscordApplicationCommandOptionChoice>();
            foreach (var choiceProviderAttribute in customAttributes)
            {
                var method = choiceProviderAttribute.ProviderType.GetMethod(nameof(IChoiceProvider.Provider));

                if(method == null)
                    throw new Exception("ChoiceProviders must inherit from IChoiceProvider.");
                else
                {
                    var instance = Activator.CreateInstance(choiceProviderAttribute.ProviderType);
                    var result = await (Task<IEnumerable<DiscordApplicationCommandOptionChoice>>) method.Invoke(instance, null);

                    if (result.Any())
                    {
                        choices.AddRange(result);
                    }
                }
            }

            return choices;
        }

        private static List<DiscordApplicationCommandOptionChoice> GetChoiceAttributesFromEnumParameter(Type enumParam)
        {
            var choices = new List<DiscordApplicationCommandOptionChoice>();
            foreach (Enum foo in Enum.GetValues(enumParam))
            {
                choices.Add(new DiscordApplicationCommandOptionChoice(foo.GetName(), foo.ToString()));
            }
            return choices;
        }

        //Handler
        private Task InteractionHandler(DiscordClient client, InteractionCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if (e.Interaction.Type == InteractionType.ApplicationCommand)
                {
                    InteractionContext context = new InteractionContext
                    {
                        Interaction = e.Interaction,
                        Channel = e.Interaction.Channel,
                        Guild = e.Interaction.Guild,
                        User = e.Interaction.User,
                        Client = client,
                        SlashCommandsExtension = this,
                        CommandName = e.Interaction.Data.Name,
                        InteractionId = e.Interaction.Id,
                        Token = e.Interaction.Token,
                        Services = this._configuration?.Services
                    };

                    try
                    {
                        if (this.Errored)
                            throw new Exception("Slash commands failed to register properly on startup.");
                        var methods = this.CommandMethods.Where(x => x.Id == e.Interaction.Data.Id);
                        var groups = this.GroupCommands.Where(x => x.Id == e.Interaction.Data.Id);
                        var subgroups = this.SubGroupCommands.Where(x => x.Id == e.Interaction.Data.Id);
                        if (!methods.Any() && !groups.Any() && !subgroups.Any())
                            throw new Exception("A slash command was executed, but no command was registered for it.");

                        if (methods.Any())
                        {
                            var method = methods.First();

                            var args = await this.ResolveInteractionCommandParameters(e, context, method.Method, e.Interaction.Data.Options);
                            var classinstance = ActivatorUtilities.CreateInstance(this._configuration?.Services, method.ParentClass);

                            await ((SlashCommandModule)classinstance).BeforeExecutionAsync(context);

                            var task = (Task)method.Method.Invoke(classinstance, args.ToArray());
                            await task;

                            await ((SlashCommandModule)classinstance).AfterExecutionAsync(context);
                        }
                        else if (groups.Any())
                        {
                            var command = e.Interaction.Data.Options.First();
                            var method = groups.First().Methods.First(x => x.Key == command.Name).Value;

                            var args = await this.ResolveInteractionCommandParameters(e, context, method, e.Interaction.Data.Options.First().Options);
                            var classinstance = ActivatorUtilities.CreateInstance(this._configuration?.Services, groups.First().ParentClass);

                            SlashCommandModule module = null;
                            if (classinstance is SlashCommandModule _module)
                                module = _module;

                            await (module?.BeforeExecutionAsync(context) ?? Task.CompletedTask);

                            var task = (Task)method.Invoke(classinstance, args.ToArray());
                            await task;

                            await (module?.AfterExecutionAsync(context) ?? Task.CompletedTask);
                        }
                        else if (subgroups.Any())
                        {
                            var command = e.Interaction.Data.Options.First();
                            var group = subgroups.First(x => x.SubCommands.Any(y => y.Name == command.Name)).SubCommands.First(x => x.Name == command.Name);

                            var method = group.Methods.First(x => x.Key == command.Options.First().Name).Value;

                            var args = await this.ResolveInteractionCommandParameters(e, context, method, e.Interaction.Data.Options.First().Options.First().Options);
                            var classinstance = ActivatorUtilities.CreateInstance(this._configuration?.Services, group.ParentClass);

                            SlashCommandModule module = null;
                            if (classinstance is SlashCommandModule _module)
                                module = _module;

                            await (module?.BeforeExecutionAsync(context) ?? Task.CompletedTask);

                            var task = (Task)method.Invoke(classinstance, args.ToArray());
                            await task;

                            await (module?.AfterExecutionAsync(context) ?? Task.CompletedTask);
                        }

                        await this._executed.InvokeAsync(this, new SlashCommandExecutedEventArgs { Context = context });
                    }
                    catch (Exception ex)
                    {
                        await this._error.InvokeAsync(this, new SlashCommandErrorEventArgs { Context = context, Exception = ex });
                    }
                }
            });
            return Task.CompletedTask;
        }

        private async Task<List<object>> ResolveInteractionCommandParameters(InteractionCreateEventArgs e, InteractionContext context, MethodInfo method, IEnumerable<DiscordInteractionDataOption> options)
        {
            var args = new List<object> { context };
            var parameters = method.GetParameters().Skip(1);

            for (int i = 0; i < parameters.Count(); i++)
            {
                var parameter = parameters.ElementAt(i);
                if (parameter.IsOptional && (options == null ||
                                             (!options?.Any(x => x.Name == parameter.GetCustomAttribute<OptionAttribute>().Name.ToLower()) ?? true)))
                    args.Add(parameter.DefaultValue);
                else
                {
                    var option = options.Single(x => x.Name == parameter.GetCustomAttribute<OptionAttribute>().Name.ToLower());

                    if (ReferenceEquals(parameter.ParameterType, typeof(string)))
                        args.Add(option.Value.ToString());
                    else if (parameter.ParameterType.IsEnum)
                        args.Add(Enum.Parse(parameter.ParameterType, (string)option.Value));
                    else if (ReferenceEquals(parameter.ParameterType, typeof(long)))
                        args.Add((long) option.Value);
                    else if (ReferenceEquals(parameter.ParameterType, typeof(bool)))
                        args.Add((bool) option.Value);
                    else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordUser)))
                    {
                        if (e.Interaction.Data.Resolved.Members != null &&
                            e.Interaction.Data.Resolved.Members.TryGetValue((ulong) option.Value, out var member))
                        {
                            args.Add(member);
                        }
                        else if (e.Interaction.Data.Resolved.Users != null &&
                                 e.Interaction.Data.Resolved.Users.TryGetValue((ulong) option.Value, out var user))
                        {
                            args.Add(user);
                        }
                        else
                        {
                            args.Add(await this.Client.GetUserAsync((ulong) option.Value));
                        }
                    }
                    else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordChannel)))
                    {
                        if (e.Interaction.Data.Resolved.Channels != null &&
                            e.Interaction.Data.Resolved.Channels.TryGetValue((ulong) option.Value, out var channel))
                        {
                            args.Add(channel);
                        }
                        else
                        {
                            args.Add(e.Interaction.Guild.GetChannel((ulong) option.Value));
                        }
                    }
                    else if (ReferenceEquals(parameter.ParameterType, typeof(DiscordRole)))
                    {
                        if (e.Interaction.Data.Resolved.Roles != null &&
                            e.Interaction.Data.Resolved.Roles.TryGetValue((ulong) option.Value, out var role))
                        {
                            args.Add(role);
                        }
                        else
                        {
                            args.Add(e.Interaction.Guild.GetRole((ulong) option.Value));
                        }
                    }
                    else
                        throw new ArgumentException($"Error resolving interaction.");
                }
            }

            return args;
        }

        //Events

        /// <summary>
        /// Fires whenver the execution of a slash command fails
        /// </summary>
        public event AsyncEventHandler<SlashCommandsExtension, SlashCommandErrorEventArgs> SlashCommandErrored
        {
            add { this._error.Register(value); }
            remove { this._error.Unregister(value); }
        }
        private AsyncEvent<SlashCommandsExtension, SlashCommandErrorEventArgs> _error;

        /// <summary>
        /// Fires when the execution of a slash command is successful
        /// </summary>
        public event AsyncEventHandler<SlashCommandsExtension, SlashCommandExecutedEventArgs> SlashCommandExecuted
        {
            add { this._executed.Register(value); }
            remove { this._executed.Unregister(value); }
        }
        private AsyncEvent<SlashCommandsExtension, SlashCommandExecutedEventArgs> _executed;

        private ApplicationCommandOptionType GetParameterType(Type type)
        {
            ApplicationCommandOptionType parametertype;
            if (ReferenceEquals(type, typeof(string)))
                parametertype = ApplicationCommandOptionType.String;
            else if (ReferenceEquals(type, typeof(long)))
                parametertype = ApplicationCommandOptionType.Integer;
            else if (ReferenceEquals(type, typeof(bool)))
                parametertype = ApplicationCommandOptionType.Boolean;
            else if (ReferenceEquals(type, typeof(DiscordChannel)))
                parametertype = ApplicationCommandOptionType.Channel;
            else if (ReferenceEquals(type, typeof(DiscordUser)))
                parametertype = ApplicationCommandOptionType.User;
            else if (ReferenceEquals(type, typeof(DiscordRole)))
                parametertype = ApplicationCommandOptionType.Role;
            else if (type.IsEnum)
                parametertype = ApplicationCommandOptionType.String;

            else
                throw new ArgumentException("Cannot convert type! Argument types must be string, long, bool, DiscordChannel, DiscordUser, DiscordRole or an Enum.");

            return parametertype;
        }

        private List<DiscordApplicationCommandOptionChoice> GetChoiceAttributesFromParameter(IEnumerable<ChoiceAttribute> choiceattributes)
        {
            if (!choiceattributes.Any())
            {
                return null;
            }

            return choiceattributes.Select(att => new DiscordApplicationCommandOptionChoice(att.Name, att.Value)).ToList();
        }

        private async Task<List<DiscordApplicationCommandOption>> ParseParameters(ParameterInfo[] parameters)
        {
            var options = new List<DiscordApplicationCommandOption>();
            foreach (var parameter in parameters)
            {
                var optionattribute = parameter.GetCustomAttribute<OptionAttribute>();
                if (optionattribute == null)
                    throw new ArgumentException("Arguments must have the Option attribute!");

                var type = parameter.ParameterType;
                var parametertype = this.GetParameterType(type);

                var choices = this.GetChoiceAttributesFromParameter(parameter.GetCustomAttributes<ChoiceAttribute>());

                if (parameter.ParameterType.IsEnum)
                {
                    choices = GetChoiceAttributesFromEnumParameter(parameter.ParameterType);
                }

                var choiceProviders = parameter.GetCustomAttributes<ChoiceProviderAttribute>();

                if (choiceProviders.Any())
                {
                    choices = await this.GetChoiceAttributesFromProvider(choiceProviders);
                }

                options.Add(new DiscordApplicationCommandOption(optionattribute.Name, optionattribute.Description, parametertype, !parameter.IsOptional, choices));
            }

            return options;
        }

    }

    internal class CommandMethod
    {
        public ulong Id;

        public string Name;

        public MethodInfo Method;

        public Type ParentClass;
    }

    internal class GroupCommand
    {
        public ulong Id;

        public string Name;

        public Dictionary<string, MethodInfo> Methods = null;

        public Type ParentClass;
    }

    internal class SubGroupCommand
    {
        public ulong Id;

        public string Name;

        public List<GroupCommand> SubCommands = new List<GroupCommand>();
    }
}