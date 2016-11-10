﻿using System;
using System.Threading.Tasks;
using System.Runtime.Caching;

namespace LazyProxy
{
    public class LazyProxy<TRequest, TResponse>
    {
        private readonly IFactory<TRequest, TResponse> _factory;
        private readonly MemoryCache _cache;
        private readonly TimeSpan _cacheExpirationTime;

        public LazyProxy(IFactory<TRequest, TResponse> factory, TimeSpan timeout)
        {
#if false
            if (timeout.Equals(TimeSpan.MaxValue) || timeout.Equals(TimeSpan.MinValue))
            {
                throw new ArgumentException("Must provide a valid timeout");
            } 
#endif
            _cache = new MemoryCache("LazyProzy.RequestCache");
            _cacheExpirationTime = timeout.Add(TimeSpan.FromMinutes(5));
            _factory = factory;
        }

        public Response<TResponse> Once(string key, TRequest request)
        {
            var val = _cache.AddOrGetExisting(key, new Lazy<TResponse>(() => _factory.Get(request)), DateTimeOffset.UtcNow.Add(_cacheExpirationTime)) as Lazy<TResponse>;

            // the first thread to insert into the cache will get a null value as return
            if (val == null)
            {
                val = _cache.Get(key) as Lazy<TResponse>;
            }

            try
            {
                var result = val.Value;
                return new Response<TResponse>()
                {
                    Success = true,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new Response<TResponse>()
                {
                    Success = false,
                    Message = string.Format("{0} - {1}", ex.GetType().FullName, ex.Message),
                    Result = default(TResponse)
                };
            }
        }

        
    }
}