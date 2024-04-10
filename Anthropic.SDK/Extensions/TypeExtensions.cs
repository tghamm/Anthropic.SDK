using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;

namespace Anthropic.SDK.Extensions
{
    internal static class TypeExtensions
    {

        public static List<Messaging.Tool> GenerateJsonToolsFromCommonTools(this IList<Common.Tool> commonTools)
        {
            var tools = new List<Messaging.Tool>();

            foreach (var commonTool in commonTools)
            {
                var tool = new Messaging.Tool
                {
                    name = commonTool.Function.Name,
                    description = commonTool.Function.Description,
                    input_schema = new Messaging.InputSchema
                    {
                        type = "object",
                        properties = new Dictionary<string, Messaging.Property>(),
                        required = new List<string>()
                    }
                };

                foreach (var property in commonTool.Function.MethodInfo.GetParameters())
                {
                    var functionPropertyAttribute = property.GetCustomAttribute<FunctionParameterAttribute>();
                    //var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
                    var propertyName = property.Name;
                    var propertyInfo = new Messaging.Property
                    {
                        description = functionPropertyAttribute?.Description
                    };
                    if (property.ParameterType == typeof(string) || property.ParameterType == typeof(char))
                    {
                        propertyInfo.type = "string";
                        tool.input_schema.properties[propertyName] = propertyInfo;
                    }
                    else if (property.ParameterType == typeof(int) ||
                             property.ParameterType == typeof(long) ||
                             property.ParameterType == typeof(uint) ||
                             property.ParameterType == typeof(byte) ||
                             property.ParameterType == typeof(sbyte) ||
                             property.ParameterType == typeof(ulong) ||
                             property.ParameterType == typeof(short) ||
                             property.ParameterType == typeof(ushort))
                    {
                        propertyInfo.type = "integer";
                        tool.input_schema.properties[propertyName] = propertyInfo;
                    }
                    else if (property.ParameterType == typeof(float) ||
                             property.ParameterType == typeof(double) ||
                             property.ParameterType == typeof(decimal))
                    {
                        propertyInfo.type = "number";
                        tool.input_schema.properties[propertyName] = propertyInfo;
                    }
                    else if (property.ParameterType == typeof(bool))
                    {
                        propertyInfo.type = "boolean";
                        tool.input_schema.properties[propertyName] = propertyInfo;
                    }
                    else if (property.ParameterType == typeof(DateTime) ||
                             property.ParameterType == typeof(DateTimeOffset))
                    {
                        propertyInfo.type = "string";
                        tool.input_schema.properties[propertyName] = propertyInfo;
                    }
                    else if (property.ParameterType == typeof(Guid))
                    {
                        propertyInfo.type = "string";
                        tool.input_schema.properties[propertyName] = propertyInfo;
                    }
                    else if (property.ParameterType.IsEnum)
                    {
                        propertyInfo.type = "string";
                        propertyInfo.@enum = Enum.GetNames(property.ParameterType);
                        tool.input_schema.properties[propertyName] = propertyInfo;
                    }
                    else if (property.ParameterType.IsArray || (property.ParameterType.IsGenericType &&
                                                                property.ParameterType.GetGenericTypeDefinition() ==
                                                                typeof(List<>)))
                    {
                        propertyInfo.type = "array";
                        var elementType = property.ParameterType.GetElementType() ??
                                         property.ParameterType.GetGenericArguments()[0];
                        if (elementType.IsClass)
                        {
                            throw new InvalidOperationException("Array or List properties must be of a primitive type.");
                        }
                        else
                        {
                            tool.input_schema.properties[propertyName] = propertyInfo;
                        }
                    }
                    else if (property.ParameterType.IsClass)
                    {
                        throw new InvalidOperationException("Array or List properties must be of a primitive type.");
                    }
                    else if (property.ParameterType.IsArray || (property.ParameterType.IsGenericType &&
                                                                property.ParameterType.GetGenericTypeDefinition() ==
                                                                typeof(List<>)))
                    {
                        propertyInfo.type = "array";
                        var elementType = property.ParameterType.GetElementType() ??
                                         property.ParameterType.GetGenericArguments()[0];
                        if (elementType.IsClass)
                        {
                            throw new InvalidOperationException("Array or List properties must be of a primitive type.");
                        }
                        else
                        {
                            
                            tool.input_schema.properties[propertyName] = propertyInfo;
                        }

                    }
                    else if (property.ParameterType.IsClass)
                    {
                        throw new InvalidOperationException("Array or List properties must be of a primitive type.");
                    }
                    else
                    {
                        propertyInfo.type = "string";
                    }


                    if (functionPropertyAttribute != null && functionPropertyAttribute.Required)
                    {
                        tool.input_schema.required.Add(propertyName);
                    }
                }

                tools.Add(tool);
            }

            return tools;
        }




        public static JsonObject GenerateJsonSchema(this MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();

            if (parameters.Length == 0)
            {
                return null;
            }

            var schema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject()
            };
            var requiredParameters = new JsonArray();

            foreach (var parameter in parameters)
            {
                if (parameter.ParameterType == typeof(CancellationToken))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(parameter.Name))
                {
                    throw new InvalidOperationException($"Failed to find a valid parameter name for {methodInfo.DeclaringType}.{methodInfo.Name}()");
                }

                if (!parameter.HasDefaultValue)
                {
                    requiredParameters.Add(parameter.Name);
                }

                schema["properties"]![parameter.Name] = GenerateJsonSchema(parameter.ParameterType, schema);

                var functionParameterAttribute = parameter.GetCustomAttribute<FunctionParameterAttribute>();

                if (functionParameterAttribute != null)
                {
                    schema["properties"]![parameter.Name]!["description"] = functionParameterAttribute.Description;
                }
            }

            if (requiredParameters.Count > 0)
            {
                schema["required"] = requiredParameters;
            }

            return schema;
        }

        public static JsonObject GenerateJsonSchema(this Type type, JsonObject rootSchema)
        {
            var schema = new JsonObject();

            if (!type.IsPrimitive &&
                type != typeof(Guid) &&
                type != typeof(DateTime) &&
                type != typeof(DateTimeOffset) &&
                rootSchema["definitions"] != null &&
                rootSchema["definitions"].AsObject().ContainsKey(type.FullName))
            {
                return new JsonObject { ["$ref"] = $"#/definitions/{type.FullName}" };
            }

            if (type == typeof(string) || type == typeof(char))
            {
                schema["type"] = "string";
            }
            else if (type == typeof(int) ||
                     type == typeof(long) ||
                     type == typeof(uint) ||
                     type == typeof(byte) ||
                     type == typeof(sbyte) ||
                     type == typeof(ulong) ||
                     type == typeof(short) ||
                     type == typeof(ushort))
            {
                schema["type"] = "integer";
            }
            else if (type == typeof(float) ||
                     type == typeof(double) ||
                     type == typeof(decimal))
            {
                schema["type"] = "number";
            }
            else if (type == typeof(bool))
            {
                schema["type"] = "boolean";
            }
            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                schema["type"] = "string";
                //schema["format"] = "date-time";
            }
            else if (type == typeof(Guid))
            {
                schema["type"] = "string";
                //schema["format"] = "uuid";
            }
            else if (type.IsEnum)
            {
                schema["type"] = "string";
                schema["enum"] = new JsonArray();

                foreach (var value in Enum.GetValues(type))
                {
                    schema["enum"].AsArray().Add(JsonNode.Parse(JsonSerializer.Serialize(value)));
                }
            }
            else if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                schema["type"] = "array";
                var elementType = type.GetElementType() ?? type.GetGenericArguments()[0];

                if (rootSchema["definitions"] != null &&
                    rootSchema["definitions"].AsObject().ContainsKey(elementType.FullName))
                {
                    schema["items"] = new JsonObject { ["$ref"] = $"#/definitions/{elementType.FullName}" };
                }
                else
                {
                    schema["items"] = GenerateJsonSchema(elementType, rootSchema);
                }
            }
            else
            {
                schema["type"] = "object";
                rootSchema["definitions"] ??= new JsonObject();
                rootSchema["definitions"][type.FullName] = new JsonObject();

                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var members = new List<MemberInfo>(properties.Length + fields.Length);
                members.AddRange(properties);
                members.AddRange(fields);

                var memberInfo = new JsonObject();
                var memberProperties = new JsonArray();

                foreach (var member in members)
                {
                    var memberType = GetMemberType(member);
                    var functionPropertyAttribute = member.GetCustomAttribute<FunctionPropertyAttribute>();
                    var jsonPropertyAttribute = member.GetCustomAttribute<JsonPropertyNameAttribute>();
                    var jsonIgnoreAttribute = member.GetCustomAttribute<JsonIgnoreAttribute>();
                    var propertyName = jsonPropertyAttribute?.Name ?? member.Name;

                    JsonObject propertyInfo;

                    if (rootSchema["definitions"] != null &&
                        rootSchema["definitions"].AsObject().ContainsKey(memberType.FullName))
                    {
                        propertyInfo = new JsonObject { ["$ref"] = $"#/definitions/{memberType.FullName}" };
                    }
                    else
                    {
                        propertyInfo = GenerateJsonSchema(memberType, rootSchema);
                    }

                    // override properties with values from function property attribute
                    if (functionPropertyAttribute != null)
                    {
                        propertyInfo["description"] = functionPropertyAttribute.Description;

                        if (functionPropertyAttribute.Required)
                        {
                            memberProperties.Add(propertyName);
                        }

                        JsonNode defaultValue = null;

                        if (functionPropertyAttribute.DefaultValue != null)
                        {
                            defaultValue = JsonNode.Parse(JsonSerializer.Serialize(functionPropertyAttribute.DefaultValue));
                            propertyInfo["default"] = defaultValue;
                        }

                        if (functionPropertyAttribute.PossibleValues is { Length: > 0 })
                        {
                            var enums = new JsonArray();

                            foreach (var value in functionPropertyAttribute.PossibleValues)
                            {
                                var @enum = JsonNode.Parse(JsonSerializer.Serialize(value));

                                if (defaultValue == null)
                                {
                                    enums.Add(@enum);
                                }
                                else
                                {
                                    if (@enum != defaultValue)
                                    {
                                        enums.Add(@enum);
                                    }
                                }
                            }

                            if (defaultValue != null && !enums.Contains(defaultValue))
                            {
                                enums.Add(JsonNode.Parse(defaultValue.ToJsonString()));
                            }

                            propertyInfo["enum"] = enums;
                        }
                    }
                    else if (jsonIgnoreAttribute != null)
                    {
                        // only add members that are required
                        switch (jsonIgnoreAttribute.Condition)
                        {
                            case JsonIgnoreCondition.Never:
                            case JsonIgnoreCondition.WhenWritingDefault:
                                memberProperties.Add(propertyName);
                                break;
                            case JsonIgnoreCondition.Always:
                            case JsonIgnoreCondition.WhenWritingNull:
                            default:
                                memberProperties.Remove(propertyName);
                                break;
                        }
                    }
                    else if (Nullable.GetUnderlyingType(memberType) == null)
                    {
                        memberProperties.Add(propertyName);
                    }

                    memberInfo[propertyName] = propertyInfo;
                }

                schema["properties"] = memberInfo;

                if (memberProperties.Count > 0)
                {
                    schema["required"] = memberProperties;
                }

                rootSchema["definitions"] ??= new JsonObject();
                rootSchema["definitions"][type.FullName] = schema;
                return new JsonObject { ["$ref"] = $"#/definitions/{type.FullName}" };
            }

            return schema;
        }

        private static Type GetMemberType(MemberInfo member)
            => member switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                _ => throw new ArgumentException($"{nameof(MemberInfo)} must be of type {nameof(FieldInfo)}, {nameof(PropertyInfo)}", nameof(member))
            };
    }
}
