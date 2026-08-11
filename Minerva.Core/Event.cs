namespace Minerva;

/// <summary>Disposable subscription token; disposing unsubscribes. Harder to leak than <c>-=</c>.</summary>
public sealed class EventSubscription(Action unsubscribe) : IDisposable
{
    public void Dispose() => unsubscribe();
}

/// <summary>Aggregates multiple subscriptions so one Dispose tears them all down.</summary>
public sealed class EventSubscriptions : IDisposable
{
    private Action? dispose;

    public EventSubscriptions(params EventSubscription[] subscriptions) => Array.ForEach(subscriptions, this.Add);
    public void Add(EventSubscription s) => this.dispose += s.Dispose;
    public void Dispose() => this.dispose?.Invoke();
}

/// <summary>Observer with disposable-based unsubscription (see <see cref="EventSubscription"/>).</summary>
public sealed class Event
{
    private Action? ev;

    public EventSubscription Subscribe(Action a)
    {
        this.ev += a;
        return new(() => this.ev -= a);
    }

    public void Fire() => this.ev?.Invoke();
    public bool HasSubscribers => this.ev != null;
}

/// <inheritdoc cref="Event"/>
public sealed class Event<T1>
{
    private Action<T1>? ev;

    public EventSubscription Subscribe(Action<T1> a)
    {
        this.ev += a;
        return new(() => this.ev -= a);
    }

    public void Fire(T1 a1) => this.ev?.Invoke(a1);
    public bool HasSubscribers => this.ev != null;
}

/// <inheritdoc cref="Event"/>
public sealed class Event<T1, T2>
{
    private Action<T1, T2>? ev;

    public EventSubscription Subscribe(Action<T1, T2> a)
    {
        this.ev += a;
        return new(() => this.ev -= a);
    }

    public void Fire(T1 a1, T2 a2) => this.ev?.Invoke(a1, a2);
    public bool HasSubscribers => this.ev != null;
}
