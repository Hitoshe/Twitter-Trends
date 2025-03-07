// Фаза 1: Анализ чувств в твитах

// TODO: Разобрать текст твита на отдельные слова, исключая все символы, кроме букв.
//       Использовать регулярные выражения или встроенные методы C# для токенизации.
using static System.Net.Mime.MediaTypeNames;

void TokenizeTweet(string tweet)
{
    // Реализация
}

// TODO: Загрузить и распарсить файл sentiments.csv, содержащий веса слов.
// TODO: Реализовать функцию, которая находит слова твита в словаре тональности 
//       и вычисляет среднее значение тональности твита.
// TODO: Вернуть null, если в твите нет слов с известной тональностью.
double? AnalyzeSentiment(string tweet, Dictionary<string, double> sentimentDictionary)
{
    // Реализация
}

// Фаза 2: Определение настроений по штатам

// TODO: Загрузить и распарсить states.json, содержащий географические данные штатов.
// TODO: Определить центр каждого штата (среднее арифметическое координат всех полигонов).
Dictionary<string, (double latitude, double longitude)> ParseStateCenters(string jsonFile)
{
    // Реализация
}

// TODO: Реализовать функцию, находящую ближайший штат к координатам твита (по евклидову расстоянию).
string FindNearestState(double tweetLat, double tweetLon, Dictionary<string, (double lat, double lon)> stateCenters)
{
    // Реализация
}

// TODO: Сгруппировать твиты по ближайшим штатам и сохранить в Dictionary<string, List<Tweet>>,
//       где ключ — код штата, значение — список твитов.
Dictionary<string, List<Tweet>> GroupTweetsByState(List<Tweet> tweets, Dictionary<string, (double lat, double lon)> stateCenters)
{
    // Реализация
}

// TODO: Для каждого штата вычислить среднее значение тональности всех его твитов.
// TODO: Исключить штаты без твитов или с твитами без известных тональностей.
Dictionary<string, double?> CalculateStateSentiment(Dictionary<string, List<Tweet>> tweetsByState, Dictionary<string, double> sentimentDictionary)
{
    // Реализация
}

// Фаза 3: Визуализация данных на карте США

// TODO: Отобразить карту США с цветовой заливкой штатов по их тональности:
//       - Жёлтый (положительная тональность).
//       - Синий (отрицательная тональность).
//       - Белый (нейтральная тональность).
//       - Серый (нет данных).
void RenderUSMap(Dictionary<string, double?> stateSentiments)
{
    // Реализация
}
