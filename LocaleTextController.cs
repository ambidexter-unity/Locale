using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace Common.Locale
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Text))]
	public class LocaleTextController : MonoBehaviour, ILocaleController
	{
		private string _key;
		private object[] _formatArgs;
		private LocalString _localString;
		private ILocaleService _localeService;
		private Text _text;

		private void Start()
		{
			_text = GetComponent<Text>();
			_key = _key ?? _text.text.Trim();
			_localString = new LocalString(_localeService, _key, _formatArgs);
			_localString.TextChangedEvent += OnLocalTextChanged;
		}

		private void OnLocalTextChanged(LocalString localString, string oldText)
		{
			_text.text = localString.Value;
		}

		private void OnDestroy()
		{
			_localString.TextChangedEvent -= OnLocalTextChanged;
			_localString.Dispose();
		}

		// ILocaleController

		ILocaleService ILocaleController.LocaleService
		{
			set => _localeService = value;
		}

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
			return _localString.Value;
		}

		// \ILocaleController
	}
}