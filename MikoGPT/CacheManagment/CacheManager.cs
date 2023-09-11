using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikoGPT.CacheManagment
{
    
    class CacheManager<TReq, TRes> where TReq : notnull
    {
        public long? MaxCapasity;
        public CacheManager() { }
        public CacheManager(long? maxCapacity) => MaxCapasity = maxCapacity;


        LinkedList<TRes> linkedList = new();
        Dictionary<TReq, LinkedListNode<TRes>> dict = new();

        public TRes? GetRecord(TReq req)
        {
            if (dict.TryGetValue(req, out var node))
            {
                linkedList.Remove(node);
                linkedList.AddFirst(node);
            }
            return default;
        }
        public bool GetRecord(TReq req, [MaybeNullWhen(false)] out TRes result)
        {
            result = GetRecord(req);
            return result != null;
        }
        
        public void AddRecord(TReq key, TRes value)
        {
            var newNode = new LinkedListNode<TRes>(value);
            linkedList.AddFirst(newNode);
            dict[key] = newNode;
        }
        public void UpdateRecord(TReq req, TRes newValue)
        {
            if (dict.TryGetValue(req, out var value))
                value.Value = newValue;
            else
                throw new IndexOutOfRangeException();
        }/*

        public TRes? this[TReq req]
        {
            get => GetRecord(req);
            set => {
                
            }
        }*/
    }
}
