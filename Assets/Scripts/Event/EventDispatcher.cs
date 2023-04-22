using System;
using System.Collections.Generic;

public static class EventDispatcher
{
    private struct FEventRouter
    {
        public Delegate Delegate;
        public bool     bRemove;
    }

    private static Dictionary<GameEvent, List<FEventRouter>> _TempAddRouter = new Dictionary<GameEvent,List<FEventRouter>>();
    private static Dictionary<GameEvent,List<FEventRouter>> _EventRouter   = new Dictionary<GameEvent,List<FEventRouter>>();

    /// <summary>
    /// 注册下一次trigger时生效
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    public static void RegEventListener(GameEvent eventType, Action handler)
    {
        RegEventListener(eventType, handler as Delegate);
    }

    /// <summary>
    /// 注册下一次trigger时生效
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    public static void RegEventListener<T>(GameEvent eventType, Action<T> handler)
    {
        RegEventListener(eventType, handler as Delegate);
    }

    /// <summary>
    /// 注册下一次trigger时生效
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    public static void RegEventListener<T, U>(GameEvent eventType, Action<T, U> handler)
    {
        RegEventListener(eventType, handler as Delegate);
    }

    /// <summary>
    /// 注册下一次trigger时生效
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    public static void RegEventListener<T, U, V>(GameEvent eventType, Action<T, U, V> handler)
    {
        RegEventListener(eventType, handler as Delegate);
    }

    /// <summary>
    /// 注册下一次trigger时生效
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    public static void RegEventListener<T, U, V, W>(GameEvent eventType, Action<T, U, V, W> handler)
    {
        RegEventListener(eventType, handler as Delegate);
    }

    public static void RegEventListener(GameEvent eventType, Delegate handler)
    {
        FEventRouter fEventRouter = new FEventRouter();
        fEventRouter.Delegate = handler;

        List<FEventRouter> tempAddRouterList;
        if (!_TempAddRouter.TryGetValue(eventType, out tempAddRouterList))
        {
            tempAddRouterList = new List<FEventRouter>();
            _TempAddRouter.Add(eventType, tempAddRouterList);
        }

        tempAddRouterList.Add(fEventRouter);
    }

    /// <summary>
    /// 反注册立马生效
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handler"></param>
    public static void UnRegEventListener(GameEvent eventType, Action handler)
    {
        UnRegEventListener(eventType, handler as Delegate);
    }

    public static void UnRegEventListener<T>(GameEvent eventType, Action<T> handler)
    {
        UnRegEventListener(eventType, handler as Delegate);
    }

    public static void UnRegEventListener<T, U>(GameEvent eventType, Action<T, U> handler)
    {
        UnRegEventListener(eventType, handler as Delegate);
    }

    public static void UnRegEventListener<T, U, V>(GameEvent eventType, Action<T, U, V> handler)
    {
        UnRegEventListener(eventType, handler as Delegate);
    }

    public static void UnRegEventListener<T, U, V, W>(GameEvent eventType, Action<T, U, V, W> handler)
    {
        UnRegEventListener(eventType, handler as Delegate);
    }

    private static void UnRegEventListener(GameEvent eventType, Delegate handler)
    {
        bool               found = false;
        List<FEventRouter> tempRemoveRouterList;
        if (_TempAddRouter.TryGetValue(eventType, out tempRemoveRouterList))
        {
            for (int i = 0; i < tempRemoveRouterList.Count; i++)
            {
                FEventRouter fEventRouter = tempRemoveRouterList[i];
                if (!fEventRouter.bRemove && fEventRouter.Delegate == handler )
                {
                    fEventRouter.bRemove    = true;
                    tempRemoveRouterList[i] = fEventRouter;
                    found                   = true;
                    break;
                }
            }
        }

        if (!found)
        {
            if (_EventRouter.TryGetValue(eventType, out tempRemoveRouterList))
            {
                for (int i = 0; i < tempRemoveRouterList.Count; i++)
                {
                    FEventRouter fEventRouter = tempRemoveRouterList[i];
                    if (!fEventRouter.bRemove && fEventRouter.Delegate == handler)
                    {
                        fEventRouter.bRemove    = true;
                        tempRemoveRouterList[i] = fEventRouter;
                        found                   = true;
                        break;
                    }
                }
            }
        }
    }

    public static void TriggerEvent(GameEvent eventType)
    {
        List<FEventRouter> tempRouterList = DealList(eventType);

        if (tempRouterList!=null)
        {
            for (int i = 0; i < tempRouterList.Count; i++)
            {
                Action action = tempRouterList[i].Delegate as Action;
                if (action !=null )
                {
                    action();
                }
            } 
        }
    }


    public static void TriggerEvent<T>(GameEvent eventType, T arg1)
    {
        List<FEventRouter> tempRouterList = DealList(eventType);

        if (tempRouterList != null)
        {
            for (int i = 0; i < tempRouterList.Count; i++)
            {
                Action<T> action = tempRouterList[i].Delegate as Action<T>;
                if (action != null)
                {
                    action(arg1);
                }
            }
        }
    }

    public static void TriggerEvent<T, U>(GameEvent eventType, T arg1, U arg2)
    {
        List<FEventRouter> tempRouterList = DealList(eventType);

        if (tempRouterList != null)
        {
            for (int i = 0; i < tempRouterList.Count; i++)
            {
                Action<T, U> action = tempRouterList[i].Delegate as Action<T, U>;
                if (action != null)
                {
                    action(arg1, arg2);
                }
            }
        }
    }

    public static void TriggerEvent<T, U, V>(GameEvent eventType, T arg1, U arg2, V arg3)
    {
        List<FEventRouter> tempRouterList = DealList(eventType);

        if (tempRouterList != null)
        {
            for (int i = 0; i < tempRouterList.Count; i++)
            {
                Action<T, U, V> action = tempRouterList[i].Delegate as Action<T, U, V>;
                if (action != null)
                {
                    action(arg1, arg2, arg3);
                }
            }
        }
    }

    public static void TriggerEvent<T, U, V, W>(GameEvent eventType, T arg1, U arg2, V arg3, W arg4)
    {
        List<FEventRouter> tempRouterList = DealList(eventType);

        if (tempRouterList != null)
        {
            for (int i = 0; i < tempRouterList.Count; i++)
            {
                Action<T, U, V, W> action = tempRouterList[i].Delegate as Action<T, U, V, W>;
                if (action != null)
                {
                    action(arg1, arg2, arg3, arg4);
                }
            }
        }
    }

    private static List<FEventRouter> DealList(GameEvent eventType)
    {
        List<FEventRouter> tempAddRouterList;

        if (_TempAddRouter.TryGetValue(eventType, out tempAddRouterList))
        {
            for (int i = tempAddRouterList.Count - 1; i >= 0; i--)
            {
                if (tempAddRouterList[i].bRemove)
                {
                    tempAddRouterList.RemoveAt(i);
                }
            }
        }

        List<FEventRouter> tempRouterList;
        if (!_EventRouter.TryGetValue(eventType, out tempRouterList))
        {
            tempRouterList=new List<FEventRouter>();
            _EventRouter.Add(eventType, tempRouterList);
        }
        
        for (int i = tempRouterList.Count - 1; i >= 0; i--)
        {
            if (tempRouterList[i].bRemove)
            {
                tempRouterList.RemoveAt(i);
            }
        }

        if (tempAddRouterList != null)
        {
            tempRouterList.AddRange(tempAddRouterList);
            tempAddRouterList.Clear();
        }
        
        return tempRouterList;
    }
}