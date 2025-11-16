using Serilog;

namespace RuNon_Client.Services;

public class MatchMakingService
{
    public static List<(string, DateTime, string, string, string, string)> PeopleInQuery = new 
        List<(string, DateTime,string, string, string, string)>();
    public static (string?, string?) pair;

    public void AddToQueue(string userId,  string UserGender, string UserAge, string SearchGender, string SearchAge)
    {
        PeopleInQuery.Add((userId,DateTime.Now, UserGender, UserAge, SearchGender, SearchAge));
        
        Log.Information("[Match-Making] Пользователь {UserId} : {UserGender}, {UserAge} ищет {SearchGender}, {SearchAge}",
            userId,
            UserGender,
            UserAge,
            SearchGender,
            SearchAge);
    }

    public void RemoveFromQueue(string userId)
    {
        PeopleInQuery.RemoveAll(e => e.Item1 == userId);
        Log.Information("[Match-Making] Пользователь {userId} был удален из очереди поиска", userId);
        
    }

    public (string?, string?) SearchCommand(string userId)
    {
        var sortedUsers = PeopleInQuery
            .GroupBy(e => e.Item1) 
            .Select(g => g.First()) 
            .OrderBy(e => e.Item2) 
            .ToList();
        if (sortedUsers.Count<2)
        {
            Log.Information("[Match-Making] Недостаточно пользователей для поиска");
            
        }
        else
        {
            List<(string, DateTime, string, string, string, string)> MaleInQuery =
                sortedUsers.Where(e => e.Item5 == "М").ToList();
            
            List<(string, DateTime, string, string, string, string)> FemaleInQuery =
                sortedUsers.Where(e => e.Item5 == "Ж").ToList();
            
            if (sortedUsers[0].Item1!=sortedUsers[1].Item1)
            {
                pair.Item1 = sortedUsers[0].Item1;
                pair.Item2 = sortedUsers[1].Item1;
                
                Log.Information("[Match-Making] Найдена пара: {pair}", pair);

                PeopleInQuery.RemoveAll(e => e.Item1==pair.Item1);
                PeopleInQuery.RemoveAll(e => e.Item1==pair.Item2);
            }
            else
            {

                Log.Information("[Match-Making] Попытка соединения юзера самим с собой");
            }


        }
        return pair;
    }
}