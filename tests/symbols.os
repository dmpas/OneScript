Перем юТест;

Функция ПолучитьСписокТестов(ЮнитТестирование) Экспорт
	
	юТест = ЮнитТестирование;
	
	ВсеТесты = Новый Массив;
	ВсеТесты.Добавить("ТестДолжен_ПроверитьЧтоСимволыПСНеПадает");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьЧтоСимволыLFНеПадает");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьЧтоCharsLFНеПадает");
	ВсеТесты.Добавить("ТестДолжен_ПроверитьЧтоCharsПСНеПадает");
	
	Возврат ВсеТесты;
	
КонецФункции

Процедура ТестДолжен_ПроверитьЧтоСимволыLFНеПадает() Экспорт
	Попытка
		Строка = Символы.LF;
	Исключение
		юТест.ТестПровален("Не работает Символы.LF");
	КонецПопытки;
КонецПроцедуры

Процедура ТестДолжен_ПроверитьЧтоСимволыПСНеПадает() Экспорт
	Попытка
		Строка = Символы.ПС;
	Исключение
		юТест.ТестПровален("Не работает Символы.ПС");
	КонецПопытки;
КонецПроцедуры

Процедура ТестДолжен_ПроверитьЧтоCharsLFНеПадает() Экспорт
	Попытка
		Строка = Chars.LF;
	Исключение
		юТест.ТестПровален("Не работает Chars.LF");
	КонецПопытки;
КонецПроцедуры

Процедура ТестДолжен_ПроверитьЧтоCharsПСНеПадает() Экспорт
	Попытка
		Строка = Chars.ПС;
	Исключение
		юТест.ТестПровален("Не работает Chars.ПС");
	КонецПопытки;
КонецПроцедуры