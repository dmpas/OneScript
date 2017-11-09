Перем юТест;

Функция ПолучитьСписокТестов(ЮнитТестирование) Экспорт
	
	юТест = ЮнитТестирование;
	
	ВсеТесты = Новый Массив;
	ВсеТесты.Добавить("ТестДолжен_ПроверитьНаличиеДокументацииПеременной");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьНаличиеДокументацииМетода");

	Возврат ВсеТесты;
	
КонецФункции

Процедура ТестДолжен_ПроверитьНаличиеДокументацииПеременной() Экспорт
	
	ПроверяемыйОбъект = ЗагрузитьСценарийИзСтроки("
	|// Просто комментарий
	|
	|/// Произвольное описание
	|/// на несколько
	|	// <просто комментарий>
	|/// строк
	|/// с пробелом в конце 
	|Перем П1 Экспорт, П2 Экспорт;
	|
	|/// А это описание никуда не попадёт
	|Перем П3;
	|
	|/// Тут своё описание
	|Перем П4 Экспорт;
	|");

	Рефлектор = Новый Рефлектор;
	Свойства = Рефлектор.ПолучитьТаблицуСвойств(ПроверяемыйОбъект);

	юТест.ПроверитьНеРавенство(Свойства.Колонки.Найти("Документация"), Неопределено, "Есть колонка Документация");

	// Документация по свойствам пока не работает

КонецПроцедуры

Процедура ТестДолжен_ПроверитьНаличиеДокументацииМетода() Экспорт

	ПроверяемыйОбъект = ЗагрузитьСценарийИзСтроки("
	|// Просто комментарий
	|
	|/// Произвольное описание
	|/// на несколько
	|	// <просто комментарий>
	|/// строк
	|/// с пробелом в конце 
	|Процедура ЭкспортнаяПроцедура() Экспорт
	|	/// А это описание никуда не попадёт
	|КонецПроцедуры
	|
	|/// Тут своё описание
	|Процедура ЭкспортнаяПроцедура2() Экспорт
	|КонецПроцедуры
	|");

	Рефлектор = Новый Рефлектор;
	Методы = Рефлектор.ПолучитьТаблицуМетодов(ПроверяемыйОбъект);
	Для Каждого мСтрокаМетода Из Методы Цикл

		Если ВРЕГ(мСтрокаМетода.Имя) = ВРЕГ("ЭкспортнаяПроцедура") Тогда

			юТест.ПроверитьРавенство(мСтрокаМетода.Документация, "Произвольное описание
			|на несколько
			|строк
			|с пробелом в конце 
			|");

		ИначеЕсли ВРЕГ(мСтрокаМетода.Имя) = ВРЕГ("ЭкспортнаяПроцедура2") Тогда
			
			юТест.ПроверитьРавенство(мСтрокаМетода.Документация, "Тут своё описание
			|");

		КонецЕсли;

	КонецЦикла;

КонецПроцедуры
