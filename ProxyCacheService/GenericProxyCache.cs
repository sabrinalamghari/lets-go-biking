using System;
using System.Runtime.Caching;

namespace ProxyCacheService
{
    public class GenericProxyCache<T> where T : class
    {
        private readonly ObjectCache _cache = MemoryCache.Default;

        public DateTimeOffset dt_default = ObjectCache.InfiniteAbsoluteExpiration;

        public T Get(string key)
        {
            return GetInternal(key, dt_default);
        }

        public T Get(string key, double dt_seconds)
        {
            return GetInternal(key, DateTimeOffset.Now.AddSeconds(dt_seconds));
        }

        public T Get(string key, DateTimeOffset dt)
        {
            return GetInternal(key, dt);
        }

        private T GetInternal(string key, DateTimeOffset expires)
        {
            var item = _cache[key] as T;
            if (item != null)
                return item;

            item = CreateInstance(key);
            _cache.Set(key, item, new CacheItemPolicy { AbsoluteExpiration = expires });

            Console.WriteLine($"[Cache] Ajouté : {key} (expire le {expires})");
            return item;
        }
        private static T CreateInstance(string key)
        {
            var ctorWithKey = typeof(T).GetConstructor(new[] { typeof(string) });
            if (ctorWithKey != null)
                return (T)ctorWithKey.Invoke(new object[] { key });

            var ctorDefault = typeof(T).GetConstructor(Type.EmptyTypes);
            if (ctorDefault != null)
                return (T)ctorDefault.Invoke(null);

            throw new InvalidOperationException(
                $"Le type {typeof(T).Name} doit avoir un constructeur (string) ou sans paramètre."
            );
        }
    }
}
