using System;
using System.Collections.Concurrent;

namespace ShootR
{
    public class RegistrationHandler
    {
        private readonly ConcurrentDictionary<string, RegisteredClient> _registrationList = new ConcurrentDictionary<string, RegisteredClient>();
        
        public bool RegistrationExists(string registrationId)
        {
            return _registrationList.ContainsKey(registrationId);
        }

        public RegisteredClient RemoveRegistration(string registrationId)
        {
            _registrationList.TryRemove(registrationId, out var rc);

            return rc;
        }

        public RegisteredClient Register(RegisteredClient existing)
        {
            existing.RegistrationID = Guid.NewGuid().ToString();
            _registrationList.TryAdd(existing.RegistrationID, existing);
            return existing;
        }
    }
}