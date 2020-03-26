using System;

namespace Newsgirl.Shared
{
    public interface InstanceProvider
    {
        object Get(Type type);
    }
}