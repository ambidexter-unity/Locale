using Common.Service;
using UniRx;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Common.Locale
{
	public interface ILocaleService : IGameService
	{
		/// <summary>
		/// Ключ текущей локализации.
		/// </summary>
		IReadOnlyReactiveProperty<SystemLanguage> CurrentLanguage { get; }

		/// <summary>
		/// Задать текущий язык локализации.
		/// </summary>
		/// <param name="lang">Новый текущий язык локализации.</param>
		void SetCurrentLanguage(SystemLanguage lang);

		/// <summary>
		/// Получить локализованную строку по ее ключу.
		/// </summary>
		/// <param name="key">Ключ.</param>
		/// <returns>Локализованное значение, или ключ, если значение для текущей локализации отсутствует.</returns>
		string GetLocalized(string key);

		/// <summary>
		/// Получить локализованную строку по ее ключу.
		/// </summary>
		/// <param name="key">Ключ.</param>
		/// <param name="language">Локализация, для которой запрашивается значение.</param>
		/// <returns></returns>
		string GetLocalized(string key, SystemLanguage language);

		/// <summary>
		/// Локализовать указанный UI.
		/// </summary>
		/// <param name="ui">Корневой объект локализуемого UI.</param>
		/// <param name="applyController">Флаг, указывающий применить ко всем найденным текстовым
		/// элементам контроллер с целью отслеживания смены локализации пользователем.</param>
		void Localize(GameObject ui, bool applyController = false);
	}
}