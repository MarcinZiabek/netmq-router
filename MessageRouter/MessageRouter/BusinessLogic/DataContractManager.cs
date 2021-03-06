﻿using System;
using System.Collections.Generic;
using System.Linq;
using MessageRouter.Helpers;
using MessageRouter.Infrastructure;
using MessageRouter.Models;

namespace MessageRouter.BusinessLogic
{
    internal class DataContractManager : IDataContractOperations, IExceptionSource
    {
        private readonly IReadOnlyDictionary<string, Route> _routes;
        private readonly IReadOnlyDictionary<string, List<Subscriber>> _subscribers;
        private readonly IReadOnlyDictionary<Type, Serializer> _serializers;

        public event Action<Exception> OnException;

        public DataContractManager(IDataContractAccess dataContract)
        {
            _routes = IndexRoutes(dataContract);
            _subscribers = IndexSubscribers(dataContract);
            _serializers = IndexSerializers(dataContract);
        }

        internal static IReadOnlyDictionary<string, Route> IndexRoutes(IDataContractAccess dataContract)
        {
            return dataContract
                .Routes
                .ToDictionary(
                    x => x.Name,
                    x => x);
        }

        internal static IReadOnlyDictionary<string, List<Subscriber>> IndexSubscribers(IDataContractAccess dataContract)
        {
            return dataContract
                .Subscribers
                .GroupBy(x => x.Incoming.Name)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList());
        }

        internal static IReadOnlyDictionary<Type, Serializer> IndexSerializers(IDataContractAccess dataContract)
        {
            var sortedSerializers = dataContract
                .Serializers
                .OrderByDescending(x => x, new SerializerComparer())
                .ToList();

            return dataContract
                .Routes
                .Select(x => x.DataType)
                .Distinct()
                .Where(x => x != typeof(void))
                .ToDictionary(
                    x => x,
                    x => FindSerializer(sortedSerializers, x));

            Serializer FindSerializer(List<Serializer> serializers, Type targetType)
            {
                var serializer = serializers.FirstOrDefault(x => !x.IsGeneral && targetType == x.TargetType);

                if (serializer != null)
                    return serializer;

                serializer = serializers.First(x => x.IsGeneral && targetType.IsSameOrSubclass(x.TargetType));
                return serializer.ToTypeSerializer(targetType);
            }
        }

        public IEnumerable<string> GetIncomingRouteNames()
        {
            return _subscribers
                .SelectMany(x => x.Value)
                .Select(x => x.Incoming.Name);
        }

        public IEnumerable<Message> CallRoute(Message message)
        {
            return _subscribers
                [message.RouteName]
                .Select(x =>
                {
                    var response = x.Method(message.Payload);
                    return (x.Outcoming == null) ? null : new Message(x.Outcoming.Name, response);
                })
                .Where(x => x != null);
        }

        private Serializer GetSerializer(string routeName)
        {
            var targetType = _routes[routeName].DataType;
            var serializer = _serializers[targetType];

            return serializer;
        }

        public SerializedMessage Serialize(Message message)
        {
            var route = _routes[message.RouteName];
            
            if(route.DataType == typeof(void))
                return new SerializedMessage(message.RouteName, null);
            
            var serializer = GetSerializer(message.RouteName);
            var data = serializer.Serialize(message.Payload);
            return new SerializedMessage(message.RouteName, data);
        }

        public Message Deserialize(SerializedMessage message)
        {
            var route = _routes[message.RouteName];
            
            if(route.DataType == typeof(void))
                return new Message(message.RouteName, null);
            
            var serializer = GetSerializer(message.RouteName);
            var payload = serializer.Deserialize(message.Data);
            return new Message(message.RouteName, payload);
        }
    }
}