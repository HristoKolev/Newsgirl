using System;

namespace Newsgirl.Shared
{
    public interface IoCResolver
    {
        object Resolve(Type type);
    }
}