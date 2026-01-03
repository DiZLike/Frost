using FrostWire.App;

namespace FrostWire.Broadcast
{
    public class MultiCastClient
    {
        private AppConfig _config;
        private IceCastClient[] _iceCastClients;

        public MultiCastClient(AppConfig config)
        {
            _config = config;
            for (int i = 0; i < _iceCastClients?.Length; i++)
            {
                _iceCastClients[i] = new IceCastClient(config);
                _iceCastClients[i].ConnectionRestored += MultiCastClient_ConnectionRestored;
            }
        }

        private void MultiCastClient_ConnectionRestored()
        {
            
        }
    }
}