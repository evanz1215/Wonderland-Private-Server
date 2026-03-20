using System;
using WloCharacterViewer.Models;

namespace WloCharacterViewer.Network.Handlers
{
    /// <summary>
    /// Handles AC 11 (battle start/end).
    /// Registered as AC-only handler. Dispatcher sets ptr=5.
    /// Sub 1 with subtype 1 = battle start -> Phase = WaitingForAction.
    /// Sub 1 with other subtypes may indicate end -> Phase = Ended/None.
    /// </summary>
    public class BattleHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                byte sub = reader.Unpack8();

                if (sub == 1)
                {
                    if (reader.Remaining < 1) return;

                    byte subtype = reader.Unpack8();

                    if (subtype == 1)
                    {
                        // Battle start
                        session.Battle.Reset();
                        session.Battle.Phase = BattlePhase.WaitingForAction;
                        session.Battle.RoundCount = 1;
                        session.Log("[Battle] Started — waiting for action");
                    }
                    else
                    {
                        // Battle end signal
                        session.Battle.Phase = BattlePhase.Ended;
                        session.Log($"[Battle] Ended (sub1 subtype={subtype})");
                        session.Battle.Reset();
                    }
                }
                else
                {
                    session.Log($"[Battle] AC11 sub={sub} (unhandled)");
                }
            }
            catch (Exception ex)
            {
                session.Log($"[BattleHandler] Parse error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles AC 50 (battle round processing).
    /// Registered as AC-only handler. Dispatcher sets ptr=5.
    /// On receiving AC 50, set Phase = WaitingForAction (new round).
    /// </summary>
    public class BattleRoundHandler : IPacketHandler
    {
        public void Handle(PacketReader reader, GameSession session)
        {
            try
            {
                if (!session.Battle.InBattle) return;

                // AC 50 signals a new round — increment and set waiting
                session.Battle.RoundCount++;
                session.Battle.Phase = BattlePhase.WaitingForAction;
                session.Log($"[Battle] Round {session.Battle.RoundCount} — waiting for action");
            }
            catch (Exception ex)
            {
                session.Log($"[BattleRoundHandler] Parse error: {ex.Message}");
            }
        }
    }
}
