using Common.Locale;
using Common.Service;
using UnityEngine;

namespace Scenes
{
	public class LocaleService : LocaleServiceBase
	{
		protected override string LocalePersistKey => @"test_locale_key";
	}

	public class TestScript : MonoBehaviour
	{
		void Start()
		{
			IGameService ls = new LocaleService();
			ls.Initialize();
		}
	}
}