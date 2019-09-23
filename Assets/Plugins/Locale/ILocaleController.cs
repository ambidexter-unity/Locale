// ReSharper disable once CheckNamespace

namespace Common.Locale
{
	/// <summary>
	/// Базовый класс контроллера локализованного текста.
	/// </summary>
	public interface ILocaleController
	{
		/// <summary>
		/// Текущий сервис локализации.
		/// </summary>
		ILocaleService LocaleService { set; }

		/// <summary>
		/// Предполагаемый ключ локализации (соответствует начальному значению текстового поля).
		/// </summary>
		string Key { get; set; }

		/// <summary>
		/// Применить форматирование к локализованной строке.
		/// </summary>
		/// <param name="args">Аргументы форматированной строки.</param>
		/// <returns>Результирующая локализованная форматированная строка.</returns>
		string Format(params object[] args);
	}
}