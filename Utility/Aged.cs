using System;
using System.Threading.Tasks;

namespace Discord.Utility
{
    /// <summary>
    /// A wrapper for an object that should expire after a period of time
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Aged<T>
    {
        /// <summary>
        /// Returns a boolean indicating whether the object has expired or not
        /// </summary>
        public bool IsExpired => Expired();
        /// <summary>
        /// Returns a boolean indicating whether the object was forced to expire or not
        /// </summary>
        public bool IsForceExpired => _forceExpire;

        private T _object;
        private bool _forceExpire = false;
        private DateTime _created;
        private readonly TimeSpan _expiry;

        /// <summary>
        /// Constructs a new aging object with the given millisecond expiration
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="expiryMs"></param>
        public Aged(T obj, int expiryMs) : this(obj, new TimeSpan(0, 0, 0, 0, expiryMs)) { }

        /// <summary>
        /// Constructs a new aging object with the given TimeSpan expiration
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="expiry"></param>
        public Aged(T obj, TimeSpan expiry)
        {
            _object = obj;
            _created = DateTime.UtcNow;
            _expiry = expiry;
        }

        /// <summary>
        /// Retrives the wrapped object, or calls the given function if the object has expired naturally. This will reset the 
        /// expiration of the object, using the original expiry timeout.
        /// If the object was forced to expire via <see cref="Expire"/> then this method returns the default value of <see cref="T"/>
        /// </summary>
        /// <param name="createIfExpiredFunc">Function which will create a new instance of <see cref="T"/> 
        /// if the existing one has expired naturally</param>
        /// <returns></returns>
        public T Retrieve(Func<T> createIfExpiredFunc)
        {
            if (_forceExpire)
            {
                return default(T);
            }

            if (IsExpired)
            {
                _object = createIfExpiredFunc();
                _created = DateTime.UtcNow;

                return _object;
            }

            return _object;
        }

        /// <summary>
        /// Retrives the wrapped object, or calls the given asynchronous function if the object has expired naturally. This will reset the 
        /// expiration of the object using the original expiry timeout.
        /// If the object was forced to expire via <see cref="Expire"/> then this method returns the default value of <see cref="T"/>
        /// </summary>
        /// <param name="createIfExpiredFunc">Function which will create a new instance of <see cref="T"/> 
        /// if the existing one has expired naturally</param>
        /// <returns></returns>
        public async Task<T> Retrieve(Func<Task<T>> createIfExpiredFuncAsync)
        {
            if (_forceExpire)
            {
                return default(T);
            }

            if (IsExpired)
            {
                _object = await createIfExpiredFuncAsync();
                _created = DateTime.UtcNow;

                return _object;
            }

            return _object;
        }

        /// <summary>
        /// Forcefully expires this aging object.
        /// A force-expired aging object must be recreated to be used again
        /// </summary>
        public void Expire()
        {
            _forceExpire = true;
        }

        private bool Expired()
        {
            if (_forceExpire)
            {
                return _forceExpire;
            }

            return (DateTime.UtcNow - _created) > _expiry;
        }
    }
}
