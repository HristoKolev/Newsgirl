namespace Newsgirl.Shared
{
    using System;

    public interface InstanceProvider
    {
        object Get(Type type);
        
        T Get<T>();
    }
}
