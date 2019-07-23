using Common.Locale;
using Common.Service;
using UnityEngine;

namespace Scenes
{
    public class TestScript : MonoBehaviour
    {
        void Start()
        {
            IGameService ls = new LocaleService();
            ls.Initialize();
        }
    }
}
