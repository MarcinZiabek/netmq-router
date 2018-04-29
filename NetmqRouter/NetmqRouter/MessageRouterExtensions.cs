﻿
using System;
using NetmqRouter.BusinessLogic;
using NetmqRouter.Infrastructure;
using NetmqRouter.Models;

namespace NetmqRouter
{
    public static class MessageRouterExtensions
    {
        public static MessageRouter WithWorkerPool(this MessageRouter router, int numberOfSerializationWorkes, int numberOfHandlingWorkes)
        {
            router.NumberOfSerializationWorkes = numberOfSerializationWorkes;
            router.NumberOfHandlingWorkes = numberOfHandlingWorkes;
            
            return router;
        }
        
        public static void SendMessage(this MessageRouter router, string routeName)
        {
            router.SendMessage(new Message(routeName, null));
        }

        public static void SendMessage(this MessageRouter router, string routeName, byte[] data)
        {
            router.SendMessage(new Message(routeName, data));
        }

        public static void SendMessage(this MessageRouter router, string routeName, string text)
        {
            router.SendMessage(new Message(routeName, text));
        }
        
        public static void SendMessage(this MessageRouter router, string routeName, object _object)
        {
            router.SendMessage(new Message(routeName, _object));
        }

        public static MessageRouter RegisterRoute(this MessageRouter router, string routeName, Type dataType)
        {
            router.DataContract.RegisterRoute(new Route(routeName, dataType));
            return router;
        }
        
        public static MessageRouter RegisterSerializerForType<T>(this MessageRouter router, ISerializer serializer)
        {
            router.DataContract.RegisterSerializer(typeof(T), serializer);
            return router;
        }
    }
}