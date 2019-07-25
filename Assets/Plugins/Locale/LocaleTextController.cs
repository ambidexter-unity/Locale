using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

// ReSharper disable once CheckNamespace
namespace Common.Locale
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Text))]
	public class LocaleTextController : MonoBehaviour, ILocaleController
	{
		private readonly CompositeDisposable _handlers = new CompositeDisposable();

		private string _key;
		private object[] _formatArgs;
		private LocalString _localString;

#pragma warning disable 649
		[Inject] private readonly LocaleServiceBase _localeService;
#pragma warning restore 649

		private void Start()
		{
			var text = GetComponent<Text>();
			_key = _key ?? text.text.Trim();
			_localString = new LocalString(_localeService, _key, _formatArgs);
			_handlers.Add(_localString);
			_handlers.Add(_localString.Value.Subscribe(s => text.text = s));
		}

		private void OnDestroy()
		{
			_handlers.Dispose();
		}

		// ILocaleController

		public string Key
		{
			get => _key;
			set
			{
				_key = value;
				if (_localString != null) _localString.Key = value;
			}
		}

		public string Format(params object[] args)
		{
			_formatArgs = args != null && args.Length > 0 ? args.ToArray() : null;
			if (_localString == null) return null;

			_localString.FormatArgs = _formatArgs;
			return _localString.Value.Value;
		}

		// \ILocaleController
	}
}