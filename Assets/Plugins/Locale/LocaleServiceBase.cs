using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Common.GameService;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace Common.Locale
{
	public abstract class LocaleServiceBase : ILocaleService
	{
		private class LocaleEntry
		{
			private readonly Dictionary<string, string> _map = new Dictionary<string, string>();

			public LocaleEntry(SystemLanguage key)
			{
				Key = key;
			}

			public SystemLanguage Key { get; }

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


		private SystemLanguage _currentLanguage = Application.systemLanguage;
		private bool _isReady;
		private bool _isInitialized;
		private int _numLoadedLocales;

		private readonly Dictionary<SystemLanguage, LocaleEntry> _localesMap =
			new Dictionary<SystemLanguage, LocaleEntry>();

		private readonly Mutex _readyMutex = new Mutex();

		private static readonly Regex TransRegex = new Regex(@"(?:\\n|\\r\\n)");

		protected abstract string LocalePersistKey { get; }
		protected abstract string LocalesManifestFileName { get; }
		protected abstract string LocalesPath { get; }

		// ILocaleService

		public SystemLanguage CurrentLanguage
		{
			get => _currentLanguage;
			private set
			{
				if (value == _currentLanguage) return;
				_currentLanguage = value;
				CurrentLanguageChangedEvent?.Invoke(_currentLanguage);
			}
		}

		public event CurrentLanguageChangedHandler CurrentLanguageChangedEvent;

		public virtual void Initialize(params object[] args)
		{
			if (IsReady || _isInitialized)
			{
				Debug.LogError("Locale service already initialized.");
				return;
			}

			_isInitialized = true;

			var manifestPath = GetPath(LocalesManifestFileName);
			LoadManifest(manifestPath, locales =>
			{
				Assert.IsFalse(_numLoadedLocales > 0);
				_numLoadedLocales = locales.Length;
				if (_numLoadedLocales > 0)
				{
					foreach (var locale in locales)
					{
						var path = GetPath(locale);
						LoadLocale(path);
					}
				}
				else
				{
					Debug.Log("... locales are not found.");
					IsReady = true;
				}
			});
		}

		public bool IsReady
		{
			get => _isReady;
			private set
			{
				if (value == _isReady) return;

				Assert.IsFalse(_isReady);
				_isReady = value;
				ReadyEvent?.Invoke(this);
			}
		}

		public event GameServiceReadyHandler ReadyEvent;

		public void SetCurrentLanguage(SystemLanguage lang)
		{
			if (lang == CurrentLanguage) return;
			CurrentLanguage = lang;
			PersistCurrentState();
		}

		public string GetLocalized(string key)
		{
			return GetLocalized(key, CurrentLanguage);
		}

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

		public void Localize(GameObject ui, bool applyController = false)
		{
			var text = ui.GetComponentsInChildren<Text>(true);
			foreach (var t in text)
			{
				if (applyController)
				{
					ILocaleController controller = t.GetComponent<LocaleTextController>();
					if (controller == null)
					{
						controller = t.gameObject.AddComponent<LocaleTextController>();
						controller.LocaleService = this;
					}
					else
					{
						Debug.LogError("Trying to localize twice.");
					}
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
					ILocaleController controller = tmp.GetComponent<LocaleTextMeshProController>();
					if (controller == null)
					{
						controller = tmp.gameObject.AddComponent<LocaleTextMeshProController>();
						controller.LocaleService = this;
					}
					else
					{
						Debug.LogError("Trying to localize twice.");
					}
				}
				else
				{
					tmp.text = GetLocalized(tmp.text.Trim());
				}
			}
		}

		// \ILocaleService

		private void PersistCurrentState()
		{
			PlayerPrefs.SetInt(LocalePersistKey, (int) CurrentLanguage);
			PlayerPrefs.Save();
		}

		protected void RestorePersistingState(SystemLanguage defaultLanguage)
		{
			var persist = false;
			var lang = defaultLanguage;
			if (PlayerPrefs.HasKey(LocalePersistKey))
			{
				var i = PlayerPrefs.GetInt(LocalePersistKey);
				persist = !Enum.IsDefined(typeof(SystemLanguage), i);
				if (!persist)
				{
					lang = (SystemLanguage) i;
				}
			}

			CurrentLanguage = IsLanguageSupported(lang) ? lang : Application.systemLanguage;
			if (persist) PersistCurrentState();
		}

		protected abstract bool IsLanguageSupported(SystemLanguage lang);

		private static void LoadManifest(string path, Action<string[]> callback)
		{
			var www = UnityWebRequest.Get(path);

			// ReSharper disable AccessToDisposedClosure
			void OnCompleted(AsyncOperation obj)
			{
				obj.completed -= OnCompleted;

				if (www.isNetworkError || www.isHttpError)
				{
					Debug.LogErrorFormat("Failed to load manifest from {0} with error: {1}", path, www.error);
					callback?.Invoke(new string[0]);
				}
				else
				{
					var lines = Encoding.UTF8.GetString(www.downloadHandler.data).Split(new[] {"\r\n", "\n"},
						StringSplitOptions.RemoveEmptyEntries);
					callback?.Invoke(lines);
				}

				www.Dispose();
			}
			// ReSharper restore AccessToDisposedClosure

			var op = www.SendWebRequest();
			if (op.isDone) OnCompleted(op);
			else op.completed += OnCompleted;
		}

		private void LoadLocale(string path)
		{
			var www = UnityWebRequest.Get(path);

			void OnCompleted(AsyncOperation obj)
			{
				var isReady = false;
				if (_readyMutex.WaitOne())
				{
					if (www.isNetworkError || www.isHttpError)
					{
						Debug.LogErrorFormat("Failed to load locales from {0} with error: {1}", path, www.error);
					}
					else
					{
						ParseLocales(Encoding.UTF8.GetString(www.downloadHandler.data));
					}

					--_numLoadedLocales;
					isReady = _numLoadedLocales <= 0;
					_readyMutex.ReleaseMutex();
				}

				if (isReady) IsReady = true;
			}

			var op = www.SendWebRequest();
			if (op.isDone) OnCompleted(op);
			else op.completed += OnCompleted;
		}

		private void ParseLocales(string raw)
		{
			if (string.IsNullOrEmpty(raw)) return;

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
							var key = KeyToLanguage(columns[j]);
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
			}

			if (locales == null)
			{
				Debug.LogWarning("Locale map is empty.");
			}
			else
			{
				for (var i = 1; i < locales.Length; i++)
				{
					var loc = locales[i];
					_localesMap[loc.Key] = loc;
				}
			}
		}

		protected abstract SystemLanguage KeyToLanguage(string key);

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

		private string GetPath(string fileName)
		{
			var fullPath = Path.Combine(Application.streamingAssetsPath, LocalesPath, fileName);
#if UNITY_IOS
			fullPath = $"file://{fullPath}";
#endif
			return fullPath;
		}

		private static string ProcessRawString(string raw)
		{
			var res = TransRegex.Replace(raw, "\n");
			return res;
		}
	}
}