using System;
using System.Collections.Generic;
using System.Linq;
using Common.Locale;
using UniRx;
using UnityEngine;

namespace Common
{
	public class LocalString : IDisposable
	{
		private readonly LocaleService _localeService;
		private readonly StringReactiveProperty _value = new StringReactiveProperty(string.Empty);
		private string _key;
		private object[] _formatArgs;

		private bool _isDisposed;
		private IDisposable _languageHandler;

		// IDisposable

		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;
			
			_languageHandler.Dispose();
			_languageHandler = null;
		}

		// \IDisposable

		public LocalString(LocaleService localeService, string key, object[] formatArgs = null)
		{
			_localeService = localeService;
			_key = key;
			_formatArgs = formatArgs;
			_languageHandler = localeService.CurrentLanguage.Subscribe(OnUpdateValue);
		}

		private void OnUpdateValue(SystemLanguage language)
		{
			if (_isDisposed) return;

			var localString = _localeService.GetLocalized(_key, language);
			if (_formatArgs != null) localString = string.Format(localString, _formatArgs);
			if (_value.Value != localString) _value.SetValueAndForceNotify(localString);
		}

		/// <summary>
		/// Итоговое локализованное форматированное значение строки.
		/// </summary>
		public IReadOnlyReactiveProperty<string> Value => _value;

		/// <summary>
		/// Ключ локализации.
		/// </summary>
		public string Key
		{
			set
			{
				if (value == _key) return;
				_key = value;
				OnUpdateValue(_localeService.CurrentLanguage.Value);
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
				OnUpdateValue(_localeService.CurrentLanguage.Value);
			}
			get => _formatArgs.ToArray();
		}
	}
}