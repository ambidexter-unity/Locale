﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Common.Service;
using Extensions;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.UI;
using Zenject;

namespace Common.Locale
{
	public static class LocaleConst
	{
		
		// Путь к локалям в StreamingAssets.
		public static readonly string LocalesPath = "Locales";
		// Имя файла манифеста для локалей.
		public static readonly string LocalesManifestFileName = "manifest";

	}
	
	public class LocaleService : IGameService
	{
		
		private class LocaleEntry
		{
			private readonly Dictionary<string, string> _map = new Dictionary<string, string>();

			public LocaleEntry(SystemLanguage key)
			{
				Key = key;
			}

			public SystemLanguage Key { get; private set; }

			public void SetValue(string key, string value)
			{
				_map[key] = value;
			}

			public string GetValue(string key)
			{
				try
				{
					var value = _map[key];
					return string.IsNullOrEmpty(value) ? key : value;
				}
				catch (KeyNotFoundException)
				{
					return key;
				}
			}
		}

		private const string LocaleKey = "tamafish_locale";

		private readonly Dictionary<SystemLanguage, LocaleEntry> _localesMap =
			new Dictionary<SystemLanguage, LocaleEntry>();

		private readonly IntReactiveProperty _numLoadedLocales = new IntReactiveProperty(0);

		private readonly ReactiveProperty<SystemLanguage> _currentLanguage =
			new ReactiveProperty<SystemLanguage>(Application.systemLanguage);

		private static readonly Regex TransRegex = new Regex(@"(?:\\n|\\r\\n)");

		private BoolReactiveProperty _ready = new BoolReactiveProperty(false);

#pragma warning disable 649
		[Inject] private readonly DiContainer _container;
#pragma warning restore 649

		// IGameService

		void IGameService.Initialize()
		{
			Debug.Log("Initialize Locale service...");

			var manifestPath = Path.Combine(Application.streamingAssetsPath, LocaleConst.LocalesPath,
				LocaleConst.LocalesManifestFileName);
			MainThreadDispatcher.StartCoroutine(
				LoadManifest(manifestPath, locales =>
				{
					Assert.IsFalse(_numLoadedLocales.Value > 0);

					foreach (var locale in locales)
					{
						var path = Path.Combine(Application.streamingAssetsPath, LocaleConst.LocalesPath, locale);
						MainThreadDispatcher.StartCoroutine(LoadLocale(path));
					}

					if (_numLoadedLocales.Value <= 0)
					{
						RestorePersistingState();
						Debug.Log("... locales are not found.");

						_ready.SetValueAndForceNotify(true);
						return;
					}

					IDisposable d = null;
					d = _numLoadedLocales.Select(i => i <= 0).Subscribe(value =>
					{
						if (!value) return;
						// ReSharper disable once AccessToModifiedClosure
						d?.Dispose();

						RestorePersistingState();
						Debug.Log("... locales are loaded.");

						_ready.SetValueAndForceNotify(true);
					});
				}));
		}

		public IReadOnlyReactiveProperty<bool> Ready => _ready;

		// \IGameService

		/// <summary>
		/// Ключ текущей локализации.
		/// </summary>
		public IReadOnlyReactiveProperty<SystemLanguage> CurrentLanguage => _currentLanguage;

		/// <summary>
		/// Задать текущий язык локализации.
		/// </summary>
		/// <param name="lang">Новый текущий язык локализации.</param>
		public void SetCurrentLanguage(SystemLanguage lang)
		{
			if (lang == CurrentLanguage.Value) return;
			_currentLanguage.SetValueAndForceNotify(lang);
			PersistCurrentState();
		}

		/// <summary>
		/// Получить локализованную строку по ее ключу.
		/// </summary>
		/// <param name="key">Ключ.</param>
		/// <returns>Локализованное значение, или ключ, если значение для текущей локализации отсутствует.</returns>
		public string GetLocalized(string key)
		{
			return GetLocalized(key, CurrentLanguage.Value);
		}

		/// <summary>
		/// Получить локализованную строку по ее ключу.
		/// </summary>
		/// <param name="key">Ключ.</param>
		/// <param name="language">Локализация, для которой запрашивается значение.</param>
		/// <returns></returns>
		public string GetLocalized(string key, SystemLanguage language)
		{
			try
			{
				var currentLocale = _localesMap[language];
				var value = currentLocale.GetValue(key);
				return string.IsNullOrEmpty(value) ? key : ProcessRawString(value);
			}
			catch (KeyNotFoundException)
			{
				return key;
			}
		}

		/// <summary>
		/// Локализовать указанный UI.
		/// </summary>
		/// <param name="ui">Корневой объект локализуемого UI.</param>
		/// <param name="applyController">Флаг, указывающий применить ко всем найденным текстовым
		/// элементам контроллер с целью отслеживания смены локализации пользователем.</param>
		public void Localize(GameObject ui, bool applyController = false)
		{
			var text = ui.GetComponentsInChildren<Text>(true);
			foreach (var t in text)
			{
				if (applyController)
				{
					_container.InstantiateComponent<LocaleTextController>(t.gameObject);
				}
				else
				{
					t.text = GetLocalized(t.text.Trim());
				}
			}

			var textPro = ui.GetComponentsInChildren<TextMeshProUGUI>(true);
			foreach (var tmp in textPro)
			{
				if (applyController)
				{
					_container.InstantiateComponent<LocaleTextMeshProController>(tmp.gameObject);
				}
				else
				{
					tmp.text = GetLocalized(tmp.text.Trim());
				}
			}
		}

		private void PersistCurrentState()
		{
			PlayerPrefs.SetString(LocaleKey, typeof(SystemLanguage).GetEnumName(CurrentLanguage.Value));
			PlayerPrefs.Save();
		}

		private void RestorePersistingState()
		{
			SystemLanguage lang;
			try
			{
				lang = PlayerPrefs.HasKey(LocaleKey)
					? (SystemLanguage) Enum.Parse(typeof(SystemLanguage), PlayerPrefs.GetString(LocaleKey))
					: Application.systemLanguage;
			}
			catch (ArgumentException)
			{
				lang = Application.systemLanguage;
			}

			switch (lang)
			{
				case SystemLanguage.Russian:
					// TODO: Add supported languages here.
					_currentLanguage.SetValueAndForceNotify(lang);
					break;
				default:
					_currentLanguage.SetValueAndForceNotify(SystemLanguage.English);
					break;
			}
		}

		private static IEnumerator LoadManifest(string path, Action<string[]> callback)
		{
			DebugConditional.LogFormat("-- load LocaleService manifest from {0}...", path);
			using (var www = UnityWebRequest.Get(path))
			{
				yield return www.SendWebRequest();

				if (www.isNetworkError || www.isHttpError)
				{
					Debug.LogErrorFormat("Failed to load manifest from {0} with error: {1}", path, www.error);
					callback?.Invoke(new string[0]);
				}
				else
				{
					var lines = new List<string>();
					using (var reader = new StringReader(www.downloadHandler.text))
					{
						string line;
						while ((line = reader.ReadLine()) != null)
						{
							lines.Add(line.Trim());
						}
					}

					DebugConditional.Log("... manifest loaded successfully.");
					callback?.Invoke(lines.ToArray());
				}
			}
		}

		private IEnumerator LoadLocale(string path)
		{
			DebugConditional.LogFormat("-- load locales from {0}...", path);
			_numLoadedLocales.SetValueAndForceNotify(_numLoadedLocales.Value + 1);

			using (var www = UnityWebRequest.Get(path))
			{
				yield return www.SendWebRequest();

				if (www.isNetworkError || www.isHttpError)
				{
					Debug.LogErrorFormat("Failed to load locales from {0} with error: {1}", path, www.error);
				}
				else
				{
					DebugConditional.Log("... locales are loaded successfully.");
					ParseLocales(www.downloadHandler.text);
				}

				_numLoadedLocales.SetValueAndForceNotify(_numLoadedLocales.Value - 1);
			}
		}

		private void ParseLocales(string raw)
		{
			if (string.IsNullOrEmpty(raw))
			{
				_numLoadedLocales.SetValueAndForceNotify(_numLoadedLocales.Value - 1);
				return;
			}

			LocaleEntry[] locales = null;
			var lines = raw.Split(new[] {"\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries);
			if (lines.Length > 1)
			{
				for (var i = 0; i < lines.Length; i++)
				{
					var columns = SeparateLine(lines[i]);
					if (i == 0)
					{
						if (columns.Length < 2)
						{
							Debug.LogWarning("Locale map is empty.");
							return;
						}

						locales = new LocaleEntry[columns.Length];
						locales[0] = null;

						for (var j = 1; j < columns.Length; j++)
						{
							var key = columns[j].AsLanguage();
							locales[j] = _localesMap.ContainsKey(key) ? _localesMap[key] : new LocaleEntry(key);
						}
					}
					else if (locales != null)
					{
						var key = columns[0];
						var limit = Math.Min(columns.Length, locales.Length);
						for (var j = 1; j < limit; j++)
						{
							var loc = locales[j];
							loc.SetValue(key, columns[j]);
						}
					}
				}

				_numLoadedLocales.SetValueAndForceNotify(_numLoadedLocales.Value - 1);
			}

			if (locales == null)
			{
				Debug.LogWarning("Locale map is empty.");
				return;
			}

			for (var i = 1; i < locales.Length; i++)
			{
				var loc = locales[i];
				_localesMap[loc.Key] = loc;
			}
		}

		private string[] SeparateLine(string line)
		{
			var res = new Queue<string>();
			var buffer = new List<char>(line.Length);
			var prev = ',';
			var quoted = false;

			// ReSharper disable AccessToModifiedClosure
			// ReSharper disable once InconsistentNaming
			Action b2s = () =>
			{
				if (quoted && prev == '"')
				{
					buffer.RemoveAt(buffer.Count - 1);
				}

				var s = new string(buffer.ToArray());
				res.Enqueue(s);
				buffer.Clear();
				prev = ',';
				quoted = false;
			};
			// ReSharper restore AccessToModifiedClosure

			foreach (var c in line)
			{
				switch (c)
				{
					case ',':
						if (quoted)
						{
							if (prev == '"')
							{
								b2s();
							}
							else
							{
								buffer.Add(c);
								prev = c;
							}
						}
						else
						{
							b2s();
						}

						break;
					case '"':
						if (prev == ',')
						{
							quoted = true;
							prev = '\0';
						}
						else if (prev == '"')
						{
							prev = '\0';
						}
						else
						{
							buffer.Add(c);
							prev = c;
						}

						break;
					default:
						buffer.Add(c);
						prev = c;
						break;
				}
			}

			b2s();

			return res.ToArray();
		}

		private static string ProcessRawString(string raw)
		{
			var res = TransRegex.Replace(raw, "\n");
			return res;
		}
	}
}