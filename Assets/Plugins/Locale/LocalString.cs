using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Common.Locale
{
	public delegate void LocalStringTextChangedHandler(LocalString localString, string oldText);

	public class LocalString : IDisposable
	{
		private readonly ILocaleService _localeService;
		private string _value = string.Empty;
		private string _key;
		private object[] _formatArgs;

		private bool _isDisposed;

		// IDisposable

		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;

			_localeService.CurrentLanguageChangedEvent -= OnUpdateValue;
		}

		// \IDisposable

		public LocalString(ILocaleService localeService, string key, object[] formatArgs = null)
		{
			_localeService = localeService;
			_key = key;
			_formatArgs = formatArgs;
			localeService.CurrentLanguageChangedEvent += OnUpdateValue;
		}

		private void OnUpdateValue(SystemLanguage language)
		{
			if (_isDisposed) return;

			var localString = _localeService.GetLocalized(_key, language);
			if (_formatArgs != null) localString = string.Format(localString, _formatArgs);
			Value = localString;
		}

		/// <summary>
		/// Итоговое локализованное форматированное значение строки.
		/// </summary>
		public string Value
		{
			get => _value;
			private set
			{
				if (value == _value) return;
				var oldValue = _value;
				_value = value;
				TextChangedEvent?.Invoke(this, oldValue);
			}
		}

		/// <summary>
		/// Событие изменения локализованного текста.
		/// </summary>
		public event LocalStringTextChangedHandler TextChangedEvent;

		/// <summary>
		/// Ключ локализации.
		/// </summary>
		public string Key
		{
			set
			{
				if (value == _key) return;
				_key = value;
				OnUpdateValue(_localeService.CurrentLanguage);
			}
			get => _key;
		}

		/// <summary>
		/// Аргументы форматированной строки.
		/// </summary>
		public IEnumerable<object> FormatArgs
		{
			set
			{
				_formatArgs = value?.ToArray();
				OnUpdateValue(_localeService.CurrentLanguage);
			}
			get => _formatArgs.ToArray();
		}
	}
}