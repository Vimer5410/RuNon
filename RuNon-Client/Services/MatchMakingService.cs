using Serilog;
using System.Collections.Concurrent;

namespace RuNon_Client.Services;

public class MatchMakingService
{
    private static readonly object _sync = new object();
    
    //словарь для хранения уведомлений о найденных парах.
    // ключ: ID пользователя, который был найден (пассивный). 
    //значение: ID того, кто его нашел (активный).
    private static readonly ConcurrentDictionary<string, string> _completedMatches = new();

    public static List<(string userId, DateTime, string userGender, string userAge, string searchGender, string searchAge)>? PeopleInQueue = 
        new List<(string, DateTime,string, string, string, string)>();
    
    
    private static List<(string, DateTime, string, string, string, string)>? GayPairs = new(); // М ищет М
    
    private static List<(string, DateTime, string, string, string, string)>? LesbiansPairs = new(); // Ж ищет Ж
    
    private static List<(string, DateTime, string, string, string, string)>? Getero_Male_to_Female = new(); // М ищет Ж
    
    private static List<(string, DateTime, string, string, string, string)>? Getero_Female_to_Male = new(); // Ж ищет М

    public void AddToQueue(string userId,  string UserGender, string UserAge, string SearchGender, string SearchAge)
    {
        
        var user = ( userId, DateTime.Now, UserGender,  UserAge,  SearchGender, SearchAge);
        lock (_sync)
        {
            // Проверка на дублирование, если пользователь уже есть
            if (PeopleInQueue.Any(x => x.userId == userId)) return;

            PeopleInQueue.Add(user);
            
            try
            {
                if (UserGender=="male"  && SearchGender=="female")
                {   
                    Getero_Male_to_Female.Add(user);
                }
                else if (UserGender=="female"  && SearchGender=="male")
                {
                    Getero_Female_to_Male.Add(user);
                }
                else if (UserGender=="female"  && SearchGender=="female")
                {
                    LesbiansPairs.Add(user);
                }
                else if (UserGender=="male"  && SearchGender=="male")
                {
                    GayPairs.Add(user);
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                throw;
            }
        }
        
        
        
        
        
        Log.Information("[Match-Making] Пользователь {UserId} : {UserGender}, {UserAge} ищет {SearchGender}, {SearchAge}",
            userId,
            UserGender,
            UserAge,
            SearchGender,
            SearchAge);
    }
    
    

    
    public void RemoveFromQueue(string userId)
    {
        lock (_sync)
        {
            // удаляем из основной оереди и из 4 сигментированных
            PeopleInQueue.RemoveAll(e => e.Item1 == userId);

            Getero_Male_to_Female.RemoveAll(e => e.Item1 == userId);
            Getero_Female_to_Male.RemoveAll(e => e.Item1 == userId);
            LesbiansPairs.RemoveAll(e => e.Item1 == userId);
            GayPairs.RemoveAll(e => e.Item1 == userId);
        }
        
        
        Log.Information("[Match-Making] Пользователь {userId} был удален из очереди поиска", userId);
        
    }

    public (string?, string?) SearchCommand(string userID)
    {
        // проверка потового ящика
        if (_completedMatches.TryRemove(userID, out var activePartnerID))
        {
            Log.Information($"[Match-Making] {userID} (пассивный) уведомлен о матче с {activePartnerID}");
            // возвращаем пару(мой айди + айди того кто меня нашел)
            return (userID, activePartnerID);
        }

        // активный поиск
        lock (_sync)
        {
            var seeker = PeopleInQueue.FirstOrDefault(e => e.userId == userID); //тот кто ищет
            
            if (seeker.userId == null) 
            {
                 // я не в очереди и не в почтовом ящике 
                 return (null, null); 
            }

            if (PeopleInQueue.Count<2)
            {
                Log.Information("[Match-Making] Недостаточно пользователей для поиска");
                return (null, null);
            }
            
            List<(string userId, DateTime, string userGender, string userAge, string searchGender, string searchAge)>? TargetQueue = null; // список нужной группы для поиска
            
            if (seeker.searchGender == "any")
            {
                TargetQueue = PeopleInQueue;
            }
            else if (seeker.userGender=="male" && seeker.searchGender=="female")
            {
                TargetQueue = Getero_Female_to_Male ;
            }
            else if (seeker.userGender=="female" && seeker.searchGender=="male")
            {
                TargetQueue = Getero_Male_to_Female;
            }
            else if (seeker.userGender=="female" && seeker.searchGender=="female")
            {
                TargetQueue = LesbiansPairs;
            }
            else if (seeker.userGender=="male" && seeker.searchGender=="male")
            {
                TargetQueue = GayPairs;
            }
            else
            {
                Log.Information("[Match-Making] Для пользователя {userId} нет ни одной подходящей очереди", userID);
                return (null, null);
            }

            // самый подходящий мэтч, который был найден
            var basedMatch = TargetQueue
                .OrderBy(e => e.Item2) //сортируем чтобы ждуны были первыми в списке
                .Where(e => e.userId != userID) //  проверяем что пользователь не нашел сам себя
                .Where(e => e.searchGender == seeker.userGender || e.searchGender == "any");     // проверяем взаимное соответствие по полу

                

            double TimeInQueue = (DateTime.Now - seeker.Item2).TotalSeconds;

            var extraMatch = basedMatch;
           
            //самый идеальный мэтч ищется первые 60 секунд, после этого ищем первого подходящего по гендеру собеседника
            if (TimeInQueue<=60)
            {
                extraMatch = basedMatch
                    .Where(e => e.userAge == seeker.searchAge) // проверяем что мэтч age совпадает с age того кого искал seeker
                    .Where(e => e.searchAge == seeker.userAge); // проверяем что age seeker'a совпадает с age который искал мэтч
                
            }

            var match = extraMatch.FirstOrDefault();
           
            if (match.Item1 != null)
            {
                var partnerID = match.Item1;
                
                // проверка на случай, если партнер был удален в миллисекунду между поиском и локом (редко, но возможно)
                var partnerStillExists = PeopleInQueue.Any(x => x.Item1 == partnerID);
                if (!partnerStillExists) 
                {
                    Log.Information($"[Match-Making] Найденный match {partnerID} был удален другим потоком");
                    return (null, null);
                }

                var foundPair = (userID, partnerID);

                Log.Information("[Match-Making] Найдена пара: {pair}", foundPair);
                
                // удаляем обоих из очередей
                RemoveFromQueue(userID); // удаляем ищущего (активного)
                RemoveFromQueue(partnerID); // удаляем того кого он искал (пассивного)
                
                // оставляем "письмо" пассивному партнеру
                _completedMatches[partnerID] = userID;
                
                // возвращаем пару активному
                return foundPair;
            }
        
            Log.Information("[Match-Making] Пара для {UserId} не найдена в целевом сегменте", userID);
            return (null, null);
    
        }
    }
}