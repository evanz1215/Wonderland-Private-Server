using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game;
using Network;
using Network.ActionCodes;

namespace Server.Events
{
    /// <summary>
    /// Manages scheduled world events (double EXP, announcements, etc.)
    /// </summary>
    public class WorldEventSystem
    {
        readonly object m_lock = new object();
        List<WorldEvent> m_events;
        Thread m_thread;
        bool m_running;

        /// <summary>
        /// Global EXP multiplier. 1.0 = normal, 2.0 = double EXP.
        /// </summary>
        public double ExpMultiplier { get; set; }

        /// <summary>
        /// Global drop rate multiplier.
        /// </summary>
        public double DropMultiplier { get; set; }

        public WorldEventSystem()
        {
            m_events = new List<WorldEvent>();
            ExpMultiplier = 1.0;
            DropMultiplier = 1.0;
            LoadDefaultEvents();
        }

        void LoadDefaultEvents()
        {
            // Scheduled announcement every 30 minutes
            AddRepeatingEvent("ServerAnnounce", TimeSpan.FromMinutes(30), () =>
            {
                AC02.BroadcastSystemAnnouncement("Welcome to Wonderland Private Server!");
            });

            // Double EXP weekend check — runs every hour
            AddRepeatingEvent("WeekendCheck", TimeSpan.FromHours(1), () =>
            {
                var day = DateTime.Now.DayOfWeek;
                if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
                {
                    if (ExpMultiplier < 2.0)
                    {
                        ExpMultiplier = 2.0;
                        AC02.BroadcastSystemAnnouncement("[Event] Double EXP weekend is active!");
                    }
                }
                else
                {
                    if (ExpMultiplier > 1.0)
                    {
                        ExpMultiplier = 1.0;
                    }
                }
            });
        }

        /// <summary>
        /// Add a one-shot event that fires at a specific time.
        /// </summary>
        public void AddTimedEvent(string name, DateTime fireTime, Action action)
        {
            lock (m_lock)
            {
                m_events.Add(new WorldEvent
                {
                    Name = name,
                    FireTime = fireTime,
                    Interval = TimeSpan.Zero,
                    Repeating = false,
                    Action = action
                });
            }
        }

        /// <summary>
        /// Add a repeating event that fires on interval.
        /// </summary>
        public void AddRepeatingEvent(string name, TimeSpan interval, Action action)
        {
            lock (m_lock)
            {
                m_events.Add(new WorldEvent
                {
                    Name = name,
                    FireTime = DateTime.Now.Add(interval),
                    Interval = interval,
                    Repeating = true,
                    Action = action
                });
            }
        }

        /// <summary>
        /// Remove an event by name.
        /// </summary>
        public void RemoveEvent(string name)
        {
            lock (m_lock)
            {
                m_events.RemoveAll(e => e.Name == name);
            }
        }

        /// <summary>
        /// Start a double EXP event for a duration (GM command: :event exp [minutes]).
        /// </summary>
        public void StartDoubleExp(int minutes)
        {
            ExpMultiplier = 2.0;
            AC02.BroadcastSystemAnnouncement("[Event] Double EXP activated for " + minutes + " minutes!");

            AddTimedEvent("DoubleExpEnd", DateTime.Now.AddMinutes(minutes), () =>
            {
                ExpMultiplier = 1.0;
                AC02.BroadcastSystemAnnouncement("[Event] Double EXP has ended.");
            });
        }

        /// <summary>
        /// Start a double drop rate event for a duration.
        /// </summary>
        public void StartDoubleDrop(int minutes)
        {
            DropMultiplier = 2.0;
            AC02.BroadcastSystemAnnouncement("[Event] Double Drop Rate activated for " + minutes + " minutes!");

            AddTimedEvent("DoubleDropEnd", DateTime.Now.AddMinutes(minutes), () =>
            {
                DropMultiplier = 1.0;
                AC02.BroadcastSystemAnnouncement("[Event] Double Drop Rate has ended.");
            });
        }

        public void Start()
        {
            m_running = true;
            m_thread = new Thread(EventLoop);
            m_thread.IsBackground = true;
            m_thread.Name = "WorldEventSystem";
            m_thread.Start();
        }

        public void Stop()
        {
            m_running = false;
        }

        void EventLoop()
        {
            while (m_running)
            {
                try
                {
                    lock (m_lock)
                    {
                        var now = DateTime.Now;
                        var toRemove = new List<WorldEvent>();

                        foreach (var evt in m_events.ToList())
                        {
                            if (now >= evt.FireTime)
                            {
                                try { evt.Action(); }
                                catch (Exception ex) { DebugSystem.Write(new ExceptionData(ex)); }

                                if (evt.Repeating)
                                    evt.FireTime = now.Add(evt.Interval);
                                else
                                    toRemove.Add(evt);
                            }
                        }

                        foreach (var evt in toRemove)
                            m_events.Remove(evt);
                    }
                }
                catch (Exception ex) { DebugSystem.Write(new ExceptionData(ex)); }

                Thread.Sleep(1000); // Check every second
            }
        }

        /// <summary>
        /// Get a summary of active events.
        /// </summary>
        public string GetStatus()
        {
            lock (m_lock)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Events: " + m_events.Count);
                sb.Append(" | EXP x" + ExpMultiplier);
                sb.Append(" | Drop x" + DropMultiplier);
                return sb.ToString();
            }
        }
    }

    class WorldEvent
    {
        public string Name;
        public DateTime FireTime;
        public TimeSpan Interval;
        public bool Repeating;
        public Action Action;
    }
}
