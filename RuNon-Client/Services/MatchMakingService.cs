using Serilog;

namespace RuNon_Client.Services;

public class MatchMakingService
{
    private static readonly object _sync = new object();
    
    public static List<(string userId, DateTime, string userGender, string userAge, string searchGender, string searchAge)>? PeopleInQueue = 
        new List<(string, DateTime,string, string, string, string)>();
    
    public static (string?, string?) pair;
    
    private static List<(string, DateTime, string, string, string, string)>? GayPairs = new(); // М ищет М
    
    private static List<(string, DateTime, string, string, string, string)>? LesbiansPairs = new(); // Ж ищет Ж
    
    private static List<(string, DateTime, string, string, string, string)>? Getero_Male_to_Female = new(); // М ищет Ж
    
    private static List<(string, DateTime, string, string, string, string)>? Getero_Female_to_Male = new(); // Ж ищет М

    public void AddToQueue(string userId,  string UserGender, string UserAge, string SearchGender, string SearchAge)
    {
        
        var user = ( userId, DateTime.Now, UserGender,  UserAge,  SearchGender, SearchAge);
        lock (_sync)
        {
            PeopleInQueue.Add(user);
            
            try
            {
                if (UserGender=="М"  && SearchGender=="Ж")
                {   
                    Getero_Male_to_Female.Add(user);
                }
                else if (UserGender=="Ж"  && SearchGender=="М")
                {
                    Getero_Female_to_Male.Add(user);
                }
                else if (UserGender=="Ж"  && SearchGender=="Ж")
                {
                    LesbiansPairs.Add(user);
                }
                else if (UserGender=="М"  && SearchGender=="М")
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

        lock (_sync)
        {
            if (PeopleInQueue.Count<2)
            {
                Log.Information("[Match-Making] Недостаточно пользователей для поиска");
            
            }
        else
        {
            var seeker = PeopleInQueue.FirstOrDefault(e => e.userId == userID); //тот кто ищет
            
            List<(string userId, DateTime, string userGender, string userAge, string searchGender, string searchAge)>? TargetQueue = null; // список нужной группы для поиска

            if (seeker.userGender=="М" && seeker.searchGender=="Ж")
            {
                TargetQueue = Getero_Female_to_Male ;
            }
            else if (seeker.userGender=="Ж" && seeker.searchGender=="М")
            {
                TargetQueue = Getero_Male_to_Female;
            }
            else if (seeker.userGender=="Ж" && seeker.searchGender=="Ж")
            {
                TargetQueue = LesbiansPairs;
            }
            else if (seeker.userGender=="М" && seeker.searchGender=="М")
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
                .Where(e => e.searchGender == seeker.userGender);     // проверяем взаимное соответствие по полу
                

            double TimeInQueue = (DateTime.Now - seeker.Item2).TotalSeconds;

            var extraMatch = basedMatch;
           
            if (TimeInQueue<=60)
            {
                extraMatch = basedMatch.Where(e => e.userAge ==
                                                   seeker.searchAge) // проверяем что мэтч age совпадает с age того кого искал seeker
                    .Where(e => e.searchAge ==
                                seeker.userAge); // проверяем что age seeker'a совпадает с age который искал мэтч
                ;
                Log.Information("[Match-Making] пользователи идеально совпадают");
                    
            }

           var match = extraMatch.FirstOrDefault();
           
            if (match.Item1 != null)
            {
                pair = (userID, match.Item1);
                
                Log.Information("[Match-Making] Найдена пара: {pair}", pair);
                
                RemoveFromQueue(userID); // удаляем ищущего
                RemoveFromQueue(match.Item1); // удаляем того кого он искал
                
                return pair;
            }
        
            Log.Information("[Match-Making] Пара для {UserId} не найдена в целевом сегменте", userID);
            return (null, null);
    
        }
        
        

        }
        return pair;
    }
}