using FrostWire.App;
using FrostWire.Audio;

namespace FrostWire.Broadcast
{
    public class MultiCastClient
    {
        private AppConfig _config;
        private IceCastClient[] _iceCastClients;

        public bool IsConnected
        {
            get
            {
                if (_iceCastClients == null || _iceCastClients.Length == 0)
                    return false;
                return _iceCastClients.All(client => client != null && client.IsConnected);
            }
        }

        public event Action<string>? ConnectionRestored;

        public MultiCastClient(AppConfig config)
        {
            _config = config;
            _iceCastClients = new IceCastClient[config.Encoders.Count];
            for (int i = 0; i < _iceCastClients?.Length; i++)
            {
                _iceCastClients[i] = new IceCastClient(config, config.Encoders[i]);
                _iceCastClients[i].ConnectionRestored += MultiCastClient_ConnectionRestored;
            }
        }

        public void Initialize(Mixer mixer)
        {
            for (int i = 0; i < _iceCastClients?.Length; i++)
                _iceCastClients[i].Initialize(mixer);
        }
        public void SetMetadata(string artist, string title)
        {
            for (int i = 0; i < _iceCastClients?.Length; i++)
                _iceCastClients[i].SetMetadata(artist, title);
        }
        public void Dispose()
        {
            for (int i = 0; i < _iceCastClients?.Length; i++)
                _iceCastClients[i].Dispose();
        }
        private void MultiCastClient_ConnectionRestored(string str)
        {
            ConnectionRestored?.Invoke(str);
        }
    }
}