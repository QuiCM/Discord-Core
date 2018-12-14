using Discord.Descriptors;
using Discord.Descriptors.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Discord.Gateway
{
    public class GatewayEvents
    {
        protected static bool _firstRun = true;
        private ConcurrentDictionary<GatewayOpCode, Dictionary<Type, Delegator>> _callbacks =
            new ConcurrentDictionary<GatewayOpCode, Dictionary<Type, Delegator>>();

        private ConcurrentDictionary<EventType, Dictionary<Type, Delegator>> _eventCallbacks =
            new ConcurrentDictionary<EventType, Dictionary<Type, Delegator>>();

        /// <summary>
        /// Registers internal handlers required by the connector
        /// </summary>
        /// <param name="connector"></param>
        public virtual void RegisterInternalHandles(Connector connector)
        {
            if (!_firstRun)
            {
                return;
            }

            _firstRun = false;
            AddCallback<object>(GatewayOpCode.All, connector.GatewayEvents_AllEventsCallback);
            AddCallback<HelloPayload>(GatewayOpCode.Hello, connector.GatewayEvents_OnGatewayHello);
            AddCallback<object>(GatewayOpCode.Heartbeat, connector.GatewayEvents_OnHeartbeatReq);
            AddCallback<object>(GatewayOpCode.HeartbeatAck, connector.GatewayEvents_OnHeartbeatAck);

            AddEventCallback<GatewayReady>(EventType.READY, connector.GatewayEvents_OnReady);
        }

        /// <summary>
        /// Adds a callback for the given opcode
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="opCode"></param>
        /// <param name="callback"></param>
        public void AddCallback<TPayload>(GatewayOpCode opCode, Action<string, GatewayEvent<TPayload>> callback)
        {
            Type t = typeof(TPayload);
            _callbacks.AddOrUpdate(opCode,
                //add factory: return a new dictionary with the required information
                new Dictionary<Type, Delegator>() { { t, new Delegator(t, callback) } },
                //update factory:
                (o, u) =>
                {
                    //if we've already added a delegator for the given type, simply add another invocation to it
                    if (u.ContainsKey(t))
                    {
                        u[t] += callback;
                    }
                    //otherwise, add a new delegator
                    else
                    {
                        u.Add(t, new Delegator(t, callback));
                    }
                    return u;
                }
            );
        }

        /// <summary>
        /// Removes the given callback for an opcode
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="opCode"></param>
        /// <param name="callback"></param>
        public void RemoveCallback<TPayload>(GatewayOpCode opCode, Action<string, GatewayEvent<TPayload>> callback)
        {
            if (_callbacks.TryGetValue(opCode, out Dictionary<Type, Delegator> delegator))
            {
                delegator[typeof(TPayload)] -= callback;
            }
        }

        /// <summary>
        /// Adds a callback for a given event
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void AddEventCallback<TPayload>(EventType type, Action<string, DispatchGatewayEvent<TPayload>> callback)
        {
            Type t = typeof(TPayload);
            _eventCallbacks.AddOrUpdate(type,
                //add factory: return a new dictionary with the required information
                new Dictionary<Type, Delegator>() { { t, new Delegator(t, callback) } },
                //update factory:
                (o, u) =>
                {
                    //if we've already added a delegator for the given type, simply add another invocation to it
                    if (u.ContainsKey(t))
                    {
                        u[t] += callback;
                    }
                    //otherwise, add a new delegator
                    else
                    {
                        u.Add(t, new Delegator(t, callback));
                    }
                    return u;
                }
            );
        }

        /// <summary>
        /// Removes the given callback for an event
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void RemoveEventCallback<TPayload>(EventType type, Action<string, GatewayEvent<TPayload>> callback)
        {
            if (_eventCallbacks.TryGetValue(type, out Dictionary<Type, Delegator> delegator))
            {
                delegator[typeof(TPayload)] -= callback;
            }
        }

        /// <summary>
        /// Invokes events for the given json string
        /// </summary>
        /// <param name="json"></param>
        public void Invoke(string json)
        {
            //We need temp event to get the OpCode for this event
            DispatchGatewayEvent<JToken> tempEvent = JsonConvert.DeserializeObject<DispatchGatewayEvent<JToken>>(json);

            if (_callbacks.TryGetValue(GatewayOpCode.All, out Dictionary<Type, Delegator> callbacks))
            {
                foreach (var callback in callbacks)
                {
                    object ev = Activator.CreateInstance(callback.Value._eventType);
                    JsonConvert.PopulateObject(json, ev);

                    callback.Value.Invoke(json, ev);
                }
            }

            if (_callbacks.TryGetValue(tempEvent.OpCode, out callbacks))
            {
                foreach (KeyValuePair<Type, Delegator> callback in callbacks)
                {
                    object ev = Activator.CreateInstance(callback.Value._eventType);
                    //Doing this means we deserialize twice per event.
                    //There doesn't really seem to be any other way to do it however
                    JsonConvert.PopulateObject(json, ev);

                    callback.Value.Invoke(json, ev);
                }
            }

            if (_eventCallbacks.TryGetValue(tempEvent.Type, out callbacks))
            {
                foreach (KeyValuePair<Type, Delegator> callback in callbacks)
                {
                    object ev = Activator.CreateInstance(callback.Value._eventType);
                    //Doing this means we deserialize twice per event.
                    //There doesn't really seem to be any other way to do it however
                    JsonConvert.PopulateObject(json, ev);

                    callback.Value.Invoke(json, ev);
                }
            }
        }

        class Delegator
        {
            internal Delegate _delegate;
            //cache these for better performance
            internal Type _eventType;
            internal MemberInfo _payloadMember;

            internal Delegator(Type type, Delegate del)
            {
                _delegate = del;

                _eventType = typeof(DispatchGatewayEvent<>).MakeGenericType(type);
                _payloadMember = _eventType.GetMember("Payload")[0];
            }

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
