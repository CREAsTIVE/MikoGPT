using MikoGPT.VKButtonGUIFieldAttributes;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Serialization;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.GroupUpdate;
using VkNet.Model.Keyboard;
using static MikoGPT.ButtonPayload;

namespace MikoGPT
{
    public class ButtonPayload
    {
        public ActionType actionType { get; set; }
        public ObjectType objectType { get; set; }
        public string Name { get; set; } = "null";
        public object? Value { get; set; }

        public static ButtonPayload FromJson(string json) =>
            JsonSerializer.Deserialize<ButtonPayload>(json, options: new()
            {
                Converters = {new ObjectDeserializer()}
            })
            ?? throw new ArgumentException("Wrong payload!");
        public string ToJson() => JsonSerializer.Serialize(this, typeof(ButtonPayload), options: new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        public enum ActionType
        {
            UpdateParameterToValue,
            RequireParameterForUpdate,
            RequireChooseParameter,
            ExecuteMethod
        }
        public enum ObjectType
        {
            Userdata,
            Chatdata
        }
        class ObjectDeserializer : JsonConverter<object>
        {
            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var type = reader.TokenType;

                if (type == JsonTokenType.Number)
                {
                    var oki = reader.TryGetInt32(out var vali);
                    if (oki)
                        return vali;
                    var okl = reader.TryGetInt64(out var vall);
                    if (okl)
                        return vall;
                    var okd = reader.TryGetDouble(out var val);
                    if (okd)
                        return val;
                }

                if (type == JsonTokenType.String)
                    return reader.GetString() ?? "";

                if (type == JsonTokenType.True || type == JsonTokenType.False)
                    return reader.GetBoolean();
                // copied from corefx repo:
                using var document = JsonDocument.ParseValue(ref reader);
                return document.RootElement.Clone();
            }
            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
    public class VKButtonGUI
    {
        public static VkApi api = new();
        public static (IEnumerable<IEnumerable<MessageKeyboardButton>>? buttons, string message) Build(object data, ObjectType objectType)
        {
            //y:6; x:5
            MessageKeyboardButton?[][] keyboard = new MessageKeyboardButton[6][];
            for (var i = 0; i < keyboard.Length; i++)
                keyboard[i] = new MessageKeyboardButton?[5];

            // Build keyboard
            var properties = data.GetType().GetProperties();
            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<VKButtonAttribute>();

                if (attribute is null)
                    continue;

                var conditionAttributes = property.GetCustomAttributes<VKButtonConditionAttribute>();
                bool destroy = false;
                foreach (var conditionAttribute in conditionAttributes)
                    if (!data.GetType().GetRuntimeProperties().Where((p) => p.Name == conditionAttribute.PropertyName).First().GetValue(data)?.Equals(conditionAttribute.Value) ?? false)
                    {
                        destroy = true;
                        break;
                    }
                if (destroy)
                    continue;

                var choosableVariants = VKButtonChoosableVariantAttribute.BuildDictionary(property);

                if (choosableVariants.Count > 0)
                {
                    keyboard[attribute.Position.y][attribute.Position.x] = new()
                    {
                        Color = KeyboardButtonColor.Default,
                        Action = new()
                        {
                            Label = attribute.DisplayName,
                            Type = KeyboardButtonActionType.Callback,
                            Payload = new ButtonPayload()
                            {
                                actionType = ActionType.RequireChooseParameter,
                                objectType = objectType,
                                Name = property.Name
                            }.ToJson()
                        }
                    };
                }
                else
                {
                    keyboard[attribute.Position.y][attribute.Position.x] = (attribute, property.GetValue(data), property.PropertyType.Name) switch
                    {
                        (null, _, _) => null,
                        (_, bool value, _) => new()
                        {
                            Color = value ? KeyboardButtonColor.Positive : KeyboardButtonColor.Negative,
                            Action = new()
                            {
                                Label = attribute.DisplayName,
                                Type = KeyboardButtonActionType.Callback,
                                Payload = new ButtonPayload()
                                {
                                    actionType = ActionType.UpdateParameterToValue,
                                    objectType = objectType,
                                    Name = property.Name,
                                    Value = !value
                                }.ToJson()
                            }
                        },
                        (_, _, "String") => new()
                        {
                            Color = KeyboardButtonColor.Default,
                            Action = new()
                            {
                                Label = attribute.DisplayName,
                                Type = KeyboardButtonActionType.Callback,
                                Payload = new ButtonPayload()
                                {
                                    actionType = ActionType.RequireParameterForUpdate,
                                    objectType = objectType,
                                    Name = property.Name
                                }.ToJson()
                            }
                        },
                        _ => null,
                    };
                }
            }

            var methods = data.GetType().GetMethods();
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<VKButtonAttribute>();

                if (attribute is null)
                    continue;

                var conditionAttributes = method.GetCustomAttributes<VKButtonConditionAttribute>();
                bool destroy = false;
                foreach (var conditionAttribute in conditionAttributes)
                    if (!data.GetType().GetProperty(conditionAttribute.PropertyName)?.GetValue(data)?.Equals(conditionAttribute.Value) ?? true)
                    {
                        destroy = true;
                        break;
                    }
                if (destroy)
                    continue;

                keyboard[attribute.Position.y][attribute.Position.x] = new()
                {
                    Color = KeyboardButtonColor.Primary,
                    Action = new()
                    {
                        Label = attribute.DisplayName,
                        Type = KeyboardButtonActionType.Callback,
                        Payload = new ButtonPayload()
                        {
                            actionType = ActionType.ExecuteMethod,
                            Name = method.Name,
                            objectType = objectType,
                        }.ToJson()
                    }
                };
            }

            // repair empty spaces
            LinkedList<LinkedList<MessageKeyboardButton>> repaired = new LinkedList<LinkedList<MessageKeyboardButton>>();
            foreach (var collumn in keyboard)
            {
                LinkedList<MessageKeyboardButton> row = new LinkedList<MessageKeyboardButton>();
                foreach (var button in collumn)
                    if (button != null)
                        row.AddLast(button);
                if (row.Count > 0)
                    repaired.AddLast(row);
            }

            return (repaired.Count > 0 ? repaired : null, data.GetType().GetMethod("GetText")?.Invoke(data, null)?.ToString() ?? "");
        }
        public delegate bool ExecuteMethod(MessageEvent messageEvent);
        public static Dictionary<(long peerId, long cmid), (string paramName, ObjectType objType)> ParametersForUpdate = new();
        public static void OnPressed(ButtonPayload buttonPayload, long peerId, long messageSettingId, long userId, MessageEvent messageEvent, IDatabase database)
        {
            object workedObject = buttonPayload.objectType == ObjectType.Userdata ?
                database.GetUserData(userId) :
                database.GetChatData(peerId);

            bool updateSettingsMessage = false;

            switch (buttonPayload.actionType)
            {
                case ActionType.UpdateParameterToValue:
                    var property = workedObject.GetType().GetProperty(buttonPayload.Name) ?? throw new ArgumentException("Unknow property");
                    property.SetValue(workedObject, buttonPayload.Value);
                    api.Messages.SendMessageEventAnswer(messageEvent.EventId, messageEvent.UserId ?? 0, messageEvent.PeerId ?? 0, new()
                    {
                        Type = MessageEventType.SnowSnackbar,
                        Text = $"Параметр \"{buttonPayload.Name}\" изменил своё значение."
                    });
                    updateSettingsMessage = true;
                    break;
                case ActionType.RequireChooseParameter:
                    property = workedObject.GetType().GetProperty(buttonPayload.Name) ?? throw new ArgumentException("Unknow property");
                    var choosableVariants = VKButtonChoosableVariantAttribute.BuildDictionary(property);

                    LinkedList<LinkedList<MessageKeyboardButton>> buttonsArray = new();
                    LinkedList<MessageKeyboardButton> current = new();
                    buttonsArray.AddLast(current);
                    foreach (var variant in choosableVariants)
                    {
                        if (current.Count > 5)
                        {
                            current = new();
                            buttonsArray.AddLast(current);
                        }
                        current.AddLast(new MessageKeyboardButton()
                        {
                            Color = KeyboardButtonColor.Default,
                            Action = new()
                            {
                                Label = variant.Key,
                                Type = KeyboardButtonActionType.Callback,
                                Payload = new ButtonPayload()
                                {
                                    actionType = ActionType.UpdateParameterToValue,
                                    Name = buttonPayload.Name,
                                    Value = variant.Value,
                                    objectType = buttonPayload.objectType
                                }.ToJson()
                            }
                        });
                    }
                    api.Messages.Edit(new()
                    {
                        ConversationMessageId = messageSettingId,
                        PeerId = peerId,
                        Message = "Выбирите значение:",
                        Keyboard = new()
                        {
                            Inline = true,
                            Buttons = buttonsArray
                        }
                    });
                    break;
                case ActionType.RequireParameterForUpdate:
                    ParametersForUpdate[(peerId, messageSettingId)] = (buttonPayload.Name, buttonPayload.objectType);
                    api.Messages.Edit(new()
                    {
                        PeerId = peerId,
                        ConversationMessageId = messageSettingId,
                        Keyboard = null,
                        Message = $"Ответьте на это сообщение указав новое значение для параметра {buttonPayload.Name}"
                    });
                    break;
                case ActionType.ExecuteMethod:
                    updateSettingsMessage = workedObject.GetType().GetMethod(buttonPayload.Name)?.CreateDelegate<ExecuteMethod>(workedObject)(messageEvent) ?? throw new ArgumentException("Wrong method");
                    api.Messages.SendMessageEventAnswer(messageEvent.EventId, userId, peerId, new()
                    {
                        Type = MessageEventType.SnowSnackbar,
                        Text = "Выполняю..."
                    });
                    break;
            }
            if (updateSettingsMessage)
            {
                var keyboardButtons = Build(workedObject, buttonPayload.objectType);
                api.Messages.Edit(new()
                {
                    ConversationMessageId = messageSettingId,
                    PeerId = peerId,
                    Message = keyboardButtons.message,
                    Keyboard = new()
                    {
                        Inline = true,
                        Buttons = keyboardButtons.buttons
                    }
                });
                if (buttonPayload.objectType == ObjectType.Userdata)
                    database.SaveUserData(userId, workedObject as UserData ?? throw new ArgumentException("Wrong workedObject type"));
                else if (buttonPayload.objectType == ObjectType.Chatdata)
                    database.SaveChatData(peerId, workedObject as ChatData ?? throw new ArgumentException("Wrong workedObject type"));
            }
        }
    }

    namespace VKButtonGUIFieldAttributes
    {
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
        public class VKButtonAttribute : Attribute
        {
            public VKButtonAttribute(string name, int xPos, int yPos)
            {
                DisplayName = name; 
                Position = (xPos,  yPos);
            }
            public string DisplayName { get; set; }
            public (int x, int y) Position;
            public bool Choosable = true;
        }
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
        public class VKButtonChoosableVariantAttribute : Attribute
        {
            public string Key;
            public object Value;
            public VKButtonChoosableVariantAttribute(string key, object value)
            {
                Key = key;
                Value = value;
            }
            public static Dictionary<string, object> BuildDictionary(PropertyInfo property)
            {
                Dictionary<string, object> dict = new();
                var attrs = property.GetCustomAttributes<VKButtonChoosableVariantAttribute>();
                foreach (var attr in attrs)
                {
                    dict.Add(attr.Key, attr.Value);
                }
                return dict;
            }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple =true)]
        public class VKButtonConditionAttribute : Attribute
        {
            public VKButtonConditionType ConditionType;
            public string PropertyName;
            public object Value;
            public VKButtonConditionAttribute(string propertyName, object value)
            {
                ConditionType = VKButtonConditionType.RequiredPropertyValue;
                PropertyName = propertyName;
                Value = value;
            }

            public enum VKButtonConditionType
            {
                RequiredPropertyValue
            }
        }
    }
    
}
