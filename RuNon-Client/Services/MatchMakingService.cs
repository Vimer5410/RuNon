namespace RuNon_Client.Services;

public class MatchMakingService
{
    public static List<(string, DateTime)> PeopleInQuery = new List<(string, DateTime)>();
    public static (string?, string?) pair;

    public void AddToQueue(string userId)
    {
        PeopleInQuery.Add((userId,DateTime.Now));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Пользователь {userId} был добавлен в очередь поиска");
    }

    public void RemoveFromQueue(string userId)
    {
        PeopleInQuery.RemoveAll(e => e.Item1 == userId);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Пользователь {userId} был удален из оереди поиска");
    }

    public (string?, string?) SearchCommand()
    {
        if (PeopleInQuery.Count<2)
        {
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Недостаточно пользователей для поиска");
        }
        else
        {
            var sortedUsers = PeopleInQuery.OrderBy(e => e.Item2).ToList();
            
            pair.Item1 = sortedUsers[0].Item1;
            pair.Item2 = sortedUsers[1].Item1;

            Console.WriteLine($"Найдена пара: {pair}");
            
        }
        return pair;
    }
}