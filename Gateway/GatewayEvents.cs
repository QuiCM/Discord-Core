using Discord.Descriptors;
using Discord.Descriptors.Payloads;
using Discord.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Discord.Gateway
{
    /// <summary>
    /// Used to register and invoke handlers for events.
    /// Invoke events by providing valid event json to <see cref="Invoke(string)"/>
    /// </summary>
    public class GatewayEvents
    {
        protected static bool _firstRun = true;
        private ConcurrentDictionary<GatewayOpCode, Dictionary<Type, Delegator>> _callbacks =
            new ConcurrentDictionary<GatewayOpCode, Dictionary<Type, Delegator>>();

        private ConcurrentDictionary<EventType, Dictionary<Type, Delegator>> _eventCallbacks =
            new ConcurrentDictionary<EventType, Dictionary<Type, Delegator>>();

        private ConcurrentDictionary<GatewayOpCode, Dictionary<Type, Delegator>> _asyncCallbacks =
            new ConcurrentDictionary<GatewayOpCode, Dictionary<Type, Delegator>>();

        private ConcurrentDictionary<EventType, Dictionary<Type, Delegator>> _asyncEventCallbacks =
            new ConcurrentDictionary<EventType, Dictionary<Type, Delegator>>();


        private JsonSerializerSettings _serializationSettings;

        /// <summary>
        /// Registers internal handlers required by the connector
        /// </summary>
        /// <param name="connector"></param>
        internal void RegisterInternalHandlers(Connector connector)
        {
            //Internal callbacks should only be registered once, as this instance isn't disposed between reconnects
            if (!_firstRun)
            {
                return;
            }
            _firstRun = false;

            AddCallback<object>(GatewayOpCode.All, connector.GatewayEvents_AllEventsCallback);
            AddCallback<HelloPayload>(GatewayOpCode.Hello, connector.GatewayEvents_OnGatewayHello);
            AddCallback<object>(GatewayOpCode.Heartbeat, connector.GatewayEvents_OnHeartbeatReq);
            AddCallback<object>(GatewayOpCode.HeartbeatAck, connector.GatewayEvents_OnHeartbeatAck);
            AddAsyncCallback<object>(GatewayOpCode.InvalidSession, connector.GatewayEvents_InvalidSession);

            AddEventCallback<GatewayReady>(EventType.READY, connector.GatewayEvents_OnReady);
        }

        /// <summary>
        /// Adds a <see cref="JsonConverter"/> to be used during event deserialization
        /// </summary>
        /// <param name="converter"></param>
        public void AddConverter(JsonConverter converter)
        {
            if (_serializationSettings == null)
            {
                _serializationSettings = new JsonSerializerSettings();
            }

            _serializationSettings.Converters.Add(converter);
        }

        /// <summary>
        /// Removes a <see cref="JsonConverter"/>
        /// </summary>
        /// <param name="converter"></param>
        public void RemoveConverter(JsonConverter converter)
        {
            if (_serializationSettings == null)
            {
                return;
            }

            _serializationSettings.Converters.Add(converter);
        }

        /// <summary>
        /// Adds a callback for the given opcode
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="opCode"></param>
        /// <param name="callback"></param>
        public void AddCallback<TPayload>(GatewayOpCode opCode, Action<string, DispatchGatewayEvent<TPayload>> callback)
        {
            Type payloadType = typeof(TPayload);
            _callbacks.AddOrUpdate(opCode,
                //add factory: return a new dictionary with the required information
                new Dictionary<Type, Delegator>() { { payloadType, new Delegator(payloadType, callback) } },
                //update factory:
                (op, dictionary) =>
                {
                    //if we've already added a delegator for the given type, simply add another invocation to it
                    if (dictionary.ContainsKey(payloadType))
                    {
                        dictionary[payloadType] += callback;
                    }
                    //otherwise, add a new delegator
                    else
                    {
                        dictionary.Add(payloadType, new Delegator(payloadType, callback));
                    }
                    return dictionary;
                }
            );
        }

        public void AddAsyncCallback<TPayload>(GatewayOpCode opCode, Func<string, DispatchGatewayEvent<TPayload>, Task> callback)
        {
            Type payloadType = typeof(TPayload);
            _asyncCallbacks.AddOrUpdate(opCode,
                //add factory: return a new dictionary with the required information
                new Dictionary<Type, Delegator>() { { payloadType, new Delegator(payloadType, callback, true) } },
                //update factory:
                (op, dictionary) =>
                {
                    //if we've already added a delegator for the given type, simply add another invocation to it
                    if (dictionary.ContainsKey(payloadType))
                    {
                        dictionary[payloadType] += callback;
                    }
                    //otherwise, add a new delegator
                    else
                    {
                        dictionary.Add(payloadType, new Delegator(payloadType, callback));
                    }
                    return dictionary;
                }
            );
        }

        /// <summary>
        /// Removes a callback of the form <code>void Callback(string, GatewayEvent{TPayload})</code> for the given opcode
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="opCode"></param>
        /// <param name="callback"></param>
        public void RemoveCallback<TPayload>(GatewayOpCode opCode, Action<string, DispatchGatewayEvent<TPayload>> callback)
        {
            if (_callbacks.TryGetValue(opCode, out Dictionary<Type, Delegator> delegator))
            {
                delegator[typeof(TPayload)] -= callback;
            }
        }

        /// <summary>
        /// Removes a callback of the form <code>async Task Callback(string, GatewayEvent{TPayload})</code> for the given opcode
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="opCode"></param>
        /// <param name="callback"></param>
        public void RemoveAsyncCallback<TPayload>(GatewayOpCode opCode, Func<string, DispatchGatewayEvent<TPayload>, Task> callback)
        {
            if (_asyncCallbacks.TryGetValue(opCode, out Dictionary<Type, Delegator> delegator))
            {
                delegator[typeof(TPayload)] -= callback;
            }
        }

        /// <summary>
        /// Adds a callback of the form <code>void Callback(string, DispatchGatewayEvent{TPayload})</code>for a given event
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void AddEventCallback<TPayload>(EventType type, Action<string, DispatchGatewayEvent<TPayload>> callback)
        {
            Type payloadType = typeof(TPayload);
            _eventCallbacks.AddOrUpdate(type,
                //add factory: return a new dictionary with the required information
                new Dictionary<Type, Delegator>() { { payloadType, new Delegator(payloadType, callback) } },
                //update factory:
                (op, dictionary) =>
                {
                    //if we've already added a delegator for the given type, simply add another invocation to it
                    if (dictionary.ContainsKey(payloadType))
                    {
                        dictionary[payloadType] += callback;
                    }
                    //otherwise, add a new delegator
                    else
                    {
                        dictionary.Add(payloadType, new Delegator(payloadType, callback));
                    }
                    return dictionary;
                }
            );
        }

        /// <summary>
        /// Adds a callback of the form <code>async Task Callback(string, DispatchGatewayEvent{TPayload})</code>for a given event
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void AddAsyncEventCallback<TPayload>(EventType type, Func<string, DispatchGatewayEvent<TPayload>, Task> callback)
        {
            Type payloadType = typeof(TPayload);
            _asyncEventCallbacks.AddOrUpdate(type,
                //add factory: return a new dictionary with the required information
                new Dictionary<Type, Delegator>() { { payloadType, new Delegator(payloadType, callback, true) } },
                //update factory:
                (op, dictionary) =>
                {
                    //if we've already added a delegator for the given type, simply add another invocation to it
                    if (dictionary.ContainsKey(payloadType))
                    {
                        dictionary[payloadType] += callback;
                    }
                    //otherwise, add a new delegator
                    else
                    {
                        dictionary.Add(payloadType, new Delegator(payloadType, callback));
                    }
                    return dictionary;
                }
            );
        }

        /// <summary>
        /// Removes the given callback of the form <code>void Callback(string, </code> for an event
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void RemoveEventCallback<TPayload>(EventType type, Action<string, DispatchGatewayEvent<TPayload>> callback)
        {
            if (_eventCallbacks.TryGetValue(type, out Dictionary<Type, Delegator> delegator))
            {
                delegator[typeof(TPayload)] -= callback;
            }
        }

        public void RemoveAsyncEventCallback<TPayload>(EventType type, Func<string, DispatchGatewayEvent<TPayload>, Task> callback)
        {
            if (_asyncEventCallbacks.TryGetValue(type, out Dictionary<Type, Delegator> delegator))
            {
                delegator[typeof(TPayload)] -= callback;
            }
        }

        /// <summary>
        /// Invokes any event handler that matches the event in the given json string
        /// </summary>
        /// <param name="json"></param>
        public async Task Invoke(string json)
        {
            //We need temp event to get the OpCode for this event
            DispatchGatewayEvent<JToken> tempEvent = JsonConvert.DeserializeObject<DispatchGatewayEvent<JToken>>(json);

            if (_callbacks.TryGetValue(GatewayOpCode.All, out Dictionary<Type, Delegator> callbacks))
            {
                foreach (var callback in callbacks)
                {
                    //GatewayOpCode.All doesn't get a first-class object deserialization, as the callback is used for every payload type
                    //This object instance therefore is a DispatchGatewayEvent<object>, with Payload being a JObject which can be further
                    //deserialized if required
                    object ev = Activator.CreateInstance(callback.Value._eventType);
                    JsonConvert.PopulateObject(json, ev, _serializationSettings);

                    callback.Value.Invoke(json, ev);
                }
            }

            if (_asyncCallbacks.TryGetValue(tempEvent.OpCode, out callbacks))
            {
                foreach (KeyValuePair<Type, Delegator> callback in callbacks)
                {
                    object ev = Activator.CreateInstance(callback.Value._eventType);
                    //Doing this means we effectively deserialize twice per event.
                    //There doesn't really seem to be any other way to do it however
                    JsonConvert.PopulateObject(json, ev, _serializationSettings);

                    await callback.Value.InvokeAsync(json, ev);
                }
            }

            if (_callbacks.TryGetValue(tempEvent.OpCode, out callbacks))
            {
                foreach (KeyValuePair<Type, Delegator> callback in callbacks)
                {
                    object ev = Activator.CreateInstance(callback.Value._eventType);
                    JsonConvert.PopulateObject(json, ev, _serializationSettings);

                    callback.Value.Invoke(json, ev);
                }
            }

            if (_asyncEventCallbacks.TryGetValue(tempEvent.Type, out callbacks))
            {
                foreach (KeyValuePair<Type, Delegator> callback in callbacks)
                {
                    object ev = Activator.CreateInstance(callback.Value._eventType);
                    JsonConvert.PopulateObject(json, ev, _serializationSettings);

                    await callback.Value.InvokeAsync(json, ev);
                }
            }

            if (_eventCallbacks.TryGetValue(tempEvent.Type, out callbacks))
            {
                foreach (KeyValuePair<Type, Delegator> callback in callbacks)
                {
                    object ev = Activator.CreateInstance(callback.Value._eventType);
                    JsonConvert.PopulateObject(json, ev, _serializationSettings);

                    callback.Value.Invoke(json, ev);
                }
            }
        }

        /// <summary>
        /// Wrapper for event delegates
        /// </summary>
        class Delegator
        {
            /// <summary>
            /// Delegate method containing event callbacks
            /// </summary>
            internal Delegate _delegate;
            //cache these for better performance
            /// <summary>
            /// Event's system type (<see cref="DispatchGatewayEvent{}"/>)
            /// </summary>
            internal Type _eventType;
            /// <summary>
            /// Member (field or property) defining the payload
            /// </summary>
            internal MemberInfo _payloadMember;
            internal bool _isAsync;

            /// <summary>
            /// Constructs a new delegator from the given type and delegate
            /// </summary>
            /// <param name="type"></param>
            /// <param name="del"></param>
            internal Delegator(Type type, Delegate del, bool isAsync = false)
            {
                _delegate = del;
                _isAsync = isAsync;

                //Craft the event type by creating a DispatchGatewayEvent<T> where T : type
                _eventType = typeof(DispatchGatewayEvent<>).MakeGenericType(type);
                _payloadMember = _eventType.GetMember("Payload")[0];
            }

            /// <summary>
            /// Sets the value of the payload object
            /// </summary>
            /// <param name="setOn"></param>
            /// <param name="value"></param>
            internal void SetPayloadValue(object setOn, object value)
            {
                if (_payloadMember.MemberType == MemberTypes.Field)
                {
                    ((FieldInfo)_payloadMember).SetValue(setOn, value);
                }
                else
                {
                    ((PropertyInfo)_payloadMember).SetValue(setOn, value);
                }
            }

            internal void Invoke(string raw, object deserialized)
            {
                _delegate.DynamicInvoke(raw, deserialized);
            }

            /// <summary>
            /// Invokes the delegates on this delegator
            /// </summary>
            /// <param name="raw"></param>
            /// <param name="deserialized"></param>
            internal async Task InvokeAsync(string raw, object deserialized)
            {
                Task t = (Task)_delegate.DynamicInvoke(raw, deserialized);
                await t;
            }

            public static Delegator operator +(Delegator left, Delegate right)
            {
                left._delegate = Delegate.Combine(left._delegate, right);
                return left;
            }

            public static Delegator operator -(Delegator left, Delegate right)
            {
                left._delegate = Delegate.Remove(left._delegate, right);
                return left;
            }
        }
    }
}
