using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.Interfaces
{
    public interface IKeyStorage
    {
        T GetObjectByKey<T>(object keyValue);
        void SetObjectToStorage(object keyValue, object obj);
        void SubscribeToChanges(object keyValue, Action action);        
    }
}
