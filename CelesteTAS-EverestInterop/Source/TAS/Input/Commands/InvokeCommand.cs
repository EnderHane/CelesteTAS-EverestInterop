﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using StudioCommunication;
using StudioCommunication.Util;
using System.Runtime.CompilerServices;
using TAS.EverestInterop;
using TAS.EverestInterop.InfoHUD;
using TAS.Utils;

namespace TAS.Input.Commands;

public static class InvokeCommand {
    private class InvokeMeta : ITasCommandMeta {
        public string Insert => $"Invoke{CommandInfo.Separator}[0;Entity.Method]{CommandInfo.Separator}[1;Parameter]";
        public bool HasArguments => true;

        public int GetHash(string[] args, string filePath, int fileLine) {
            int hash = SetCommand.SetMeta.GetTargetArgs(args)
                .Aggregate(17, (current, arg) => 31 * current + arg.GetStableHashCode());
            // The other argument don't influence each other, so just the length matters
            return 31 * hash + 17 * args.Length;
        }

        public IEnumerator<CommandAutoCompleteEntry> GetAutoCompleteEntries(string[] args, string filePath, int fileLine) {
            var targetArgs = SetCommand.SetMeta.GetTargetArgs(args).ToArray();

            // Parameters
            if (args.Length > 1) {
                using var enumerator = GetParameterAutoCompleteEntries(targetArgs, args.Length - 2);
                while (enumerator.MoveNext()) {
                    yield return enumerator.Current;
                }
                yield break;
            }

            if (targetArgs.Length == 0) {
                var allTypes = ModUtils.GetTypes();
                foreach ((string typeName, var type) in allTypes
                             .Select(type => (type.CSharpName(), type))
                             .Order(new SetCommand.SetMeta.NamespaceComparer()))
                {
                    if (
                        // Filter-out types which probably aren't useful
                        !type.IsClass || !type.IsPublic || type.FullName == null || type.Namespace == null || SetCommand.SetMeta.ignoredNamespaces.Any(ns => type.Namespace.StartsWith(ns)) ||

                        // Filter-out compiler generated types
                        !type.GetCustomAttributes<CompilerGeneratedAttribute>().IsEmpty() || type.FullName.Contains('<') || type.FullName.Contains('>') ||

                        // Require either an entity, level, session
                        !type.IsSameOrSubclassOf(typeof(Entity)) && !type.IsSameOrSubclassOf(typeof(Level)) && !type.IsSameOrSubclassOf(typeof(Session)) &&
                        // Or type with static (invokable) methods
                        !type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            .Any(IsInvokableMethod))
                    {
                        continue;
                    }

                    // Strip the namespace and add the @modname suffix if the typename isn't unique
                    string uniqueTypeName = typeName;
                    foreach (var otherType in allTypes) {
                        if (otherType.FullName == null || otherType.Namespace == null) {
                            continue;
                        }

                        string otherName = otherType.CSharpName();
                        if (type != otherType && typeName == otherName) {
                            uniqueTypeName = $"{typeName}@{ConsoleEnhancements.GetModName(type)}";
                            break;
                        }
                    }

                    yield return new CommandAutoCompleteEntry { Name = $"{uniqueTypeName}.", Extra = type.Namespace ?? string.Empty, IsDone = false };
                }
            } else if (targetArgs.Length >= 1 && InfoCustom.TryParseTypes(targetArgs[0], out var types, out _, out _)) {
                // Let's just assume the first type
                foreach (var entry in GetInvokeTypeAutoCompleteEntries(types[0], targetArgs.Length == 1)) {
                    yield return entry with { Name = entry.Name + (entry.IsDone ? "" : "."), Prefix = string.Join('.', targetArgs) + ".", HasNext = true };
                }
            }
        }

        private static IEnumerable<CommandAutoCompleteEntry> GetInvokeTypeAutoCompleteEntries(Type type, bool isRootType) {
            bool staticMembers = isRootType && !(type.IsSameOrSubclassOf(typeof(Entity)) || type.IsSameOrSubclassOf(typeof(Level)) || type.IsSameOrSubclassOf(typeof(Session)) || type.IsSameOrSubclassOf(typeof(EverestModuleSettings)));
            var bindingFlags = staticMembers
                ? BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
                : BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            foreach (var m in type.GetMethods(bindingFlags).OrderBy(m => m.Name)) {
                // Filter-out compiler generated methods
                if (m.GetCustomAttributes<CompilerGeneratedAttribute>().IsEmpty() && !m.Name.Contains('<') && !m.Name.Contains('>') && !m.Name.StartsWith("set_") && !m.Name.StartsWith("get_") &&
                    IsInvokableMethod(m))
                {
                    yield return new CommandAutoCompleteEntry { Name = m.Name, Extra = $"({string.Join(", ", m.GetParameters().Select(p => p.HasDefaultValue ? $"[{p.ParameterType.CSharpName()}]" : p.ParameterType.CSharpName()))})", IsDone = true, };
                }
            }
        }

        private static IEnumerator<CommandAutoCompleteEntry> GetParameterAutoCompleteEntries(string[] targetArgs, int parameterIndex) {
            if (targetArgs.Length == 2 && InfoCustom.TryParseTypes(targetArgs[0], out var types, out _, out _)) {
                // Let's just assume the first type
                var parameters = types[0].GetMethodInfo(targetArgs[1]).GetParameters();
                if (parameterIndex >= 0 && parameterIndex < parameters.Length) {
                    // End arguments if further parameters aren't settable anymore
                    bool final = parameterIndex == parameters.Length - 1 ||
                                 parameterIndex < parameters.Length - 1 && !SetCommand.SetMeta.IsSettableType(parameters[parameterIndex].ParameterType);

                    return SetCommand.SetMeta.GetParameterTypeAutoCompleteEntries(parameters[parameterIndex].ParameterType, hasNextArgument: !final);
                }
            }

            return Enumerable.Empty<CommandAutoCompleteEntry>().GetEnumerator();
        }

        private static bool IsInvokableMethod(MethodInfo info) {
            // Generic methods could probably be supported somehow, but that's probably not worth
            if (info.IsGenericMethod) {
                return false;
            }
            // To be invokable, all parameters need to be settable or have a default value from a non-settable onwards
            bool requireDefaults = false;
            foreach (var param in info.GetParameters()) {
                if (!requireDefaults && !SetCommand.SetMeta.IsSettableType(param.ParameterType)) {
                    requireDefaults = true;
                }

                if (requireDefaults && !param.HasDefaultValue) {
                    return false;
                }
            }

            return true;
        }
    }

    private static bool consolePrintLog;
    private const string logPrefix = "Invoke Command Failed: ";
    private static readonly object nonReturnObject = new();

    private static readonly List<string> errorLogs = new List<string>();
    private static bool suspendLog = false;

    [Monocle.Command("invoke", "Invoke level/session/entity method. eg invoke Level.Pause; invoke Player.Jump (CelesteTAS)")]
    private static void Invoke(string arg1, string arg2, string arg3, string arg4, string arg5, string arg6, string arg7, string arg8,
        string arg9) {
        string[] args = {arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9};
        consolePrintLog = true;
        Invoke(args.TakeWhile(arg => arg != null).ToArray());
        consolePrintLog = false;
    }

    // Invoke, Type.StaticMethod, Parameters...
    // Invoke, Level.Method, Parameters...
    // Invoke, Session.Method, Parameters...
    // Invoke, Entity.Method, Parameters...
    [TasCommand("Invoke", LegalInFullGame = false, MetaDataProvider = typeof(InvokeMeta))]
    private static void Invoke(CommandLine commandLine, int studioLine, string filePath, int fileLine) {
        Invoke(commandLine.Arguments);
    }

    private static void Invoke(string[] args) {
        if (args.Length < 1) {
            return;
        }

        try {
            if (args[0].Contains(".")) {
                string[] parameters = args.Skip(1).ToArray();
                if (InfoCustom.TryParseMemberNames(args[0], out string typeText, out List<string> memberNames, out string errorMessage)
                    && InfoCustom.TryParseTypes(typeText, out List<Type> types, out string entityId, out errorMessage)) {
                    bool existSuccess = false;
                    bool forSpecific = entityId.IsNotNullOrEmpty();
                    suspendLog = true;
                    foreach (Type type in types) {
                        object result = FindObjectAndInvoke(type, entityId, memberNames, parameters);
                        bool hasReturned = result != nonReturnObject;
                        if (hasReturned) {
                            result ??= "null";
                            result.Log(consolePrintLog);
                        }
                        existSuccess |= hasReturned;
                        if (forSpecific && hasReturned) {
                            break;
                        }
                    }
                    suspendLog = false;
                    if (!forSpecific || !existSuccess) {
                        errorLogs.Where(text => !existSuccess || !text.EndsWith(" entity is not found") && !text.EndsWith(" object is not found")).ToList().ForEach(Log);
                    }
                    errorLogs.Clear();
                } else {
                    errorMessage.Log(consolePrintLog, LogLevel.Warn);
                }
            }
        } catch (Exception e) {
            e.Log(consolePrintLog, LogLevel.Warn);
        }
    }

    private static object FindObjectAndInvoke(Type type, string entityId, List<string> memberNames, string[] parameters) {
        if (memberNames.IsEmpty()) {
            return nonReturnObject;
        }

        string lastMemberName = memberNames.Last();
        memberNames = memberNames.SkipLast(1).ToList();

        Type objType;
        object obj = null;
        if (memberNames.IsEmpty() && type.GetMethodInfo(lastMemberName) is {IsStatic: true}) {
            objType = type;
        } else if (memberNames.IsNotEmpty() && InfoCustom.GetMemberValue(type, null, memberNames) is { } value) {
            obj = value;
            if (TryPrintErrorLog()) {
                return nonReturnObject;
            }

            objType = obj.GetType();
        } else {
            if (memberNames.IsEmpty() && type.GetMethodInfo(lastMemberName, null) == null) {
                Log($"{type.FullName}.{lastMemberName} method is not found");
                return nonReturnObject;
            }

            obj = SetCommand.FindSpecialObject(type, entityId);
            if (obj == null) {
                Log($"{type.FullName}{entityId.LogId()} object is not found");
                return nonReturnObject;
            } else {
                if (type.IsSameOrSubclassOf(typeof(Entity)) && obj is List<Entity> entities) {
                    if (entities.IsEmpty()) {
                        Log($"{type.FullName}{entityId.LogId()} entity is not found");
                        return nonReturnObject;
                    } else {
                        List<object> memberValues = new();
                        foreach (Entity entity in entities) {
                            object memberValue = InfoCustom.GetMemberValue(type, entity, memberNames);
                            if (TryPrintErrorLog()) {
                                return nonReturnObject;
                            }

                            if (memberValue != null) {
                                memberValues.Add(memberValue);
                            }
                        }

                        if (memberValues.IsEmpty()) {
                            return nonReturnObject;
                        }

                        obj = memberValues;
                        objType = memberValues.First().GetType();
                    }
                } else {
                    obj = InfoCustom.GetMemberValue(type, obj, memberNames);
                    if (TryPrintErrorLog()) {
                        return null;
                    }

                    objType = obj.GetType();
                }
            }
        }

        if (type.IsSameOrSubclassOf(typeof(Entity)) && obj is List<object> objects) {
            List<object> result = new();
            foreach (object o in objects) {
                if (TryInvokeMethod(o, out object r)) {
                    r ??= "null";
                    result.Add(r);
                }
            }

            return result.IsEmpty() ? nonReturnObject : string.Join("\n", result);
        } else {
            if (TryInvokeMethod(obj, out object r)) {
                return r;
            } else {
                return nonReturnObject;
            }
        }

        bool TryInvokeMethod(object @object, out object returnObject) {
            if (objType.GetMethodInfo(lastMemberName) is { } methodInfo) {
                List<ParameterInfo> parameterInfos = methodInfo.GetParameters().ToList();
                object[] p = new object[parameterInfos.Count];
                for (int i = 0; i < parameterInfos.Count; i++) {
                    object convertedObj;
                    ParameterInfo parameterInfo = parameterInfos[i];
                    Type parameterType = parameterInfo.ParameterType;

                    if (parameters.IsEmpty()) {
                        p[i] = parameterInfo.HasDefaultValue ? parameterInfo.DefaultValue : SetCommand.Convert(null, parameterType);
                        continue;
                    }

                    if (parameterType == typeof(Vector2)) {
                        string[] array = parameters.Take(2).ToArray();
                        float.TryParse(array.GetValueOrDefault(0), out float x);
                        float.TryParse(array.GetValueOrDefault(1), out float y);
                        convertedObj = new Vector2(x, y);
                        parameters = parameters.Skip(2).ToArray();
                    } else if (parameterType.IsSameOrSubclassOf(typeof(Entity))) {
                        if (InfoCustom.TryParseType(parameters[0], out Type entityType, out string id, out string errorMessage)) {
                            convertedObj = ((List<Entity>) SetCommand.FindSpecialObject(entityType, id)).FirstOrDefault();
                        } else {
                            Log(errorMessage);
                            convertedObj = null;
                        }
                    } else if (parameterType == typeof(Level)) {
                        convertedObj = Engine.Scene.GetLevel();
                    } else if (parameterType == typeof(Session)) {
                        convertedObj = Engine.Scene.GetSession();
                    } else {
                        convertedObj = SetCommand.Convert(parameters.FirstOrDefault(), parameterType);
                        parameters = parameters.Skip(1).ToArray();
                    }

                    p[i] = convertedObj;
                }

                returnObject = methodInfo.Invoke(@object, p);
                return methodInfo.ReturnType != typeof(void);
            } else {
                Log($"{objType.FullName}.{lastMemberName} member not found");
                returnObject = nonReturnObject;
                return false;
            }
        }

        bool TryPrintErrorLog() {
            if (obj == null) {
                Log($"{type.FullName}{entityId.LogId()} member value is null");
                return true;
            } else if (obj is string errorMsg && errorMsg.EndsWith(" not found")) {
                Log(errorMsg);
                return true;
            }

            return false;
        }

    }

    private static string LogId(this string entityId) {
        return entityId.IsNullOrEmpty() ? "" : $"[{entityId}]";
    }

    private static void Log(string text) {
        if (suspendLog) {
            errorLogs.Add(text);
            return;
        }
        if (!consolePrintLog) {
            text = $"{logPrefix}{text}";
        }

        text.Log(consolePrintLog, LogLevel.Warn);
    }
}
