using System.Collections.Concurrent;

namespace GlamourGames.Android;

static class TouchQueue
{
    static readonly ConcurrentQueue<Action> q = new();

    public static void Post(Action a) => q.Enqueue(a);

    public static void Drain()
    {
        while (q.TryDequeue(out var a)) a();
    }
}
